using System.Linq;
using Xunit;
using XrmToolSuite.AttributeAuditor.Audit;
using XrmToolSuite.AttributeAuditor.Reporting;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Attribute Auditor engine: usage scanners, retirement-candidate
    /// classification, the shared-ReportModel projection, and the CSV export. The Dataverse collector
    /// that feeds these is covered separately in CollectorTests. Traces to US-AA-2 / US-AA-3 / US-AA-4.
    /// </summary>
    public class AttributeAuditTests
    {
        // ---- usage scanners (US-AA-2) ----

        [Fact]
        public void FormColumns_ExtractsDataFieldNames()
        {
            var xml = "<form><tab><section><rows>" +
                      "<row><cell><control id='new_a' datafieldname='new_a'/></cell></row>" +
                      "<row><cell><control id='sub' classid='{E7A81278}'/></cell></row>" + // subgrid, no field
                      "<row><cell><control datafieldname='new_b'/></cell></row>" +
                      "</rows></section></tab></form>";
            var cols = UsageScanners.FormColumns(xml);
            Assert.Equal(new[] { "new_a", "new_b" }, cols.OrderBy(x => x));
        }

        [Fact]
        public void FetchColumns_ExtractsAttributesConditionsOrders()
        {
            var xml = "<fetch><entity name='account'>" +
                      "<attribute name='new_a'/>" +
                      "<filter><condition attribute='new_b' operator='eq' value='1'/></filter>" +
                      "<order attribute='new_c' descending='true'/>" +
                      "</entity></fetch>";
            var cols = UsageScanners.FetchColumns(xml);
            Assert.Equal(new[] { "new_a", "new_b", "new_c" }, cols.OrderBy(x => x));
        }

        [Fact]
        public void LayoutColumns_ExtractsCellNames()
        {
            var xml = "<grid><row><cell name='new_a' width='100'/><cell name='new_b' width='120'/></row></grid>";
            Assert.Equal(new[] { "new_a", "new_b" }, UsageScanners.LayoutColumns(xml).OrderBy(x => x));
        }

        [Theory]
        [InlineData("...set new_field to 1...", "new_field", true)]   // delimited by spaces
        [InlineData("\"new_field\"", "new_field", true)]              // delimited by quotes
        [InlineData("new_fieldextra", "new_field", false)]            // substring, not a whole token
        [InlineData("prefix_new_field", "new_field", false)]         // trailing part of a longer token
        [InlineData("", "new_field", false)]
        public void ReferencesToken_MatchesWholeTokenOnly(string body, string name, bool expected)
        {
            Assert.Equal(expected, UsageScanners.ReferencesToken(body, name));
        }

        // ---- classification (US-AA-3) ----

        private static ColumnAudit Col(bool custom = true, bool managed = false) =>
            new ColumnAudit { Table = "account", LogicalName = "new_x", DisplayName = "X", AttributeType = "String", IsCustom = custom, IsManaged = managed };

        [Fact]
        public void CustomUnmanagedWithNoEvidence_IsCandidate()
        {
            var c = Col();
            Assert.True(c.IsRetirementCandidate);
            Assert.Equal("No usage signals", c.UsageSummary());
        }

        [Fact]
        public void ColumnWithEvidence_IsUsed_NotCandidate()
        {
            var c = Col();
            c.Add(UsageSignal.Form, "Form: Account Main");
            Assert.True(c.IsUsed);
            Assert.False(c.IsRetirementCandidate);
            Assert.Contains("Account Main", c.UsageSummary());
        }

        [Fact]
        public void ManagedOrSystemColumn_IsNeverCandidate()
        {
            Assert.False(Col(custom: true, managed: true).IsRetirementCandidate);  // managed
            Assert.False(Col(custom: false).IsRetirementCandidate);                 // system
        }

        [Fact]
        public void Add_DeDupesIdenticalEvidence()
        {
            var c = Col();
            c.Add(UsageSignal.View, "View: Active");
            c.Add(UsageSignal.View, "View: Active");
            Assert.Single(c.Evidence);
        }

        [Fact]
        public void AuditResult_CountsAndGrouping()
        {
            var r = new AuditResult { EnvironmentName = "DEV" };
            var used = Col(); used.Add(UsageSignal.Form, "Form: A");
            r.Columns.Add(used);
            r.Columns.Add(Col());                                   // candidate on account
            r.Columns.Add(new ColumnAudit { Table = "contact", LogicalName = "new_y", IsCustom = true }); // candidate on contact

            Assert.Equal(3, r.TotalColumns);
            Assert.Equal(1, r.UsedColumns);
            Assert.Equal(2, r.CandidateColumns);
            Assert.Equal(2, r.CandidatesByTable().Count());
        }

        // ---- report projection (US-AA-4) ----

        [Fact]
        public void ToReportModel_ProjectsCandidatesAndMetrics()
        {
            var r = new AuditResult { EnvironmentName = "DEV" };
            var used = Col(); used.Add(UsageSignal.Process, "Flow: Nightly");
            r.Columns.Add(used);
            r.Columns.Add(Col());

            var m = AttributeAuditReport.ToReportModel(r);
            Assert.Equal("Attribute Auditor", m.ToolName);
            Assert.Equal("cleanup", m.ScoreWord);
            Assert.Equal(1, m.Score);                       // 1 candidate
            Assert.Equal(ScoreBand.Medium, m.Band);          // 1..9 -> Medium
            Assert.Single(m.Findings);                       // one finding per candidate
            Assert.Contains(m.Metrics, x => x.Label == "Retirement candidates" && x.Value == "1");
        }

        [Fact]
        public void ToReportModel_NoCandidates_BandsLow()
        {
            var r = new AuditResult { EnvironmentName = "DEV" };
            var used = Col(); used.Add(UsageSignal.Form, "Form: A");
            r.Columns.Add(used);

            var m = AttributeAuditReport.ToReportModel(r);
            Assert.Equal(0, m.Score);
            Assert.Equal(ScoreBand.Low, m.Band);
            Assert.Empty(m.Findings);
        }

        // ---- CSV export (US-AA-4.1) ----

        [Fact]
        public void Csv_HasHeaderAndRow()
        {
            var r = new AuditResult { EnvironmentName = "DEV" };
            r.Columns.Add(Col());
            var csv = AuditCsvExporter.ToCsv(r);
            Assert.StartsWith("Table,Column,DisplayName,Type,Custom,Managed,Used,Usage", csv);
            Assert.Contains("account,new_x,X,String,yes,no,no,No usage signals", csv);
        }

        [Fact]
        public void Csv_QuotesFieldsWithCommas()
        {
            var r = new AuditResult { EnvironmentName = "DEV" };
            var c = Col(); c.Add(UsageSignal.View, "View: Active, Open"); // comma in evidence
            r.Columns.Add(c);
            var csv = AuditCsvExporter.ToCsv(r);
            Assert.Contains("\"View: Active, Open\"", csv);
        }

        // Regression: a display name Excel would read as a formula (leading =, +, -, @ or tab) is neutralized
        // with a leading apostrophe (this exporter even writes a BOM for Excel).
        [Fact]
        public void Csv_NeutralizesFormulaInjection()
        {
            var r = new AuditResult { EnvironmentName = "DEV" };
            r.Columns.Add(new ColumnAudit
            {
                Table = "account", LogicalName = "new_x",
                DisplayName = "=HYPERLINK(\"http://evil\")", AttributeType = "String", IsCustom = true
            });
            var csv = AuditCsvExporter.ToCsv(r);
            Assert.Contains("'=HYPERLINK", csv);       // apostrophe-prefixed => read as text
            Assert.DoesNotContain(",=HYPERLINK", csv); // never starts a cell with '='
        }
    }
}
