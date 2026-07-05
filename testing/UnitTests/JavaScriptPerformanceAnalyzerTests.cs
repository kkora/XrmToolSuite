using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.JavaScriptPerformanceAnalyzer.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the JavaScript Performance Analyzer's pure logic — the static rule engine
    /// (JsRules), the models (JsModels), and the FormXML event mapper (FormEventMap). No Dataverse,
    /// no WinForms. The SDK collector (JsCollector) is manual-tested against a live org.
    /// Traces to US-PERF8.2.x (rules), US-PERF8.4.1 (score/band) and US-PERF8.3.x (form mapping).
    /// </summary>
    public class JavaScriptPerformanceAnalyzerTests
    {
        private static JsScriptAnalysis Analyze(string code, JsAnalysisOptions opts = null) =>
            JsRules.Analyze("script.js", code, opts);

        private static bool Has(JsScriptAnalysis a, string titleFragment, Severity sev) =>
            a.Findings.Any(f => f.Severity == sev && f.Title.Contains(titleFragment));

        private static JsFinding Finding(JsScriptAnalysis a, string titleFragment) =>
            a.Findings.OfType<JsFinding>().First(f => f.Title.Contains(titleFragment));

        // ---------------------------------------------------------------- Rules (US-PERF8.2.1)

        [Fact]
        public void XrmPage_IsMedium_WithLineContext()
        {
            var a = Analyze("function f() {\n    var name = Xrm.Page.getAttribute('name');\n}");
            Assert.True(Has(a, "Xrm.Page", Severity.Medium));
            var f = Finding(a, "Xrm.Page");
            Assert.Equal(2, f.Line);
            Assert.Contains("Xrm.Page", f.CodeLine);
            Assert.Contains("heuristic", f.Confidence);
        }

        [Fact]
        public void SynchronousXhr_Open_IsHigh()
        {
            var a = Analyze("var x = new XMLHttpRequest();\nx.open('GET', url, false);\nx.send();");
            Assert.True(Has(a, "Synchronous", Severity.High));
            Assert.Equal(2, Finding(a, "Synchronous").Line);
        }

        [Fact]
        public void SynchronousXhr_AsyncFalse_IsHigh()
        {
            var a = Analyze("$.ajax({ url: '/x', async: false });");
            Assert.True(Has(a, "Synchronous", Severity.High));
        }

        [Fact]
        public void Alert_IsHigh()
        {
            var a = Analyze("function onSave() {\n    alert('saved');\n}");
            Assert.True(Has(a, "alert", Severity.High));
            Assert.Equal(2, Finding(a, "alert").Line);
        }

        [Fact]
        public void Console_OverThreshold_IsLow()
        {
            var lines = string.Join("\n", Enumerable.Range(0, 12).Select(i => $"console.log('m{i}');"));
            var a = Analyze(lines, new JsAnalysisOptions { ConsoleWarn = 10 });
            Assert.True(Has(a, "console", Severity.Low));
        }

        [Fact]
        public void Console_UnderThreshold_NotFlagged()
        {
            var lines = string.Join("\n", Enumerable.Range(0, 3).Select(i => $"console.log('m{i}');"));
            var a = Analyze(lines);
            Assert.DoesNotContain(a.Findings, f => f.Title.Contains("console"));
        }

        [Fact]
        public void HardcodedGuid_IsMedium()
        {
            var a = Analyze("var id = '2f9f2f9f-1a2b-3c4d-5e6f-7a8b9c0d1e2f';");
            Assert.True(Has(a, "GUID", Severity.Medium));
            Assert.Contains("heuristic", Finding(a, "GUID").Confidence);
        }

        [Fact]
        public void HardcodedUrl_IsMedium()
        {
            var a = Analyze("var api = 'https://contoso.crm.dynamics.com/api/data/v9.2/accounts';");
            Assert.True(Has(a, "URL", Severity.Medium));
        }

        [Fact]
        public void RepeatedRetrieves_OverThreshold_IsMedium()
        {
            var a = Analyze(
                "Xrm.WebApi.retrieveRecord('a', id1);\n" +
                "Xrm.WebApi.retrieveMultipleRecords('b');\n" +
                "svc.retrieve('c');\n" +
                "svc.retrieveMultiple(q);\n",
                new JsAnalysisOptions { RepeatedRetrieveWarn = 3 });
            Assert.True(Has(a, "retrieval", Severity.Medium));
        }

        [Fact]
        public void DomManipulation_IsMedium()
        {
            var a = Analyze("var el = document.getElementById('field_name');");
            Assert.True(Has(a, "DOM", Severity.Medium));
        }

        [Fact]
        public void SizeBands_HighAndLow()
        {
            var opts = new JsAnalysisOptions { SizeWarnBytes = 100, SizeHighBytes = 200 };

            var low = Analyze(new string('a', 150), opts);
            Assert.True(Has(low, "Large script", Severity.Low));

            var high = Analyze(new string('b', 300), opts);
            Assert.True(Has(high, "Very large script", Severity.Medium));
        }

        [Fact]
        public void CommentLine_IsSkipped()
        {
            var a = Analyze("// Xrm.Page is deprecated, do not use\nvar ok = true;");
            Assert.DoesNotContain(a.Findings, f => f.Title.Contains("Xrm.Page"));
        }

        // ---------------------------------------------------------------- Clean script + score/band (US-PERF8.4.1)

        [Fact]
        public void CleanScript_ProducesSingleInfo_ScoreZero_Low()
        {
            var a = Analyze("function onLoad(executionContext) {\n" +
                            "    var formContext = executionContext.getFormContext();\n" +
                            "    formContext.getAttribute('name');\n}");
            Assert.Single(a.Findings);
            Assert.Equal(Severity.Info, a.Findings[0].Severity);
            Assert.Contains("No JavaScript performance risks", a.Findings[0].Title);
            Assert.Equal(0, a.Score);
            Assert.Equal(ScoreBand.Low, a.Band);
        }

        [Fact]
        public void RiskyScript_ScoresHigh_Band()
        {
            // Two High (alert + sync XHR = 12+12) plus a Medium (Xrm.Page = 5) => >= 40 => High band.
            var a = Analyze(
                "var name = Xrm.Page.getAttribute('name');\n" +
                "alert('hi');\n" +
                "x.open('GET', url, false);\n" +
                "req.open('POST', url2, false);\n");
            Assert.True(a.Score >= 40, $"expected High-band score, got {a.Score}");
            Assert.Equal(ScoreBand.High, a.Band);
        }

        [Fact]
        public void Rank_OrdersByScoreDescending()
        {
            var clean = Analyze("var ok = true;");
            var risky = Analyze("alert('x');\nvar id = '2f9f2f9f-1a2b-3c4d-5e6f-7a8b9c0d1e2f';");
            var ranked = JsRules.Rank(new[] { clean, risky });
            Assert.True(ranked[0].Score >= ranked[1].Score);
            Assert.Equal(risky.Score, ranked[0].Score);
        }

        // ---------------------------------------------------------------- FormEventMap (US-PERF8.3.x)

        private const string FormXml =
            "<form><events>" +
            "<event name='onload' application='false' active='false'><Handlers>" +
            "<Handler functionName='acc.onLoad' libraryName='new_account.js' passExecutionContext='true' />" +
            "<Handler functionName='acc.init' libraryName='new_shared.js' />" +
            "</Handlers></event>" +
            "<event name='onchange' attribute='telephone1'><Handlers>" +
            "<Handler functionName='acc.onPhoneChange' libraryName='new_account.js' />" +
            "</Handlers></event>" +
            "<event name='onsave'><Handlers>" +
            "<Handler functionName='acc.onSave' libraryName='new_account.js' />" +
            "</Handlers></event>" +
            "</events></form>";

        [Fact]
        public void Map_LinksLibraryToFormAndEvent()
        {
            var usages = FormEventMap.Map(new[] { ("Account Main", "account", FormXml) });

            Assert.Equal(4, usages.Count);
            Assert.Contains(usages, u => u.ScriptLibrary == "new_account.js" && u.Event == "OnLoad");
            Assert.Contains(usages, u => u.ScriptLibrary == "new_shared.js" && u.Event == "OnLoad");
            Assert.Contains(usages, u => u.Event == "OnChange(telephone1)");
            Assert.Contains(usages, u => u.Event == "OnSave" && u.FunctionName == "acc.onSave");
            Assert.All(usages, u => Assert.Equal("Account Main", u.FormName));
        }

        [Fact]
        public void OnLoadHandlerCount_CountsOnlyOnLoad()
        {
            Assert.Equal(2, FormEventMap.OnLoadHandlerCount(FormXml));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("<form><events><event name='onload'></form>")] // malformed
        public void FormXml_BlankOrMalformed_ReturnsZeroAndEmpty(string xml)
        {
            Assert.Equal(0, FormEventMap.OnLoadHandlerCount(xml));
            Assert.Empty(FormEventMap.Map(new[] { ("F", "e", xml) }));
        }
    }
}
