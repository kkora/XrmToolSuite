using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.SharingAnalyzer.Analysis
{
    /// <summary>
    /// Pure, deterministic, SDK-free risk rules over a <see cref="SharingSummary"/>. Every finding states
    /// the concrete record/principal evidence behind it. Category is always "Sharing". Never touches
    /// Dataverse — the collector does the reads and hands a fully-aggregated summary in. Read-only: no
    /// rule performs or implies a mutation; cleanup is preview-only.
    /// </summary>
    public static class SharingRiskRules
    {
        public const string Category = "Sharing";

        /// <summary>Shared scorer for the composite sharing-risk score/band.</summary>
        public static readonly ScoreCalculator Scorer = ScoreCalculator.RiskDefault;

        /// <summary>
        /// Evaluates every sharing-hygiene rule and returns the findings (highest severity first). When no
        /// rule trips, returns a single Info "No sharing risks detected" finding.
        /// </summary>
        public static List<Finding> Evaluate(SharingSummary s, SharingRiskOptions opts = null)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            opts = opts ?? new SharingRiskOptions();

            var findings = new List<Finding>();
            var records = s.RecordStats();
            var principals = s.PrincipalStats();

            // ---- Excessive sharing: a record shared with > MaxPrincipalsPerRecord principals (High) ----
            foreach (var r in records.Where(r => r.DistinctPrincipals > opts.MaxPrincipalsPerRecord))
            {
                findings.Add(new Finding(
                    Category, Severity.High,
                    "Excessive record sharing",
                    $"{r.Table} record {r.ObjectId} is shared with {r.DistinctPrincipals} principals " +
                    $"(threshold {opts.MaxPrincipalsPerRecord}). Broad manual sharing is hard to audit and " +
                    "can mask a missing security-role or team design.",
                    $"{r.Table}:{r.ObjectId}",
                    "Replace broad per-record sharing with an owning team or a security role, then revoke the shares."));
            }

            // ---- Statistical outlier: record's principal count is unusually high vs. the rest (Medium) ----
            // Only records that are NOT already flagged as excessive, at/above the noise floor.
            var eligible = records
                .Where(r => r.DistinctPrincipals <= opts.MaxPrincipalsPerRecord &&
                            r.DistinctPrincipals >= opts.OutlierFloor)
                .ToList();
            if (eligible.Count >= 3)
            {
                var counts = records.Select(r => (double)r.DistinctPrincipals).ToList();
                double mean = counts.Average();
                double variance = counts.Sum(c => (c - mean) * (c - mean)) / counts.Count;
                double stdDev = Math.Sqrt(variance);
                double cutoff = mean + opts.OutlierSigma * stdDev;

                foreach (var r in eligible.Where(r => stdDev > 0 && r.DistinctPrincipals > cutoff))
                {
                    findings.Add(new Finding(
                        Category, Severity.Medium,
                        "Record with unusually high shared-principal count",
                        $"{r.Table} record {r.ObjectId} is shared with {r.DistinctPrincipals} principals — a " +
                        $"statistical outlier (mean {mean:0.0}, cutoff {cutoff:0.0} at {opts.OutlierSigma}σ). " +
                        "It sits well above typical sharing for the scanned records.",
                        $"{r.Table}:{r.ObjectId}",
                        "Review why this record is shared so widely; consolidate onto a team if the access is intentional."));
                }
            }

            // ---- Sharing with an inactive user (Medium) ----
            foreach (var p in principals.Where(p =>
                         string.Equals(p.PrincipalType, "User", StringComparison.OrdinalIgnoreCase) &&
                         !p.PrincipalActive))
            {
                findings.Add(new Finding(
                    Category, Severity.Medium,
                    "Shared with an inactive user",
                    $"Disabled user '{p.PrincipalName}' still holds {p.InboundShares} share(s) across " +
                    $"{p.InboundRecords} record(s). Shares to disabled users are stale access that survives the disable.",
                    p.PrincipalName,
                    "Revoke the shares held by the disabled user."));
            }

            // ---- Sharing with a disabled / empty team (Medium) ----
            foreach (var p in principals.Where(p =>
                         string.Equals(p.PrincipalType, "Team", StringComparison.OrdinalIgnoreCase) &&
                         !p.PrincipalActive))
            {
                findings.Add(new Finding(
                    Category, Severity.Medium,
                    "Shared with a disabled or empty team",
                    $"Team '{p.PrincipalName}' is empty or disabled yet holds {p.InboundShares} share(s) across " +
                    $"{p.InboundRecords} record(s). No active member inherits this access, so the shares are dead weight.",
                    p.PrincipalName,
                    "Revoke the team's shares, or add members / re-enable the team if it is meant to be in use."));
            }

            // ---- User with unusually high inbound shared access (Medium) ----
            foreach (var p in principals.Where(p =>
                         string.Equals(p.PrincipalType, "User", StringComparison.OrdinalIgnoreCase) &&
                         p.InboundRecords > opts.MaxInboundPerPrincipal))
            {
                findings.Add(new Finding(
                    Category, Severity.Medium,
                    "User with high inbound shared access",
                    $"User '{p.PrincipalName}' has {p.InboundRecords} records shared to them " +
                    $"(threshold {opts.MaxInboundPerPrincipal}). Large inbound sharing is access sprawl that a role " +
                    "or team would express more cleanly.",
                    p.PrincipalName,
                    "Review whether a security role or team membership should replace the accumulated per-record shares."));
            }

            if (findings.Count == 0)
            {
                var scope = s.ScannedTables != null && s.ScannedTables.Count > 0
                    ? string.Join(", ", s.ScannedTables)
                    : "the scanned tables";
                findings.Add(new Finding(
                    Category, Severity.Info,
                    "No sharing risks detected",
                    $"Across {s.TotalShares} share(s) on {s.DistinctRecords} record(s) in {scope}, no excessive, " +
                    "stale, or outlier sharing was found."));
            }

            return findings.OrderByDescending(f => f.Severity).ToList();
        }

        /// <summary>Composite sharing-risk score (0-100) for a set of findings.</summary>
        public static int Score(IEnumerable<Finding> findings) => Scorer.Score(findings);

        /// <summary>Low/Medium/High band for a set of findings.</summary>
        public static ScoreBand Band(IEnumerable<Finding> findings) => Scorer.Band(findings, Score(findings));
    }
}
