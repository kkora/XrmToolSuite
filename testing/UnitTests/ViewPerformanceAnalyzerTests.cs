using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.ViewPerformanceAnalyzer.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the View Performance Analyzer's pure logic — the LayoutXML parser and the
    /// per-view scorer that reuses the shared FetchXML engine (US-PERF4.2.x / 4.3.x / 4.4.x). No
    /// Dataverse, no WinForms. The SDK collector (ViewCollector) is manual-tested against a live org.
    /// </summary>
    public class ViewPerformanceAnalyzerTests
    {
        // A filtered, bounded, lean query — the shared engine finds no risks (single Info, cost 0).
        private const string LeanFetch =
            "<fetch top='50'><entity name='account'>" +
            "<attribute name='name' />" +
            "<filter><condition attribute='statecode' operator='eq' value='0' /></filter>" +
            "</entity></fetch>";

        private static string Layout(int columns)
        {
            var cells = string.Concat(Enumerable.Range(0, columns)
                .Select(i => $"<cell name='col{i}' width='100' />"));
            return "<grid name='resultset' object='1' jump='name' select='1' icon='1' preview='1'>" +
                   $"<row name='result' id='accountid'>{cells}</row></grid>";
        }

        // ---------------------------------------------------------------- LayoutXmlParser (US-PERF4.3.1)

        [Fact]
        public void LayoutXml_Normal_CountsDisplayedCells()
        {
            var xml = Layout(4);
            Assert.Equal(4, LayoutXmlParser.CountColumns(xml));
            Assert.Equal(new[] { "col0", "col1", "col2", "col3" }, LayoutXmlParser.Columns(xml).ToArray());
        }

        [Fact]
        public void LayoutXml_HiddenCell_IsNotCounted()
        {
            var xml = "<grid><row><cell name='visible' /><cell name='secret' ishidden='1' /></row></grid>";
            Assert.Equal(1, LayoutXmlParser.CountColumns(xml));
            Assert.DoesNotContain("secret", LayoutXmlParser.Columns(xml));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("<grid><row><cell name='a' ></row></grid>")] // malformed (unclosed cell)
        public void LayoutXml_BlankOrMalformed_ReturnsZero(string xml)
        {
            Assert.Equal(0, LayoutXmlParser.CountColumns(xml));
            Assert.Empty(LayoutXmlParser.Columns(xml));
        }

        // ---------------------------------------------------------------- ViewScorer (US-PERF4.4.1)

        // Heavy view: all-attributes + no filter + many links + a wide layout => High band.
        [Fact]
        public void Analyze_HeavyView_ScoresHigh()
        {
            var links = string.Concat(Enumerable.Range(0, 5).Select(i =>
                $"<link-entity name='contact{i}' from='parentcustomerid' to='accountid' />"));
            var fetch = $"<fetch><entity name='account'><all-attributes />{links}</entity></fetch>";

            var v = ViewScorer.Analyze("Heavy View", "System", "account", fetch, Layout(20));

            Assert.Equal(ScoreBand.High, v.Band);
            Assert.True(v.Score >= 40, $"expected High-band score, got {v.Score}");
            Assert.True(v.AllAttributes);
            Assert.Equal(5, v.LinkCount);
            Assert.Equal(20, v.LayoutColumnCount);
            // Reused engine findings are present...
            Assert.Contains(v.Findings, f => f.Category == "FetchXML" && f.Severity == Severity.High);
            // ...alongside this tool's own over-wide layout finding.
            Assert.Contains(v.Findings, f => f.Category == "View" && f.Title.Contains("Over-wide"));
        }

        // Lean view: filtered, bounded, narrow layout => Low band, no risk findings.
        [Fact]
        public void Analyze_LeanView_ScoresLow()
        {
            var v = ViewScorer.Analyze("Lean View", "System", "account", LeanFetch, Layout(3));

            Assert.Equal(ScoreBand.Low, v.Band);
            Assert.Equal(0, v.Score);
            Assert.DoesNotContain(v.Findings, f => f.Severity >= Severity.Medium);
            Assert.Contains(v.Findings, f => f.Severity == Severity.Info);
        }

        // US-PERF4.3.1 — an over-wide layout on an otherwise-lean view raises a Medium View finding.
        [Fact]
        public void Analyze_WideLayout_RaisesMediumFinding()
        {
            var v = ViewScorer.Analyze("Wide View", "Personal", "account", LeanFetch, Layout(25));

            Assert.Contains(v.Findings, f =>
                f.Category == "View" && f.Severity == Severity.Medium && f.Title.Contains("Over-wide"));
            Assert.True(v.Score > 0); // the labeled layout penalty lifts the score above the lean baseline
            // Regression: a view flagged for a wide layout must NOT also carry the reused engine's
            // "No performance risks detected" placeholder (a self-contradictory finding).
            Assert.DoesNotContain(v.Findings, f => f.Title == "No performance risks detected");
        }

        // Regression: a view whose FetchXML can't be parsed but which has a genuinely wide layout must still
        // surface the over-wide-layout risk (and score it) — the parse failure must not hide it (US-PERF4.3.1).
        [Fact]
        public void Analyze_ParseFailureButWideLayout_StillFlagsAndScoresLayout()
        {
            var v = ViewScorer.Analyze("Broken Wide", "System", "account",
                "<fetch><entity name='account'></fetch>", Layout(40));

            Assert.Contains(v.Findings, f => f.Category == "View" && f.Title.Contains("Over-wide"));
            Assert.Contains(v.Findings, f => f.Title.Contains("could not be parsed"));
            Assert.True(v.Score > 0); // the layout penalty still counts even though the query cost is unknown
        }

        // US-PERF4.2.1 — an unparseable FetchXML degrades to a single Info note and score 0 (never throws).
        [Fact]
        public void Analyze_ParseFailure_IsInfoAndScoreZero()
        {
            var v = ViewScorer.Analyze("Broken View", "System", "account",
                "<fetch><entity name='account'></fetch>", Layout(2));

            Assert.Equal(0, v.Score);
            Assert.Equal(ScoreBand.Low, v.Band);
            Assert.Single(v.Findings);
            Assert.Equal(Severity.Info, v.Findings[0].Severity);
            Assert.Contains("could not be parsed", v.Findings[0].Title);
            // Layout is parsed independently of the fetch, so its columns are still available.
            Assert.Equal(2, v.LayoutColumnCount);
        }

        // US-PERF4.4.2 — Rank orders the analyzed views by score, worst first.
        [Fact]
        public void Rank_OrdersByScoreDescending()
        {
            var links = string.Concat(Enumerable.Range(0, 5).Select(i =>
                $"<link-entity name='c{i}' from='parentcustomerid' to='accountid' />"));
            var heavy = ViewScorer.Analyze("Heavy", "System", "account",
                $"<fetch><entity name='account'><all-attributes />{links}</entity></fetch>", Layout(20));
            var lean = ViewScorer.Analyze("Lean", "System", "account", LeanFetch, Layout(3));
            var mid = ViewScorer.Analyze("Mid", "System", "account",
                "<fetch><entity name='account'><attribute name='name' /></entity></fetch>", Layout(3));

            var ranked = ViewScorer.Rank(new[] { lean, heavy, mid });

            Assert.Equal("Heavy", ranked[0].Name);
            Assert.Equal("Lean", ranked[2].Name);
            for (int i = 1; i < ranked.Count; i++)
                Assert.True(ranked[i - 1].Score >= ranked[i].Score);
        }
    }
}
