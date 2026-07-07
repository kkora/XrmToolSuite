using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.FetchXml;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the shared FetchXML parser + rule engine (US-PERF03.2.x / 3.3.x / 3.4.1).
    /// Pure logic — no Dataverse, no WinForms.
    /// </summary>
    public class FetchXmlAnalyzerTests
    {
        private const string SimpleFiltered =
            "<fetch top='50'><entity name='account'>" +
            "<attribute name='name' />" +
            "<filter><condition attribute='statecode' operator='eq' value='0' /></filter>" +
            "</entity></fetch>";

        // US-PERF03.2.1 — parser breaks the query into shape with counts.
        [Fact]
        public void Parse_Success_PopulatesSummaryCounts()
        {
            var xml =
                "<fetch distinct='true' no-lock='true' top='10'><entity name='account'>" +
                "<attribute name='name' />" +
                "<attribute name='revenue' />" +
                "<filter><condition attribute='statecode' operator='eq' value='0' /></filter>" +
                "<order attribute='name' />" +
                "<link-entity name='contact' from='parentcustomerid' to='accountid' link-type='outer'>" +
                "<attribute name='fullname' />" +
                "<order attribute='fullname' descending='true' />" +
                "</link-entity>" +
                "</entity></fetch>";

            var result = FetchXmlParser.Parse(xml);

            Assert.True(result.Success);
            var q = result.Query;
            Assert.Equal("account", q.RootEntity);
            Assert.Equal(3, q.TotalAttributeCount);   // name, revenue, fullname
            Assert.Equal(2, q.Attributes.Count);      // root-only attributes
            Assert.True(q.HasRootFilter);
            Assert.Equal(1, q.LinkCount);
            Assert.True(q.Links[0].IsOuter);
            Assert.Equal(2, q.Orders.Count);
            Assert.Contains(q.Orders, o => o.OnLinkEntity);
            Assert.True(q.Distinct);
            Assert.True(q.NoLock);
            Assert.Equal(10, q.Top);
        }

        // US-PERF03.2.1 — nested link-entities are counted at every depth.
        [Fact]
        public void Parse_NestedLinks_CountsAllDepths()
        {
            var xml =
                "<fetch><entity name='account'>" +
                "<link-entity name='contact' from='parentcustomerid' to='accountid'>" +
                "<link-entity name='task' from='regardingobjectid' to='contactid' />" +
                "</link-entity></entity></fetch>";

            var q = FetchXmlParser.Parse(xml).Query;
            Assert.Equal(2, q.LinkCount);
        }

        // US-PERF03.1.1 — invalid FetchXML yields a clear parse error, not an exception.
        [Fact]
        public void Parse_MalformedXml_ReturnsError()
        {
            var result = FetchXmlParser.Parse("<fetch><entity name='account'></fetch>");
            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
        }

        // US-PERF03.1.1 — non-fetch root is rejected.
        [Fact]
        public void Parse_MissingFetchRoot_ReturnsError()
        {
            var result = FetchXmlParser.Parse("<root><entity name='account' /></root>");
            Assert.False(result.Success);
            Assert.Contains("fetch", result.Error);
        }

        // US-PERF03.3.1 — <all-attributes/> is High.
        [Fact]
        public void Analyze_AllAttributes_IsHigh()
        {
            var q = FetchXmlParser.Parse(
                "<fetch top='10'><entity name='account'><all-attributes />" +
                "<filter><condition attribute='statecode' operator='eq' value='0' /></filter>" +
                "</entity></fetch>").Query;

            var a = FetchXmlRules.Analyze(q);
            Assert.Contains(a.Findings, f => f.Severity == Severity.High && f.Title.Contains("all attributes"));
            Assert.Contains(a.Suggestions, s => s.Contains("all-attributes"));
        }

        // US-PERF03.3.2 — missing root filter is High.
        [Fact]
        public void Analyze_MissingFilter_IsHigh()
        {
            var q = FetchXmlParser.Parse(
                "<fetch top='10'><entity name='account'><attribute name='name' /></entity></fetch>").Query;

            var a = FetchXmlRules.Analyze(q);
            Assert.Contains(a.Findings, f => f.Severity == Severity.High && f.Title.Contains("No filter"));
        }

        // US-PERF03.3.3 — link-entity count over MaxLinkEntities is High.
        [Fact]
        public void Analyze_ExcessiveLinks_IsHigh()
        {
            var links = string.Concat(Enumerable.Range(0, 5).Select(i =>
                $"<link-entity name='contact{i}' from='parentcustomerid' to='accountid' />"));
            var q = FetchXmlParser.Parse(
                $"<fetch top='10'><entity name='account'>" +
                "<filter><condition attribute='statecode' operator='eq' value='0' /></filter>" +
                links + "</entity></fetch>").Query;

            var a = FetchXmlRules.Analyze(q);
            Assert.Equal(5, q.LinkCount);
            Assert.Contains(a.Findings, f => f.Severity == Severity.High && f.Title.Contains("Excessive"));
        }

        // US-PERF03.3.3 — several (but not excessive) links is Medium.
        [Fact]
        public void Analyze_SeveralLinks_IsMedium()
        {
            var links = string.Concat(Enumerable.Range(0, 3).Select(i =>
                $"<link-entity name='contact{i}' from='parentcustomerid' to='accountid' />"));
            var q = FetchXmlParser.Parse(
                $"<fetch top='10'><entity name='account'>" +
                "<filter><condition attribute='statecode' operator='eq' value='0' /></filter>" +
                links + "</entity></fetch>").Query;

            var a = FetchXmlRules.Analyze(q);
            Assert.Contains(a.Findings, f => f.Severity == Severity.Medium && f.Title.Contains("Several"));
        }

        // US-PERF03.4.1 — cost estimate accumulates and bands High; multiple High findings exceed 40.
        [Fact]
        public void Analyze_CostAndBand_ComputedFromFindings()
        {
            // all-attributes (High=12) + no filter (High=12) + excessive links (High=12) + outer (Low=2) => 38+ ...
            var links = string.Concat(Enumerable.Range(0, 5).Select(i =>
                $"<link-entity name='c{i}' from='parentcustomerid' to='accountid' link-type='outer' />"));
            var q = FetchXmlParser.Parse(
                $"<fetch><entity name='account'><all-attributes />{links}</entity></fetch>").Query;

            var a = FetchXmlRules.Analyze(q);
            Assert.True(a.CostEstimate > 0);
            Assert.True(a.CostEstimate <= 100);
            Assert.Equal(ScoreBand.High, a.Band);
            Assert.Equal(ScoreCalculator.BandFor(a.CostEstimate, 15, 40), a.Band);
        }

        // US-PERF03.4.1 — a clean, bounded query has no risks and lands in the Low band.
        [Fact]
        public void Analyze_NoIssues_IsInfoAndLowBand()
        {
            var q = FetchXmlParser.Parse(SimpleFiltered).Query;

            var a = FetchXmlRules.Analyze(q);
            Assert.Single(a.Findings);
            Assert.Equal(Severity.Info, a.Findings[0].Severity);
            Assert.Contains("No performance risks", a.Findings[0].Title);
            Assert.Equal(ScoreBand.Low, a.Band);
            Assert.Equal(0, a.CostEstimate);
        }
    }
}
