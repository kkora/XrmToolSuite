using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.AttributeAuditor.Audit;

namespace XrmToolSuite.CollectorTests
{
    /// <summary>
    /// Headless tests for the Attribute Auditor's Dataverse collector. It audits custom columns and marks
    /// usage from forms, views, processes, and field security, all over <see cref="IOrganizationService"/>,
    /// so it runs against the shared <see cref="FakeOrganizationService"/> with metadata seeded via
    /// <see cref="MetaBuilder"/>. The reference-detection scanners and classification are unit-tested SDK-free
    /// in testing/UnitTests. Traces to US-ADMIN10-2 / US-ADMIN10-3. TC-ADMIN10-COL-01..08.
    /// </summary>
    public class AttributeAuditCollectorTests
    {
        private static AuditResult Run(FakeOrganizationService fake, bool customOnly = true) =>
            AttributeUsageCollector.Collect(new AttributeAuditContext(fake, "DEV"), customOnly, _ => { });

        private static FakeOrganizationService WithEntity(params AttributeMetadata[] attrs) =>
            new FakeOrganizationService().SeedAllEntities(MetaBuilder.Entity("new_widget", isCustom: true, attributes: attrs));

        private static Entity Form(string entity, string name, string formXml) =>
            new Entity("systemform", System.Guid.NewGuid()) { ["objecttypecode"] = entity, ["name"] = name, ["formxml"] = formXml };

        private static Entity View(string entity, string name, string fetchXml = null, string layoutXml = null)
        {
            var e = new Entity("savedquery", System.Guid.NewGuid()) { ["returnedtypecode"] = entity, ["name"] = name };
            if (fetchXml != null) e["fetchxml"] = fetchXml;
            if (layoutXml != null) e["layoutxml"] = layoutXml;
            return e;
        }

        private static Entity Workflow(string entity, string name, string xaml) =>
            new Entity("workflow", System.Guid.NewGuid()) { ["primaryentity"] = entity, ["name"] = name, ["xaml"] = xaml };

        // TC-ADMIN10-COL-01: a custom column with no usage evidence is a retirement candidate.
        [Fact]
        public void UnusedCustomColumn_IsCandidate()
        {
            var r = Run(WithEntity(MetaBuilder.Attribute("new_unused")));
            var col = Assert.Single(r.Columns);
            Assert.False(col.IsUsed);
            Assert.True(col.IsRetirementCandidate);
            Assert.Equal(1, r.CandidateColumns);
        }

        // TC-ADMIN10-COL-02: a column bound to a form control is used (Form evidence).
        [Fact]
        public void ColumnOnForm_IsUsed()
        {
            var fake = WithEntity(MetaBuilder.Attribute("new_onform"))
                .Seed("systemform", Form("new_widget", "Main", "<form><control datafieldname='new_onform'/></form>"));
            var col = Assert.Single(Run(fake).Columns);
            Assert.True(col.IsUsed);
            Assert.Contains(UsageSignal.Form, col.Signals);
            Assert.Contains("Main", col.UsageSummary());
        }

        // TC-ADMIN10-COL-03: a column in a view's fetchxml is used (View evidence).
        [Fact]
        public void ColumnInViewFetch_IsUsed()
        {
            var fake = WithEntity(MetaBuilder.Attribute("new_inview"))
                .Seed("savedquery", View("new_widget", "Active", fetchXml: "<fetch><entity name='new_widget'><attribute name='new_inview'/></entity></fetch>"));
            Assert.Contains(UsageSignal.View, Assert.Single(Run(fake).Columns).Signals);
        }

        // TC-ADMIN10-COL-04: a column shown only via layoutxml (grid cell) is also detected as used.
        [Fact]
        public void ColumnInViewLayout_IsUsed()
        {
            var fake = WithEntity(MetaBuilder.Attribute("new_ingrid"))
                .Seed("savedquery", View("new_widget", "Grid", layoutXml: "<grid><row><cell name='new_ingrid'/></row></grid>"));
            Assert.False(Assert.Single(Run(fake).Columns).IsRetirementCandidate);
        }

        // TC-ADMIN10-COL-05: a column referenced in a workflow/flow definition is used (Process evidence).
        [Fact]
        public void ColumnInProcess_IsUsed()
        {
            var fake = WithEntity(MetaBuilder.Attribute("new_inflow"))
                .Seed("workflow", Workflow("new_widget", "Nightly", "<Activity>... set \"new_inflow\" ...</Activity>"));
            Assert.Contains(UsageSignal.Process, Assert.Single(Run(fake).Columns).Signals);
        }

        // TC-ADMIN10-COL-06: a field-secured column is used (FieldSecurity evidence) with no query needed.
        [Fact]
        public void FieldSecuredColumn_IsUsed()
        {
            var col = Assert.Single(Run(WithEntity(MetaBuilder.Attribute("new_secure", isSecured: true))).Columns);
            Assert.Contains(UsageSignal.FieldSecurity, col.Signals);
            Assert.False(col.IsRetirementCandidate);
        }

        // TC-ADMIN10-COL-07: a managed custom column with no usage is never a retirement candidate.
        [Fact]
        public void ManagedCustomColumn_NeverCandidate()
        {
            var col = Assert.Single(Run(WithEntity(MetaBuilder.Attribute("new_managed", isManaged: true))).Columns);
            Assert.True(col.IsManaged);
            Assert.False(col.IsRetirementCandidate);
        }

        // TC-ADMIN10-COL-08: a form referencing a NON-audited (system) column leaves the custom candidate untouched;
        //               and the custom-entities-only scope excludes custom columns on system tables.
        [Fact]
        public void SystemColumnReference_AndScopeFilter()
        {
            // A form binds only 'name' (a system column) — the custom column stays a candidate.
            var fake = WithEntity(MetaBuilder.Attribute("new_unused"))
                .Seed("systemform", Form("new_widget", "Main", "<form><control datafieldname='name'/></form>"));
            Assert.True(Assert.Single(Run(fake).Columns).IsRetirementCandidate);

            // A custom column on a SYSTEM table: excluded when customEntitiesOnly, included otherwise.
            var onSystem = new FakeOrganizationService()
                .SeedAllEntities(MetaBuilder.Entity("account", isCustom: false, attributes: new[] { MetaBuilder.Attribute("new_oncore") }));
            Assert.Empty(Run(onSystem, customOnly: true).Columns);
            Assert.Single(Run(onSystem, customOnly: false).Columns);
        }
    }
}
