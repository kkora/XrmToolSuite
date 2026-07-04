using System;
using System.Collections.Generic;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Inspects the forms in the solution for deployment-risky references — currently form scripts /
    /// controls that point at a web resource which does not exist in the source (the script or control
    /// will fail to load in the target). Source-only; degrades to an informational finding on failure.
    /// </summary>
    public class FormAnalyzer : IAnalyzer
    {
        public string Name => "Form Changes";
        public AnalyzerCategory Category => AnalyzerCategory.Forms;
        public bool BenefitsFromTarget => false;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();
            progress("Forms: reading form definitions…");

            EntityCollection forms;
            try
            {
                forms = ctx.QuerySolutionRows("systemform", "formid", "name", "objecttypecode", "formxml");
            }
            catch (Exception ex)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Form scan unavailable", ex.Message,
                    ctx.SolutionUniqueName, "Verify access to the systemform table."));
                return findings;
            }

            if (forms.Entities.Count == 0)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "No forms in solution",
                    "The solution contains no system forms to analyze.", ctx.SolutionUniqueName, "No action required."));
                return findings;
            }

            var sourceWrs = WebResourceReferences.SourceNames(ctx);

            foreach (var form in forms.Entities)
            {
                var name = form.GetAttributeValue<string>("name");
                var entity = form.GetAttributeValue<string>("objecttypecode");
                var xml = form.GetAttributeValue<string>("formxml");

                foreach (var wr in WebResourceReferences.Extract(xml, includeFormLibraries: true))
                {
                    if (!sourceWrs.Contains(wr))
                    {
                        findings.Add(new RiskFinding(Category, Severity.High, "Form references missing web resource",
                            $"Form '{name}' ({entity}) references web resource '{wr}', which does not exist in the source. The form script/control will fail to load after import.",
                            $"{entity}: {name}",
                            $"Add web resource '{wr}' to the solution (or fix the reference) before export."));
                    }
                }
            }

            return findings;
        }
    }
}
