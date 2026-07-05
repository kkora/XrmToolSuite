using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.FetchXml;

namespace XrmToolSuite.ViewPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Tunable thresholds for per-view scoring. The FetchXML thresholds are delegated straight to the
    /// shared <see cref="FetchXmlAnalysisOptions"/> (so view analysis stays consistent with the standalone
    /// FetchXML Performance Analyzer); the layout threshold is this tool's own addition.
    /// </summary>
    public sealed class ViewScoreOptions
    {
        /// <summary>Displayed-column count above which a view's layout is flagged as over-wide (Medium).</summary>
        public int MaxLayoutColumns { get; set; } = 15;

        /// <summary>FetchXML rule thresholds passed to the shared engine (null = engine defaults).</summary>
        public FetchXmlAnalysisOptions FetchOptions { get; set; }

        public static ViewScoreOptions Default => new ViewScoreOptions();
    }

    /// <summary>
    /// The result of analyzing one view: its identity, structural counts, findings (the shared FetchXML
    /// engine's plus this tool's layout findings), and a labeled-heuristic 0–100 score + band. SDK-free
    /// (no <c>Microsoft.Xrm.Sdk</c>) so it stays unit-testable off a live connection.
    /// </summary>
    public sealed class ViewAnalysis
    {
        public string Name { get; set; }

        /// <summary>"System" (savedquery) or "Personal" (userquery).</summary>
        public string ViewType { get; set; }

        public string Entity { get; set; }

        public string FetchXml { get; set; }

        public string LayoutXml { get; set; }

        /// <summary>Count of <c>&lt;attribute&gt;</c> elements across the whole FetchXML (root + links).</summary>
        public int FetchAttributeCount { get; set; }

        /// <summary>Count of displayed grid columns parsed from the LayoutXML.</summary>
        public int LayoutColumnCount { get; set; }

        /// <summary>Number of link-entities (joins) at every depth in the FetchXML.</summary>
        public int LinkCount { get; set; }

        /// <summary>True if the FetchXML uses <c>&lt;all-attributes/&gt;</c> (flagged High by the shared engine).</summary>
        public bool AllAttributes { get; set; }

        /// <summary>Labeled-heuristic 0–100 cost score (FetchXML cost + a layout penalty, capped at 100).</summary>
        public int Score { get; set; }

        public ScoreBand Band { get; set; }

        /// <summary>Layout column names parsed from the LayoutXML (for the detail panel).</summary>
        public List<string> LayoutColumns { get; } = new List<string>();

        public List<Finding> Findings { get; } = new List<Finding>();
    }
}
