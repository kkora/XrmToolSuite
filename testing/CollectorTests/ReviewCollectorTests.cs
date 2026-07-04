using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.AiSolutionReviewer.Analysis;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.CollectorTests
{
    /// <summary>
    /// Headless tests for the AI Solution Reviewer's Dataverse collectors (the AI/HTTP summarization
    /// path stays manual). Each collector implements <c>IAnalyzer&lt;ReviewContext&gt;</c> and depends only on
    /// <see cref="IOrganizationService"/>, so it runs against the shared <see cref="FakeOrganizationService"/>.
    /// Traces to US-AR-1 (design concerns). TC-AR-COL-01..09.
    /// </summary>
    public class ReviewCollectorTests
    {
        private static Entity Solution(bool managed = true, string uniqueName = "contoso_core", string version = "1.0.0.0") =>
            new Entity("solution", Guid.NewGuid())
            {
                ["uniquename"] = uniqueName,
                ["friendlyname"] = "Contoso Core",
                ["version"] = version,
                ["ismanaged"] = managed
            };

        private static ReviewContext Ctx(FakeOrganizationService fake, Entity solution = null) =>
            new ReviewContext(fake, solution ?? Solution());

        private static Entity Step(int mode, string name, string filtering = null)
        {
            var e = new Entity("sdkmessageprocessingstep", Guid.NewGuid())
            {
                ["name"] = name,
                ["mode"] = new OptionSetValue(mode) // 0 = sync, 1 = async
            };
            if (filtering != null) e["filteringattributes"] = filtering;
            return e;
        }

        private static Entity Web(int type, string content = null)
        {
            var e = new Entity("webresource", Guid.NewGuid()) { ["webresourcetype"] = new OptionSetValue(type) };
            if (content != null) e["content"] = content;
            e["name"] = "wr_" + e.Id.ToString("N").Substring(0, 6);
            return e;
        }

        private static Entity Workflow(int category) =>
            new Entity("workflow", Guid.NewGuid()) { ["category"] = new OptionSetValue(category), ["name"] = "proc" };

        private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

        // ---- PluginReviewCollector -------------------------------------------------

        // TC-AR-COL-01: a synchronous step with no filtering attributes flags Medium.
        [Fact]
        public void SyncStep_NoFiltering_FlagsMedium()
        {
            var fake = new FakeOrganizationService().Seed("sdkmessageprocessingstep", Step(0, "acct_postcreate"));
            var f = Assert.Single(new PluginReviewCollector().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Synchronous step without filtering attributes");
            Assert.Equal(Severity.Medium, f.Severity);
            Assert.Equal("acct_postcreate", f.Component);
        }

        // TC-AR-COL-02: an async step, or a sync step WITH filtering, produces no sync finding.
        [Fact]
        public void AsyncOrFilteredStep_NoSyncFinding()
        {
            var fake = new FakeOrganizationService().Seed("sdkmessageprocessingstep",
                Step(1, "async_step"),                      // async
                Step(0, "filtered_step", "name,statecode"));// sync but filtered
            Assert.DoesNotContain(new PluginReviewCollector().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Synchronous step without filtering attributes");
        }

        // TC-AR-COL-03: 20+ steps flag a Low "Heavy plugin footprint".
        [Fact]
        public void ManySteps_FlagsHeavyFootprint()
        {
            var steps = Enumerable.Range(0, 20).Select(i => Step(1, "s" + i)).ToArray(); // async: no sync noise
            var fake = new FakeOrganizationService().Seed("sdkmessageprocessingstep", steps);
            Assert.Contains(new PluginReviewCollector().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Heavy plugin footprint" && x.Severity == Severity.Low);
        }

        // TC-AR-COL-04: no plugin steps yields an Info finding.
        [Fact]
        public void NoSteps_FlagsInfo()
        {
            var fake = new FakeOrganizationService(); // nothing seeded
            var f = Assert.Single(new PluginReviewCollector().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "No plugin steps in solution");
            Assert.Equal(Severity.Info, f.Severity);
        }

        // ---- ScriptReviewCollector -------------------------------------------------

        // TC-AR-COL-05: a JScript web resource using a deprecated API flags Medium; non-JScript is ignored.
        [Fact]
        public void DeprecatedApi_InJScript_FlagsMedium()
        {
            var fake = new FakeOrganizationService().Seed("webresource",
                Web(3, B64("function onLoad(){ Xrm.Page.getAttribute('name'); }")),
                Web(1, B64("Xrm.Page")));  // type 1 (HTML) — filtered out entirely

            var f = Assert.Single(new ScriptReviewCollector().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Deprecated client API in use");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // TC-AR-COL-06: 15+ JScript web resources flag Low "Heavy client-side scripting".
        [Fact]
        public void ManyScripts_FlagsHeavyScripting()
        {
            var scripts = Enumerable.Range(0, 15).Select(i => Web(3, B64("var x=" + i + ";"))).ToArray();
            var fake = new FakeOrganizationService().Seed("webresource", scripts);
            var findings = new ScriptReviewCollector().Analyze(Ctx(fake), _ => { });
            Assert.Contains(findings, x => x.Title == "Heavy client-side scripting" && x.Severity == Severity.Low);
            Assert.DoesNotContain(findings, x => x.Title == "Deprecated client API in use"); // clean code
        }

        // ---- AutomationReviewCollector --------------------------------------------

        // TC-AR-COL-07: a classic workflow (category 0) flags Medium; 25+ processes add "Automation sprawl".
        [Fact]
        public void ClassicWorkflows_FlagMediumAndSprawl()
        {
            var procs = Enumerable.Range(0, 25).Select(_ => Workflow(0)).ToArray(); // 25 classic
            var fake = new FakeOrganizationService().Seed("workflow", procs);
            var findings = new AutomationReviewCollector().Analyze(Ctx(fake), _ => { });
            Assert.Contains(findings, x => x.Title == "Legacy classic workflows" && x.Severity == Severity.Medium);
            Assert.Contains(findings, x => x.Title == "Automation sprawl" && x.Severity == Severity.Low);
        }

        // ---- AlmGovernanceReviewCollector -----------------------------------------

        // TC-AR-COL-08: an unmanaged solution flags Medium, and a Version Info finding is always emitted.
        [Fact]
        public void UnmanagedSolution_FlagsMedium_AndVersionInfo()
        {
            var findings = new AlmGovernanceReviewCollector()
                .Analyze(Ctx(new FakeOrganizationService(), Solution(managed: false)), _ => { });
            Assert.Contains(findings, x => x.Title == "Unmanaged solution" && x.Severity == Severity.Medium);
            Assert.Contains(findings, x => x.Title == "Version" && x.Severity == Severity.Info);
        }

        // TC-AR-COL-09: the default 'new_' publisher prefix flags Low.
        [Fact]
        public void DefaultPublisherPrefix_FlagsLow()
        {
            var findings = new AlmGovernanceReviewCollector()
                .Analyze(Ctx(new FakeOrganizationService(), Solution(managed: true, uniqueName: "new_widgets")), _ => { });
            Assert.Contains(findings, x => x.Title == "Default publisher prefix" && x.Severity == Severity.Low);
        }
    }
}
