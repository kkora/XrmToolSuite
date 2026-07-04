using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Ribbon Changes analyzer: command handlers referencing a web resource
    /// missing from the source are flagged. Traces to US-DG-12 (ribbon changes).
    /// </summary>
    public class RibbonAnalyzerTests
    {
        private static System.Collections.Generic.List<RiskFinding> Run(FakeOrganizationService source)
        {
            source.SeedIfAbsent("webresource");
            return new RibbonAnalyzer().Analyze(TestData.Context(source), _ => { });
        }

        // TC-DG-RB-01: a ribbon command whose $webresource library is missing from the source is flagged High.
        [Fact]
        public void Ribbon_MissingWebResource_FlagsHigh()
        {
            var xml = @"<RibbonDiffXml><CommandDefinitions><CommandDefinition><Actions>
                <JavaScriptFunction FunctionName=""onClick"" Library=""$webresource:cc_ribbon.js"" />
                </Actions></CommandDefinition></CommandDefinitions></RibbonDiffXml>";
            var source = new FakeOrganizationService()
                .Seed("ribboncustomization", TestData.RibbonCustomization("account", xml))
                .Seed("webresource"); // none present

            var f = Assert.Single(Run(source), x => x.Title == "Ribbon command references missing web resource");
            Assert.Equal(Severity.High, f.Severity);
            Assert.Contains("cc_ribbon.js", f.Description);
        }

        // TC-DG-RB-02: when the referenced library exists in the source, there is no finding.
        [Fact]
        public void Ribbon_KnownWebResource_NoFinding()
        {
            var xml = @"<RibbonDiffXml><CommandDefinitions><CommandDefinition><Actions>
                <JavaScriptFunction FunctionName=""onClick"" Library=""$webresource:cc_ribbon.js"" />
                </Actions></CommandDefinition></CommandDefinitions></RibbonDiffXml>";
            var source = new FakeOrganizationService()
                .Seed("ribboncustomization", TestData.RibbonCustomization("account", xml))
                .Seed("webresource", TestData.WebResource("cc_ribbon.js"));

            Assert.DoesNotContain(Run(source), x => x.Title == "Ribbon command references missing web resource");
        }

        // TC-DG-RB-03: an empty solution (no ribbon customizations) reports a single informational finding.
        [Fact]
        public void NoRibbons_ReportsInfo()
        {
            var source = new FakeOrganizationService().Seed("ribboncustomization");
            var f = Assert.Single(Run(source), x => x.Title == "No ribbon customizations in solution");
            Assert.Equal(Severity.Info, f.Severity);
        }
    }
}
