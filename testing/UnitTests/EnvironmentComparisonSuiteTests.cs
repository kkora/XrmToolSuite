using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.EnvironmentComparisonSuite.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Environment Comparison Suite's pure diff engine — the classifier
    /// (<see cref="SnapshotComparer.Compare"/>) and the roll-up (<see cref="SnapshotComparer.Roll"/>).
    /// No Dataverse, no WinForms; the SDK collector (<c>ComparisonCollector</c>) is manual-tested against
    /// live orgs. Traces to US-MIG1.2.2 / US-MIG1.3.3 / US-MIG1.4.1 / US-MIG1.4.2.
    /// </summary>
    public class EnvironmentComparisonSuiteTests
    {
        private const string Cat = ComparisonCategories.Tables;

        private static ComponentSnapshot Snap(string key, bool managed = false, string version = null,
            params (string prop, string val)[] props)
        {
            var s = new ComponentSnapshot(Cat, key, key, managed, version);
            foreach (var p in props) s.With(p.prop, p.val);
            return s;
        }

        // ---------------------------------------------------------------- classification (US-MIG1.4.1)

        [Fact] // US-MIG1.4.1 — a component in source but not target is Missing.
        public void Compare_InSourceOnly_IsMissing()
        {
            var diffs = SnapshotComparer.Compare(Cat,
                new[] { Snap("account") },
                new ComponentSnapshot[0]);

            var d = Assert.Single(diffs);
            Assert.Equal(DiffClass.Missing, d.Class);
            Assert.Equal("account", d.Key);
        }

        [Fact] // US-MIG1.4.1 — a component in target but not source is Extra.
        public void Compare_InTargetOnly_IsExtra()
        {
            var diffs = SnapshotComparer.Compare(Cat,
                new ComponentSnapshot[0],
                new[] { Snap("contact") });

            var d = Assert.Single(diffs);
            Assert.Equal(DiffClass.Extra, d.Class);
        }

        [Fact] // US-MIG1.2.2 — matched components with a differing property are Changed, with the delta listed.
        public void Compare_DifferingProperty_IsChangedWithDelta()
        {
            var diffs = SnapshotComparer.Compare(Cat,
                new[] { Snap("account", props: ("type", "String")) },
                new[] { Snap("account", props: ("type", "Memo")) });

            var d = Assert.Single(diffs);
            Assert.Equal(DiffClass.Changed, d.Class);
            var cp = Assert.Single(d.ChangedProperties);
            Assert.Equal("type", cp.Prop);
            Assert.Equal("String", cp.Source);
            Assert.Equal("Memo", cp.Target);
        }

        [Fact] // US-MIG1.4.1 — a managed/unmanaged layering difference is its own class (takes precedence).
        public void Compare_ManagedDiffers_IsManagedVsUnmanaged()
        {
            var diffs = SnapshotComparer.Compare(Cat,
                new[] { Snap("account", managed: true) },
                new[] { Snap("account", managed: false) });

            var d = Assert.Single(diffs);
            Assert.Equal(DiffClass.ManagedVsUnmanaged, d.Class);
            Assert.True(d.SourceManaged);
            Assert.False(d.TargetManaged);
        }

        [Fact] // US-MIG1.4.1 — identical components (same props, same managed state) are Identical.
        public void Compare_SameEverything_IsIdentical()
        {
            var diffs = SnapshotComparer.Compare(Cat,
                new[] { Snap("account", props: ("type", "String")) },
                new[] { Snap("account", props: ("type", "String")) });

            var d = Assert.Single(diffs);
            Assert.Equal(DiffClass.Identical, d.Class);
            Assert.Empty(d.ChangedProperties);
        }

        // Regression: the roll-up buckets a null diff Category (allowed for the reusable engine) instead of
        // throwing ArgumentNullException on a null Dictionary key.
        [Fact]
        public void Roll_WithNullCategory_DoesNotThrow()
        {
            var diffs = SnapshotComparer.Compare(null,
                new[] { new ComponentSnapshot(null, "a", "a", false, null) },
                new ComponentSnapshot[0]);
            var report = SnapshotComparer.Roll(diffs); // must not throw
            Assert.NotNull(report);
            Assert.Single(report.Diffs);
        }

        // Regression: a category that was compared but is fully empty on both sides (no diffs) must still get
        // a zeroed row in the count matrix when its category is passed, so summary cards don't silently vanish.
        [Fact]
        public void Roll_SeedsRowsForEnabledButEmptyCategories()
        {
            var report = SnapshotComparer.Roll(
                new ComponentDiff[0], // nothing differed / nothing present
                opts: null,
                categories: new[] { ComparisonCategories.Solutions, ComparisonCategories.Tables });

            Assert.True(report.CountsByCategoryAndClass.ContainsKey(ComparisonCategories.Solutions));
            Assert.True(report.CountsByCategoryAndClass.ContainsKey(ComparisonCategories.Tables));
            // Seeded rows are all-zero across every DiffClass.
            Assert.All(report.CountsByCategoryAndClass[ComparisonCategories.Solutions].Values, v => Assert.Equal(0, v));
        }

        [Fact] // Version is compared like a property (solutions/publishers carry versions).
        public void Compare_VersionDiffers_IsChanged()
        {
            var diffs = SnapshotComparer.Compare(ComparisonCategories.Solutions,
                new[] { new ComponentSnapshot(ComparisonCategories.Solutions, "sol", "sol", false, "1.0.0.0") },
                new[] { new ComponentSnapshot(ComparisonCategories.Solutions, "sol", "sol", false, "1.0.0.1") });

            var d = Assert.Single(diffs);
            Assert.Equal(DiffClass.Changed, d.Class);
            Assert.Contains(d.ChangedProperties, c => c.Prop == "version");
        }

        // ---------------------------------------------------------------- severity (US-MIG1.4.2)

        [Fact] // Missing a structural component (table) is High; managed/unmanaged is High.
        public void SeverityFor_StructuralMissing_IsHigh()
        {
            Assert.Equal(Severity.High, SnapshotComparer.SeverityFor(ComparisonCategories.Tables, DiffClass.Missing));
            Assert.Equal(Severity.High, SnapshotComparer.SeverityFor(ComparisonCategories.Roles, DiffClass.ManagedVsUnmanaged));
        }

        [Fact] // Missing a soft/UI component (view) is Medium, not High.
        public void SeverityFor_SoftMissing_IsMedium()
        {
            Assert.Equal(Severity.Medium, SnapshotComparer.SeverityFor(ComparisonCategories.Views, DiffClass.Missing));
        }

        [Fact] // Identical never carries weight.
        public void SeverityFor_Identical_IsInfo()
        {
            Assert.Equal(Severity.Info, SnapshotComparer.SeverityFor(ComparisonCategories.Tables, DiffClass.Identical));
        }

        [Fact] // A caller-supplied resolver overrides the default table.
        public void SeverityFor_ResolverOverride_Wins()
        {
            var opts = new CompareOptions { SeverityResolver = (c, cls) => Severity.Critical };
            Assert.Equal(Severity.Critical, SnapshotComparer.SeverityFor(ComparisonCategories.Views, DiffClass.Extra, opts));
        }

        // ---------------------------------------------------------------- roll-up (US-MIG1.4.2)

        [Fact] // Roll produces a weighted score/band, findings (excluding identical), and a count matrix.
        public void Roll_ScoresBandsAndCounts()
        {
            var diffs = new List<ComponentDiff>();
            diffs.AddRange(SnapshotComparer.Compare(ComparisonCategories.Tables,
                new[] { Snap("a"), Snap("b"), Snap("same", props: ("p", "1")) },
                new[] { Snap("c"), Snap("same", props: ("p", "1")) }));
            // a,b missing (High x2 = 24), c extra (Medium = 5), same identical (0)

            var report = SnapshotComparer.Roll(diffs);

            Assert.Equal(2, report.CountsByCategoryAndClass[ComparisonCategories.Tables][DiffClass.Missing]);
            Assert.Equal(1, report.CountsByCategoryAndClass[ComparisonCategories.Tables][DiffClass.Extra]);
            Assert.Equal(1, report.CountsByCategoryAndClass[ComparisonCategories.Tables][DiffClass.Identical]);
            // Findings exclude the identical component.
            Assert.Equal(3, report.Findings.Count);
            Assert.DoesNotContain(report.Findings, f => f.Title.Contains("same"));
            // Weighted score = 12+12+5 = 29 → Medium band (High threshold is 40).
            Assert.Equal(29, report.Score);
            Assert.Equal(ScoreBand.Medium, report.Band);
        }

        [Fact] // Deterministic: same inputs → identical score and diff count.
        public void Roll_IsDeterministic()
        {
            var mk = new System.Func<List<ComponentDiff>>(() => SnapshotComparer.Compare(Cat,
                new[] { Snap("x"), Snap("y", props: ("t", "1")) },
                new[] { Snap("y", props: ("t", "2")) }));

            var r1 = SnapshotComparer.Roll(mk());
            var r2 = SnapshotComparer.Roll(mk());
            Assert.Equal(r1.Score, r2.Score);
            Assert.Equal(r1.Diffs.Count, r2.Diffs.Count);
        }

        // ---------------------------------------------------------------- secret masking (US-MIG1.3.3)

        [Fact] // A secret-typed property (marked on the snapshot) is masked in the diff output, though the
                // underlying change is still detected.
        public void Compare_SecretProperty_IsMaskedButStillDetected()
        {
            var src = new ComponentSnapshot(ComparisonCategories.EnvironmentVariables, "ev", "ev");
            src.With("currentvalue", "super-secret-source", secret: true);
            var tgt = new ComponentSnapshot(ComparisonCategories.EnvironmentVariables, "ev", "ev");
            tgt.With("currentvalue", "super-secret-target", secret: true);

            var d = Assert.Single(SnapshotComparer.Compare(ComparisonCategories.EnvironmentVariables, new[] { src }, new[] { tgt }));

            Assert.Equal(DiffClass.Changed, d.Class); // change detected
            var cp = Assert.Single(d.ChangedProperties);
            Assert.Equal(CompareOptions.Mask, cp.Source); // but values masked
            Assert.Equal(CompareOptions.Mask, cp.Target);
            Assert.DoesNotContain("secret", cp.Source);
        }

        [Fact] // A caller may also mark a property secret globally via CompareOptions.
        public void Compare_SecretViaOptions_IsMasked()
        {
            var opts = new CompareOptions();
            opts.SecretProperties.Add("token");
            var src = Snap("api", props: ("token", "abc"));
            var tgt = Snap("api", props: ("token", "xyz"));

            var d = Assert.Single(SnapshotComparer.Compare(Cat, new[] { src }, new[] { tgt }, opts));
            var cp = Assert.Single(d.ChangedProperties);
            Assert.Equal(CompareOptions.Mask, cp.Source);
            Assert.Equal(CompareOptions.Mask, cp.Target);
        }
    }
}
