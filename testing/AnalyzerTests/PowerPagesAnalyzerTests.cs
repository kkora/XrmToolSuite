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
    /// Executable tests for the Power Pages Readiness analyzer. It is entirely query-driven (no
    /// metadata, no target), so the fake connection exercises the full path. Traces to US-ALM07-7.
    /// </summary>
    public class PowerPagesAnalyzerTests
    {
        private static Entity Row(string logicalName, params (string, object)[] attrs)
        {
            var e = new Entity(logicalName, Guid.NewGuid());
            foreach (var (k, v) in attrs) e[k] = v;
            return e;
        }

        private static List<RiskFinding> Run(FakeOrganizationService source)
        {
            var ctx = TestData.Context(source);
            return new PowerPagesAnalyzer().Analyze(ctx, _ => { });
        }

        // TC-ALM07-PP-01: no portal tables present -> single Info "no site detected".
        [Fact]
        public void NoSite_SingleInfo()
        {
            var f = Assert.Single(Run(new FakeOrganizationService()));
            Assert.Equal(Severity.Info, f.Severity);
            Assert.Equal("No Power Pages site detected", f.Title);
        }

        // TC-ALM07-PP-02: site present but no web roles -> High (all permissions hang off web roles).
        [Fact]
        public void SiteWithoutWebRoles_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .Seed("adx_website", Row("adx_website"));

            var f = Assert.Single(Run(source), x => x.Title == "No web roles found");
            Assert.Equal(Severity.High, f.Severity);
        }

        // TC-ALM07-PP-03: a table surfaced by a basic form with no table-permission record -> High.
        [Fact]
        public void TableOnFormWithoutPermission_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .Seed("adx_website", Row("adx_website"))
                .Seed("adx_webrole", Row("adx_webrole",
                    ("adx_anonymoususersrole", true), ("adx_authenticatedusersrole", true)))
                .Seed("adx_entityform", Row("adx_entityform",
                    ("adx_name", "Case form"), ("adx_entityname", "contoso_case"),
                    ("adx_entitypermissionsenabled", true)))
                .Seed("adx_entitypermission"); // none defined

            var f = Assert.Single(Run(source), x => x.Title == "Table used on portal without table permission");
            Assert.Equal(Severity.High, f.Severity);
            Assert.Equal("contoso_case", f.AffectedComponent);
        }

        // TC-ALM07-PP-04: a basic form with table permissions turned OFF -> High (runs elevated).
        [Fact]
        public void FormWithPermissionsOff_FlagsHigh()
        {
            var source = new FakeOrganizationService()
                .Seed("adx_website", Row("adx_website"))
                .Seed("adx_webrole", Row("adx_webrole",
                    ("adx_anonymoususersrole", true), ("adx_authenticatedusersrole", true)))
                .Seed("adx_entityform", Row("adx_entityform",
                    ("adx_name", "Open form"), ("adx_entityname", "contoso_case"),
                    ("adx_entitypermissionsenabled", false)));

            Assert.Contains(Run(source), x =>
                x.Title == "Basic form bypasses table permissions" && x.Severity == Severity.High);
        }

        // TC-ALM07-PP-05: a web file with no attached document annotation -> Medium (will 404).
        [Fact]
        public void WebFileWithoutContent_FlagsMedium()
        {
            var source = new FakeOrganizationService()
                .Seed("adx_website", Row("adx_website"))
                .Seed("adx_webfile", Row("adx_webfile", ("adx_name", "logo.png")))
                .Seed("annotation"); // no document notes

            Assert.Contains(Run(source), x =>
                x.Title == "Web file has no content" && x.Severity == Severity.Medium);
        }

        // TC-ALM07-PP-06: a content snippet with an empty value -> Low (renders blank).
        [Fact]
        public void EmptyContentSnippet_FlagsLow()
        {
            var source = new FakeOrganizationService()
                .Seed("adx_website", Row("adx_website"))
                .Seed("adx_contentsnippet", Row("adx_contentsnippet",
                    ("adx_name", "Footer"), ("adx_value", "  ")));

            Assert.Contains(Run(source), x =>
                x.Title == "Empty content snippet" && x.Severity == Severity.Low);
        }

        // TC-ALM07-PP-07: the enhanced 'mspp_' data model is detected as well as classic 'adx_'.
        [Fact]
        public void EnhancedMsppSchema_IsDetected()
        {
            var source = new FakeOrganizationService()
                .Seed("mspp_website", Row("mspp_website"));

            var findings = Run(source);
            Assert.DoesNotContain(findings, x => x.Title == "No Power Pages site detected");
            Assert.Contains(findings, x => x.AffectedComponent == "mspp_website"); // post-deploy cache note
        }
    }
}
