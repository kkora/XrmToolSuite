using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Cloud flow readiness (draft state, broken/missing connection references)
    /// and plugin step health (missing assemblies, disabled steps, missing target tables,
    /// duplicate registrations and execution-rank conflicts).
    /// </summary>
    public class FlowPluginAnalyzer : IAnalyzer
    {
        public string Name => "Flows & Plugins";
        public AnalyzerCategory Category => AnalyzerCategory.FlowsAndPlugins;
        public bool BenefitsFromTarget => true;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();
            AnalyzeFlows(ctx, findings, progress);
            AnalyzePluginSteps(ctx, findings, progress);
            return findings;
        }

        private void AnalyzeFlows(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Flows: reading workflow records…");

            EntityCollection flows;
            try
            {
                flows = ctx.QuerySolutionRows("workflow", "workflowid",
                    "name", "category", "statecode", "clientdata", "type");
            }
            catch (Exception ex)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Flow scan unavailable", ex.Message,
                    ctx.SolutionUniqueName, "Verify access to the workflow table."));
                return;
            }

            // All connection reference logical names known in the SOURCE environment
            var knownCrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var crQe = new QueryExpression("connectionreference")
            {
                ColumnSet = new ColumnSet("connectionreferencelogicalname")
            };
            foreach (var cr in AnalyzerContext.SafeRetrieve(ctx.Source, crQe).Entities)
                knownCrs.Add(cr.GetAttributeValue<string>("connectionreferencelogicalname") ?? "");

            foreach (var wf in flows.Entities)
            {
                var name = wf.GetAttributeValue<string>("name");
                var category = wf.GetAttributeValue<OptionSetValue>("category")?.Value ?? -1;
                var state = wf.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? -1;
                var type = wf.GetAttributeValue<OptionSetValue>("type")?.Value ?? 1;
                if (type != 1) continue; // only definitions, skip activations/templates

                bool isCloudFlow = category == 5;
                bool isClassicWf = category == 0;
                bool isBpf = category == 4;

                if ((isCloudFlow || isClassicWf || isBpf) && state == 0) // 0 = Draft
                {
                    findings.Add(new RiskFinding(Category,
                        isCloudFlow ? Severity.Medium : Severity.Low,
                        isCloudFlow ? "Cloud flow is OFF (draft)" : "Process is in Draft state",
                        $"'{name}' is not activated in the source. Managed import preserves state — it will arrive OFF in the target.",
                        name,
                        $"Turn on '{name}' before export, or plan a post-deployment activation step (pipelines: activate via deployment settings / pac cli)."));
                }

                if (isCloudFlow)
                {
                    var clientData = wf.GetAttributeValue<string>("clientdata");
                    foreach (var missing in FindMissingConnectionRefs(clientData, knownCrs))
                    {
                        findings.Add(new RiskFinding(Category, Severity.High, "Flow references missing connection reference",
                            $"Flow '{name}' references connection reference '{missing}', which does not exist in this environment. The flow cannot be turned on.",
                            name,
                            $"Recreate or fix the connection reference '{missing}', or edit the flow to point at an existing one, then re-add the flow to the solution."));
                    }
                }
            }
        }

        private static IEnumerable<string> FindMissingConnectionRefs(string clientData, HashSet<string> knownCrs)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(clientData)) return missing;

            try
            {
                var json = JObject.Parse(clientData);
                var crs = json.SelectToken("properties.connectionReferences") as JObject;
                if (crs == null) return missing;

                foreach (var prop in crs.Properties())
                {
                    // Solution-aware flows: { "shared_xxx": { "connection": { "connectionReferenceLogicalName": "prefix_name" }, ... } }
                    var logical =
                        (string)prop.Value.SelectToken("connection.connectionReferenceLogicalName") ??
                        (string)prop.Value.SelectToken("connectionReferenceLogicalName");

                    if (!string.IsNullOrEmpty(logical) && !knownCrs.Contains(logical))
                        missing.Add(logical);
                }
            }
            catch { /* malformed clientdata — ignore */ }

            return missing.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void AnalyzePluginSteps(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Plugins: validating steps and assemblies…");

            var qe = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("name", "statecode", "plugintypeid", "sdkmessageid",
                    "sdkmessagefilterid", "stage", "mode", "rank", "filteringattributes")
            };
            var solLink = qe.AddLink("solutioncomponent", "sdkmessageprocessingstepid", "objectid");
            solLink.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, ctx.SolutionId);

            // Outer joins so a broken chain (missing type/assembly) still returns the step
            var typeLink = qe.AddLink("plugintype", "plugintypeid", "plugintypeid", JoinOperator.LeftOuter);
            typeLink.EntityAlias = "pt";
            typeLink.Columns = new ColumnSet("typename", "pluginassemblyid");

            var asmLink = typeLink.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid", JoinOperator.LeftOuter);
            asmLink.EntityAlias = "pa";
            asmLink.Columns = new ColumnSet("name");

            var filterLink = qe.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter);
            filterLink.EntityAlias = "mf";
            filterLink.Columns = new ColumnSet("primaryobjecttypecode");

            var msgLink = qe.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid", JoinOperator.LeftOuter);
            msgLink.EntityAlias = "sm";
            msgLink.Columns = new ColumnSet("name");

            EntityCollection steps;
            try { steps = ctx.Source.RetrieveMultiple(qe); }
            catch (Exception ex)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Plugin step scan unavailable", ex.Message,
                    ctx.SolutionUniqueName, "Verify access to sdkmessageprocessingstep."));
                return;
            }

            var targetEntityNames = ctx.HasTarget
                ? new HashSet<string>(ctx.TargetEntities().Select(e => e.LogicalName), StringComparer.OrdinalIgnoreCase)
                : null;

            foreach (var step in steps.Entities)
            {
                var name = step.GetAttributeValue<string>("name");
                var state = step.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0;
                string typeName = Alias<string>(step, "pt.typename");
                string asmName = Alias<string>(step, "pa.name");
                string primaryEntity = Alias<string>(step, "mf.primaryobjecttypecode");

                if (state == 1)
                {
                    findings.Add(new RiskFinding(Category, Severity.Low, "Plugin step disabled",
                        $"Step '{name}' is disabled. Managed import preserves this — the step will remain disabled in the target.",
                        name, $"Confirm the disabled state is intentional; otherwise enable '{name}' before export."));
                }

                if (step.GetAttributeValue<EntityReference>("plugintypeid") != null && typeName == null)
                {
                    findings.Add(new RiskFinding(Category, Severity.Critical, "Plugin step points to missing plugin type",
                        $"Step '{name}' references a plugin type that no longer exists (assembly re-registered with different types?). Import will fail.",
                        name, $"Re-register the correct assembly/type or delete the orphaned step '{name}'."));
                }
                else if (typeName != null && asmName == null)
                {
                    findings.Add(new RiskFinding(Category, Severity.Critical, "Plugin type missing its assembly",
                        $"Step '{name}' → type '{typeName}' has no backing plugin assembly. Import will fail.",
                        name, "Re-register the plugin assembly (PAC / PRT) and re-associate the step."));
                }

                if (!string.IsNullOrEmpty(primaryEntity) && targetEntityNames != null &&
                    !targetEntityNames.Contains(primaryEntity))
                {
                    findings.Add(new RiskFinding(Category, Severity.High, "Plugin step targets table missing in target",
                        $"Step '{name}' runs on table '{primaryEntity}', which does not exist in the target. Import order problem or missing dependency.",
                        name, $"Ensure table '{primaryEntity}' is included in this solution or deployed to the target first."));
                }
            }

            AnalyzeStepConflicts(steps, findings, progress);
        }

        /// <summary>
        /// Detects steps that fire on the same event (message + message filter + stage): the same plugin
        /// type registered twice with overlapping filtering attributes (a genuine double-execution bug),
        /// and steps of different types that share an execution rank (non-deterministic ordering).
        /// Only enabled steps are considered — disabled steps never execute.
        /// </summary>
        private void AnalyzeStepConflicts(EntityCollection steps, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Plugins: checking for duplicate / conflicting steps…");

            var enabled = steps.Entities
                .Where(s => (s.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0) == 0)
                .ToList();

            var groups = enabled.GroupBy(s => new
            {
                Message = s.GetAttributeValue<EntityReference>("sdkmessageid")?.Id ?? Guid.Empty,
                Filter = s.GetAttributeValue<EntityReference>("sdkmessagefilterid")?.Id ?? Guid.Empty,
                Stage = s.GetAttributeValue<OptionSetValue>("stage")?.Value ?? -1
            });

            foreach (var group in groups)
            {
                var list = group.ToList();
                if (list.Count < 2) continue;

                string messageName = list.Select(s => Alias<string>(s, "sm.name")).FirstOrDefault(n => n != null);
                string entity = list.Select(s => Alias<string>(s, "mf.primaryobjecttypecode")).FirstOrDefault(n => n != null);
                string scope = DescribeScope(messageName, entity, group.Key.Stage);

                // Duplicate registrations: same plugin type + same mode + overlapping filtering attributes.
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var a = list[i];
                        var b = list[j];
                        var typeA = a.GetAttributeValue<EntityReference>("plugintypeid")?.Id;
                        var typeB = b.GetAttributeValue<EntityReference>("plugintypeid")?.Id;
                        if (typeA == null || typeA != typeB) continue;
                        if (StepMode(a) != StepMode(b)) continue;
                        if (!FilterAttributesOverlap(a.GetAttributeValue<string>("filteringattributes"),
                                                     b.GetAttributeValue<string>("filteringattributes"))) continue;

                        var nameA = a.GetAttributeValue<string>("name");
                        var nameB = b.GetAttributeValue<string>("name");
                        findings.Add(new RiskFinding(Category, Severity.High, "Duplicate SDK step registration",
                            $"Steps '{nameA}' and '{nameB}' register the same plugin type on {scope} with overlapping filtering attributes ({ModeLabel(StepMode(a))}). The plugin executes twice per operation — double writes, doubled side effects, or an infinite loop.",
                            nameA,
                            $"Remove or disable one of the duplicate steps, or narrow their filtering attributes so they no longer overlap."));
                    }

                // Rank collisions across DIFFERENT types (same-type ties are already reported as duplicates above).
                var rankClashes = list
                    .GroupBy(s => new { Mode = StepMode(s), Rank = s.GetAttributeValue<int>("rank") })
                    .Where(g => g.Count() > 1
                        && g.Select(s => s.GetAttributeValue<EntityReference>("plugintypeid")?.Id).Distinct().Count() > 1);

                foreach (var clash in rankClashes)
                {
                    var names = clash.Select(s => "'" + s.GetAttributeValue<string>("name") + "'").ToList();
                    findings.Add(new RiskFinding(Category, Severity.Medium, "Plugin steps share an execution rank",
                        $"{clash.Count()} steps on {scope} share rank {clash.Key.Rank} ({ModeLabel(clash.Key.Mode)}): {string.Join(", ", names)}. Dataverse does not guarantee execution order when ranks tie, so behaviour can differ between environments.",
                        string.Join(", ", names),
                        "Assign distinct rank values so the execution order is deterministic across environments."));
                }
            }
        }

        private static int StepMode(Entity step) => step.GetAttributeValue<OptionSetValue>("mode")?.Value ?? 0;

        private static string ModeLabel(int mode) => mode == 1 ? "asynchronous" : "synchronous";

        private static string DescribeScope(string messageName, string entity, int stage)
        {
            var stageLabel = StageLabel(stage);
            if (!string.IsNullOrEmpty(messageName))
            {
                var target = string.IsNullOrEmpty(entity) ? messageName : $"{messageName} of {entity}";
                return $"{target} ({stageLabel})";
            }
            return string.IsNullOrEmpty(entity)
                ? $"the same message ({stageLabel})"
                : $"{entity} ({stageLabel})";
        }

        private static string StageLabel(int stage)
        {
            switch (stage)
            {
                case 10: return "Pre-validation";
                case 20: return "Pre-operation";
                case 40: return "Post-operation";
                default: return $"stage {stage}";
            }
        }

        /// <summary>
        /// Two steps overlap when their filtering-attribute sets intersect. An empty set means the step
        /// fires on every change to the record, so it overlaps any other step on the same event.
        /// </summary>
        private static bool FilterAttributesOverlap(string a, string b)
        {
            var setA = SplitAttributes(a);
            var setB = SplitAttributes(b);
            if (setA.Count == 0 || setB.Count == 0) return true;
            return setA.Overlaps(setB);
        }

        private static HashSet<string> SplitAttributes(string csv) =>
            new HashSet<string>(
                (csv ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()),
                StringComparer.OrdinalIgnoreCase);

        private static T Alias<T>(Entity e, string key) where T : class =>
            e.Contains(key) ? ((AliasedValue)e[key]).Value as T : null;
    }
}
