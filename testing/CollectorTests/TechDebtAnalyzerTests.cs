using System;
using System.Text;
using Microsoft.Xrm.Sdk;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.TechnicalDebtAnalyzer.Analysis;

namespace XrmToolSuite.CollectorTests
{
    /// <summary>
    /// Headless tests for the Technical Debt Analyzer's row-driven analyzers (plugins, processes, web
    /// resources, security roles). Each implements <c>IAnalyzer&lt;TechDebtContext&gt;</c> and depends only on
    /// <see cref="IOrganizationService"/>, so it runs against the shared <see cref="FakeOrganizationService"/>.
    /// Metadata-driven branches (custom-table row counts, wide tables, publisher-prefix on tables/columns,
    /// secured-column counts) are covered separately in <c>TechDebtMetadataTests</c> via a reflection
    /// EntityMetadata builder. Traces to US-TD-1..5. TC-TD-COL-01..07.
    /// </summary>
    public class TechDebtAnalyzerTests
    {
        private static TechDebtContext Ctx(FakeOrganizationService fake) => new TechDebtContext(fake, "TEST");

        private static Entity Step(string name, int? statecode = null, int? mode = null,
            Guid? messageId = null, string filtering = null, Guid? pluginTypeId = null)
        {
            var e = new Entity("sdkmessageprocessingstep", Guid.NewGuid()) { ["name"] = name };
            if (statecode.HasValue) e["statecode"] = new OptionSetValue(statecode.Value);
            if (mode.HasValue) e["mode"] = new OptionSetValue(mode.Value);
            if (messageId.HasValue) e["sdkmessageid"] = new EntityReference("sdkmessage", messageId.Value);
            if (filtering != null) e["filteringattributes"] = filtering;
            if (pluginTypeId.HasValue) e["plugintypeid"] = new EntityReference("plugintype", pluginTypeId.Value);
            return e;
        }

        private static Entity Msg(Guid id, string name) => new Entity("sdkmessage", id) { ["name"] = name };

        private static Entity Web(int type, string name, string displayName = null, string content = null)
        {
            var e = new Entity("webresource", Guid.NewGuid()) { ["name"] = name, ["webresourcetype"] = new OptionSetValue(type) };
            if (displayName != null) e["displayname"] = displayName;
            if (content != null) e["content"] = content;
            return e;
        }

        private static Entity Role(string name) => new Entity("role", Guid.NewGuid()) { ["name"] = name };
        private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

        // TC-TD-COL-01: a plugin step on RetrieveMultiple is High (per-query latency source).
        [Fact]
        public void Performance_PluginOnRetrieveMultiple_High()
        {
            var msg = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("sdkmessage", Msg(msg, "RetrieveMultiple"))
                .Seed("sdkmessageprocessingstep", Step("acct_rm", messageId: msg));

            var f = Assert.Single(new PerformanceAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Plugin on RetrieveMultiple");
            Assert.Equal(Severity.High, f.Severity);
        }

        // TC-TD-COL-02: a synchronous Update plugin with no filtering attributes is Medium.
        [Fact]
        public void Performance_SyncUpdateNoFilter_Medium()
        {
            var msg = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("sdkmessage", Msg(msg, "Update"))
                .Seed("sdkmessageprocessingstep", Step("acct_update", mode: 0, messageId: msg));

            Assert.Contains(new PerformanceAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Synchronous Update plugin without filtering attributes" && x.Severity == Severity.Medium);
        }

        // TC-TD-COL-03: dead-plugin registration flags disabled step, step-less type, and step-less assembly.
        [Fact]
        public void DeadPlugins_DisabledStep_UnsteppedTypeAndAssembly()
        {
            Guid typeId = Guid.NewGuid(), asmId = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("sdkmessageprocessingstep", Step("legacy_step", statecode: TechDebtContext.StateInactive))
                .Seed("plugintype", new Entity("plugintype", typeId)
                {
                    ["typename"] = "Contoso.Plugins.AccountHandler",
                    ["pluginassemblyid"] = new EntityReference("pluginassembly", asmId)
                })
                .Seed("pluginassembly", new Entity("pluginassembly", asmId) { ["name"] = "Contoso.Plugins.dll" });

            var findings = new DeadPluginsAnalyzer().Analyze(Ctx(fake), _ => { });
            Assert.Contains(findings, x => x.Title == "Disabled plugin step" && x.Severity == Severity.Low);
            Assert.Contains(findings, x => x.Title == "Plugin type has no steps" && x.Severity == Severity.Low);
            Assert.Contains(findings, x => x.Title == "Plugin assembly has no active steps" && x.Severity == Severity.Medium);
        }

        // TC-TD-COL-04: a draft process (type 1, statecode 0) is Low; an active process is not flagged.
        [Fact]
        public void Orphaned_DraftProcess_Low_ActiveIgnored()
        {
            var fake = new FakeOrganizationService().Seed("workflow",
                new Entity("workflow", Guid.NewGuid()) { ["name"] = "Never Ran", ["type"] = new OptionSetValue(1), ["statecode"] = new OptionSetValue(0) },
                new Entity("workflow", Guid.NewGuid()) { ["name"] = "Live", ["type"] = new OptionSetValue(1), ["statecode"] = new OptionSetValue(1) });

            var findings = new OrphanedComponentsAnalyzer().Analyze(Ctx(fake), _ => { });
            var f = Assert.Single(findings, x => x.Title == "Draft process never activated");
            Assert.Equal("Never Ran", f.Component);   // only the draft, not the active process
        }

        // TC-TD-COL-05: a JScript web resource using a deprecated API is Medium (titled by token).
        [Fact]
        public void DeprecatedApi_XrmPage_Medium()
        {
            var fake = new FakeOrganizationService().Seed("webresource",
                Web(3, "form_lib.js", content: B64("function onLoad(){ Xrm.Page.getAttribute('name'); }")));

            var f = Assert.Single(new DeprecatedApiAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Deprecated API: Xrm.Page");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // TC-TD-COL-06: two web resources sharing a display name flag a Low duplicate finding.
        [Fact]
        public void DuplicateArtifacts_SharedDisplayName_Low()
        {
            var fake = new FakeOrganizationService().Seed("webresource",
                Web(3, "ribbon_a.js", displayName: "Ribbon Helper"),
                Web(3, "ribbon_b.js", displayName: "Ribbon Helper"),
                Web(3, "unique.js", displayName: "Unique"));

            var f = Assert.Single(new DuplicateArtifactsAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Duplicate web-resource display name");
            Assert.Equal(Severity.Low, f.Severity);
        }

        // TC-TD-COL-07: a "Copy of …" security role is flagged Low (unmanaged security drift).
        [Fact]
        public void Security_CopiedRole_Low()
        {
            var fake = new FakeOrganizationService().Seed("role",
                Role("Copy of Sales Manager"),
                Role("Sales Manager"));

            var f = Assert.Single(new SecurityAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Ad-hoc copied security role");
            Assert.Equal(Severity.Low, f.Severity);
        }
    }
}
