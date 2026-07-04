using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Finds components present in the TARGET's installed copy of the solution but absent from the
    /// source being deployed. On a managed upgrade (Stage for Upgrade / pac solution upgrade) those
    /// components are DELETED — tables and columns take their data with them. Requires a target;
    /// only the managed-upgrade path deletes, so an unmanaged target is reported informationally.
    /// </summary>
    public class DeletedComponentAnalyzer : IAnalyzer
    {
        public string Name => "Deleted Components";
        public AnalyzerCategory Category => AnalyzerCategory.DeletedComponents;
        public bool BenefitsFromTarget => true;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();

            if (!ctx.HasTarget)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Deleted-component check skipped",
                    "No target environment connected — the source cannot be diffed against the installed solution.",
                    ctx.SolutionUniqueName, "Connect a target environment (toolbar button) and re-run the analysis."));
                return findings;
            }

            progress("Deletions: locating installed solution in target…");
            var tgtSol = FindTargetSolution(ctx);
            if (tgtSol == null)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "No prior version in target",
                    $"'{ctx.SolutionUniqueName}' is not installed in the target — this is a fresh install, so nothing can be deleted.",
                    ctx.SolutionUniqueName, "No action needed for deletions."));
                return findings;
            }

            progress("Deletions: comparing solution components…");
            var removed = RemovedComponents(ctx, tgtSol.Id);
            if (removed.Count == 0) return findings;

            bool tgtManaged = tgtSol.GetAttributeValue<bool?>("ismanaged") ?? false;
            if (!tgtManaged)
            {
                findings.Add(new RiskFinding(Category, Severity.Info,
                    "Components removed since target's version (unmanaged)",
                    $"{removed.Count} component(s) exist in the target's unmanaged solution but not in the source. An unmanaged import does NOT delete them — they remain in the target as unsolutioned drift rather than being removed.",
                    ctx.SolutionUniqueName,
                    "If the intent was to remove them, delete them manually in the target; otherwise no action is needed."));
                return findings;
            }

            foreach (var c in removed)
            {
                var typeLabel = ComponentTypeLabels.Get(c.ComponentType);
                var severity = SeverityFor(c.ComponentType);
                bool losesData = c.ComponentType == AnalyzerContext.CT_Entity ||
                                 c.ComponentType == AnalyzerContext.CT_Attribute;

                findings.Add(new RiskFinding(Category, severity,
                    $"{typeLabel} deleted on managed upgrade",
                    $"A {typeLabel} ({c.ObjectId}) exists in the target's managed solution but not in the source. " +
                    $"A managed upgrade will delete it{(losesData ? " — and all data it holds is permanently lost" : "")}.",
                    $"{typeLabel}:{c.ObjectId}",
                    losesData
                        ? "Back up affected data, then confirm the removal is intentional. Use 'Stage for Upgrade' to review the deletion set before applying; a plain 'Update' import will not delete it."
                        : "Confirm the removal is intentional. Use 'Stage for Upgrade' to review the deletion set before applying; a plain 'Update' import will not delete it."));
            }

            return findings;
        }

        private static Entity FindTargetSolution(AnalyzerContext ctx)
        {
            var qe = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("ismanaged"),
                TopCount = 1,
                Criteria = { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, ctx.SolutionUniqueName) } }
            };
            return AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities.FirstOrDefault();
        }

        private static List<Component> RemovedComponents(AnalyzerContext ctx, Guid targetSolutionId)
        {
            var sourceKeys = new HashSet<string>(
                (ctx.SolutionComponents ?? new List<Entity>())
                    .Select(Key)
                    .Where(k => k != null));

            var qe = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("componenttype", "objectid"),
                Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, targetSolutionId) } }
            };

            return AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities
                .Where(e => Key(e) != null && !sourceKeys.Contains(Key(e)))
                .Select(e => new Component
                {
                    ComponentType = e.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? -1,
                    ObjectId = e.GetAttributeValue<Guid>("objectid")
                })
                .GroupBy(c => c.ComponentType + ":" + c.ObjectId)
                .Select(g => g.First())
                .ToList();
        }

        private static string Key(Entity component)
        {
            var type = component.GetAttributeValue<OptionSetValue>("componenttype")?.Value;
            var oid = component.GetAttributeValue<Guid>("objectid");
            return type.HasValue && oid != Guid.Empty ? type.Value + ":" + oid : null;
        }

        private static Severity SeverityFor(int componentType)
        {
            if (componentType == AnalyzerContext.CT_Entity) return Severity.Critical;   // table + data
            if (componentType == AnalyzerContext.CT_Attribute) return Severity.High;    // column + data
            return Severity.Medium;                                                      // metadata-only deletion
        }

        private struct Component
        {
            public int ComponentType;
            public Guid ObjectId;
        }
    }
}
