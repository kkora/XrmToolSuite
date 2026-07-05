using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.AuditComplianceChecker.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the SDK-free audit-compliance engine: <see cref="SensitivityHeuristics"/>
    /// classification and <see cref="AuditComplianceRules.Evaluate"/> (findings, deterministic scoring,
    /// banding, category breakdown, activity rules). All rules are pure functions of populated
    /// <see cref="AuditCoverage"/>/<see cref="AuditActivitySummary"/> models, so exact verdicts are
    /// asserted with no live org. Traces to EPIC-SEC5 (US-SEC5.1.x coverage, US-SEC5.2.x activity,
    /// US-SEC5.4.1 score, US-SEC5.5.1 recommendations).
    /// </summary>
    public class AuditComplianceCheckerTests
    {
        // ---- fixtures ---------------------------------------------------------------------------

        private static ColumnAudit Col(string name, string type, bool audit, bool sensitive) =>
            new ColumnAudit { LogicalName = name, Type = type, IsAuditEnabled = audit, IsSensitive = sensitive };

        private static TableAudit Table(string logical, bool audit, bool sensitive, params ColumnAudit[] cols) =>
            new TableAudit
            {
                LogicalName = logical,
                DisplayName = logical,
                IsAuditEnabled = audit,
                IsSensitive = sensitive,
                Columns = cols.ToList()
            };

        /// <summary>A fully compliant environment: org on, sensitive table + column audited.</summary>
        private static AuditCoverage HealthyCoverage() => new AuditCoverage
        {
            OrgAuditEnabled = true,
            Tables = new List<TableAudit>
            {
                Table("contact", audit: true, sensitive: true,
                    Col("emailaddress1", "String", audit: true, sensitive: true),
                    Col("firstname", "String", audit: true, sensitive: false)),
                Table("account", audit: true, sensitive: false)
            }
        };

        // ---- SensitivityHeuristics (US-SEC5.1.2) ------------------------------------------------

        [Theory]
        [InlineData("cr123_ssn", true)]
        [InlineData("new_employeesalary", true)]
        [InlineData("contact_bankaccount", true)]
        [InlineData("passportnumber", true)]
        [InlineData("account", false)]
        [InlineData("cr123_color", false)]
        public void IsSensitiveTable_ClassifiesByName(string logical, bool expected) =>
            Assert.Equal(expected, SensitivityHeuristics.IsSensitiveTable(logical));

        [Theory]
        [InlineData("emailaddress1", "String", true)]   // name pattern
        [InlineData("mobilephone", "String", true)]     // name pattern
        [InlineData("annualrevenue", "Money", true)]    // type: money is inherently sensitive
        [InlineData("description", "Memo", false)]      // neither
        [InlineData("cr123_dob", "DateTime", true)]     // name pattern (dob)
        public void IsSensitiveColumn_ClassifiesByNameOrType(string logical, string type, bool expected) =>
            Assert.Equal(expected, SensitivityHeuristics.IsSensitiveColumn(logical, type));

        // ---- org audit disabled -> Critical (US-SEC5.1.1) ---------------------------------------

        [Fact]
        public void OrgAuditDisabled_YieldsCritical_AndLowBand()
        {
            var cov = HealthyCoverage();
            cov.OrgAuditEnabled = false;

            var report = AuditComplianceRules.Evaluate(cov, null);

            var f = report.Findings.Single(x => x.Title == "Organization auditing is disabled");
            Assert.Equal(Severity.Critical, f.Severity);
            Assert.Equal(ScoreBand.Low, report.Band); // org-off caps the score into Low
            Assert.True(report.Score <= AuditComplianceRules.OrgDisabledCap);
        }

        // ---- sensitive table without audit -> High (US-SEC5.1.2) --------------------------------

        [Fact]
        public void SensitiveTableWithoutAudit_YieldsHigh()
        {
            var cov = new AuditCoverage
            {
                OrgAuditEnabled = true,
                Tables = new List<TableAudit> { Table("cr123_patientssn", audit: false, sensitive: true) }
            };

            var report = AuditComplianceRules.Evaluate(cov, null);

            var f = report.Findings.Single(x => x.Title == "Sensitive table is not audited");
            Assert.Equal(Severity.High, f.Severity);
            Assert.Equal("cr123_patientssn", f.Component);
            Assert.False(string.IsNullOrEmpty(f.Recommendation)); // remediation present (US-SEC5.5.1)
        }

        // ---- sensitive column without audit on an audited table -> Medium (US-SEC5.1.2) ---------

        [Fact]
        public void SensitiveColumnWithoutAudit_OnAuditedTable_YieldsMedium()
        {
            var cov = new AuditCoverage
            {
                OrgAuditEnabled = true,
                Tables = new List<TableAudit>
                {
                    Table("contact", audit: true, sensitive: true,
                        Col("cr123_ssn", "String", audit: false, sensitive: true))
                }
            };

            var report = AuditComplianceRules.Evaluate(cov, null);

            var f = report.Findings.Single(x => x.Title == "Sensitive column is not audited");
            Assert.Equal(Severity.Medium, f.Severity);
            Assert.Equal("contact.cr123_ssn", f.Component);
        }

        // ---- all covered -> good (US-SEC5.4.1) --------------------------------------------------

        [Fact]
        public void AllCovered_YieldsHighScoreAndBand_WithCategoryBreakdown()
        {
            var report = AuditComplianceRules.Evaluate(HealthyCoverage(), null);

            Assert.Equal(ScoreBand.High, report.Band);
            Assert.True(report.Score >= AuditComplianceRules.HighThreshold, $"score was {report.Score}");
            Assert.DoesNotContain(report.Findings, f => f.Severity >= Severity.Medium);

            // Category breakdown present in the metrics (US-SEC5.4.1 AC).
            Assert.Contains(report.Metrics, m => m.Label == "Org config score");
            Assert.Contains(report.Metrics, m => m.Label == "Table coverage score");
            Assert.Contains(report.Metrics, m => m.Label == "Column coverage score");
            Assert.Contains(report.Metrics, m => m.Label == "Activity health score");
        }

        // ---- deterministic (US-SEC5.4.1 AC: deterministic from evidence) ------------------------

        [Fact]
        public void Evaluate_IsDeterministic_SameInputTwiceEqual()
        {
            var a = AuditComplianceRules.Evaluate(HealthyCoverage(), SampleActivity());
            var b = AuditComplianceRules.Evaluate(HealthyCoverage(), SampleActivity());

            Assert.Equal(a.Score, b.Score);
            Assert.Equal(a.Band, b.Band);
            Assert.Equal(a.Findings.Count, b.Findings.Count);
            Assert.Equal(
                a.Findings.Select(f => f.Title + "|" + f.Component),
                b.Findings.Select(f => f.Title + "|" + f.Component));
        }

        // ---- activity rules (US-SEC5.2.2) -------------------------------------------------------

        private static AuditActivitySummary SampleActivity() => new AuditActivitySummary
        {
            TotalRecords = 500,
            DeleteCount = 250,          // over the default threshold of 100
            SecurityChangeCount = 3,
            AfterHoursCount = 10
        };

        [Fact]
        public void ActivityRules_Fire_ForDeletesSecurityAndAfterHours()
        {
            var report = AuditComplianceRules.Evaluate(HealthyCoverage(), SampleActivity());

            Assert.Contains(report.Findings, f => f.Title == "High delete volume" && f.Severity == Severity.Medium);
            Assert.Contains(report.Findings, f => f.Title == "Security-role / privilege changes detected" && f.Severity == Severity.Medium);
            Assert.Contains(report.Findings, f => f.Title == "After-hours changes detected" && f.Severity == Severity.Low);
        }

        [Fact]
        public void HighDeleteVolume_RespectsCustomThreshold()
        {
            var activity = new AuditActivitySummary { TotalRecords = 20, DeleteCount = 15 };

            var report = AuditComplianceRules.Evaluate(HealthyCoverage(), activity,
                new AuditComplianceOptions { HighDeleteVolumeThreshold = 10 });

            Assert.Contains(report.Findings, f => f.Title == "High delete volume");
        }

        [Fact]
        public void NoActivity_DoesNotFireActivityFindings()
        {
            var report = AuditComplianceRules.Evaluate(HealthyCoverage(), null);
            Assert.DoesNotContain(report.Findings, f => f.Component == "Activity");
        }
    }
}
