using System.Linq;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.TechnicalDebtAnalyzer.Analysis;

namespace XrmToolSuite.CollectorTests
{
    /// <summary>
    /// Headless tests for the Technical Debt Analyzer's metadata-driven branches: custom-table row counts
    /// (aggregate FetchXML), wide tables, default-publisher prefixes on tables/columns, and secured-column
    /// sprawl. These seed <see cref="Microsoft.Xrm.Sdk.Metadata.EntityMetadata"/> via <see cref="MetaBuilder"/>
    /// (reflection) and use the fake's row-count support. Traces to US-TD-2/3/4. TC-TD-COL-08..11.
    /// </summary>
    public class TechDebtMetadataTests
    {
        private static TechDebtContext Ctx(FakeOrganizationService fake) => new TechDebtContext(fake, "TEST");

        // TC-TD-COL-08: a custom table with zero rows is flagged Medium (candidate for removal).
        [Fact]
        public void Unused_CustomTableNoRows_Medium()
        {
            var fake = new FakeOrganizationService()
                .SeedAllEntities(MetaBuilder.Entity("new_thing", schemaName: "new_thing", displayName: "Thing"))
                .SeedRowCount("new_thing", 0);

            var f = Assert.Single(new UnusedMetadataAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Custom table has no data");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // TC-TD-COL-09: a custom table with 200+ custom columns is flagged Low (very wide).
        [Fact]
        public void Unused_VeryWideTable_Low()
        {
            var attrs = Enumerable.Range(0, 200).Select(i => MetaBuilder.Attribute("new_col" + i)).ToArray();
            var fake = new FakeOrganizationService()
                .SeedAllEntities(MetaBuilder.Entity("new_wide", schemaName: "new_wide"))
                .SeedEntityDetail("new_wide", MetaBuilder.Entity("new_wide", attributes: attrs))
                .SeedRowCount("new_wide", 10); // non-zero, so the "no data" branch stays quiet

            Assert.Contains(new UnusedMetadataAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Very wide custom table" && x.Severity == Severity.Low);
        }

        // TC-TD-COL-10: default 'new_' prefix + missing description + prefixed columns all flag on a custom table.
        [Fact]
        public void Naming_DefaultPrefixAndNoDescription_Flag()
        {
            var fake = new FakeOrganizationService()
                .SeedAllEntities(MetaBuilder.Entity("new_widget", schemaName: "new_widget")) // no description
                .SeedEntityDetail("new_widget", MetaBuilder.Entity("new_widget",
                    attributes: new[] { MetaBuilder.Attribute("new_field") }));

            var findings = new NamingViolationsAnalyzer().Analyze(Ctx(fake), _ => { });
            Assert.Contains(findings, x => x.Title == "Default publisher prefix on table" && x.Severity == Severity.Low);
            Assert.Contains(findings, x => x.Title == "Table has no description" && x.Severity == Severity.Info);
            Assert.Contains(findings, x => x.Title == "Default publisher prefix on columns" && x.Severity == Severity.Low);
        }

        // TC-TD-COL-11: secured columns on custom tables raise an informational field-security finding.
        [Fact]
        public void Security_SecuredColumns_Info()
        {
            var fake = new FakeOrganizationService()
                .SeedAllEntities(MetaBuilder.Entity("new_secure", schemaName: "new_secure"))
                .SeedEntityDetail("new_secure", MetaBuilder.Entity("new_secure",
                    attributes: new[] { MetaBuilder.Attribute("f1", isSecured: true), MetaBuilder.Attribute("f2", isSecured: true) }));

            Assert.Contains(new SecurityAnalyzer().Analyze(Ctx(fake), _ => { }),
                x => x.Title == "Field-level security in use" && x.Severity == Severity.Info);
        }
    }
}
