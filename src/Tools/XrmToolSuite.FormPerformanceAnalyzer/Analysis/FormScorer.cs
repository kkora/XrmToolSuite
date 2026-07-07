using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.FormPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Scores a parsed <see cref="FormModel"/> into a deterministic 0–100 composite of every load-cost
    /// metric (fields, tabs, sections, custom controls, subgrids, quick views, script libraries, event
    /// handlers, and attached business rules), bands it Light/Moderate/Heavy/Critical against configurable
    /// thresholds, and emits structural-bloat findings plus targeted recommendations. Every judgement is a
    /// transparent labeled heuristic: without runtime telemetry the tool measures structural weight, not
    /// measured milliseconds. Pure, SDK-free, and unit-tested — identical input always yields identical output.
    /// </summary>
    public static class FormScorer
    {
        private const string Category = "Form";

        public static FormScore Score(FormModel m, int businessRuleCount, FormScoreOptions opts = null)
        {
            opts = opts ?? FormScoreOptions.Default;
            if (m == null) m = new FormModel { ParseFailed = true };
            if (businessRuleCount < 0) businessRuleCount = 0;

            var result = new FormScore
            {
                Model = m,
                BusinessRuleCount = businessRuleCount
            };

            // A form we couldn't parse is not scored as heavy — surfaced as a single warning, banded Light.
            if (m.ParseFailed)
            {
                result.Score = 0;
                result.Band = FormBand.Light;
                result.Findings.Add(new Finding(Category, Severity.Medium,
                    "Form XML could not be parsed",
                    "This form's FormXML was empty or malformed, so no load-cost analysis was run for it.",
                    component: m.FormName,
                    recommendation: "Open the form in the maker portal and re-save it to regenerate valid FormXML."));
                return result;
            }

            // ---- weighted composite (each metric row carries its own contribution) ----
            double total = 0;
            total += AddMetric(result, "Visible fields (above the fold)", m.VisibleFields, opts.WeightPerVisibleField);
            total += AddMetric(result, "Hidden fields", m.HiddenFields, opts.WeightPerHiddenField);
            total += AddMetric(result, "Tabs", m.Tabs, opts.WeightPerTab,
                hint: m.HiddenTabs > 0 ? $"{m.HiddenTabs} hidden by default" : null);
            total += AddMetric(result, "Sections", m.Sections, opts.WeightPerSection);
            total += AddMetric(result, "Custom / PCF controls", m.CustomControls, opts.WeightPerCustomControl);
            total += AddMetric(result, "Subgrids", m.Subgrids, opts.WeightPerSubgrid);
            total += AddMetric(result, "Quick-view controls", m.QuickViews, opts.WeightPerQuickView);
            total += AddMetric(result, "Script libraries", m.JsLibraries, opts.WeightPerJsLibrary);
            total += AddMetric(result, "OnLoad handlers", m.OnLoadHandlers, opts.WeightPerOnLoadHandler);
            total += AddMetric(result, "OnChange handlers", m.OnChangeHandlers, opts.WeightPerOnChangeHandler);
            total += AddMetric(result, "Tab state-change handlers", m.TabStateChangeHandlers, opts.WeightPerTabStateChangeHandler);
            total += AddMetric(result, "Active business rules", businessRuleCount, opts.WeightPerBusinessRule);

            result.Score = (int)Math.Round(Math.Min(opts.Cap, total), MidpointRounding.AwayFromZero);
            result.Band = BandFor(result.Score, opts);

            EmitFindingsAndRecommendations(result, m, businessRuleCount, opts);

            return result;
        }

        /// <summary>Bands a raw score against the option thresholds.</summary>
        public static FormBand BandFor(int score, FormScoreOptions opts)
        {
            opts = opts ?? FormScoreOptions.Default;
            if (score >= opts.CriticalThreshold) return FormBand.Critical;
            if (score >= opts.HeavyThreshold) return FormBand.Heavy;
            if (score >= opts.ModerateThreshold) return FormBand.Moderate;
            return FormBand.Light;
        }

        /// <summary>Orders scored forms by score descending (heaviest first), then name.</summary>
        public static List<FormScore> Rank(IEnumerable<FormScore> forms) =>
            (forms ?? Enumerable.Empty<FormScore>())
                .OrderByDescending(f => f.Score)
                .ThenBy(f => f.FormName, StringComparer.OrdinalIgnoreCase)
                .ToList();

        // ---- internals ----

        private static double AddMetric(FormScore result, string label, int count, double weight, string hint = null)
        {
            double contribution = count * weight;
            result.Metrics.Add(new MetricRow(
                label,
                count.ToString(CultureInfo.InvariantCulture),
                hint ?? $"+{contribution.ToString("0.#", CultureInfo.InvariantCulture)}"));
            return contribution;
        }

        private static void EmitFindingsAndRecommendations(
            FormScore result, FormModel m, int businessRuleCount, FormScoreOptions opts)
        {
            // Many above-the-fold fields — the biggest first-paint cost on data-dense forms.
            if (m.VisibleFields > opts.MaxAboveFoldFields)
            {
                int excess = m.VisibleFields - opts.MaxAboveFoldFields;
                result.Findings.Add(new Finding(Category, SeverityFor(m.VisibleFields, opts.MaxAboveFoldFields),
                    "Many above-the-fold fields",
                    $"The form shows {m.VisibleFields} visible fields (heuristic budget {opts.MaxAboveFoldFields}). " +
                    "Every visible field is retrieved and rendered on first paint, so a dense first tab slows form load.",
                    component: m.FormName,
                    recommendation: $"Move {excess} rarely-used field(s) to a collapsed/secondary tab."));
                result.Recommendations.Add(new FormRecommendation(
                    $"Reduce {excess} above-the-fold field(s) by moving rarely-used fields to a collapsed tab.",
                    "Structural", "Medium", "Visible fields"));
            }

            // Many tabs — collapsing/lazy-loading defers their render.
            if (m.Tabs > opts.MaxTabs)
            {
                int excess = m.Tabs - opts.MaxTabs;
                result.Findings.Add(new Finding(Category, SeverityFor(m.Tabs, opts.MaxTabs),
                    "Many tabs",
                    $"The form has {m.Tabs} tabs (heuristic budget {opts.MaxTabs}). " +
                    "Tabs that render eagerly add to the initial load even when the user never opens them.",
                    component: m.FormName,
                    recommendation: $"Collapse or lazy-load {excess} tab(s) so they render on demand."));
                result.Recommendations.Add(new FormRecommendation(
                    $"Collapse or lazy-load {excess} tab(s) so they render on demand.",
                    "Quick win", "Low", "Tabs"));
            }

            // Many subgrids — each is a separate retrieve/render.
            if (m.Subgrids > opts.MaxSubgrids)
            {
                result.Findings.Add(new Finding(Category, SeverityFor(m.Subgrids, opts.MaxSubgrids),
                    "Many subgrids",
                    $"The form embeds {m.Subgrids} subgrids (heuristic budget {opts.MaxSubgrids}). " +
                    "Each subgrid issues its own query and grid render on load.",
                    component: m.FormName,
                    recommendation: "Defer subgrid load by placing subgrids on collapsed tabs (load on tab activation)."));
                result.Recommendations.Add(new FormRecommendation(
                    $"Defer {m.Subgrids} subgrid load(s) — place them on collapsed tabs so they query on activation.",
                    "Structural", "Medium", "Subgrids"));
            }

            // Many script libraries — request count and parse cost.
            if (m.JsLibraries > opts.MaxScriptLibraries)
            {
                result.Findings.Add(new Finding(Category, SeverityFor(m.JsLibraries, opts.MaxScriptLibraries),
                    "Many script libraries",
                    $"The form references {m.JsLibraries} JavaScript libraries (heuristic budget {opts.MaxScriptLibraries}). " +
                    "Each web resource is a separate request the client loads before the form is interactive.",
                    component: m.FormName,
                    recommendation: "Consolidate the libraries into fewer web resources to cut request count."));
                result.Recommendations.Add(new FormRecommendation(
                    $"Consolidate {m.JsLibraries} script libraries into fewer web resources.",
                    "Structural", "High", "Script libraries"));
            }

            // Many custom/PCF controls — heavy render components.
            if (m.CustomControls > opts.MaxCustomControls)
            {
                result.Findings.Add(new Finding(Category, SeverityFor(m.CustomControls, opts.MaxCustomControls),
                    "Many custom / PCF controls",
                    $"The form hosts {m.CustomControls} custom/PCF controls (heuristic budget {opts.MaxCustomControls}). " +
                    "Custom controls bundle their own scripts and render logic, adding to first paint.",
                    component: m.FormName,
                    recommendation: "Review each custom control; defer or replace the heaviest with native controls."));
                result.Recommendations.Add(new FormRecommendation(
                    $"Review {m.CustomControls} custom/PCF control(s); defer or replace the heaviest.",
                    "Structural", "High", "Custom / PCF controls"));
            }

            // Many quick-view controls — each is an extra related-record retrieve.
            if (m.QuickViews > opts.MaxQuickViews)
            {
                result.Findings.Add(new Finding(Category, SeverityFor(m.QuickViews, opts.MaxQuickViews),
                    "Many quick-view controls",
                    $"The form embeds {m.QuickViews} quick-view controls (heuristic budget {opts.MaxQuickViews}). " +
                    "Each quick view triggers an extra retrieve of a related record on load.",
                    component: m.FormName,
                    recommendation: "Reduce quick-view controls or move them to a collapsed tab."));
                result.Recommendations.Add(new FormRecommendation(
                    $"Reduce {m.QuickViews} quick-view control(s) or move them to a collapsed tab.",
                    "Structural", "Medium", "Quick-view controls"));
            }
        }

        /// <summary>High when a metric is at least double its budget, otherwise Medium.</summary>
        private static Severity SeverityFor(int value, int threshold)
        {
            if (threshold > 0 && value >= threshold * 2) return Severity.High;
            return Severity.Medium;
        }
    }
}
