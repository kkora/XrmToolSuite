using System;
using System.Linq;
using System.Text;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>
    /// Deterministic, offline executive summary (no network calls). This is the default and also the
    /// fallback used when the AI service is unavailable or the AI opt-in is declined. Generic over
    /// <see cref="ReportModel"/>: verdict phrasing comes from the model (see
    /// <see cref="ReportModel.Verdict"/>), so each tool keeps its own vocabulary.
    /// </summary>
    public sealed class TemplatedSummaryGenerator : ISummaryGenerator
    {
        private readonly ScoreCalculator _scorer;

        public TemplatedSummaryGenerator(ScoreCalculator scorer = null)
        {
            _scorer = scorer ?? ScoreCalculator.RiskDefault;
        }

        public SummaryResult Generate(ReportModel r, SummaryOptions options, Action<string> progress)
        {
            string subject = string.IsNullOrEmpty(r.SubjectVersion) ? r.SubjectName : $"{r.SubjectName} v{r.SubjectVersion}";
            string managed = r.IsManaged.HasValue ? (r.IsManaged.Value ? " (managed)" : " (unmanaged)") : "";
            string targetClause = r.TargetEnvironment != null
                ? $", assessed against target '{r.TargetEnvironment}'."
                : (r.SourceEnvironment != null ? "." : ", with no target connected.");

            var sb = new StringBuilder();
            sb.AppendLine(
                $"{Capitalize(r.ScoreWord)} for {subject}{managed} is {r.Band.ToString().ToUpperInvariant()} " +
                $"(score {r.Score}/100){targetClause}");
            sb.AppendLine();
            sb.AppendLine("Recommendation: " + r.Verdict());
            sb.AppendLine();
            sb.AppendLine(ScoreCalculator.Explain(r.Findings, r.Score, r.Band, r.ScoreWord));

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
                    sb.AppendLine($"  • [{f.Severity}] {f.Title} ({f.Component}) — {f.Recommendation}");
            }

            return new SummaryResult { Text = sb.ToString().TrimEnd(), FromAi = false };
        }

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
