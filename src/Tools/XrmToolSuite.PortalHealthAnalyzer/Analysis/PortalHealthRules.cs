using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.PortalHealthAnalyzer.Analysis
{
    /// <summary>Tunable inputs for the health evaluation. All optional — sensible defaults apply.</summary>
    public sealed class PortalHealthOptions
    {
        /// <summary>
        /// Site-setting names that should exist for a healthy website, keyed per schema. Absent settings
        /// are flagged High. Override to match your organisation's baseline. Null = the curated defaults.
        /// </summary>
        public IReadOnlyDictionary<PortalSchema, IReadOnlyList<string>> RequiredSettings { get; set; }

        public static PortalHealthOptions Default => new PortalHealthOptions();

        /// <summary>
        /// Curated baseline of site settings a portal is generally expected to define. The setting
        /// <em>names</em> are identical across the two schemas (only the table differs), so the same
        /// list is returned for each, but the map is per-schema so callers can diverge if needed.
        /// </summary>
        internal static readonly IReadOnlyList<string> DefaultRequiredSettings = new List<string>
        {
            "Authentication/Registration/Enabled",
            "HTTP/X-Frame-Options",
            "Header/OutputCache/Enabled",
        };

        public IReadOnlyList<string> RequiredSettingsFor(PortalSchema schema)
        {
            if (RequiredSettings != null && RequiredSettings.TryGetValue(schema, out var list) && list != null)
                return list;
            return DefaultRequiredSettings;
        }
    }

    /// <summary>The result of a health evaluation: a 0–100 score, a band, findings, and summary metrics.</summary>
    public sealed class PortalHealthReport
    {
        public int Score { get; set; }
        public ScoreBand Band { get; set; }
        public List<Finding> Findings { get; } = new List<Finding>();
        public List<MetricRow> Metrics { get; } = new List<MetricRow>();
    }

    /// <summary>
    /// Deterministic, SDK-free health rules over a normalized <see cref="PortalInventory"/>. Produces a
    /// weighted 0–100 score (higher = less healthy), a Low/Medium/High band, and a categorized set of
    /// findings — each carrying a plain-language recommendation. Pure and side-effect-free so it is
    /// fully unit-testable and liftable into a console/CI wrapper.
    /// </summary>
    public static class PortalHealthRules
    {
        // Finding categories (grouped in the issue grid and exports).
        public const string CatStructure = "Structure";
        public const string CatSettings = "Site Settings";
        public const string CatSecurity = "Security";

        // Weighting mirrors the suite's risk family: any Critical forces the High band.
        private static readonly ScoreCalculator Calculator = ScoreCalculator.RiskDefault;

        public static PortalHealthReport Evaluate(PortalInventory inv, PortalHealthOptions opts = null)
        {
            if (inv == null) throw new ArgumentNullException(nameof(inv));
            opts = opts ?? PortalHealthOptions.Default;

            var report = new PortalHealthReport();

            EvaluateStructure(inv, report.Findings);
            EvaluateSiteSettings(inv, opts, report.Findings);
            EvaluateSecurity(inv, report.Findings);
            AddUnavailableTableNotes(inv, report.Findings);

            report.Score = Calculator.Score(report.Findings);
            report.Band = Calculator.Band(report.Findings, report.Score);
            report.Metrics.AddRange(BuildMetrics(inv, report));
            return report;
        }

        // --------------------------------------------------------------- Structural integrity

        private static void EvaluateStructure(PortalInventory inv, List<Finding> findings)
        {
            var pageIds = new HashSet<Guid>(inv.Pages.Select(p => p.Id));
            var pageTemplateIds = new HashSet<Guid>(inv.PageTemplates.Select(t => t.Id));
            var webFileIds = new HashSet<Guid>(inv.WebFiles.Select(f => f.Id));

            foreach (var page in inv.Pages)
            {
                var label = page.Name ?? page.Id.ToString();

                // Missing / dangling parent (a page whose parent no longer exists can't render in its tree).
                if (page.ParentId.HasValue && !pageIds.Contains(page.ParentId.Value))
                {
                    findings.Add(new Finding(CatStructure, Severity.High,
                        "Web page has a missing parent",
                        $"Web page '{label}' references parent page {page.ParentId} which does not exist in this website. " +
                        "The page cannot be placed in the site hierarchy and will fail to render.",
                        component: label,
                        recommendation: "Re-point the page to a valid parent page, or recreate the missing parent."));
                }

                // Missing or dangling page template (no layout to render with).
                if (!page.TemplateId.HasValue)
                {
                    findings.Add(new Finding(CatStructure, Severity.High,
                        "Web page has no page template",
                        $"Web page '{label}' has no page template assigned, so it has no layout to render with.",
                        component: label,
                        recommendation: "Assign a page template to the web page."));
                }
                else if (!pageTemplateIds.Contains(page.TemplateId.Value))
                {
                    findings.Add(new Finding(CatStructure, Severity.High,
                        "Web page references a missing page template",
                        $"Web page '{label}' references page template {page.TemplateId} which does not exist in this website (dangling reference).",
                        component: label,
                        recommendation: "Re-point the page to an existing page template, or recreate the missing template."));
                }

                if (!page.Active)
                    AddInactive(findings, "web page", label);
            }

            foreach (var t in inv.Templates.Where(x => !x.Active))
                AddInactive(findings, "web template", t.Name ?? t.Id.ToString());
            foreach (var t in inv.PageTemplates.Where(x => !x.Active))
                AddInactive(findings, "page template", t.Name ?? t.Id.ToString());

            // Web files referenced by pages/templates but absent from the website.
            foreach (var missing in inv.ReferencedWebFileIds.Where(id => !webFileIds.Contains(id)).Distinct())
            {
                findings.Add(new Finding(CatStructure, Severity.High,
                    "Web file referenced but absent",
                    $"A page or template references web file {missing}, but no such web file exists in this website. " +
                    "The linked asset (image, script, stylesheet, download) will 404 at runtime.",
                    component: missing.ToString(),
                    recommendation: "Restore the missing web file, or remove the reference to it."));
            }

            // Forms / lists bound to a non-existent or disabled Dataverse table.
            foreach (var f in inv.Forms.Where(x => !x.EntityExists))
            {
                findings.Add(new Finding(CatStructure, Severity.High,
                    $"{f.Kind} bound to a missing/disabled table",
                    $"{f.Kind} '{f.Name}' is bound to Dataverse table '{f.EntityLogicalName}', which does not exist or is disabled in this environment. " +
                    "The component will error when rendered.",
                    component: f.Name,
                    recommendation: $"Point the {f.Kind.ToLowerInvariant()} at a valid, enabled table, or remove it."));
            }
        }

        private static void AddInactive(List<Finding> findings, string kind, string label)
        {
            findings.Add(new Finding(CatStructure, Severity.Medium,
                $"Inactive {kind}",
                $"The {kind} '{label}' is inactive (statecode off). Inactive components are dead assets that " +
                "clutter the site and can confuse authors.",
                component: label,
                recommendation: $"Reactivate the {kind} if it is needed, otherwise delete it."));
        }

        // --------------------------------------------------------------- Site settings

        private static void EvaluateSiteSettings(PortalInventory inv, PortalHealthOptions opts, List<Finding> findings)
        {
            // If the site-settings table itself could not be read, its emptiness is a permission/availability
            // gap, not genuinely-absent configuration. Flagging every required setting as High here would be a
            // false positive that inflates the (higher = less healthy) score — note it once and skip the check.
            var settingsUnavailable = inv.UnavailableTables != null &&
                inv.UnavailableTables.Any(t => t != null && t.IndexOf("sitesetting", StringComparison.OrdinalIgnoreCase) >= 0);
            if (settingsUnavailable)
            {
                findings.Add(new Finding(CatSettings, Severity.Info,
                    "Site settings could not be verified",
                    "The site-settings table could not be read, so required-setting coverage was not verified. " +
                    "Missing-setting findings are suppressed to avoid false positives.",
                    component: null,
                    recommendation: "Ensure the connection can read the portal site-settings table, then re-run."));
                return;
            }

            var present = new HashSet<string>(
                inv.Settings.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var required in opts.RequiredSettingsFor(inv.Schema))
            {
                if (!present.Contains(required))
                {
                    findings.Add(new Finding(CatSettings, Severity.High,
                        "Missing required site setting",
                        $"Required site setting '{required}' is not defined for this website. Its baseline behaviour " +
                        "falls back to a default that may not match the intended configuration.",
                        component: required,
                        recommendation: $"Add a site setting named '{required}' with the appropriate value."));
                }
            }

            // Duplicate / conflicting settings — same name defined more than once within the website.
            var dupes = inv.Settings
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var g in dupes)
            {
                var values = string.Join(" | ", g.Select(x => x.Value ?? "(null)").Distinct());
                findings.Add(new Finding(CatSettings, Severity.Medium,
                    "Duplicate site setting",
                    $"Site setting '{g.Key}' is defined {g.Count()} times (values: {values}). Which value wins is " +
                    "ambiguous and depends on retrieval order.",
                    component: g.Key,
                    recommendation: "Remove the duplicate site-setting records so exactly one value remains."));
            }
        }

        // --------------------------------------------------------------- Security surface (summary only)

        private static void EvaluateSecurity(PortalInventory inv, List<Finding> findings)
        {
            var anonymous = inv.Permissions.Where(p => p.AnonymousReadWriteOrDelete).ToList();

            foreach (var p in anonymous)
            {
                findings.Add(new Finding(CatSecurity, Severity.High,
                    "Permission grants anonymous access",
                    $"Table permission '{p.Name}' on table '{p.EntityLogicalName}' grants anonymous (unauthenticated) " +
                    "read, write, or delete. Anonymous data access is a common portal exposure.",
                    component: p.Name,
                    recommendation: "Restrict the permission to authenticated web roles, or scope it to Contact/Account.",
                    helpUrl: null));
            }

            if (anonymous.Count > 0)
            {
                findings.Add(new Finding(CatSecurity, Severity.Critical,
                    "Anonymous access to portal data",
                    $"{anonymous.Count} table permission(s) grant anonymous access: " +
                    string.Join(", ", anonymous.Select(p => p.Name)) + ". " +
                    "This is a health-level flag — run the Portal Security Scanner for a deep, per-role analysis.",
                    component: inv.WebsiteName,
                    recommendation: "Review each anonymous grant and remove or tighten it; then run the Portal Security Scanner."));
            }

            // Over-broad Global scope where a Contact/Account scope is normally expected.
            foreach (var p in inv.Permissions.Where(IsGlobalScope))
            {
                findings.Add(new Finding(CatSecurity, Severity.Medium,
                    "Over-broad Global-scope permission",
                    $"Table permission '{p.Name}' on table '{p.EntityLogicalName}' uses Global scope, granting access to " +
                    "every record of the table rather than the signed-in Contact's or Account's records.",
                    component: p.Name,
                    recommendation: "Use a Contact, Account, or Self scope unless every record is intentionally public to the role."));
            }
        }

        private static bool IsGlobalScope(PortalPermission p) =>
            string.Equals(p.Scope?.Trim(), "Global", StringComparison.OrdinalIgnoreCase);

        // --------------------------------------------------------------- Unavailable tables (informational)

        private static void AddUnavailableTableNotes(PortalInventory inv, List<Finding> findings)
        {
            foreach (var table in inv.UnavailableTables)
            {
                findings.Add(new Finding(CatStructure, Severity.Info,
                    "Table not available",
                    $"The table '{table}' could not be retrieved (it may not be provisioned in this environment or the " +
                    "portal may use the other schema). It was skipped; results for it are incomplete.",
                    component: table,
                    recommendation: null));
            }
        }

        // --------------------------------------------------------------- Metrics

        private static IEnumerable<MetricRow> BuildMetrics(PortalInventory inv, PortalHealthReport report)
        {
            yield return new MetricRow("Schema", inv.SchemaLabel);
            yield return new MetricRow("Web pages", inv.Pages.Count.ToString());
            yield return new MetricRow("Web templates", inv.Templates.Count.ToString());
            yield return new MetricRow("Page templates", inv.PageTemplates.Count.ToString());
            yield return new MetricRow("Content snippets", inv.Snippets.Count.ToString());
            yield return new MetricRow("Site settings", inv.Settings.Count.ToString());
            yield return new MetricRow("Web roles", inv.WebRoles.Count.ToString());
            yield return new MetricRow("Table permissions", inv.Permissions.Count.ToString());
            yield return new MetricRow("Forms", inv.Forms.Count(f => f.Kind == "Form").ToString());
            yield return new MetricRow("Lists", inv.Lists.Count.ToString());
            yield return new MetricRow("Web files", inv.WebFiles.Count.ToString());
            yield return new MetricRow("Redirects", inv.Redirects.Count.ToString());
            yield return new MetricRow("Findings", report.Findings.Count.ToString(),
                $"{report.Findings.Count(f => f.Severity == Severity.Critical)} critical, " +
                $"{report.Findings.Count(f => f.Severity == Severity.High)} high, " +
                $"{report.Findings.Count(f => f.Severity == Severity.Medium)} medium");
        }
    }
}
