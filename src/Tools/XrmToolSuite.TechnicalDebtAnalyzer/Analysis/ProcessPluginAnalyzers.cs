using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Analysis
{
    /// <summary>Shared retrieval of the plugin-registration graph (assemblies, types, steps), fail-soft.</summary>
    internal static class PluginRegistration
    {
        public static List<Entity> Steps(TechDebtContext ctx) =>
            ctx.SafeRetrieveAll(new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("name", "statecode", "mode", "stage", "filteringattributes",
                                          "plugintypeid", "sdkmessageid")
            });

        public static List<Entity> Types(TechDebtContext ctx) =>
            ctx.SafeRetrieveAll(new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("typename", "friendlyname", "pluginassemblyid", "isworkflowactivity")
            });

        public static List<Entity> Assemblies(TechDebtContext ctx) =>
            ctx.SafeRetrieveAll(new QueryExpression("pluginassembly")
            {
                ColumnSet = new ColumnSet("name")
            });

        public static Dictionary<Guid, string> MessageNames(TechDebtContext ctx) =>
            ctx.SafeRetrieveAll(new QueryExpression("sdkmessage") { ColumnSet = new ColumnSet("name") })
               .ToDictionary(m => m.Id, m => m.GetAttributeValue<string>("name") ?? "");
    }

    /// <summary>Flags dead plugin registrations: disabled steps, plugin types with no steps,
    /// and assemblies whose types register no steps at all.</summary>
    public sealed class DeadPluginsAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Dead Plugins";
        public string Category => "Dead Plugins";

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Inspecting plugin registrations…");

            var steps = PluginRegistration.Steps(ctx);
            var types = PluginRegistration.Types(ctx);
            var assemblies = PluginRegistration.Assemblies(ctx);

            foreach (var s in steps.Where(x => x.GetAttributeValue<OptionSetValue>("statecode")?.Value == TechDebtContext.StateInactive))
                findings.Add(new Finding(Category, Severity.Low, "Disabled plugin step",
                    $"Step '{s.GetAttributeValue<string>("name")}' is disabled but still registered.",
                    s.GetAttributeValue<string>("name"),
                    "Delete the step if it is no longer needed; keeping disabled steps hides intent."));

            var typeIdsWithSteps = new HashSet<Guid>(steps
                .Select(s => s.GetAttributeValue<EntityReference>("plugintypeid")?.Id ?? Guid.Empty)
                .Where(id => id != Guid.Empty));

            // Non-workflow-activity plugin types that register no steps are dead code.
            foreach (var t in types.Where(t => t.GetAttributeValue<bool?>("isworkflowactivity") != true))
            {
                if (!typeIdsWithSteps.Contains(t.Id))
                    findings.Add(new Finding(Category, Severity.Low, "Plugin type has no steps",
                        $"Plugin type '{t.GetAttributeValue<string>("typename")}' registers no steps.",
                        t.GetAttributeValue<string>("typename"),
                        "Remove the unused plugin type, or register the intended step."));
            }

            var assemblyIdsWithSteppedTypes = new HashSet<Guid>(types
                .Where(t => typeIdsWithSteps.Contains(t.Id))
                .Select(t => t.GetAttributeValue<EntityReference>("pluginassemblyid")?.Id ?? Guid.Empty));

            foreach (var a in assemblies)
                if (!assemblyIdsWithSteppedTypes.Contains(a.Id))
                    findings.Add(new Finding(Category, Severity.Medium, "Plugin assembly has no active steps",
                        $"Assembly '{a.GetAttributeValue<string>("name")}' has no type registering a step.",
                        a.GetAttributeValue<string>("name"),
                        "Unregister the assembly if it is obsolete to cut deployment weight and confusion."));

            return findings;
        }
    }

    /// <summary>Flags process definitions that were never activated (draft) — clutter that hides intent.</summary>
    public sealed class OrphanedComponentsAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Orphaned Components";
        public string Category => "Orphaned Components";

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Looking for orphaned processes…");

            // type 1 = Definition; statecode 0 = Draft (never activated).
            var drafts = ctx.SafeRetrieveAll(new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("name", "category"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("type", ConditionOperator.Equal, 1),
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                    }
                }
            });

            foreach (var w in drafts)
                findings.Add(new Finding(Category, Severity.Low, "Draft process never activated",
                    $"Process '{w.GetAttributeValue<string>("name")}' is a draft that has never been activated.",
                    w.GetAttributeValue<string>("name"),
                    "Activate it if it is needed, or delete it to remove dead configuration."));

            return findings;
        }
    }

    /// <summary>Flags plugin-step registrations with known performance-risk patterns.</summary>
    public sealed class PerformanceAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Performance Bottlenecks";
        public string Category => "Performance";

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Evaluating plugin performance patterns…");

            var messages = PluginRegistration.MessageNames(ctx);
            var steps = PluginRegistration.Steps(ctx)
                .Where(s => s.GetAttributeValue<OptionSetValue>("statecode")?.Value != TechDebtContext.StateInactive)
                .ToList();

            foreach (var s in steps)
            {
                var name = s.GetAttributeValue<string>("name");
                var msgId = s.GetAttributeValue<EntityReference>("sdkmessageid")?.Id ?? Guid.Empty;
                messages.TryGetValue(msgId, out var message);
                int mode = s.GetAttributeValue<OptionSetValue>("mode")?.Value ?? 0; // 0 sync, 1 async
                string filtering = s.GetAttributeValue<string>("filteringattributes");

                if (string.Equals(message, "RetrieveMultiple", StringComparison.OrdinalIgnoreCase))
                    findings.Add(new Finding(Category, Severity.High, "Plugin on RetrieveMultiple",
                        $"Step '{name}' runs on RetrieveMultiple — it executes on every query of the table and is a common latency source.",
                        name, "Move the logic elsewhere or scope it tightly; avoid per-query plugins."));

                if (mode == 0 && string.Equals(message, "Update", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(filtering))
                    findings.Add(new Finding(Category, Severity.Medium, "Synchronous Update plugin without filtering attributes",
                        $"Step '{name}' fires synchronously on every Update with no filtering attributes.",
                        name, "Set filtering attributes so the plugin only runs when relevant fields change."));
            }

            return findings;
        }
    }
}
