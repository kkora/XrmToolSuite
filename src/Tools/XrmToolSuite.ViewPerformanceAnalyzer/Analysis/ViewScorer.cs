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

            // Layout analysis is independent of the FetchXML parse (a broken fetch can still have a wide
            // layout), so evaluate the over-wide-layout rule UP FRONT — otherwise a parse failure would hide
            // a genuinely wide layout's risk.
            view.LayoutColumns.AddRange(LayoutXmlParser.Columns(layoutXml));
            view.LayoutColumnCount = view.LayoutColumns.Count;

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

            var parse = FetchXmlParser.Parse(fetchXml);
            if (!parse.Success)
            {
                // FetchXML can't be parsed: no query-cost analysis is run, but any over-wide-layout finding
                // above still stands and still contributes its (labeled, heuristic) penalty to the score.
                view.Findings.Add(new Finding(Category, Severity.Info,
                    "View FetchXML could not be parsed",
                    "This view's FetchXML could not be parsed, so no query-cost analysis was run for it: " + parse.Error,
                    component: name,
                    recommendation: "Open the view and verify its FetchXML is well-formed."));
                view.Score = Math.Min(100, layoutPenalty);
                view.Band = ScoreCalculator.BandFor(view.Score, 15, 40);
                return view;
            }

            var q = parse.Query;
            view.FetchAttributeCount = q.TotalAttributeCount;
            view.LinkCount = q.LinkCount;
            view.AllAttributes = q.AllAttributes;

            // Reuse the shared FetchXML engine for query findings + the heuristic cost. Drop its standalone
            // "No performance risks detected" placeholder: it is only meaningful for the FetchXML tool in
            // isolation, and here it would contradict a layout finding this consumer adds (a view flagged
            // Medium that simultaneously claims it has no risks).
            var analysis = FetchXmlRules.Analyze(q, opts.FetchOptions);
            view.Findings.AddRange(analysis.Findings.Where(f => f.Title != "No performance risks detected"));

            // Score = shared FetchXML cost estimate + transparent, labeled layout penalty, capped at 100.
            view.Score = Math.Min(100, analysis.CostEstimate + layoutPenalty);
            view.Band = ScoreCalculator.BandFor(view.Score, 15, 40);

            // Only after ALL rules (query + layout): if nothing actionable was found, restore a single clean
            // note so a genuinely risk-free view still reads clearly.
            if (!view.Findings.Any(f => f.Severity > Severity.Info))
                view.Findings.Add(new Finding(Category, Severity.Info,
                    "No performance risks detected",
                    "No FetchXML or layout performance risks were found for this view.",
                    component: name));

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
