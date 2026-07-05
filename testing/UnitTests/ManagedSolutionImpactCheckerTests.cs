using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.ManagedSolutionImpactChecker.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Managed Solution Impact Checker's pure layering engine
    /// (<see cref="LayerImpactRules"/> over <see cref="LayerAnalysisInput"/>). No Dataverse, no WinForms.
    /// The SDK collector (<c>ImpactCollector</c>) is manual-tested against a live org. Traces to
    /// docs/user-stories/ManagedSolutionImpactChecker.md (US-ALM04.x).
    /// </summary>
    public class ManagedSolutionImpactCheckerTests
    {
        private const string Category = "Layering";

        private static LayerAnalysisInput Input(
            IEnumerable<ComponentLayer> layers = null,
            IEnumerable<string> removed = null,
            IEnumerable<(string, string)> missing = null,
            string srcPrefix = null,
            string tgtPrefix = null,
            bool assessed = true) => new LayerAnalysisInput
            {
                Layers = (layers ?? Enumerable.Empty<ComponentLayer>()).ToList(),
                RemovedComponents = (removed ?? Enumerable.Empty<string>()).ToList(),
                RemovedComponentsAssessed = assessed, // tests supply a fully-assessed input by default
                MissingDependencies = (missing ?? Enumerable.Empty<(string, string)>()).ToList(),
                SourcePublisherPrefix = srcPrefix,
                TargetPublisherPrefix = tgtPrefix
            };

        // ---------------------------------------------------------------- Clean input (US-ALM04.5.1)

        [Fact]
        public void CleanInput_ProducesSingleInfoFinding_LowBand()
        {
            var report = LayerImpactRules.Evaluate(Input(), DeploymentPath.Upgrade);

            Assert.Single(report.Findings);
            Assert.Equal(Severity.Info, report.Findings[0].Severity);
            Assert.Equal(0, report.Score);
            Assert.Equal(ScoreBand.Low, report.Band);
            // Checklist + rollback are always generated, even for a clean run (US-ALM04.5.2).
            Assert.NotEmpty(report.Checklist);
            Assert.NotEmpty(report.RollbackGuidance);
            Assert.NotEmpty(report.Metrics);
        }

        // Regression: an Upgrade whose removed components were NEVER assessed must NOT look deletion-safe —
        // it surfaces an honest "not assessed" Info note instead of silently omitting the risk (US-ALM04.2.2).
        [Fact]
        public void Upgrade_WithUnassessedRemovals_NotesDeletionNotEvaluated()
        {
            var report = LayerImpactRules.Evaluate(Input(assessed: false), DeploymentPath.Upgrade);
            Assert.Contains(report.Findings, f =>
                f.Severity == Severity.Info && f.Title == "Deletion / data-loss impact not assessed");
        }

        // A Patch never deletes, so an unassessed removal list produces no "not assessed" alarm.
        [Fact]
        public void Patch_WithUnassessedRemovals_NoNotAssessedNote()
        {
            var report = LayerImpactRules.Evaluate(Input(assessed: false), DeploymentPath.Patch);
            Assert.DoesNotContain(report.Findings, f => f.Title == "Deletion / data-loss impact not assessed");
        }

        // ---------------------------------------------------------------- Deletion: Upgrade vs Update/Patch (US-ALM04.2.2 / US-ALM04.3.1)

        [Fact]
        public void RemovedTable_OnUpgrade_IsCriticalDataLoss()
        {
            var report = LayerImpactRules.Evaluate(
                Input(removed: new[] { "Entity: new_widget" }), DeploymentPath.Upgrade);

            var f = Assert.Single(report.Findings, x => x.Title == "Table would be deleted (data loss)");
            Assert.Equal(Severity.Critical, f.Severity);
            Assert.Equal(Category, f.Category);
            // Any Critical forces a High band (US-ALM04.5.1).
            Assert.Equal(ScoreBand.High, report.Band);
        }

        [Fact]
        public void RemovedColumn_OnUpgrade_IsHigh()
        {
            var report = LayerImpactRules.Evaluate(
                Input(removed: new[] { "Attribute: new_field" }), DeploymentPath.Upgrade);

            var f = Assert.Single(report.Findings, x => x.Title == "Column would be deleted (data loss)");
            Assert.Equal(Severity.High, f.Severity);
        }

        [Fact]
        public void RemovedOtherComponent_OnUpgrade_IsMedium()
        {
            var report = LayerImpactRules.Evaluate(
                Input(removed: new[] { "Web Resource: new_script.js" }), DeploymentPath.Upgrade);

            var f = Assert.Single(report.Findings, x => x.Title == "Component would be deleted");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // Regression: a multi-word type must not be substring-escalated. "Entity Relationship"/"Entity Key"
        // are NOT tables (no data loss) and "Field Security Profile" is NOT a column — they classify Medium.
        [Theory]
        [InlineData("Entity Relationship: new_rel")]
        [InlineData("Entity Key: new_key")]
        [InlineData("Field Security Profile: new_fsp")]
        public void RemovedMultiWordType_IsNotEscalatedToTableOrColumn(string entry)
        {
            var report = LayerImpactRules.Evaluate(Input(removed: new[] { entry }), DeploymentPath.Upgrade);
            Assert.DoesNotContain(report.Findings, f =>
                f.Title == "Table would be deleted (data loss)" || f.Title == "Column would be deleted (data loss)");
            Assert.Contains(report.Findings, f => f.Title == "Component would be deleted" && f.Severity == Severity.Medium);
        }

        [Theory]
        [InlineData(DeploymentPath.Update)]
        [InlineData(DeploymentPath.Patch)]
        public void RemovedComponents_OnUpdateOrPatch_DoNotDelete(DeploymentPath path)
        {
            var report = LayerImpactRules.Evaluate(
                Input(removed: new[] { "Entity: new_widget", "Attribute: new_field" }), path);

            // No deletion findings on non-deleting paths — just one informational note, no data-loss risk.
            Assert.DoesNotContain(report.Findings, f => f.Severity >= Severity.High);
            Assert.Contains(report.Findings, f =>
                f.Severity == Severity.Info && f.Title == "Removed components are not deleted on this path");
            Assert.Equal(ScoreBand.Low, report.Band);
        }

        [Fact]
        public void RemovedTable_OnHolding_IsCritical()
        {
            // A Holding (staged) upgrade eventually deletes, so it surfaces the data-loss risk.
            var report = LayerImpactRules.Evaluate(
                Input(removed: new[] { "Entity: new_widget" }), DeploymentPath.Holding);

            Assert.Contains(report.Findings, f =>
                f.Title == "Table would be deleted (data loss)" && f.Severity == Severity.Critical);
        }

        // ---------------------------------------------------------------- Unmanaged layer above managed (US-ALM04.1.2 / US-ALM04.2.1)

        [Fact]
        public void UnmanagedLayerAbove_OnUpgrade_RaisesMediumDetectAndHighOverwrite()
        {
            var layer = new ComponentLayer
            {
                ComponentType = "Entity", Name = "account", ObjectId = Guid.NewGuid(),
                OwningSolution = "MySolution", IsManaged = true, HasUnmanagedLayerAbove = true
            };
            var report = LayerImpactRules.Evaluate(Input(layers: new[] { layer }), DeploymentPath.Upgrade);

            Assert.Contains(report.Findings, f =>
                f.Title == "Unmanaged customization above managed layer" && f.Severity == Severity.Medium);
            Assert.Contains(report.Findings, f =>
                f.Title == "Component would be overwritten" && f.Severity == Severity.High);
        }

        [Fact]
        public void UnmanagedLayerAbove_OnUpdate_RaisesMediumOnly_NoOverwrite()
        {
            var layer = new ComponentLayer
            {
                ComponentType = "Entity", Name = "account", ObjectId = Guid.NewGuid(),
                OwningSolution = "MySolution", IsManaged = true, HasUnmanagedLayerAbove = true
            };
            var report = LayerImpactRules.Evaluate(Input(layers: new[] { layer }), DeploymentPath.Update);

            Assert.Contains(report.Findings, f =>
                f.Title == "Unmanaged customization above managed layer" && f.Severity == Severity.Medium);
            // Update never replaces the managed base, so there is no overwrite finding.
            Assert.DoesNotContain(report.Findings, f => f.Title == "Component would be overwritten");
        }

        // ---------------------------------------------------------------- Missing dependency (US-ALM04.4.1)

        [Fact]
        public void MissingDependency_IsHigh()
        {
            var report = LayerImpactRules.Evaluate(
                Input(missing: new[] { ("Entity", "new_prereq") }), DeploymentPath.Upgrade);

            var f = Assert.Single(report.Findings, x => x.Title == "Missing dependency");
            Assert.Equal(Severity.High, f.Severity);
        }

        // ---------------------------------------------------------------- Publisher prefix mismatch (US-ALM04.4.1)

        [Fact]
        public void PublisherPrefixMismatch_IsMedium()
        {
            var report = LayerImpactRules.Evaluate(
                Input(srcPrefix: "abc", tgtPrefix: "xyz"), DeploymentPath.Upgrade);

            var f = Assert.Single(report.Findings, x => x.Title == "Publisher prefix mismatch");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        [Fact]
        public void PublisherPrefix_Match_NoFinding()
        {
            var report = LayerImpactRules.Evaluate(
                Input(srcPrefix: "abc", tgtPrefix: "ABC"), DeploymentPath.Upgrade);

            Assert.DoesNotContain(report.Findings, f => f.Title == "Publisher prefix mismatch");
        }

        // ---------------------------------------------------------------- Restrictive managed properties (US-ALM04.4.2)

        [Fact]
        public void RestrictiveManagedProperties_IsMedium_ListsComponents()
        {
            var layers = new[]
            {
                new ComponentLayer { ComponentType = "Entity", Name = "account", RestrictiveManagedProperties = true },
                new ComponentLayer { ComponentType = "Entity", Name = "contact", RestrictiveManagedProperties = true }
            };
            var report = LayerImpactRules.Evaluate(Input(layers: layers), DeploymentPath.Upgrade);

            var f = Assert.Single(report.Findings, x => x.Title == "Restrictive managed properties");
            Assert.Equal(Severity.Medium, f.Severity);
            Assert.Contains("account", f.Description);
            Assert.Contains("contact", f.Description);
        }

        // ---------------------------------------------------------------- Score, band, checklist, rollback (US-ALM04.5.1 / US-ALM04.5.2)

        [Fact]
        public void AggregateScore_BandsHigh_AndGeneratesChecklistAndRollback()
        {
            var layer = new ComponentLayer
            {
                ComponentType = "Entity", Name = "account", ObjectId = Guid.NewGuid(),
                IsManaged = true, HasUnmanagedLayerAbove = true, RestrictiveManagedProperties = true
            };
            var report = LayerImpactRules.Evaluate(
                Input(
                    layers: new[] { layer },
                    removed: new[] { "Entity: new_widget", "Attribute: new_field" },
                    missing: new[] { ("Entity", "new_prereq") },
                    srcPrefix: "abc", tgtPrefix: "xyz"),
                DeploymentPath.Upgrade);

            // A Critical (removed table) alone forces High; the accumulated weight is well over threshold too.
            Assert.Equal(ScoreBand.High, report.Band);
            Assert.True(report.Score >= 40, $"expected High-band score, got {report.Score}");

            // Checklist references the data-loss backup step and dependency resolution.
            Assert.Contains(report.Checklist, s => s.Contains("Back up") && s.Contains("new_widget"));
            Assert.Contains(report.Checklist, s => s.Contains("missing dependencies"));

            // Rollback calls out that a managed upgrade cannot be rolled back by uninstall alone.
            Assert.Contains(report.RollbackGuidance, s => s.Contains("cannot be rolled back"));

            // Findings are ordered worst-first.
            for (int i = 1; i < report.Findings.Count; i++)
                Assert.True(report.Findings[i - 1].Severity >= report.Findings[i].Severity);
        }

        [Fact]
        public void NullInput_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LayerImpactRules.Evaluate(null, DeploymentPath.Upgrade));
        }
    }
}
