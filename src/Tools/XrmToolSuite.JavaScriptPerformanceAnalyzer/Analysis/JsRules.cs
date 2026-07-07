using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.JavaScriptPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Statically analyzes a JavaScript web resource for performance and deprecation risks. Pure line/regex
    /// scanning — no runtime, no Dataverse SDK — so it is deterministic, unit-testable, and CI-liftable.
    /// <para>
    /// Every regex-based finding carries a 1-based line number, the trimmed source line as context, and an
    /// explicit confidence note, because heuristics can false-positive (a pattern inside a comment or string
    /// literal). Whole-line comments (a trimmed line starting with <c>//</c>) are skipped cheaply; everything
    /// else that survives is labeled "heuristic". This measures pattern risk, not measured execution time.
    /// </para>
    /// </summary>
    public static class JsRules
    {
        private const string Category = "JavaScript";
        private const string HeuristicNote = "heuristic — may match comments/strings";
        private const string MeasuredNote = "measured (decoded byte length)";

        // Compiled once; SDK-free, BCL regex only.
        private static readonly Regex RxXrmPage = new Regex(@"\bXrm\.Page\b", RegexOptions.Compiled);
        // Synchronous XMLHttpRequest: open(method, url, false, ...) — the 3rd (async) arg is literal false.
        private static readonly Regex RxSyncXhrOpen = new Regex(@"\.open\s*\(\s*[^,()]+,\s*[^,()]+,\s*false\b", RegexOptions.Compiled);
        private static readonly Regex RxAsyncFalse = new Regex(@"async\s*:\s*false\b", RegexOptions.Compiled);
        private static readonly Regex RxAlert = new Regex(@"\balert\s*\(", RegexOptions.Compiled);
        private static readonly Regex RxConsole = new Regex(@"\bconsole\s*\.", RegexOptions.Compiled);
        // retrieve / retrieveMultiple / Xrm.WebApi — word boundaries so "retrieve" doesn't double-count "retrieveMultiple".
        private static readonly Regex RxRetrieve = new Regex(@"Xrm\.WebApi|\bretrieveMultiple\b|\bretrieve\b", RegexOptions.Compiled);
        private static readonly Regex RxGuid = new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
        private static readonly Regex RxUrl = new Regex(@"https?://[^\s'"" )]+", RegexOptions.Compiled);
        private static readonly Regex RxDom = new Regex(@"document\.getElementById|document\.querySelector|window\.parent", RegexOptions.Compiled);

        public static JsScriptAnalysis Analyze(string scriptName, string code, JsAnalysisOptions opts = null)
        {
            opts = opts ?? JsAnalysisOptions.Default;
            code = code ?? string.Empty;

            var analysis = new JsScriptAnalysis
            {
                ScriptName = scriptName,
                Code = code,
                SizeBytes = Encoding.UTF8.GetByteCount(code)
            };

            var lines = code.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            analysis.LineCount = lines.Length;

            var findings = analysis.Findings;

            int consoleCount = 0, retrieveCount = 0;
            int firstConsoleLine = 0, firstRetrieveLine = 0;
            string firstConsoleText = null, firstRetrieveText = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var trimmed = raw.Trim();
                if (trimmed.Length == 0) continue;

                // Cheap comment guard: a whole-line comment is very unlikely to be live code.
                bool isComment = trimmed.StartsWith("//");
                int lineNo = i + 1;

                if (!isComment && RxXrmPage.IsMatch(raw))
                    findings.Add(Make(scriptName, Severity.Medium, "Deprecated Xrm.Page usage",
                        "Uses the deprecated Xrm.Page object model, which Microsoft has removed from the supported API surface.",
                        lineNo, trimmed, HeuristicNote,
                        "Refactor to the form execution context: accept executionContext and call executionContext.getFormContext()."));

                if (!isComment && (RxSyncXhrOpen.IsMatch(raw) || RxAsyncFalse.IsMatch(raw)))
                    findings.Add(Make(scriptName, Severity.High, "Synchronous XMLHttpRequest",
                        "A synchronous HTTP request blocks the UI thread until it returns, freezing the form during the call.",
                        lineNo, trimmed, HeuristicNote,
                        "Use an asynchronous request (async: true) or Xrm.WebApi, which returns a Promise and never blocks the UI."));

                if (!isComment && RxAlert.IsMatch(raw))
                    findings.Add(Make(scriptName, Severity.High, "Blocking alert() in form logic",
                        "alert() halts all form script execution until the user dismisses it, blocking form load/save.",
                        lineNo, trimmed, HeuristicNote,
                        "Replace with a non-blocking notification: Xrm.Navigation.openAlertDialog or formContext.ui.setFormNotification."));

                if (!isComment && RxDom.IsMatch(raw))
                    findings.Add(Make(scriptName, Severity.Medium, "Unsupported DOM manipulation",
                        "Directly reaching into the form DOM (document.getElementById / querySelector / window.parent) is unsupported and breaks across releases.",
                        lineNo, trimmed, HeuristicNote,
                        "Use the supported client API (formContext.getControl / getAttribute) instead of touching the DOM."));

                if (!isComment)
                {
                    var guid = RxGuid.Match(raw);
                    if (guid.Success)
                        findings.Add(Make(scriptName, Severity.Medium, "Hardcoded GUID",
                            "A hardcoded record/component GUID (" + guid.Value + ") is environment-specific and breaks when the solution is deployed elsewhere.",
                            lineNo, trimmed, HeuristicNote,
                            "Move the id to an environment variable or look it up by a stable key at runtime."));

                    var url = RxUrl.Match(raw);
                    if (url.Success)
                        findings.Add(Make(scriptName, Severity.Medium, "Hardcoded absolute URL",
                            "A hardcoded absolute URL (" + url.Value + ") ties the script to one environment and can break on deployment.",
                            lineNo, trimmed, HeuristicNote,
                            "Derive the base URL from Xrm.Utility.getGlobalContext().getClientUrl() or an environment variable."));
                }

                if (!isComment && RxConsole.IsMatch(raw))
                {
                    consoleCount += RxConsole.Matches(raw).Count;
                    if (firstConsoleLine == 0) { firstConsoleLine = lineNo; firstConsoleText = trimmed; }
                }

                if (!isComment && RxRetrieve.IsMatch(raw))
                {
                    retrieveCount += RxRetrieve.Matches(raw).Count;
                    if (firstRetrieveLine == 0) { firstRetrieveLine = lineNo; firstRetrieveText = trimmed; }
                }
            }

            // Aggregate rules (counts across the whole script).
            if (consoleCount > opts.ConsoleWarn)
                findings.Add(Make(scriptName, Severity.Low, "Excessive console logging",
                    $"{consoleCount} console.* calls (threshold {opts.ConsoleWarn}). Verbose logging adds overhead and can leak data in production.",
                    firstConsoleLine, firstConsoleText, HeuristicNote,
                    "Remove or gate debug logging behind a flag before shipping."));

            if (retrieveCount > opts.RepeatedRetrieveWarn)
                findings.Add(Make(scriptName, Severity.Medium, "Repeated data retrieval calls",
                    $"{retrieveCount} retrieve/retrieveMultiple/Xrm.WebApi calls (threshold {opts.RepeatedRetrieveWarn}). Multiple round-trips per form event add latency.",
                    firstRetrieveLine, firstRetrieveText, HeuristicNote,
                    "Batch the reads, cache results, or fetch related data in a single query."));

            // Size rules (measured from the decoded byte length — high confidence).
            if (analysis.SizeBytes > opts.SizeHighBytes)
                findings.Add(Make(scriptName, Severity.Medium, "Very large script",
                    $"Decoded size is {analysis.SizeBytes:N0} bytes (threshold {opts.SizeHighBytes:N0}). Large scripts slow form load — they are downloaded and parsed on every use.",
                    0, null, MeasuredNote,
                    "Split into smaller libraries, minify, and load only what each form needs."));
            else if (analysis.SizeBytes > opts.SizeWarnBytes)
                findings.Add(Make(scriptName, Severity.Low, "Large script",
                    $"Decoded size is {analysis.SizeBytes:N0} bytes (threshold {opts.SizeWarnBytes:N0}). Consider minifying or splitting the library.",
                    0, null, MeasuredNote,
                    "Minify the web resource and remove unused code."));

            if (findings.Count == 0)
                findings.Add(Make(scriptName, Severity.Info, "No JavaScript performance risks detected",
                    "No deprecated APIs, synchronous calls, blocking dialogs, excessive logging, hardcoded ids/URLs, or DOM access were found by the static scan.",
                    0, null, MeasuredNote, null));

            analysis.Score = ScoreCalculator.RiskDefault.Score(findings);
            analysis.Band = ScoreCalculator.BandFor(analysis.Score, 15, 40);
            return analysis;
        }

        /// <summary>Orders analyzed scripts by score descending (riskiest first), then name.</summary>
        public static List<JsScriptAnalysis> Rank(IEnumerable<JsScriptAnalysis> scripts) =>
            (scripts ?? Enumerable.Empty<JsScriptAnalysis>())
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.ScriptName, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static JsFinding Make(string scriptName, Severity severity, string title, string description,
            int line, string codeLine, string confidence, string recommendation)
        {
            // Fold the location + confidence into the base Description so text exporters (CSV/MD/HTML/PDF),
            // which only see the base Finding fields, still show the context and the honesty label.
            var desc = description;
            if (line > 0 && !string.IsNullOrEmpty(codeLine))
                desc += $"\nLine {line}: {codeLine}";
            if (!string.IsNullOrEmpty(confidence))
                desc += $"\nConfidence: {confidence}";

            return new JsFinding
            {
                Category = Category,
                Severity = severity,
                Title = title,
                Description = desc,
                Component = scriptName,
                Recommendation = recommendation,
                Line = line,
                CodeLine = codeLine,
                Confidence = confidence
            };
        }
    }
}
