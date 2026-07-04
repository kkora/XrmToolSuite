using System.Collections.Generic;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Environment Variables &amp; Connection References analyzer.
    /// Exercises the query-driven paths with a fake IOrganizationService (no live org).
    /// Traces to US-DG-4 (environment variables / connection references).
    /// </summary>
    public class EnvironmentVariableAnalyzerTests
    {
        private const int TypeSecret = 100000005;
        private const int TypeDataSource = 100000004;

        private static List<RiskFinding> Run(FakeOrganizationService source, FakeOrganizationService target = null)
        {
            var ctx = TestData.Context(source, target);
            return new EnvironmentVariableAnalyzer().Analyze(ctx, _ => { });
        }

        // TC-DG-EV-01: a Secret-type variable always flags High (secrets never transport).
        [Fact]
        public void SecretVariable_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_ApiKey", type: TypeSecret));

            var f = Assert.Single(Run(source), x => x.Title == "Secret environment variable requires manual setup");
            Assert.Equal(Severity.High, f.Severity);
            Assert.Equal("contoso_ApiKey", f.AffectedComponent);
        }

        // TC-DG-EV-02: a value row packaged inside the solution flags Medium (overwrites target on import).
        [Fact]
        public void PackagedValue_FlagsMedium()
        {
            var def = TestData.EnvVarDef("contoso_Url", defaultValue: "https://dev.example");
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", def)
                .Seed("environmentvariablevalue", TestData.EnvVarValue(def.Id, "https://dev.example"));

            var findings = Run(source);
            Assert.Contains(findings, x =>
                x.Title == "Environment variable VALUE packaged in solution" && x.Severity == Severity.Medium);
        }

        // TC-DG-EV-03: no default + no current value + no target -> Medium ("configure it somewhere").
        [Fact]
        public void NoDefaultNoValue_NoTarget_FlagsMedium()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_Empty"));

            var f = Assert.Single(Run(source), x => x.Title == "Environment variable has no default value");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // TC-DG-EV-04: a variable with a default value and no target produces no finding.
        [Fact]
        public void HasDefault_NoTarget_NoFinding()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_Ok", defaultValue: "42"));

            Assert.DoesNotContain(Run(source), x => x.Category == AnalyzerCategory.EnvironmentVariables
                                                    && x.Severity != Severity.Info);
        }

        // TC-DG-EV-05: variable absent from target with no default -> High.
        [Fact]
        public void NewToTarget_NoDefault_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_New"));
            var target = new FakeOrganizationService()
                .Seed("environmentvariabledefinition")   // target has none
                .Seed("environmentvariablevalue");

            var f = Assert.Single(Run(source, target), x => x.Title == "Environment variable new to target");
            Assert.Equal(Severity.High, f.Severity);
        }

        // TC-DG-EV-06: variable present in target but unset (no value, no default) -> High.
        [Fact]
        public void ExistsInTargetButUnset_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_Shared", defaultValue: "seed"));
            var target = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_Shared"))  // no default in target
                .Seed("environmentvariablevalue");                                            // no value in target

            Assert.Contains(Run(source, target), x =>
                x.Title == "Environment variable unset in target" && x.Severity == Severity.High);
        }

        // TC-DG-EV-07: data-source variable with no target binding -> Medium.
        [Fact]
        public void DataSourceVariable_UnboundInTarget_FlagsMedium()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_Sp", type: TypeDataSource, defaultValue: "x"));
            var target = new FakeOrganizationService()
                .Seed("environmentvariabledefinition", TestData.EnvVarDef("contoso_Sp", type: TypeDataSource, defaultValue: "x"))
                .Seed("environmentvariablevalue");

            Assert.Contains(Run(source, target), x =>
                x.Title == "Data source variable unbound in target" && x.Severity == Severity.Medium);
        }

        // TC-DG-EV-08: connection reference with no target -> Info (bind-at-import reminder).
        [Fact]
        public void ConnectionReference_NoTarget_FlagsInfo()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition")
                .Seed("environmentvariablevalue")
                .Seed("connectionreference",
                    TestData.ConnRef("contoso_sharedmailbox", connectorId: "/providers/Microsoft.PowerApps/apis/shared_office365"));

            var f = Assert.Single(Run(source), x => x.Title == "Connection reference must be bound at import");
            Assert.Equal(Severity.Info, f.Severity);
        }

        // TC-DG-EV-09: connection reference present in target but with no connection bound -> High.
        [Fact]
        public void ConnectionReference_UnboundInTarget_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .Seed("environmentvariabledefinition")
                .Seed("environmentvariablevalue")
                .Seed("connectionreference", TestData.ConnRef("contoso_mailbox", connectorId: "shared_office365"));
            var target = new FakeOrganizationService()
                .Seed("connectionreference", TestData.ConnRef("contoso_mailbox", connectionId: null));

            var f = Assert.Single(Run(source, target), x => x.Title == "Connection reference unbound in target");
            Assert.Equal(Severity.High, f.Severity);
        }
    }
}
