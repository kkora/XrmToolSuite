using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Summarization
{
    /// <summary>
    /// Anonymized, compact projection of an <see cref="AnalysisResult"/> — the ONLY data sent to the AI.
    /// Contains finding metadata only: no record/business data, no credentials, no connection strings,
    /// and no environment names (source/target are reduced to a <see cref="HasTarget"/> flag). Component
    /// and schema names are optional (see <see cref="SummaryPayloadBuilder.Build"/> redaction).
    /// </summary>
    public sealed class SummaryPayload
    {
        public int Score { get; set; }
        public string Risk { get; set; }
        public SolutionInfo Solution { get; set; }
        public bool HasTarget { get; set; }
        public Dictionary<string, int> SeverityCounts { get; set; }
        public int FindingsTotal { get; set; }
        public List<FindingInfo> TopFindings { get; set; }
        public bool Truncated { get; set; }

        public sealed class SolutionInfo
        {
            public string Version { get; set; }
            public bool Managed { get; set; }
        }

        public sealed class FindingInfo
        {
            public string Category { get; set; }
            public string Severity { get; set; }
            public string Title { get; set; }
            /// <summary>Null when component redaction is on (Mode C).</summary>
            public string Component { get; set; }
            public string Recommendation { get; set; }
        }
    }

    /// <summary>Builds the anonymized <see cref="SummaryPayload"/> from an analysis result. Pure/UI-free.</summary>
    public static class SummaryPayloadBuilder
    {
        public static SummaryPayload Build(AnalysisResult r, bool includeComponents, int topN = 40)
        {
            return new SummaryPayload
            {
                Score = r.Score,
                Risk = r.Risk.ToString(),
                Solution = new SummaryPayload.SolutionInfo { Version = r.SolutionVersion, Managed = r.SolutionIsManaged },
                HasTarget = r.TargetEnvironment != null,
                SeverityCounts = r.SeveritySummary(),
                FindingsTotal = r.Findings.Count,
                Truncated = r.Findings.Count > topN,
                TopFindings = r.Findings
                    .OrderByDescending(f => f.Severity)
                    .Take(topN)
                    .Select(f => new SummaryPayload.FindingInfo
                    {
                        Category = f.Category.ToString(),
                        Severity = f.Severity.ToString(),
                        Title = f.Title,
                        Component = includeComponents ? f.AffectedComponent : null,
                        Recommendation = f.Recommendation
                    })
                    .ToList()
            };
        }
    }
}
