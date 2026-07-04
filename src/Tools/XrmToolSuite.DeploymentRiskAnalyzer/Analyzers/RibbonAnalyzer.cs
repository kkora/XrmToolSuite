using System;
using System.Collections.Generic;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Inspects ribbon (command bar) customizations for command handlers that reference a web resource
    /// which does not exist in the source (the button command will fail in the target). Source-only;
    /// degrades to an informational finding on failure.
    /// </summary>
    public class RibbonAnalyzer : IAnalyzer
    {
        public string Name => "Ribbon Changes";
        public AnalyzerCategory Category => AnalyzerCategory.Ribbon;
        public bool BenefitsFromTarget => false;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();
            progress("Ribbon: reading ribbon customizations…");

            EntityCollection ribbons;
            try
            {
                ribbons = ctx.QuerySolutionRows("ribboncustomization", "ribboncustomizationid", "entity", "ribbondiffxml");
            }
            catch (Exception ex)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Ribbon scan unavailable", ex.Message,
                    ctx.SolutionUniqueName, "Verify access to the ribboncustomization table."));
                return findings;
            }

            if (ribbons.Entities.Count == 0)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "No ribbon customizations in solution",
                    "The solution contains no ribbon (command bar) customizations to analyze.",
                    ctx.SolutionUniqueName, "No action required."));
                return findings;
            }

            var sourceWrs = WebResourceReferences.SourceNames(ctx);

            foreach (var rc in ribbons.Entities)
            {
                var entity = rc.GetAttributeValue<string>("entity");
                var xml = rc.GetAttributeValue<string>("ribbondiffxml");
                var scope = string.IsNullOrEmpty(entity) ? "application ribbon" : entity;

                foreach (var wr in WebResourceReferences.Extract(xml, includeFormLibraries: false))
                {
                    if (!sourceWrs.Contains(wr))
                    {
                        findings.Add(new RiskFinding(Category, Severity.High, "Ribbon command references missing web resource",
                            $"Ribbon for '{scope}' references web resource '{wr}', which does not exist in the source. The button command will fail after import.",
                            scope,
                            $"Add web resource '{wr}' to the solution (or fix the command reference) before export."));
                    }
                }
            }

            return findings;
        }
    }
}
