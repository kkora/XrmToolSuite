using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Form Changes analyzer: form scripts/controls that reference a web
    /// resource missing from the source are flagged. Traces to US-DG-11 (form changes).
    /// </summary>
    public class FormAnalyzerTests
    {
        private static System.Collections.Generic.List<RiskFinding> Run(FakeOrganizationService source)
        {
            source.SeedIfAbsent("webresource");
            return new FormAnalyzer().Analyze(TestData.Context(source), _ => { });
        }

        // TC-DG-FM-01: a form referencing a web resource absent from the source is flagged High.
        [Fact]
        public void Form_MissingWebResource_FlagsHigh()
        {
            var xml = @"<form><formLibraries><Library name=""cc_/scripts/app.js"" /></formLibraries></form>";
            var source = new FakeOrganizationService()
                .Seed("systemform", TestData.SystemForm("Account Main", "account", xml))
                .Seed("webresource"); // none present

            var f = Assert.Single(Run(source), x => x.Title == "Form references missing web resource");
            Assert.Equal(Severity.High, f.Severity);
            Assert.Contains("cc_/scripts/app.js", f.Description);
        }

        // TC-DG-FM-02: when the referenced web resource exists in the source, there is no finding.
        [Fact]
        public void Form_KnownWebResource_NoFinding()
        {
            var xml = @"<form><formLibraries><Library name=""cc_/scripts/app.js"" /></formLibraries></form>";
            var source = new FakeOrganizationService()
                .Seed("systemform", TestData.SystemForm("Account Main", "account", xml))
                .Seed("webresource", TestData.WebResource("cc_/scripts/app.js"));

            Assert.DoesNotContain(Run(source), x => x.Title == "Form references missing web resource");
        }

        // TC-DG-FM-03: a $webresource: handler token missing from the source is flagged.
        [Fact]
        public void Form_MissingHandlerToken_FlagsHigh()
        {
            var xml = @"<form><events><event><Handler functionName=""go"" libraryName=""cc_missing.js"" /></event></events></form>";
            var source = new FakeOrganizationService()
                .Seed("systemform", TestData.SystemForm("Contact Main", "contact", xml))
                .Seed("webresource", TestData.WebResource("cc_other.js"));

            Assert.Single(Run(source), x => x.Title == "Form references missing web resource");
        }

        // TC-DG-FM-04: an empty solution (no forms) reports a single informational finding.
        [Fact]
        public void NoForms_ReportsInfo()
        {
            var source = new FakeOrganizationService().Seed("systemform");
            var f = Assert.Single(Run(source), x => x.Title == "No forms in solution");
            Assert.Equal(Severity.Info, f.Severity);
        }
    }
}
