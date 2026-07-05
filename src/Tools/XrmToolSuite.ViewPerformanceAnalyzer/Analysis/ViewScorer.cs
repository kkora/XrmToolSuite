using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.FetchXml;

namespace XrmToolSuite.ViewPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Scores a single view by <em>reusing</em> the shared FetchXML rule engine (never reimplementing its
    /// rules) and layering LayoutXML column analysis on top. Pure, deterministic, and SDK-free, so it is
    /// unit-testable and liftable into a console/CI wrapper. Every judgement is a labeled heuristic — without
    /// server statistics the tool measures structural risk, not true query cost.
    /// </summary>
    public static class ViewScorer
    {
        private const string Category = "View";

        /// <summary>Layout-penalty weight per column over the threshold (heuristic, transparent).</summary>
        private const int LayoutPenaltyPerColumn = 2;

        /// <summary>Maximum contribution the layout width can add to the score (heuristic cap).</summary>
        private const int MaxLayoutPenalty = 20;

        public static ViewAnalysis Analyze(
            string name,
            string viewType,
            string entity,
            string fetchXml,
            string layoutXml,
            ViewScoreOptions opts = null)
        {
            opts = opts ?? ViewScoreOptions.Default;

            var view = new ViewAnalysis
            {
                Name = name,
                ViewType = viewType,
                Entity = entity,
                FetchXml = fetchXml,
                LayoutXml = layoutXml
            };

            // Layout analysis is independent of the FetchXML parse (a broken fetch can still have a layout).
            view.LayoutColumns.AddRange(LayoutXmlParser.Columns(layoutXml));
            view.LayoutColumnCount = view.LayoutColumns.Count;

            var parse = FetchXmlParser.Parse(fetchXml);
            if (!parse.Success)
            {
                // A view whose FetchXML can't be parsed is not scored as risky — surfaced as an informational
                // note so the row is still visible and actionable, but contributes nothing to the score.
                view.Findings.Add(new Finding(Category, Severity.Info,
                    "View FetchXML could not be parsed",
                    "This view's FetchXML could not be parsed, so no query-cost analysis was run for it: " + parse.Error,
                    component: name,
                    recommendation: "Open the view and verify its FetchXML is well-formed."));
                view.Score = 0;
                view.Band = ScoreBand.Low;
                return view;
            }

            var q = parse.Query;
            view.FetchAttributeCount = q.TotalAttributeCount;
            view.LinkCount = q.LinkCount;
            view.AllAttributes = q.AllAttributes;

            // Reuse the shared FetchXML engine for all query findings + the heuristic cost estimate.
            var analysis = FetchXmlRules.Analyze(q, opts.FetchOptions);
            view.Findings.AddRange(analysis.Findings);

            // View-specific rule: an over-wide grid layout (many displayed columns) is a payload/render cost.
            int layoutPenalty = 0;
            if (view.LayoutColumnCount > opts.MaxLayoutColumns)
            {
                layoutPenalty = Math.Min(MaxLayoutPenalty,
                    (view.LayoutColumnCount - opts.MaxLayoutColumns) * LayoutPenaltyPerColumn);

                view.Findings.Add(new Finding(Category, Severity.Medium,
                    "Over-wide view layout",
                    $"The view displays {view.LayoutColumnCount} columns (heuristic threshold {opts.MaxLayoutColumns}). " +
                    "Wide grids increase the columns retrieved and rendered per row, slowing view load. " +
                    $"Heuristic estimate: adds +{layoutPenalty} to the view cost score.",
                    component: name,
                    recommendation: "Remove rarely-used columns from the view layout so users load only what they need."));
            }

            // Score = shared FetchXML cost estimate + transparent, labeled layout penalty, capped at 100.
            view.Score = Math.Min(100, analysis.CostEstimate + layoutPenalty);
            view.Band = ScoreCalculator.BandFor(view.Score, 15, 40);

            return view;
        }

        /// <summary>Orders analyzed views by score descending (slowest/riskiest first).</summary>
        public static List<ViewAnalysis> Rank(IEnumerable<ViewAnalysis> views) =>
            (views ?? Enumerable.Empty<ViewAnalysis>())
                .OrderByDescending(v => v.Score)
                .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
