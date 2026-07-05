using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.AuditComplianceChecker.Analysis
{
    /// <summary>
    /// Pure, deterministic, SDK-free compliance rules over an <see cref="AuditCoverage"/> plus an
    /// optional <see cref="AuditActivitySummary"/>. Produces findings (each with concrete evidence and,
    /// where actionable, a remediation) and a 0–100 <b>compliance readiness score</b>.
    /// <para>
    /// <b>Score convention:</b> HIGHER = MORE compliant. The score is a weighted blend of four
    /// category sub-scores (each 0–100, higher = better):
    /// Org config (25%), Sensitive-table coverage (30%), Sensitive-column coverage (25%),
    /// Activity health (20%). Because the org switch gates everything, a disabled org audit caps the
    /// overall score into the Low band (<see cref="OrgDisabledCap"/>). Banding is delegated to the
    /// shared <see cref="ScoreCalculator.BandFor"/> (Low &lt; <see cref="MediumThreshold"/> ≤ Medium
    /// &lt; <see cref="HighThreshold"/> ≤ High), so <b>High = good / most compliant</b>.
    /// </para>
    /// Never touches Dataverse — the collector does the reads and hands populated models in.
    /// </summary>
    public static class AuditComplianceRules
    {
        public const string Category = "Audit";

        // ---- scoring constants (documented, deterministic) --------------------------------------
        public const double OrgWeight = 0.25;
        public const double TableWeight = 0.30;
        public const double ColumnWeight = 0.25;
        public const double ActivityWeight = 0.20;

        /// <summary>Compliance score at/above this bands High (good); below <see cref="MediumThreshold"/> bands Low.</summary>
        public const int HighThreshold = 85;
        public const int MediumThreshold = 60;

        /// <summary>Overall score is capped to this when org auditing is disabled (forces the Low band).</summary>
        public const int OrgDisabledCap = 40;

        /// <summary>Rough per-record size used ONLY for the labelled storage estimate.</summary>
        public const double EstimatedKbPerAuditRecord = 2.0;

        // activity penalties (points off the Activity sub-score)
        private const int DeleteVolumePenalty = 25;
        private const int SecurityChangePenalty = 25;
        private const int AfterHoursPenalty = 15;

        /// <summary>
        /// Evaluates coverage + activity and returns the compliance report (findings ordered by
        /// severity, score, band, and category-breakdown metrics).
        /// </summary>
        public static AuditComplianceReport Evaluate(
            AuditCoverage cov, AuditActivitySummary activity, AuditComplianceOptions opts = null)
        {
            opts = opts ?? new AuditComplianceOptions();
            cov = cov ?? new AuditCoverage();
            var tables = cov.Tables ?? new List<TableAudit>();
            var findings = new List<Finding>();

            // ---- Rule: org auditing disabled -> Critical ----------------------------------------
            if (!cov.OrgAuditEnabled)
            {
                findings.Add(new Finding(
                    Category, Severity.Critical,
                    "Organization auditing is disabled",
                    "Organization-level auditing (organization.isauditenabled) is OFF. While it is off, no " +
                    "table- or column-level audit setting has any effect and no change history is recorded " +
                    "anywhere in the environment.",
                    "Organization",
                    "Enable auditing under Settings > Administration > System Settings > Auditing before relying " +
                    "on any table/column audit configuration."));
            }

            // ---- Rule: sensitive table without audit -> High (per table) ------------------------
            var sensitiveTables = tables.Where(t => t != null && t.IsSensitive).ToList();
            foreach (var t in sensitiveTables.Where(t => !t.IsAuditEnabled)
                         .OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                findings.Add(new Finding(
                    Category, Severity.High,
                    "Sensitive table is not audited",
                    $"Table '{Display(t)}' matches sensitive-data heuristics but has auditing disabled. " +
                    "Changes to potentially sensitive/regulated records are not being tracked.",
                    t.LogicalName,
                    $"Enable auditing on the '{Display(t)}' table so changes to sensitive records are recorded."));
            }

            // ---- Rule: sensitive column without audit on an AUDITED table -> Medium -------------
            foreach (var t in sensitiveTables.Where(t => t.IsAuditEnabled)
                         .OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var c in (t.Columns ?? new List<ColumnAudit>())
                             .Where(c => c != null && c.IsSensitive && !c.IsAuditEnabled)
                             .OrderBy(c => c.LogicalName, StringComparer.OrdinalIgnoreCase))
                {
                    findings.Add(new Finding(
                        Category, Severity.Medium,
                        "Sensitive column is not audited",
                        $"Column '{c.LogicalName}' ({c.Type}) on the audited table '{Display(t)}' matches " +
                        "sensitive-data heuristics but has column-level auditing disabled, so field-level " +
                        "changes to it are not captured.",
                        $"{t.LogicalName}.{c.LogicalName}",
                        $"Enable auditing on the '{c.LogicalName}' column of '{Display(t)}'."));
                }
            }

            // ---- Rule: non-sensitive tables with audit off -> single informational note ---------
            var nonSensitiveOff = tables.Count(t => t != null && !t.IsSensitive && !t.IsAuditEnabled);
            if (nonSensitiveOff > 0 && cov.OrgAuditEnabled)
            {
                findings.Add(new Finding(
                    Category, Severity.Info,
                    "Tables without auditing (non-sensitive)",
                    $"{nonSensitiveOff} non-sensitive table(s) have auditing disabled. This is often expected — " +
                    "auditing every table increases storage and noise — but review any that hold business-critical data.",
                    "Coverage"));
            }

            // ---- Activity rules -----------------------------------------------------------------
            if (activity != null)
            {
                if (activity.DeleteCount >= opts.HighDeleteVolumeThreshold)
                {
                    findings.Add(new Finding(
                        Category, Severity.Medium,
                        "High delete volume",
                        $"{activity.DeleteCount} delete operation(s) recorded in the analyzed window " +
                        $"(threshold {opts.HighDeleteVolumeThreshold}). Bulk deletes can indicate data loss, " +
                        "cleanup jobs, or misuse.",
                        "Activity",
                        "Confirm the deletes were expected (e.g. a bulk-delete job) and that retention rules allow them."));
                }

                if (activity.SecurityChangeCount > 0)
                {
                    findings.Add(new Finding(
                        Category, Severity.Medium,
                        "Security-role / privilege changes detected",
                        $"{activity.SecurityChangeCount} role/privilege/team-membership change(s) recorded in the " +
                        "analyzed window. Privilege changes alter who can access data and warrant review.",
                        "Activity",
                        "Review the security changes and confirm each was authorised."));
                }

                if (activity.AfterHoursCount > 0)
                {
                    findings.Add(new Finding(
                        Category, Severity.Low,
                        "After-hours changes detected",
                        $"{activity.AfterHoursCount} change(s) were recorded outside business hours / on weekends. " +
                        "After-hours activity is not inherently bad but is worth a spot-check.",
                        "Activity",
                        "Spot-check the after-hours changes against expected batch jobs and integrations."));
                }
            }

            // ---- Category sub-scores + overall (HIGHER = MORE compliant) ------------------------
            int orgScore = cov.OrgAuditEnabled ? 100 : 0;

            int tableScore = sensitiveTables.Count > 0
                ? Pct(sensitiveTables.Count(t => t.IsAuditEnabled), sensitiveTables.Count)
                : 100; // nothing sensitive to protect at the table level

            var auditedSensitive = sensitiveTables.Where(t => t.IsAuditEnabled).ToList();
            int totalSensCols = auditedSensitive.Sum(t => (t.Columns ?? new List<ColumnAudit>()).Count(c => c != null && c.IsSensitive));
            int auditedSensCols = auditedSensitive.Sum(t =>
                (t.Columns ?? new List<ColumnAudit>()).Count(c => c != null && c.IsSensitive && c.IsAuditEnabled));
            int columnScore = totalSensCols > 0 ? Pct(auditedSensCols, totalSensCols) : 100;

            int activityScore = 100;
            if (activity != null)
            {
                if (activity.DeleteCount >= opts.HighDeleteVolumeThreshold) activityScore -= DeleteVolumePenalty;
                if (activity.SecurityChangeCount > 0) activityScore -= SecurityChangePenalty;
                if (activity.AfterHoursCount > 0) activityScore -= AfterHoursPenalty;
                if (activityScore < 0) activityScore = 0;
            }

            int overall = (int)Math.Round(
                orgScore * OrgWeight + tableScore * TableWeight +
                columnScore * ColumnWeight + activityScore * ActivityWeight,
                MidpointRounding.AwayFromZero);

            if (!cov.OrgAuditEnabled) overall = Math.Min(overall, OrgDisabledCap);
            overall = Math.Max(0, Math.Min(100, overall));

            var band = ScoreCalculator.BandFor(overall, MediumThreshold, HighThreshold);

            if (findings.Count == 0)
            {
                findings.Add(new Finding(
                    Category, Severity.Info,
                    "No audit-compliance gaps detected",
                    "Organization auditing is on, all sensitive tables and columns in scope are audited, and no " +
                    "risky activity patterns were found in the analyzed window.",
                    "Compliance"));
            }

            // ---- Metrics (headline + category breakdown) ----------------------------------------
            var metrics = new List<MetricRow>
            {
                new MetricRow("Compliance score", $"{overall}/100", $"{band} — higher is more compliant"),
                new MetricRow("Organization auditing", cov.OrgAuditEnabled ? "On" : "Off"),
                new MetricRow("Tables audited",
                    $"{tables.Count(t => t != null && t.IsAuditEnabled)}/{tables.Count}"),
                new MetricRow("Sensitive tables covered",
                    $"{sensitiveTables.Count(t => t.IsAuditEnabled)}/{sensitiveTables.Count}"),
                new MetricRow("Sensitive columns covered (audited tables)",
                    $"{auditedSensCols}/{totalSensCols}"),
                new MetricRow("Org config score", orgScore.ToString(), "category (25%)"),
                new MetricRow("Table coverage score", tableScore.ToString(), "category (30%)"),
                new MetricRow("Column coverage score", columnScore.ToString(), "category (25%)"),
                new MetricRow("Activity health score", activityScore.ToString(), "category (20%)"),
            };

            if (activity != null)
            {
                metrics.Add(new MetricRow("Audit records analyzed", activity.TotalRecords.ToString()));
                metrics.Add(new MetricRow("Estimated audit storage",
                    $"{activity.EstimatedStorageMb:0.##} MB", "estimate from record volume — not billed storage"));
            }

            return new AuditComplianceReport
            {
                Score = overall,
                Band = band,
                Findings = findings.OrderByDescending(f => f.Severity)
                    .ThenBy(f => f.Component, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Metrics = metrics
            };
        }

        private static int Pct(int part, int whole) =>
            whole <= 0 ? 100 : (int)Math.Round(100.0 * part / whole, MidpointRounding.AwayFromZero);

        private static string Display(TableAudit t) =>
            string.IsNullOrEmpty(t.DisplayName) ? t.LogicalName : t.DisplayName;
    }
}
