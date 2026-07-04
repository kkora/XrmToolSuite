using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.Core.Analysis
{
    /// <summary>A key/value metric row for report summary panels and Excel summary sheets.</summary>
    public sealed class MetricRow
    {
        public string Label { get; set; }
        public string Value { get; set; }
        /// <summary>Optional secondary caption (e.g. a unit or short explanation).</summary>
        public string Hint { get; set; }

        public MetricRow() { }
        public MetricRow(string label, string value, string hint = null)
        {
            Label = label; Value = value; Hint = hint;
        }
    }

    /// <summary>A titled next-step / call-to-action rendered in the HTML and PDF reports.</summary>
    public sealed class NextStep
    {
        public string Title { get; set; }
        public string Detail { get; set; }

        public NextStep() { }
        public NextStep(string title, string detail) { Title = title; Detail = detail; }
    }

    /// <summary>
    /// The single, tool-agnostic model every exporter (JSON / Markdown / HTML / Excel / PDF) consumes.
    /// Each tool populates it — identity/branding strings, the score + band, findings, metric rows, and
    /// next-steps — so the shared reporters render an equivalent dashboard for any tool without knowing
    /// which one produced it. SDK-free (BCL only) so it lives in the shared core and stays unit-testable.
    /// </summary>
    public sealed class ReportModel
    {
        // ---- identity / branding ----
        public string ToolName { get; set; }            // "Deployment Risk Analyzer"
        public string ToolVersion { get; set; }
        public string ReportTitle { get; set; }         // "Deployment Risk Report"
        public string Subtitle { get; set; }            // brand line under the title
        /// <summary>What the score measures, lower-cased, e.g. "risk" or "technical debt".</summary>
        public string ScoreWord { get; set; } = "risk";

        // ---- subject ----
        public string SubjectName { get; set; }         // solution friendly name / environment name
        public string SubjectKey { get; set; }          // solution unique name (optional)
        public string SubjectVersion { get; set; }
        public bool? IsManaged { get; set; }
        public string SourceEnvironment { get; set; }
        public string TargetEnvironment { get; set; }   // null when single-connection
        public DateTime AnalyzedOnUtc { get; set; } = DateTime.UtcNow;

        // ---- scoring ----
        public int Score { get; set; }
        public ScoreBand Band { get; set; }

        // ---- content ----
        public List<Finding> Findings { get; } = new List<Finding>();
        public List<string> AnalyzersRun { get; } = new List<string>();
        public List<string> AnalyzersSkipped { get; } = new List<string>();
        /// <summary>Optional headline metrics (used heavily by the complexity tool's dashboard).</summary>
        public List<MetricRow> Metrics { get; } = new List<MetricRow>();
        public List<NextStep> NextSteps { get; } = new List<NextStep>();
        /// <summary>Extra bullet lines appended to the Markdown fix checklist (e.g. rollback guidance).</summary>
        public List<string> ChecklistGuidance { get; } = new List<string>();
        /// <summary>Hero paragraph shown beside the gauge.</summary>
        public string LeadIn { get; set; }
        /// <summary>Footer note (defaults to a generic print-to-PDF hint when null).</summary>
        public string FooterNote { get; set; }
        /// <summary>Executive summary (AI-generated or offline template); null until generated.</summary>
        public string AiSummary { get; set; }

        // ---- offline-summary verdict phrasing (optional; generic defaults used when null) ----
        /// <summary>Recommendation sentence the offline summary uses when the band is Low.</summary>
        public string VerdictLow { get; set; }
        /// <summary>Recommendation sentence the offline summary uses when the band is Medium.</summary>
        public string VerdictMedium { get; set; }
        /// <summary>Recommendation sentence the offline summary uses when the band is High.</summary>
        public string VerdictHigh { get; set; }

        /// <summary>The verdict sentence for the current band, or a generic default when unset.</summary>
        public string Verdict()
        {
            switch (Band)
            {
                case ScoreBand.High:
                    return VerdictHigh ?? $"Address the critical and high {ScoreWord} findings before proceeding.";
                case ScoreBand.Medium:
                    return VerdictMedium ?? "Proceed with caution — review and mitigate the findings below.";
                default:
                    return VerdictLow ?? $"No significant {ScoreWord} detected.";
            }
        }

        // ---- derived helpers ----
        public int CountBySeverity(Severity s) => Findings.Count(f => f.Severity == s);

        public Dictionary<string, int> SeveritySummary() =>
            Enum.GetValues(typeof(Severity)).Cast<Severity>()
                .ToDictionary(s => s.ToString(), CountBySeverity);

        public string BandText() =>
            Band == ScoreBand.High ? $"HIGH {ScoreWord.ToUpperInvariant()}"
            : Band == ScoreBand.Medium ? $"MEDIUM {ScoreWord.ToUpperInvariant()}"
            : $"LOW {ScoreWord.ToUpperInvariant()}";

        /// <summary>
        /// A transparent, history-free "category score" (0–100) = weight of the worst finding in the
        /// group. Shared by the HTML and PDF reports so both surfaces derive the same category scores.
        /// </summary>
        public static int CategoryScore(IEnumerable<Finding> findings)
        {
            var worst = (Severity)findings.Max(f => (int)f.Severity);
            switch (worst)
            {
                case Severity.Critical: return 92;
                case Severity.High: return 76;
                case Severity.Medium: return 54;
                case Severity.Low: return 30;
                default: return 12;
            }
        }
    }
}
