using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Missing component dependencies, publisher prerequisites, required managed
    /// solutions and unmanaged-layer / duplicate-component risks.
    /// </summary>
    public class DependencyAnalyzer : IAnalyzer
    {
        public string Name => "Solution Dependencies";
        public AnalyzerCategory Category => AnalyzerCategory.Dependencies;
        public bool BenefitsFromTarget => true;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();

            CheckMissingDependencies(ctx, findings, progress);
            CheckPublisher(ctx, findings, progress);
            CheckDuplicateUnmanagedLayers(ctx, findings, progress);
            CheckManagedState(ctx, findings);

            return findings;
        }

        private void CheckMissingDependencies(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Dependencies: retrieving missing dependencies…");
            EntityCollection missing;
            try
            {
                var resp = (RetrieveMissingDependenciesResponse)ctx.Source.Execute(
                    new RetrieveMissingDependenciesRequest { SolutionUniqueName = ctx.SolutionUniqueName });
                missing = resp.EntityCollection;
            }
            catch (Exception ex)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Dependency check partially unavailable",
                    $"RetrieveMissingDependencies failed: {ex.Message}", ctx.SolutionUniqueName,
                    "Re-run with a user having System Administrator or System Customizer role."));
                return;
            }

            if (missing.Entities.Count == 0)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "No missing dependencies",
                    "All required components are contained in the solution.", ctx.SolutionUniqueName,
                    "No action required."));
                return;
            }

            // Group required components by their base solution to surface "install solution X first" guidance.
            var requiredSolutionIds = new HashSet<Guid>();
            var targetEntityNames = ctx.HasTarget
                ? new HashSet<string>(ctx.TargetEntities().Select(e => e.LogicalName), StringComparer.OrdinalIgnoreCase)
                : null;

            foreach (var dep in missing.Entities)
            {
                var reqType = dep.GetAttributeValue<OptionSetValue>("requiredcomponenttype")?.Value ?? 0;
                var reqId = dep.GetAttributeValue<Guid?>("requiredcomponentobjectid") ?? Guid.Empty;
                var baseSolution = dep.GetAttributeValue<Guid?>("requiredcomponentbasesolutionid");
                if (baseSolution.HasValue) requiredSolutionIds.Add(baseSolution.Value);

                var typeLabel = ComponentTypeLabels.Get(reqType);
                string componentName = ResolveComponentName(ctx, reqType, reqId) ?? reqId.ToString();

                // If we have a target, try to confirm whether the required entity actually exists there.
                var severity = Severity.High;
                var detail = "This component is required by the solution but is not part of it. " +
                             "Import will fail if it is absent from the target environment.";

                if (reqType == AnalyzerContext.CT_Entity && targetEntityNames != null)
                {
                    var meta = ctx.SourceEntities().FirstOrDefault(e => e.MetadataId == reqId);
                    if (meta != null && targetEntityNames.Contains(meta.LogicalName))
                    {
                        severity = Severity.Low;
                        detail = "Required table exists in the target environment — verify its version/columns are compatible.";
                    }
                }

                findings.Add(new RiskFinding(Category, severity,
                    $"Missing dependency: {typeLabel}",
                    detail, componentName,
                    $"Ensure '{componentName}' ({typeLabel}) exists in the target before import, or add it to the solution.",
                    "https://learn.microsoft.com/power-platform/alm/solution-concepts-alm"));
            }

            // Required base (managed) solutions
            if (requiredSolutionIds.Count > 0)
            {
                var qe = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("friendlyname", "uniquename", "version"),
                    Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.In, requiredSolutionIds.Cast<object>().ToArray()) } }
                };
                foreach (var sol in AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities)
                {
                    var unique = sol.GetAttributeValue<string>("uniquename");
                    if (string.Equals(unique, "Active", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(unique, "Default", StringComparison.OrdinalIgnoreCase))
                    {
                        findings.Add(new RiskFinding(Category, Severity.Medium,
                            "Dependency on unmanaged (Active) layer",
                            "Some required components live only in the unmanaged/Default layer of the source. They travel with no managed solution and must be recreated or packaged separately.",
                            sol.GetAttributeValue<string>("friendlyname"),
                            "Move these components into a managed base solution or include them in this solution."));
                    }
                    else
                    {
                        bool presentInTarget = ctx.HasTarget && TargetHasSolution(ctx, unique);
                        findings.Add(new RiskFinding(Category,
                            presentInTarget ? Severity.Low : Severity.High,
                            "Required managed solution",
                            presentInTarget
                                ? $"Prerequisite solution '{unique}' is already installed in the target."
                                : $"Prerequisite solution '{unique}' v{sol.GetAttributeValue<string>("version")} was not found in the target (or no target is connected).",
                            unique,
                            presentInTarget ? "Verify the installed version is equal or higher."
                                            : $"Install '{unique}' v{sol.GetAttributeValue<string>("version")} (or higher) in the target before importing."));
                    }
                }
            }
        }

        private static bool TargetHasSolution(AnalyzerContext ctx, string uniqueName)
        {
            var qe = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("version"),
                TopCount = 1,
                Criteria = { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, uniqueName) } }
            };
            return AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities.Count > 0;
        }

        private void CheckPublisher(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Dependencies: validating publisher…");
            if (ctx.PublisherId == Guid.Empty) return;

            Entity pub;
            try { pub = ctx.Source.Retrieve("publisher", ctx.PublisherId, new ColumnSet("uniquename", "customizationprefix", "customizationoptionvalueprefix")); }
            catch { return; }

            var prefix = pub.GetAttributeValue<string>("customizationprefix");
            var unique = pub.GetAttributeValue<string>("uniquename");

            if (!ctx.HasTarget)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Publisher check (no target connected)",
                    $"Solution publisher is '{unique}' with prefix '{prefix}_'. Connect a target environment to verify prefix collisions.",
                    unique, "Connect a target environment for a full publisher validation."));
                return;
            }

            var qe = new QueryExpression("publisher")
            {
                ColumnSet = new ColumnSet("uniquename", "customizationprefix", "customizationoptionvalueprefix"),
                Criteria = { Conditions = { new ConditionExpression("customizationprefix", ConditionOperator.Equal, prefix) } }
            };
            var matches = AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities;

            var sameUnique = matches.FirstOrDefault(m =>
                string.Equals(m.GetAttributeValue<string>("uniquename"), unique, StringComparison.OrdinalIgnoreCase));

            if (sameUnique == null && matches.Count > 0)
            {
                findings.Add(new RiskFinding(Category, Severity.High, "Publisher prefix collision",
                    $"Target has a different publisher using prefix '{prefix}_'. Importing will create a second publisher and can split customizations across publishers.",
                    unique, "Align publisher unique names/prefixes across environments before deploying."));
            }
            else if (sameUnique != null)
            {
                var srcOpt = pub.GetAttributeValue<int?>("customizationoptionvalueprefix");
                var tgtOpt = sameUnique.GetAttributeValue<int?>("customizationoptionvalueprefix");
                if (srcOpt.HasValue && tgtOpt.HasValue && srcOpt != tgtOpt)
                {
                    findings.Add(new RiskFinding(Category, Severity.Medium, "Publisher option value prefix mismatch",
                        $"Option value prefix differs between source ({srcOpt}) and target ({tgtOpt}) for publisher '{unique}'. New choices created in each environment will collide.",
                        unique, "Standardize the option value prefix on the publisher record in all environments."));
                }
            }
        }

        private void CheckDuplicateUnmanagedLayers(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Dependencies: scanning for duplicate components in other unmanaged solutions…");

            var ids = ctx.SolutionComponents
                .Select(c => c.GetAttributeValue<Guid>("objectid"))
                .Distinct()
                .Take(500) // guard against enormous IN clauses
                .Cast<object>()
                .ToArray();
            if (ids.Length == 0) return;

            var qe = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype", "solutionid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("objectid", ConditionOperator.In, ids),
                        new ConditionExpression("solutionid", ConditionOperator.NotEqual, ctx.SolutionId)
                    }
                }
            };
            var link = qe.AddLink("solution", "solutionid", "solutionid");
            link.EntityAlias = "sol";
            link.Columns = new ColumnSet("friendlyname", "uniquename", "ismanaged");
            link.LinkCriteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
            link.LinkCriteria.AddCondition("isvisible", ConditionOperator.Equal, true);

            var dupes = AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities
                .GroupBy(e => ((AliasedValue)e["sol.uniquename"]).Value as string)
                .Where(g => !string.Equals(g.Key, "Active", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(g.Key, "Default", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var g in dupes)
            {
                findings.Add(new RiskFinding(Category, Severity.Low, "Component shared with another unmanaged solution",
                    $"{g.Count()} component(s) of this solution also belong to unmanaged solution '{g.Key}'. Parallel edits across solutions can create conflicting layers and surprise overwrites.",
                    g.Key, "Consolidate ownership: keep each component in a single authoritative solution per stream of work."));
            }
        }

        private void CheckManagedState(AnalyzerContext ctx, List<RiskFinding> findings)
        {
            if (!ctx.SolutionIsManaged)
            {
                findings.Add(new RiskFinding(Category, Severity.Medium, "Solution is unmanaged",
                    "Deploying unmanaged solutions to downstream environments prevents clean rollback (components merge into the Active layer and cannot be removed by uninstalling).",
                    ctx.SolutionUniqueName,
                    "Export and deploy as MANAGED to test/production. Reserve unmanaged for development environments.",
                    "https://learn.microsoft.com/power-platform/alm/solution-concepts-alm#managed-and-unmanaged-solutions"));
            }
        }

        private static string ResolveComponentName(AnalyzerContext ctx, int type, Guid objectId)
        {
            try
            {
                switch (type)
                {
                    case AnalyzerContext.CT_Entity:
                        return ctx.SourceEntities().FirstOrDefault(e => e.MetadataId == objectId)?.LogicalName;
                    case AnalyzerContext.CT_WebResource:
                        return ctx.Source.Retrieve("webresource", objectId, new ColumnSet("name"))
                                  .GetAttributeValue<string>("name");
                    case AnalyzerContext.CT_Workflow:
                        return ctx.Source.Retrieve("workflow", objectId, new ColumnSet("name"))
                                  .GetAttributeValue<string>("name");
                    case AnalyzerContext.CT_Role:
                        return ctx.Source.Retrieve("role", objectId, new ColumnSet("name"))
                                  .GetAttributeValue<string>("name");
                    default:
                        return null;
                }
            }
            catch { return null; }
        }
    }
}
