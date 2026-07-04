using System;
using System.Linq;
using System.Text;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using XrmToolSuite.DeploymentRiskAnalyzer.Scoring;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Summarization
{
    /// <summary>
    /// Deterministic, offline executive summary (no network calls). This is the default (Mode A) and
    /// also the fallback used when the AI service is unavailable or the AI opt-in is declined.
    /// </summary>
    public sealed class TemplatedSummaryGenerator : ISummaryGenerator
    {
        public DeploymentSummary Generate(AnalysisResult r, SummaryOptions options, Action<string> progress)
        {
            string verdict =
                r.Risk == OverallRisk.High
                    ? "NO-GO — remediate the critical/high findings before deploying."
                    : r.Risk == OverallRisk.Medium
                        ? "GO WITH CAUTION — review the findings below and mitigate where feasible."
                        : "GO — no significant deployment risk detected.";

            var sb = new StringBuilder();
            sb.AppendLine(
                $"Deployment risk for {r.SolutionFriendlyName} v{r.SolutionVersion} " +
                $"({(r.SolutionIsManaged ? "managed" : "unmanaged")}) is {r.Risk.ToString().ToUpperInvariant()} " +
                $"(score {r.Score}/100)" +
                (r.TargetEnvironment != null
                    ? $", assessed against target '{r.TargetEnvironment}'."
                    : ", with no target connected (schema & target checks limited)."));
            sb.AppendLine();
            sb.AppendLine("Recommendation: " + verdict);
            sb.AppendLine();
            sb.AppendLine(RiskScoreCalculator.Explain(r));

            var top = r.Findings
                .Where(f => f.Severity >= Severity.Medium)
                .OrderByDescending(f => f.Severity)
                .Take(5)
                .ToList();
            if (top.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Top risks:");
                foreach (var f in top)
                    sb.AppendLine($"  • [{f.Severity}] {f.Title} ({f.AffectedComponent}) — {f.Recommendation}");
            }

            return new DeploymentSummary { Text = sb.ToString().TrimEnd(), FromAi = false };
        }
    }
}
