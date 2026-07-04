using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.AiSolutionReviewer.Analysis
{
    /// <summary>
    /// Reviews the plugin layer: expensive registrations, sync-on-write without filtering, and unmanaged
    /// registration in a managed solution — the facts an architect weighs when judging plugin design.
    /// </summary>
    public sealed class PluginReviewCollector : IAnalyzer<ReviewContext>
    {
        public string Name => "Plugins";
        public string Category => "Plugins";

        public List<Finding> Analyze(ReviewContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Reviewing plugins…");

            var steps = ctx.QuerySolutionRows("sdkmessageprocessingstep", "sdkmessageprocessingstepid",
                "name", "mode", "filteringattributes", "stage").Entities;
            var messages = ctx.SafeRetrieve(new Microsoft.Xrm.Sdk.Query.QueryExpression("sdkmessage")
            { ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name") })
                .Entities.ToDictionary(m => m.Id, m => m.GetAttributeValue<string>("name") ?? "");

            int syncNoFilter = 0;
            foreach (var s in steps)
            {
                var name = s.GetAttributeValue<string>("name");
                int mode = s.GetAttributeValue<OptionSetValue>("mode")?.Value ?? 0;
                if (mode == 0 && string.IsNullOrWhiteSpace(s.GetAttributeValue<string>("filteringattributes")))
                {
                    syncNoFilter++;
                    if (syncNoFilter <= 5)
                        findings.Add(new Finding(Category, Severity.Medium, "Synchronous step without filtering attributes",
                            $"Step '{name}' runs synchronously with no filtering attributes — it fires on every write.",
                            name, "Add filtering attributes or move non-critical work to async."));
                }
            }

            if (steps.Count >= 20)
                findings.Add(new Finding(Category, Severity.Low, "Heavy plugin footprint",
                    $"The solution registers {steps.Count} plugin steps — significant hidden logic to maintain and test.",
                    "Plugin steps", "Review for redundancy; prefer low-code where a plugin is not required."));

            if (steps.Count == 0)
                findings.Add(new Finding(Category, Severity.Info, "No plugin steps in solution",
                    "No plugin steps were found in this solution.", "Plugins", null));

            return findings;
        }
    }

    /// <summary>Reviews client-side scripting: deprecated APIs and script volume.</summary>
    public sealed class ScriptReviewCollector : IAnalyzer<ReviewContext>
    {
        public string Name => "JavaScript";
        public string Category => "JavaScript";

        private static readonly string[] DeprecatedTokens = { "Xrm.Page", "crmForm", "/2011/Organization.svc", "getServerUrl" };

        public List<Finding> Analyze(ReviewContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Reviewing JavaScript…");

            var scripts = ctx.QuerySolutionRows("webresource", "webresourceid", "name", "webresourcetype", "content")
                .Entities.Where(w => w.GetAttributeValue<OptionSetValue>("webresourcetype")?.Value == 3).ToList();

            foreach (var w in scripts)
            {
                var name = w.GetAttributeValue<string>("name");
                var code = Decode(w.GetAttributeValue<string>("content"));
                if (string.IsNullOrEmpty(code)) continue;
                var hits = DeprecatedTokens.Where(t => code.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                if (hits.Count > 0)
                    findings.Add(new Finding(Category, Severity.Medium, "Deprecated client API in use",
                        $"'{name}' uses deprecated API(s): {string.Join(", ", hits)}.",
                        name, "Migrate to the supported executionContext / Xrm.WebApi client API."));
            }

            if (scripts.Count >= 15)
                findings.Add(new Finding(Category, Severity.Low, "Heavy client-side scripting",
                    $"{scripts.Count} JavaScript web resources — high maintenance and upgrade cost.",
                    "JavaScript", "Audit for dead code; prefer business rules / low-code where possible."));

            return findings;
        }

        private static string Decode(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(base64)); } catch { return null; }
        }
    }

    /// <summary>Reviews automation: legacy classic workflows and automation sprawl.</summary>
    public sealed class AutomationReviewCollector : IAnalyzer<ReviewContext>
    {
        public string Name => "Automation";
        public string Category => "Automation";

        public List<Finding> Analyze(ReviewContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Reviewing automation…");

            var procs = ctx.QuerySolutionRows("workflow", "workflowid", "name", "category").Entities;
            int classic = procs.Count(p => p.GetAttributeValue<OptionSetValue>("category")?.Value == 0);
            int flows = procs.Count(p => p.GetAttributeValue<OptionSetValue>("category")?.Value == 5);

            if (classic > 0)
                findings.Add(new Finding(Category, Severity.Medium, "Legacy classic workflows",
                    $"{classic} classic workflow(s) are present — Microsoft steers new automation to Power Automate flows.",
                    "Classic workflows", "Plan migration of classic workflows to cloud flows."));

            if (classic + flows >= 25)
                findings.Add(new Finding(Category, Severity.Low, "Automation sprawl",
                    $"{classic + flows} processes make behaviour hard to reason about and test.",
                    "Automation", "Consolidate overlapping automation and retire unused processes."));

            return findings;
        }
    }

    /// <summary>Reviews ALM readiness and governance: managed state, publisher prefix, descriptions.</summary>
    public sealed class AlmGovernanceReviewCollector : IAnalyzer<ReviewContext>
    {
        public string Name => "ALM & Governance";
        public string Category => "ALM & Governance";

        public List<Finding> Analyze(ReviewContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Reviewing ALM readiness…");

            if (!ctx.SolutionIsManaged)
                findings.Add(new Finding(Category, Severity.Medium, "Unmanaged solution",
                    "This solution is unmanaged — unmanaged customisations in downstream environments are hard to govern and cannot be cleanly uninstalled.",
                    ctx.SolutionUniqueName, "Ship as a managed solution through the ALM pipeline; keep dev unmanaged only."));

            if ((ctx.SolutionUniqueName ?? "").StartsWith("new_", StringComparison.OrdinalIgnoreCase))
                findings.Add(new Finding(Category, Severity.Low, "Default publisher prefix",
                    "The solution/publisher uses the default 'new_' prefix, which is not attributable and collides across makers.",
                    ctx.SolutionUniqueName, "Use a dedicated publisher with a meaningful customization prefix."));

            findings.Add(new Finding(Category, Severity.Info, "Version",
                $"Solution version {ctx.SolutionVersion}.", ctx.SolutionUniqueName,
                "Adopt semantic versioning and bump per release."));

            return findings;
        }
    }
}
