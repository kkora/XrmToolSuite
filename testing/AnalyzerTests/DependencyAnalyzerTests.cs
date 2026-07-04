using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Solution Dependencies analyzer's managed-state and missing-dependency
    /// paths. Publisher checks (Retrieve) and duplicate-layer detection (aliased joins) stay manual.
    /// Traces to US-DG-2 (dependencies) and US-DG-3 (managed state).
    /// </summary>
    public class DependencyAnalyzerTests
    {
        private static List<RiskFinding> Run(FakeOrganizationService source, Entity solution)
        {
            var ctx = TestData.Context(source, solution: solution);
            return new DependencyAnalyzer().Analyze(ctx, _ => { });
        }

        // TC-DG-DEP-01: an unmanaged solution is flagged Medium (no clean rollback downstream).
        [Fact]
        public void UnmanagedSolution_FlagsMedium()
        {
            var source = new FakeOrganizationService(); // no missing deps -> "No missing dependencies" Info
            var findings = Run(source, TestData.Solution(managed: false));

            Assert.Contains(findings, x => x.Title == "Solution is unmanaged" && x.Severity == Severity.Medium);
        }

        // TC-DG-DEP-02: a managed solution produces no unmanaged-state finding.
        [Fact]
        public void ManagedSolution_NoUnmanagedFinding()
        {
            var source = new FakeOrganizationService();
            Assert.DoesNotContain(Run(source, TestData.Solution(managed: true)),
                x => x.Title == "Solution is unmanaged");
        }

        // TC-DG-DEP-03: no missing dependencies -> reassuring Info.
        [Fact]
        public void NoMissingDependencies_InfoNote()
        {
            var source = new FakeOrganizationService();
            Assert.Contains(Run(source, TestData.Solution(managed: true)),
                x => x.Title == "No missing dependencies" && x.Severity == Severity.Info);
        }

        // TC-DG-DEP-04: a missing component dependency (no target to confirm) is flagged High.
        [Fact]
        public void MissingDependency_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .SeedMissingDependencies(TestData.MissingDependency(AnalyzerContext.CT_Entity));

            var findings = Run(source, TestData.Solution(managed: true));
            var f = Assert.Single(findings, x => x.Title.StartsWith("Missing dependency"));
            Assert.Equal(Severity.High, f.Severity);
        }
    }
}
