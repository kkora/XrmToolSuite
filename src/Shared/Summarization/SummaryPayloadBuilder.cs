using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>
    /// Anonymized, compact projection of a <see cref="ReportModel"/> — the ONLY data sent to the AI.
    /// Contains finding metadata and headline metrics only: no record/business data, no credentials, no
    /// connection strings, and no environment names (source/target reduce to a <see cref="HasTarget"/>
    /// flag). Component/schema names are optional (redacted when <c>includeComponents</c> is false).
    /// </summary>
    public sealed class SummaryPayload
    {
        public int Score { get; set; }
        public string Band { get; set; }
        public string ScoreWord { get; set; }
        public SubjectInfo Subject { get; set; }
        public bool HasTarget { get; set; }
        public Dictionary<string, int> SeverityCounts { get; set; }
        public int FindingsTotal { get; set; }
        public List<FindingInfo> TopFindings { get; set; }
        public List<MetricInfo> Metrics { get; set; }
        public bool Truncated { get; set; }

        public sealed class SubjectInfo
        {
            public string Version { get; set; }
            public bool? Managed { get; set; }
        }

        public sealed class FindingInfo
        {
            public string Category { get; set; }
            public string Severity { get; set; }
            public string Title { get; set; }
            /// <summary>Null when component redaction is on.</summary>
            public string Component { get; set; }
            public string Recommendation { get; set; }
        }

        public sealed class MetricInfo
        {
            public string Label { get; set; }
            public string Value { get; set; }
        }
    }

    /// <summary>Builds the anonymized <see cref="SummaryPayload"/> from a report model. Pure/UI-free.</summary>
    public static class SummaryPayloadBuilder
    {
        public static SummaryPayload Build(ReportModel r, bool includeComponents, int topN = 40)
        {
            return new SummaryPayload
            {
                Score = r.Score,
                Band = r.Band.ToString(),
                ScoreWord = r.ScoreWord,
                Subject = new SummaryPayload.SubjectInfo { Version = r.SubjectVersion, Managed = r.IsManaged },
                HasTarget = r.TargetEnvironment != null,
                SeverityCounts = r.SeveritySummary(),
                FindingsTotal = r.Findings.Count,
                Truncated = r.Findings.Count > topN,
                Metrics = r.Metrics.Select(m => new SummaryPayload.MetricInfo { Label = m.Label, Value = m.Value }).ToList(),
                TopFindings = r.Findings
                    .OrderByDescending(f => f.Severity)
                    .Take(topN)
                    .Select(f => new SummaryPayload.FindingInfo
                    {
                        Category = f.Category,
                        Severity = f.Severity.ToString(),
                        Title = f.Title,
                        Component = includeComponents ? f.Component : null,
                        Recommendation = f.Recommendation
                    })
                    .ToList()
            };
        }
    }
}
