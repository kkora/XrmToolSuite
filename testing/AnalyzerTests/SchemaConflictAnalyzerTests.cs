using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Data Model Conflicts analyzer's solution-version paths.
    /// Metadata-level comparisons (attribute type/length, option sets, relationships) need constructed
    /// EntityMetadata and stay in the manual GUI suite; here we cover the version/managed-state logic,
    /// which is pure row data. Traces to US-DG-6 (schema conflicts).
    /// </summary>
    public class SchemaConflictAnalyzerTests
    {
        private static Entity TargetSolution(string uniqueName, string version, bool managed)
        {
            var e = new Entity("solution");
            e["version"] = version;
            e["ismanaged"] = managed;
            e["uniquename"] = uniqueName;
            return e;
        }

        private static List<RiskFinding> Run(Entity sourceSolution, FakeOrganizationService target)
        {
            var source = new FakeOrganizationService(); // empty metadata -> no entity-level comparisons
            var ctx = TestData.Context(source, target, sourceSolution);
            return new SchemaConflictAnalyzer().Analyze(ctx, _ => { });
        }

        // TC-DG-SC-01: with no target connection the analyzer degrades to a single Info note.
        [Fact]
        public void NoTarget_SkipsWithInfo()
        {
            var ctx = TestData.Context(new FakeOrganizationService(), target: null, solution: TestData.Solution());
            var findings = new SchemaConflictAnalyzer().Analyze(ctx, _ => { });

            var f = Assert.Single(findings);
            Assert.Equal(Severity.Info, f.Severity);
            Assert.Equal("Schema conflict check skipped", f.Title);
        }

        // TC-DG-SC-02: source version not higher than target -> High ("version not incremented").
        [Fact]
        public void VersionNotIncremented_FlagsHigh()
        {
            var source = TestData.Solution(uniqueName: "contoso_core", version: "1.0.0.0", managed: true);
            var target = new FakeOrganizationService()
                .Seed("solution", TargetSolution("contoso_core", "1.0.0.0", managed: true));

            Assert.Contains(Run(source, target), x =>
                x.Title == "Solution version not incremented" && x.Severity == Severity.High);
        }

        // TC-DG-SC-03: source version higher than target -> Info ("managed upgrade will run").
        [Fact]
        public void VersionIncremented_FlagsUpgradeInfo()
        {
            var source = TestData.Solution(uniqueName: "contoso_core", version: "2.0.0.0", managed: true);
            var target = new FakeOrganizationService()
                .Seed("solution", TargetSolution("contoso_core", "1.0.0.0", managed: true));

            Assert.Contains(Run(source, target), x =>
                x.Title == "Managed upgrade will run" && x.Severity == Severity.Info);
        }

        // TC-DG-SC-04: managed state differs between source and target -> Critical (import will fail).
        [Fact]
        public void ManagedStateMismatch_FlagsCritical()
        {
            var source = TestData.Solution(uniqueName: "contoso_core", version: "2.0.0.0", managed: true);
            var target = new FakeOrganizationService()
                .Seed("solution", TargetSolution("contoso_core", "1.0.0.0", managed: false)); // unmanaged in target

            Assert.Contains(Run(source, target), x =>
                x.Title == "Managed/unmanaged mismatch" && x.Severity == Severity.Critical);
        }

        // TC-DG-SC-05: solution not present in target yet -> no version finding (nothing to compare).
        [Fact]
        public void SolutionAbsentFromTarget_NoVersionFinding()
        {
            var source = TestData.Solution(uniqueName: "contoso_core", version: "1.0.0.0");
            var target = new FakeOrganizationService().Seed("solution"); // target has no such solution

            Assert.DoesNotContain(Run(source, target), x =>
                x.Title == "Solution version not incremented" || x.Title == "Managed/unmanaged mismatch");
        }
    }
}
