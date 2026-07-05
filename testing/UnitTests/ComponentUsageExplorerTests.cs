using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.ComponentUsageExplorer.Analysis;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the SDK-free change-safety rules (<see cref="UsageVerdictRules"/>) and the
    /// usage-by-type tally (<see cref="UsageFootprint.BuildUsageByType"/>). Every verdict is a pure function
    /// of a hand-built <see cref="UsageFootprint"/>, so exact verdicts, score bands, and tallies are asserted
    /// with no live org. Traces to EPIC-SOLN01 (US-SOLN01.2.1/2.2, US-SOLN01.3.3, US-SOLN01.4.1/4.2).
    /// </summary>
    public class ComponentUsageExplorerTests
    {
        // ---- fixtures -------------------------------------------------------------------------

        private static ComponentRef Comp(int type, string typeName, string name,
            bool managed = false, params string[] solutions) => new ComponentRef
            {
                ComponentType = type,
                ComponentTypeName = typeName,
                ObjectId = Guid.NewGuid(),
                Name = name,
                IsManaged = managed,
                OwningSolutions = solutions?.ToList() ?? new List<string>()
            };

        /// <summary>The selected component: a web resource shipping in solution "Core".</summary>
        private static ComponentRef Subject(int type = 61, string typeName = "Web Resource", bool managed = false) =>
            Comp(type, typeName, "acme_lib.js", managed, "Core");

        private static UsageFootprint Footprint(ComponentRef subject,
            List<ComponentRef> dependents = null, List<ComponentRef> required = null, bool incomplete = false)
        {
            var fp = new UsageFootprint
            {
                Component = subject,
                DependentComponents = dependents ?? new List<ComponentRef>(),
                RequiredComponents = required ?? new List<ComponentRef>(),
                DependencyDataIncomplete = incomplete
            };
            fp.UsageByType = UsageFootprint.BuildUsageByType(fp.DependentComponents);
            return fp;
        }

        // ---- SafeToChange: no dependents (US-SOLN01.4.1) ---------------------------------------

        [Fact]
        public void NoDependents_YieldsSafeToChange()
        {
            var report = UsageVerdictRules.Evaluate(Footprint(Subject()));

            Assert.Equal(ChangeSafety.SafeToChange, report.Verdict);
            Assert.Equal(ScoreBand.Low, report.Band);
            Assert.Contains(report.Findings, f => f.Title == "No dependent components found");
        }

        // ---- ChangeWithCaution: a few plain dependents (US-SOLN01.4.1) -------------------------

        [Fact]
        public void FewPlainDependents_YieldsChangeWithCaution()
        {
            var deps = new List<ComponentRef>
            {
                Comp(26, "Saved Query", "Active Accounts", solutions: "Core"),
                Comp(26, "Saved Query", "My Accounts", solutions: "Core"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps));

            Assert.Equal(ChangeSafety.ChangeWithCaution, report.Verdict);
            Assert.Equal(ScoreBand.Medium, report.Band);
        }

        // ---- HighImpact: high-value dependent types (US-SOLN01.4.1) ----------------------------

        [Fact]
        public void HighValueDependents_YieldHighImpact()
        {
            var deps = new List<ComponentRef>
            {
                Comp(60, "System Form", "Account Main Form", solutions: "Core"),
                Comp(29, "Workflow / Flow", "On Create", solutions: "Core"),
                Comp(90, "Plugin Type", "Acme.Plugins.Foo", solutions: "Core"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps));

            Assert.Equal(ChangeSafety.HighImpact, report.Verdict);
            Assert.Equal(ScoreBand.High, report.Band);
        }

        [Fact]
        public void ManyPlainDependents_YieldHighImpact()
        {
            // 9 plain saved queries — over the default HighImpactDependentThreshold of 8, no high-value type.
            var deps = Enumerable.Range(0, 9)
                .Select(i => Comp(26, "Saved Query", "View " + i, solutions: "Core"))
                .ToList();

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps));

            Assert.Equal(ChangeSafety.HighImpact, report.Verdict);
        }

        // ---- RequiresAlmReview: managed or cross-solution dependents (US-SOLN01.4.1) -----------

        [Fact]
        public void ManagedDependents_YieldRequiresAlmReview()
        {
            var deps = new List<ComponentRef>
            {
                Comp(60, "System Form", "ISV Form", managed: true, solutions: "ISVManaged"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps));

            Assert.Equal(ChangeSafety.RequiresAlmReview, report.Verdict);
            Assert.Contains(report.Findings, f => f.Title.Contains("managed dependent"));
        }

        [Fact]
        public void CrossSolutionDependents_YieldRequiresAlmReview()
        {
            // Dependent ships only in "Other" — the subject ships in "Core": no shared solution.
            var deps = new List<ComponentRef>
            {
                Comp(26, "Saved Query", "Other View", solutions: "Other"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps));

            Assert.Equal(ChangeSafety.RequiresAlmReview, report.Verdict);
            Assert.Contains(report.Findings, f => f.Title.Contains("cross-solution"));
        }

        // ---- DoNotDelete: a table with many dependents (US-SOLN01.4.1) -------------------------

        [Fact]
        public void TableWithManyDependents_YieldsDoNotDelete()
        {
            var table = Comp(1, "Entity", "acme_project", solutions: "Core");
            var deps = new List<ComponentRef>
            {
                Comp(2, "Attribute", "acme_name", solutions: "Core"),
                Comp(26, "Saved Query", "All Projects", solutions: "Core"),
                Comp(26, "Saved Query", "My Projects", solutions: "Core"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(table, deps));

            Assert.Equal(ChangeSafety.DoNotDelete, report.Verdict);
            Assert.Equal(ScoreBand.High, report.Band);
        }

        // ---- RequiresDependencyReview: incomplete platform data (US-SOLN01.2.2) ----------------

        [Fact]
        public void IncompleteDependencyData_YieldsRequiresDependencyReview()
        {
            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), incomplete: true));

            Assert.Equal(ChangeSafety.RequiresDependencyReview, report.Verdict);
            Assert.Contains(report.Findings, f => f.Title == "Dependency data incomplete");
        }

        [Fact]
        public void IncompleteData_DoesNotDowngradeAHigherVerdict()
        {
            var deps = new List<ComponentRef>
            {
                Comp(60, "System Form", "Account Main Form", solutions: "Core"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps, incomplete: true));

            // High-value dependent outranks the incomplete-data signal.
            Assert.Equal(ChangeSafety.HighImpact, report.Verdict);
        }

        // ---- score / band ordering (US-SOLN01.4.1) --------------------------------------------

        [Fact]
        public void Score_IncreasesWithVerdictSeverity()
        {
            var safe = UsageVerdictRules.Evaluate(Footprint(Subject()));
            var caution = UsageVerdictRules.Evaluate(Footprint(Subject(),
                new List<ComponentRef> { Comp(26, "Saved Query", "V", solutions: "Core") }));
            var doNotDelete = UsageVerdictRules.Evaluate(Footprint(
                Comp(1, "Entity", "acme_project", solutions: "Core"),
                Enumerable.Range(0, 4).Select(i => Comp(26, "Saved Query", "V" + i, solutions: "Core")).ToList()));

            Assert.True(safe.Score < caution.Score);
            Assert.True(caution.Score < doNotDelete.Score);
            Assert.InRange(doNotDelete.Score, 0, 100);
        }

        // ---- usage-by-type tally (US-SOLN01.3.3) ----------------------------------------------

        [Fact]
        public void UsageByType_TalliesDependentsByTypeName()
        {
            var deps = new List<ComponentRef>
            {
                Comp(26, "Saved Query", "V1", solutions: "Core"),
                Comp(26, "Saved Query", "V2", solutions: "Core"),
                Comp(60, "System Form", "F1", solutions: "Core"),
            };

            var map = UsageFootprint.BuildUsageByType(deps);

            Assert.Equal(2, map["Saved Query"]);
            Assert.Equal(1, map["System Form"]);
        }

        [Fact]
        public void Evaluate_PopulatesUsageByTypeMetric()
        {
            var deps = new List<ComponentRef>
            {
                Comp(26, "Saved Query", "V1", solutions: "Core"),
                Comp(26, "Saved Query", "V2", solutions: "Core"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps));

            var dependentMetric = report.Metrics.Single(m => m.Label == "Dependent components");
            Assert.Equal("2", dependentMetric.Value);
        }

        // ---- explanation is populated for every verdict (US-SOLN01.4.2) ------------------------

        [Fact]
        public void Explanation_NamesVerdictAndNextSteps()
        {
            var deps = new List<ComponentRef>
            {
                Comp(90, "Plugin Type", "Acme.Plugins.Foo", solutions: "Core"),
            };

            var report = UsageVerdictRules.Evaluate(Footprint(Subject(), deps));

            Assert.False(string.IsNullOrWhiteSpace(report.Explanation));
            Assert.Contains("High impact", report.Explanation);
            Assert.Contains("Next:", report.Explanation);
        }
    }
}
