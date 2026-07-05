using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.DuplicateMetadataFinder.Analysis;

namespace XrmToolSuite.DuplicateMetadataFinder.Reporting
{
    /// <summary>
    /// Projects a <see cref="DuplicateScanResult"/> into the suite-shared <see cref="ReportModel"/> so the
    /// standard Excel / PDF / HTML / JSON exporters render it. SDK-free (no Dataverse, no ClosedXML), so the
    /// whole projection — score, banding, per-group findings, recommended keep — is unit-testable.
    /// </summary>
    public static class DuplicateReport
    {
        public const string ToolName = "Duplicate Metadata Finder";

        /// <summary>A group's severity comes from its strongest pair: exact/very-high = High, near = Medium, weak = Low.</summary>
        public static Severity SeverityFor(DuplicateGroup group)
        {
            if (group == null) return Severity.Info;
            if (group.Pairs.Any(p => p.IsExactContentMatch) || group.TopScore >= 90) return Severity.High;
            if (group.TopScore >= 80) return Severity.Medium;
            return Severity.Low;
        }

        /// <summary>
        /// Overall duplication burden 0..100: five points per redundant component (group members beyond the
        /// one recommended keep), capped at 100. No duplicates → 0.
        /// </summary>
        public static int Score(DuplicateScanResult scan)
        {
            if (scan == null) return 0;
            var redundant = scan.Groups.Sum(g => Math.Max(0, g.Members.Count - 1));
            return Math.Min(100, redundant * 5);
        }

        public static ScoreBand BandFor(int score) =>
            score >= 60 ? ScoreBand.High : score >= 30 ? ScoreBand.Medium : ScoreBand.Low;

        public static ReportModel ToReportModel(DuplicateScanResult scan)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));

            var score = Score(scan);
            var redundant = scan.Groups.Sum(g => Math.Max(0, g.Members.Count - 1));

            var model = new ReportModel
            {
                ToolName = ToolName,
                ReportTitle = "Duplicate Metadata Report",
                ScoreWord = "duplication",
                SubjectName = scan.EnvironmentName,
                SourceEnvironment = scan.EnvironmentName,
                AnalyzedOnUtc = scan.ScannedOnUtc,
                Score = score,
                Band = BandFor(score),
                LeadIn = scan.GroupCount == 0
                    ? $"No duplicate groups found at/above a similarity threshold of {scan.Threshold}%."
                    : $"{scan.GroupCount} duplicate group(s) covering {scan.DuplicateComponentCount} component(s) " +
                      $"({redundant} redundant) at/above a {scan.Threshold}% similarity threshold. " +
                      "Read-only — the tool recommends a primary to keep; it never merges or deletes.",
                VerdictLow = "Little duplication — metadata is largely unique.",
                VerdictMedium = "Some duplicate metadata — consolidation would reduce confusion.",
                VerdictHigh = "Substantial duplicate metadata — plan a consolidation pass.",
                FooterNote = "Similarity is heuristic; review each group before consolidating. Web-resource/JS matches " +
                             "with an identical content hash are exact; name/type-based matches may include false positives.",
            };

            model.Metrics.Add(new MetricRow("Similarity threshold", scan.Threshold + "%"));
            model.Metrics.Add(new MetricRow("Duplicate groups", scan.GroupCount.ToString()));
            model.Metrics.Add(new MetricRow("Duplicate components", scan.DuplicateComponentCount.ToString()));
            model.Metrics.Add(new MetricRow("Redundant components", redundant.ToString(),
                "Members beyond the one recommended keep per group"));

            foreach (var kind in scan.Groups.GroupBy(g => g.Kind).OrderBy(g => g.Key.ToString()))
            {
                model.AnalyzersRun.Add(kind.Key.ToString());
                model.Metrics.Add(new MetricRow(kind.Key + " groups", kind.Count().ToString()));
            }
            foreach (var note in scan.Notes)
                model.AnalyzersSkipped.Add(note);

            foreach (var group in scan.Ranked())
            {
                var primary = group.RecommendedPrimary;
                var members = string.Join(", ", group.Members.Select(m => m.Key));
                var topFactors = group.Pairs
                    .OrderByDescending(p => p.Score)
                    .Take(1)
                    .SelectMany(p => p.Factors)
                    .OrderByDescending(f => f.Value * f.Weight)
                    .Select(f => f.ToString());

                model.Findings.Add(new Finding(
                    category: group.Kind.ToString(),
                    severity: SeverityFor(group),
                    title: $"{group.Members.Count} near-duplicate {group.Kind.ToString().ToLowerInvariant()}(s) " +
                           $"(top {group.TopScore}%)",
                    description: $"Members: {members}." +
                                 (topFactors.Any() ? $" Top-pair factors: {string.Join(", ", topFactors)}." : string.Empty),
                    component: primary?.Container == null ? members : $"{members} [{group.Kind}]",
                    recommendation: group.RecommendationReason()));
            }

            if (scan.GroupCount > 0)
            {
                model.NextSteps.Add(new NextStep(
                    "Review each duplicate group",
                    "Confirm the members are truly redundant before acting; heuristic matches can be false positives."));
                model.NextSteps.Add(new NextStep(
                    "Consolidate onto the recommended primary",
                    "Repoint dependencies to the most-referenced member, then retire the rest through your normal ALM process."));
            }

            return model;
        }
    }
}
