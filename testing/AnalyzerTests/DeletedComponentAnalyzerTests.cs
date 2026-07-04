using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Deleted Components analyzer: components present in the target's
    /// installed solution but absent from the source are deleted on a managed upgrade. Diff is by
    /// (componenttype, objectid) — no metadata joins — so it runs automated. Traces to US-DG-2.3.
    /// </summary>
    public class DeletedComponentAnalyzerTests
    {
        private const int CtEntity = AnalyzerContext.CT_Entity;        // 1
        private const int CtAttribute = AnalyzerContext.CT_Attribute;  // 2
        private const int CtWorkflow = AnalyzerContext.CT_Workflow;    // 29
        private const string Sln = "contoso_core";

        private static List<RiskFinding> Run(Entity solution, FakeOrganizationService source, FakeOrganizationService target)
        {
            var ctx = TestData.Context(source, target, solution);
            return new DeletedComponentAnalyzer().Analyze(ctx, _ => { });
        }

        // TC-DG-DC-01: with no target connection the analyzer degrades to a single Info note.
        [Fact]
        public void NoTarget_SkipsWithInfo()
        {
            var ctx = TestData.Context(new FakeOrganizationService(), null, TestData.Solution(Sln));
            var f = Assert.Single(new DeletedComponentAnalyzer().Analyze(ctx, _ => { }),
                x => x.Title == "Deleted-component check skipped");
            Assert.Equal(Severity.Info, f.Severity);
        }

        // TC-DG-DC-02: the solution not being installed in the target is a fresh install → Info, nothing deletable.
        [Fact]
        public void SolutionAbsentFromTarget_FlagsFreshInstallInfo()
        {
            var target = new FakeOrganizationService().Seed("solution"); // no matching solution row
            var f = Assert.Single(Run(TestData.Solution(Sln), new FakeOrganizationService(), target),
                x => x.Title == "No prior version in target");
            Assert.Equal(Severity.Info, f.Severity);
        }

        // TC-DG-DC-03: a table in the managed target but not in source is deleted with its data → Critical.
        [Fact]
        public void ManagedTarget_RemovedTable_FlagsCritical()
        {
            var solution = TestData.Solution(Sln);
            var tgtSol = TestData.TargetSolution(Sln, managed: true);
            var target = new FakeOrganizationService()
                .Seed("solution", tgtSol)
                .Seed("solutioncomponent", TestData.SolutionComponent(CtEntity, Guid.NewGuid(), tgtSol.Id));

            var f = Assert.Single(Run(solution, new FakeOrganizationService(), target),
                x => x.Title == "Entity deleted on managed upgrade");
            Assert.Equal(Severity.Critical, f.Severity);
            Assert.Contains("data", f.Description);
        }

        // TC-DG-DC-04: a removed column is High (data loss, but scoped to the column).
        [Fact]
        public void ManagedTarget_RemovedAttribute_FlagsHigh()
        {
            var solution = TestData.Solution(Sln);
            var tgtSol = TestData.TargetSolution(Sln, managed: true);
            var target = new FakeOrganizationService()
                .Seed("solution", tgtSol)
                .Seed("solutioncomponent", TestData.SolutionComponent(CtAttribute, Guid.NewGuid(), tgtSol.Id));

            var f = Assert.Single(Run(solution, new FakeOrganizationService(), target),
                x => x.Title == "Attribute deleted on managed upgrade");
            Assert.Equal(Severity.High, f.Severity);
        }

        // TC-DG-DC-05: a removed metadata-only component (workflow) is Medium — no data loss.
        [Fact]
        public void ManagedTarget_RemovedWorkflow_FlagsMedium()
        {
            var solution = TestData.Solution(Sln);
            var tgtSol = TestData.TargetSolution(Sln, managed: true);
            var target = new FakeOrganizationService()
                .Seed("solution", tgtSol)
                .Seed("solutioncomponent", TestData.SolutionComponent(CtWorkflow, Guid.NewGuid(), tgtSol.Id));

            var f = Assert.Single(Run(solution, new FakeOrganizationService(), target),
                x => x.Severity == Severity.Medium);
            Assert.Contains("deleted on managed upgrade", f.Title);
        }

        // TC-DG-DC-06: a component present in both source and target is retained → no finding.
        [Fact]
        public void ComponentInBothSides_NoFinding()
        {
            var solution = TestData.Solution(Sln);
            var kept = Guid.NewGuid();
            var source = new FakeOrganizationService()
                .Seed("solutioncomponent", TestData.SolutionComponent(CtEntity, kept, solution.Id));
            var tgtSol = TestData.TargetSolution(Sln, managed: true);
            var target = new FakeOrganizationService()
                .Seed("solution", tgtSol)
                .Seed("solutioncomponent", TestData.SolutionComponent(CtEntity, kept, tgtSol.Id));

            Assert.Empty(Run(solution, source, target));
        }

        // TC-DG-DC-07: removals against an UNMANAGED target are drift, not deletions → single Info, no Critical.
        [Fact]
        public void UnmanagedTarget_RemovedTable_FlagsInfoNotCritical()
        {
            var solution = TestData.Solution(Sln);
            var tgtSol = TestData.TargetSolution(Sln, managed: false);
            var target = new FakeOrganizationService()
                .Seed("solution", tgtSol)
                .Seed("solutioncomponent", TestData.SolutionComponent(CtEntity, Guid.NewGuid(), tgtSol.Id));

            var findings = Run(solution, new FakeOrganizationService(), target);
            var f = Assert.Single(findings, x => x.Severity == Severity.Info);
            Assert.Contains("unmanaged", f.Title);
            Assert.DoesNotContain(findings, x => x.Severity == Severity.Critical);
        }
    }
}
