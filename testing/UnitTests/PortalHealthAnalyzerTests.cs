using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.PortalHealthAnalyzer.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Portal Health Analyzer's deterministic rule engine (PortalModels +
    /// PortalHealthRules). No Dataverse, no WinForms — the SDK collector (PortalCollector) is manual-tested
    /// against a live org. Fixtures are hand-built <see cref="PortalInventory"/> objects. Traces to
    /// US-PP01.3.x / 1.4.x / 1.5.x / 1.6.x.
    /// </summary>
    public class PortalHealthAnalyzerTests
    {
        private static readonly Guid PageId = Guid.NewGuid();
        private static readonly Guid TemplateId = Guid.NewGuid();

        /// <summary>A fully-healthy website: one active page with a valid page template, all required
        /// settings present, no duplicates, no anonymous/global permissions, and a form on an existing table.</summary>
        private static PortalInventory Clean(PortalSchema schema = PortalSchema.Adx)
        {
            var inv = new PortalInventory { Schema = schema, WebsiteName = "Contoso" };
            inv.Pages.Add(new PortalRecord { Id = PageId, Name = "Home", Active = true, TemplateId = TemplateId });
            inv.PageTemplates.Add(new PortalRecord { Id = TemplateId, Name = "Full Page", Active = true });
            foreach (var name in PortalHealthOptions.DefaultRequiredSettings)
                inv.Settings.Add(new PortalSetting { Name = name, Value = "true" });
            inv.Forms.Add(new PortalForm { Name = "Case", EntityLogicalName = "incident", EntityExists = true, Kind = "Form" });
            return inv;
        }

        // ---------------------------------------------------------------- Clean baseline (US-PP01.6.1)

        [Fact]
        public void Clean_ScoresLow_NoActionableFindings()
        {
            var report = PortalHealthRules.Evaluate(Clean());

            Assert.Equal(ScoreBand.Low, report.Band);
            Assert.Equal(0, report.Score);
            Assert.DoesNotContain(report.Findings, f => f.Severity >= Severity.Medium);
        }

        // ---------------------------------------------------------------- Structural (US-PP01.3.x)

        [Fact]
        public void MissingPageTemplate_IsHigh()
        {
            var inv = Clean();
            inv.Pages[0].TemplateId = null; // no template assigned

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Category == PortalHealthRules.CatStructure &&
                f.Severity == Severity.High &&
                f.Title.Contains("page template"));
            Assert.All(report.Findings.Where(f => f.Title.Contains("page template")),
                f => Assert.False(string.IsNullOrWhiteSpace(f.Recommendation)));
        }

        [Fact]
        public void DanglingPageTemplateReference_IsHigh()
        {
            var inv = Clean();
            inv.Pages[0].TemplateId = Guid.NewGuid(); // points at a template that doesn't exist

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Severity == Severity.High && f.Title.Contains("missing page template"));
        }

        [Fact]
        public void MissingParentPage_IsHigh()
        {
            var inv = Clean();
            inv.Pages[0].ParentId = Guid.NewGuid(); // parent not present in the website

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Severity == Severity.High && f.Title.Contains("missing parent"));
        }

        [Fact]
        public void InactivePage_IsMedium()
        {
            var inv = Clean();
            inv.Pages[0].Active = false;

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Category == PortalHealthRules.CatStructure &&
                f.Severity == Severity.Medium &&
                f.Title.Contains("Inactive"));
        }

        [Fact]
        public void WebFileReferencedButAbsent_IsHigh()
        {
            var inv = Clean();
            inv.ReferencedWebFileIds.Add(Guid.NewGuid()); // referenced, but no matching web file exists

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Severity == Severity.High && f.Title.Contains("Web file referenced but absent"));
        }

        [Fact]
        public void FormBoundToMissingTable_IsHigh()
        {
            var inv = Clean();
            inv.Forms.Add(new PortalForm { Name = "Ghost", EntityLogicalName = "nx_missing", EntityExists = false, Kind = "Form" });

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Severity == Severity.High && f.Title.Contains("missing/disabled table"));
        }

        // ---------------------------------------------------------------- Site settings (US-PP01.4.x)

        [Fact]
        public void MissingRequiredSetting_IsHigh()
        {
            var inv = Clean();
            inv.Settings.RemoveAt(0); // drop one required setting

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Category == PortalHealthRules.CatSettings &&
                f.Severity == Severity.High &&
                f.Title.Contains("Missing required site setting"));
        }

        // Regression: if the site-settings table itself could not be read, missing-required-setting checks
        // must be suppressed (else every required setting false-flags High and inflates the score); a single
        // informational note is emitted instead.
        [Fact]
        public void SettingsTableUnavailable_SuppressesMissingSettingFalsePositives()
        {
            var inv = Clean();
            inv.Settings.Clear();                       // couldn't read any settings...
            inv.UnavailableTables.Add("adx_sitesetting"); // ...because the table was unavailable

            var report = PortalHealthRules.Evaluate(inv);

            Assert.DoesNotContain(report.Findings, f => f.Title.Contains("Missing required site setting"));
            Assert.Contains(report.Findings, f =>
                f.Severity == Severity.Info && f.Title == "Site settings could not be verified");
        }

        [Fact]
        public void DuplicateSetting_IsMedium()
        {
            var inv = Clean();
            var dupeName = PortalHealthOptions.DefaultRequiredSettings[0];
            inv.Settings.Add(new PortalSetting { Name = dupeName, Value = "false" }); // conflicting second value

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Category == PortalHealthRules.CatSettings &&
                f.Severity == Severity.Medium &&
                f.Title.Contains("Duplicate"));
        }

        // ---------------------------------------------------------------- Security surface (US-PP01.5.x)

        [Fact]
        public void AnonymousPermission_RaisesCriticalAndHigh()
        {
            var inv = Clean();
            inv.Permissions.Add(new PortalPermission
            {
                Name = "Public read", EntityLogicalName = "contact",
                Scope = "Global", AnonymousReadWriteOrDelete = true
            });

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f => f.Category == PortalHealthRules.CatSecurity && f.Severity == Severity.Critical);
            Assert.Contains(report.Findings, f => f.Category == PortalHealthRules.CatSecurity && f.Severity == Severity.High);
            // A Critical finding forces the High band regardless of the numeric score.
            Assert.Equal(ScoreBand.High, report.Band);
        }

        [Fact]
        public void GlobalScopePermission_IsMedium()
        {
            var inv = Clean();
            inv.Permissions.Add(new PortalPermission
            {
                Name = "All accounts", EntityLogicalName = "account", Scope = "Global"
            });

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f =>
                f.Category == PortalHealthRules.CatSecurity &&
                f.Severity == Severity.Medium &&
                f.Title.Contains("Global-scope"));
        }

        // ---------------------------------------------------------------- Determinism & schema normalization

        [Fact]
        public void Evaluate_IsDeterministic()
        {
            var inv = Clean();
            inv.Pages[0].TemplateId = null;
            inv.Permissions.Add(new PortalPermission { Name = "G", EntityLogicalName = "account", Scope = "Global" });

            var a = PortalHealthRules.Evaluate(inv);
            var b = PortalHealthRules.Evaluate(inv);

            Assert.Equal(a.Score, b.Score);
            Assert.Equal(a.Band, b.Band);
            Assert.Equal(a.Findings.Count, b.Findings.Count);
        }

        [Fact]
        public void BothSchemas_NormalizeIdentically()
        {
            var adx = PortalHealthRules.Evaluate(Clean(PortalSchema.Adx));
            var mspp = PortalHealthRules.Evaluate(Clean(PortalSchema.Mspp));

            Assert.Equal(adx.Score, mspp.Score);
            Assert.Equal(adx.Band, mspp.Band);
            Assert.Equal(adx.Findings.Count, mspp.Findings.Count);
            Assert.Equal(ScoreBand.Low, adx.Band);
        }

        [Fact]
        public void UnavailableTable_DegradesToInfo_NeverThrows()
        {
            var inv = Clean();
            inv.UnavailableTables.Add("adx_redirect");

            var report = PortalHealthRules.Evaluate(inv);

            Assert.Contains(report.Findings, f => f.Severity == Severity.Info && f.Title.Contains("not available"));
            // Info findings carry zero weight, so the clean baseline stays Low.
            Assert.Equal(ScoreBand.Low, report.Band);
        }
    }
}
