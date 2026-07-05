using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.FormPerformanceAnalyzer.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Form Performance Analyzer's pure logic — the FormXML parser (structural
    /// counts + malformed tolerance) and the deterministic scorer (composite score, band thresholds,
    /// findings, and recommendations). No Dataverse, no WinForms. The SDK collector (FormCollector) is
    /// manual-tested against a live org. Traces to US-PERF10.1.2 / 10.2.x / 10.3.x / 10.4.x.
    /// </summary>
    public class FormPerformanceAnalyzerTests
    {
        // Well-known control classids used by the fixtures (must match FormXmlParser).
        private const string SubgridId = "{E7A81278-8635-4d9e-8D4D-59480B391C5B}";
        private const string QuickViewId = "{5C5600E0-1D6E-4205-A272-BE48DA2CA630}";
        private const string FieldId = "{4273EDBD-AC1D-40d3-9FB2-095C621B552D}";
        private const string CustomId = "{AAAAAAAA-1111-2222-3333-444444444444}";

        /// <summary>
        /// Builds a well-formed main FormXML from component counts. The parser counts by descendant name, so
        /// the (deliberately flat) structure still exercises every counter. Custom controls carry a
        /// &lt;customControl&gt; binding and no datafieldname (dataset PCF), so they never inflate the field count.
        /// </summary>
        private static string BuildForm(
            int tabs, int hiddenTabs, int sections, int visibleFields, int hiddenFields,
            int subgrids, int quickViews, int customControls,
            IEnumerable<string> libraries = null,
            int onload = 0, int onchange = 0, int tabstate = 0)
        {
            var libs = (libraries ?? Enumerable.Empty<string>()).ToList();
            var sb = new StringBuilder();
            sb.Append("<form><tabs>");
            for (int i = 0; i < tabs; i++)
                sb.Append($"<tab name='t{i}' visible='{(i < hiddenTabs ? "false" : "true")}' />");
            sb.Append("</tabs>");

            sb.Append("<sections>");
            for (int i = 0; i < sections; i++) sb.Append($"<section name='s{i}' />");
            sb.Append("</sections>");

            sb.Append("<controls>");
            for (int i = 0; i < visibleFields; i++)
                sb.Append($"<cell visible='true'><control id='vf{i}' classid='{FieldId}' datafieldname='vf{i}' /></cell>");
            for (int i = 0; i < hiddenFields; i++)
                sb.Append($"<cell visible='false'><control id='hf{i}' classid='{FieldId}' datafieldname='hf{i}' /></cell>");
            for (int i = 0; i < subgrids; i++)
                sb.Append($"<cell visible='true'><control id='sg{i}' classid='{SubgridId}' /></cell>");
            for (int i = 0; i < quickViews; i++)
                sb.Append($"<cell visible='true'><control id='qv{i}' classid='{QuickViewId}' /></cell>");
            for (int i = 0; i < customControls; i++)
                sb.Append($"<cell visible='true'><control id='cc{i}' classid='{CustomId}'><customControl name='pcf{i}' /></control></cell>");
            sb.Append("</controls>");

            if (libs.Count > 0)
            {
                sb.Append("<formLibraries>");
                foreach (var l in libs) sb.Append($"<Library name='{l}' />");
                sb.Append("</formLibraries>");
            }

            var lib0 = libs.FirstOrDefault() ?? "shared.js";
            sb.Append("<events>");
            if (onload > 0)
            {
                sb.Append("<event name='onload'><Handlers>");
                for (int i = 0; i < onload; i++) sb.Append($"<Handler functionName='onLoad{i}' libraryName='{lib0}' />");
                sb.Append("</Handlers></event>");
            }
            if (onchange > 0)
            {
                sb.Append("<event name='onchange' attribute='name'><Handlers>");
                for (int i = 0; i < onchange; i++) sb.Append($"<Handler functionName='onChange{i}' libraryName='{lib0}' />");
                sb.Append("</Handlers></event>");
            }
            if (tabstate > 0)
            {
                sb.Append("<event name='tabstatechange'><Handlers>");
                for (int i = 0; i < tabstate; i++) sb.Append($"<Handler functionName='onTab{i}' libraryName='{lib0}' />");
                sb.Append("</Handlers></event>");
            }
            sb.Append("</events>");

            sb.Append("</form>");
            return sb.ToString();
        }

        // ---------------------------------------------------------------- Parser counts (US-PERF10.2.x)

        [Fact]
        public void Parse_CountsAllComponents()
        {
            var xml = BuildForm(
                tabs: 3, hiddenTabs: 1, sections: 4, visibleFields: 6, hiddenFields: 2,
                subgrids: 2, quickViews: 1, customControls: 3,
                libraries: new[] { "libA", "libB" }, onload: 2, onchange: 3, tabstate: 1);

            var m = FormXmlParser.Parse(xml);

            Assert.False(m.ParseFailed);
            Assert.Equal(3, m.Tabs);
            Assert.Equal(1, m.HiddenTabs);
            Assert.Equal(2, m.VisibleTabs);
            Assert.Equal(4, m.Sections);
            Assert.Equal(8, m.Fields);           // 6 visible + 2 hidden
            Assert.Equal(2, m.HiddenFields);
            Assert.Equal(6, m.VisibleFields);
            Assert.Equal(2, m.Subgrids);
            Assert.Equal(1, m.QuickViews);
            Assert.Equal(3, m.CustomControls);
            Assert.Equal(2, m.JsLibraries);      // libA, libB (handlers reference libA — still distinct = 2)
            Assert.Equal(2, m.OnLoadHandlers);
            Assert.Equal(3, m.OnChangeHandlers);
            Assert.Equal(1, m.TabStateChangeHandlers);
        }

        [Fact]
        public void Parse_HandlerLibrary_IsCountedWhenNotInFormLibraries()
        {
            // A handler that references a library not listed in <formLibraries> still counts toward the total.
            var xml =
                "<form><events><event name='onload'><Handlers>" +
                "<Handler functionName='f' libraryName='extra.js' />" +
                "</Handlers></event></events></form>";
            var m = FormXmlParser.Parse(xml);
            Assert.Equal(1, m.JsLibraries);
            Assert.Equal(1, m.OnLoadHandlers);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("<form><tabs><tab></form>")]           // malformed (unclosed)
        [InlineData("not xml at all {")]
        public void Parse_BlankOrMalformed_FlagsParseFailed_NoThrow(string xml)
        {
            var m = FormXmlParser.Parse(xml);
            Assert.True(m.ParseFailed);
            Assert.Equal(0, m.Fields);
            Assert.Equal(0, m.Tabs);
        }

        // ---------------------------------------------------------------- Scorer determinism (US-PERF10.3.1)

        [Fact]
        public void Score_IsDeterministic()
        {
            var m = FormXmlParser.Parse(BuildForm(4, 0, 6, 20, 3, 2, 1, 2, new[] { "a", "b" }, 2, 4, 1));

            var s1 = FormScorer.Score(m, businessRuleCount: 3);
            var s2 = FormScorer.Score(m, businessRuleCount: 3);

            Assert.Equal(s1.Score, s2.Score);
            Assert.Equal(s1.Band, s2.Band);
            Assert.Equal(s1.Recommendations.Count, s2.Recommendations.Count);
            Assert.Equal(s1.Findings.Count, s2.Findings.Count);
        }

        // ---------------------------------------------------------------- Band thresholds (US-PERF10.3.2)

        [Fact]
        public void Score_LightForm_BandsLight()
        {
            var m = FormXmlParser.Parse(BuildForm(1, 0, 2, 8, 0, 0, 0, 0));
            var s = FormScorer.Score(m, businessRuleCount: 0);

            Assert.Equal(FormBand.Light, s.Band);
            Assert.True(s.Score < 25, $"expected Light score, got {s.Score}");
            Assert.Empty(s.Recommendations);
        }

        [Fact]
        public void Score_HeavyForm_BandsHeavyOrCritical()
        {
            var m = FormXmlParser.Parse(BuildForm(
                tabs: 8, hiddenTabs: 0, sections: 14, visibleFields: 60, hiddenFields: 5,
                subgrids: 8, quickViews: 4, customControls: 6,
                libraries: new[] { "l1", "l2", "l3", "l4", "l5", "l6" },
                onload: 4, onchange: 12, tabstate: 3));

            var s = FormScorer.Score(m, businessRuleCount: 6);

            Assert.True(s.Band == FormBand.Heavy || s.Band == FormBand.Critical,
                $"expected Heavy/Critical, got {s.Band} (score {s.Score})");
            Assert.True(s.Score >= 50);
        }

        // ---------------------------------------------------------------- Recommendations (US-PERF10.4.x)

        [Fact]
        public void Score_ManyTabs_RecommendsCollapseWithTabsTrigger()
        {
            // Default MaxTabs = 5; 8 tabs must fire the "collapse/lazy-load" quick-win recommendation.
            var m = FormXmlParser.Parse(BuildForm(8, 0, 3, 5, 0, 0, 0, 0));
            var s = FormScorer.Score(m, 0);

            var rec = s.Recommendations.SingleOrDefault(r => r.TriggeredBy == "Tabs");
            Assert.NotNull(rec);
            Assert.Equal("Quick win", rec.Impact);
            Assert.Contains("tab", rec.Text.ToLowerInvariant());
            Assert.Contains(s.Findings, f => f.Title == "Many tabs" && f.Severity >= Severity.Medium);
        }

        [Fact]
        public void Score_ManyFieldsSubgridsScripts_FireStructuralRecommendations()
        {
            var m = FormXmlParser.Parse(BuildForm(
                tabs: 2, hiddenTabs: 0, sections: 3, visibleFields: 45, hiddenFields: 0,
                subgrids: 6, quickViews: 0, customControls: 0,
                libraries: new[] { "l1", "l2", "l3", "l4", "l5" }));
            var s = FormScorer.Score(m, 0);

            Assert.Contains(s.Recommendations, r => r.TriggeredBy == "Visible fields" && r.Impact == "Structural");
            Assert.Contains(s.Recommendations, r => r.TriggeredBy == "Subgrids" && r.Impact == "Structural");
            Assert.Contains(s.Recommendations, r => r.TriggeredBy == "Script libraries" && r.Impact == "Structural");
            // Every recommendation names the metric that triggered it.
            Assert.All(s.Recommendations, r => Assert.False(string.IsNullOrWhiteSpace(r.TriggeredBy)));
        }

        // ---------------------------------------------------------------- Parse-failure scoring (US-PERF10.1.2)

        [Fact]
        public void Score_ParseFailedModel_IsLightWithSingleWarning()
        {
            var m = FormXmlParser.Parse("<broken");
            Assert.True(m.ParseFailed);

            var s = FormScorer.Score(m, 0);
            Assert.Equal(0, s.Score);
            Assert.Equal(FormBand.Light, s.Band);
            Assert.Single(s.Findings);
            Assert.Empty(s.Recommendations);
            Assert.Contains("could not be parsed", s.Findings[0].Title);
        }

        // ---------------------------------------------------------------- Ranking (US-PERF10.5.2)

        [Fact]
        public void Rank_OrdersByScoreDescending()
        {
            var light = FormScorer.Score(FormXmlParser.Parse(BuildForm(1, 0, 1, 4, 0, 0, 0, 0)), 0);
            var heavy = FormScorer.Score(FormXmlParser.Parse(
                BuildForm(8, 0, 14, 60, 5, 8, 4, 6, new[] { "a", "b", "c", "d", "e", "f" }, 4, 12, 3)), 6);
            var mid = FormScorer.Score(FormXmlParser.Parse(BuildForm(4, 0, 4, 20, 0, 2, 0, 1, new[] { "a" })), 1);

            var ranked = FormScorer.Rank(new[] { light, heavy, mid });

            Assert.Same(heavy, ranked[0]);
            Assert.Same(light, ranked[2]);
            for (int i = 1; i < ranked.Count; i++)
                Assert.True(ranked[i - 1].Score >= ranked[i].Score);
        }
    }
}
