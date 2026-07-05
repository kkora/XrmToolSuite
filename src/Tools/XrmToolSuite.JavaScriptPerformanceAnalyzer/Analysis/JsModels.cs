using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.JavaScriptPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Tunable thresholds for the static JavaScript rule engine. All heuristic — the analyzer measures
    /// structural/pattern risk in source text, never runtime cost. SDK-free so it stays unit-testable.
    /// </summary>
    public sealed class JsAnalysisOptions
    {
        /// <summary>Number of <c>console.*</c> calls above which a script is flagged (Low).</summary>
        public int ConsoleWarn { get; set; } = 10;

        /// <summary>Decoded script size (bytes) above which the script is flagged Low.</summary>
        public int SizeWarnBytes { get; set; } = 51200;      // 50 KB

        /// <summary>Decoded script size (bytes) above which the script is flagged Medium.</summary>
        public int SizeHighBytes { get; set; } = 204800;     // 200 KB

        /// <summary>Number of retrieve / retrieveMultiple / Xrm.WebApi calls above which a script is flagged Medium.</summary>
        public int RepeatedRetrieveWarn { get; set; } = 3;

        /// <summary>OnLoad handler count per form above which the form is flagged (Medium).</summary>
        public int OnLoadHandlerWarn { get; set; } = 5;

        public static JsAnalysisOptions Default => new JsAnalysisOptions();
    }

    /// <summary>
    /// A <see cref="Finding"/> enriched with the source location that triggered it. The base fields
    /// (severity, title, description, recommendation) drive the shared exporters; the extra fields let
    /// the UI show a line/context/confidence column and label regex heuristics honestly.
    /// </summary>
    public sealed class JsFinding : Finding
    {
        /// <summary>1-based source line the rule matched (0 = whole-script rule such as size).</summary>
        public int Line { get; set; }

        /// <summary>The trimmed source line that matched, for context in the findings grid.</summary>
        public string CodeLine { get; set; }

        /// <summary>Confidence note — e.g. "heuristic — may match comments/strings" for regex rules.</summary>
        public string Confidence { get; set; }
    }

    /// <summary>
    /// The result of statically analyzing one JavaScript web resource: identity, size, findings, and a
    /// labeled-heuristic 0–100 score + band. SDK-free (no <c>Microsoft.Xrm.Sdk</c>) so it is unit-testable
    /// off a live connection and liftable into a console/CI wrapper.
    /// </summary>
    public sealed class JsScriptAnalysis
    {
        public string ScriptName { get; set; }

        /// <summary>Decoded source text (kept so the UI can preview it and search filters it by content).</summary>
        public string Code { get; set; }

        public int SizeBytes { get; set; }

        public int LineCount { get; set; }

        /// <summary>Labeled-heuristic 0–100 risk score (weighted finding severities, capped at 100).</summary>
        public int Score { get; set; }

        public ScoreBand Band { get; set; }

        public List<Finding> Findings { get; } = new List<Finding>();
    }
}
