using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.FormPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// The structured, SDK-free result of parsing one main form's FormXML. Every count the scorer needs is
    /// captured here so scoring never touches <c>Microsoft.Xrm.Sdk</c> or a live connection (fully
    /// unit-testable). A form whose XML is null/blank/malformed is returned with <see cref="ParseFailed"/>
    /// set — the parser never throws.
    /// </summary>
    public sealed class FormModel
    {
        public string FormName { get; set; }
        public string Entity { get; set; }

        /// <summary>True when the FormXML was null/blank/malformed; the scorer degrades to a single warning.</summary>
        public bool ParseFailed { get; set; }

        // ---- structure ----
        /// <summary>Total number of tabs (including hidden-by-default).</summary>
        public int Tabs { get; set; }
        /// <summary>Tabs marked <c>visible="false"</c> (hidden by default).</summary>
        public int HiddenTabs { get; set; }
        public int Sections { get; set; }

        /// <summary>Total data-bound fields (controls with a <c>datafieldname</c>).</summary>
        public int Fields { get; set; }
        /// <summary>Fields whose containing cell is <c>visible="false"</c> (not above the fold).</summary>
        public int HiddenFields { get; set; }

        // ---- expensive render elements ----
        /// <summary>PCF / custom controls (a <c>&lt;control&gt;</c> carrying a <c>&lt;customControl&gt;</c> binding).</summary>
        public int CustomControls { get; set; }
        public int Subgrids { get; set; }
        public int QuickViews { get; set; }

        // ---- scripting load ----
        /// <summary>Distinct JavaScript web-resource libraries referenced (form libraries + handler bindings).</summary>
        public int JsLibraries { get; set; }
        public int OnLoadHandlers { get; set; }
        public int OnChangeHandlers { get; set; }
        public int TabStateChangeHandlers { get; set; }

        /// <summary>Every <c>classid</c> GUID found on a <c>&lt;control&gt;</c> (for friendly-name resolution).</summary>
        public List<string> ControlClassIds { get; } = new List<string>();

        /// <summary>Visible (above-the-fold) fields = total minus hidden, never negative.</summary>
        public int VisibleFields => Fields - HiddenFields < 0 ? 0 : Fields - HiddenFields;

        /// <summary>Visible tabs = total minus hidden, never negative.</summary>
        public int VisibleTabs => Tabs - HiddenTabs < 0 ? 0 : Tabs - HiddenTabs;
    }

    /// <summary>Load-cost band a form's score falls into. Integer order is meaningful (do not renumber).</summary>
    public enum FormBand
    {
        Light = 0,
        Moderate = 1,
        Heavy = 2,
        Critical = 3
    }

    /// <summary>
    /// One targeted optimization suggestion. <see cref="Impact"/> is "Quick win" or "Structural";
    /// <see cref="Effort"/> is a Low/Medium/High tag; <see cref="TriggeredBy"/> names the metric that
    /// fired the rule so the reader can trace every recommendation back to a number.
    /// </summary>
    public sealed class FormRecommendation
    {
        public string Text { get; set; }
        public string Impact { get; set; }
        public string Effort { get; set; }
        public string TriggeredBy { get; set; }

        public FormRecommendation() { }

        public FormRecommendation(string text, string impact, string effort, string triggeredBy)
        {
            Text = text;
            Impact = impact;
            Effort = effort;
            TriggeredBy = triggeredBy;
        }

        /// <summary>Sort rank for "sortable by impact" — Structural (bigger payoff) before Quick win.</summary>
        public int ImpactRank => string.Equals(Impact, "Structural", System.StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    /// <summary>
    /// The scored result for one form: its 0–100 composite score, band, the per-metric breakdown (each row
    /// carries its contribution to the score), the findings, and the recommendations. Also carries the
    /// source <see cref="Model"/>, the resolved business-rule count, and the form state so the UI/exports
    /// have everything without re-querying. SDK-free.
    /// </summary>
    public sealed class FormScore
    {
        public int Score { get; set; }
        public FormBand Band { get; set; }

        public List<MetricRow> Metrics { get; } = new List<MetricRow>();
        public List<Finding> Findings { get; } = new List<Finding>();
        public List<FormRecommendation> Recommendations { get; } = new List<FormRecommendation>();

        /// <summary>The parsed model the score was computed from (counts + identity).</summary>
        public FormModel Model { get; set; }

        /// <summary>Active form-scoped business rules on the entity (not literally in FormXML).</summary>
        public int BusinessRuleCount { get; set; }

        /// <summary>"Active" / "Inactive" — set by the collector from <c>formactivationstate</c>.</summary>
        public string State { get; set; }

        public string FormName => Model?.FormName;
        public string Entity => Model?.Entity;
    }

    /// <summary>
    /// Configurable per-metric weights, band thresholds, and rule-trigger thresholds for form scoring.
    /// Every weight is a per-unit contribution to the 0–100 (capped) composite; identical options + input
    /// always produce an identical score. A <see cref="Default"/> factory documents the shipped defaults and
    /// <see cref="Reset"/> restores them in place (used by the settings pane's reset-to-defaults button).
    /// </summary>
    public sealed class FormScoreOptions
    {
        // ---- per-unit weights (contribution to the composite score) ----
        public double WeightPerVisibleField { get; set; } = 0.6;
        public double WeightPerHiddenField { get; set; } = 0.2;
        public double WeightPerTab { get; set; } = 2.0;
        public double WeightPerSection { get; set; } = 0.5;
        public double WeightPerCustomControl { get; set; } = 5.0;
        public double WeightPerSubgrid { get; set; } = 5.0;
        public double WeightPerQuickView { get; set; } = 3.0;
        public double WeightPerJsLibrary { get; set; } = 4.0;
        public double WeightPerOnLoadHandler { get; set; } = 2.0;
        public double WeightPerOnChangeHandler { get; set; } = 1.0;
        public double WeightPerTabStateChangeHandler { get; set; } = 2.0;
        public double WeightPerBusinessRule { get; set; } = 2.0;

        // ---- band thresholds (score >= threshold => that band) ----
        public int ModerateThreshold { get; set; } = 25;
        public int HeavyThreshold { get; set; } = 50;
        public int CriticalThreshold { get; set; } = 75;

        // ---- rule-trigger thresholds (findings + recommendations fire when exceeded) ----
        public int MaxAboveFoldFields { get; set; } = 30;
        public int MaxTabs { get; set; } = 5;
        public int MaxSubgrids { get; set; } = 3;
        public int MaxQuickViews { get; set; } = 3;
        public int MaxCustomControls { get; set; } = 5;
        public int MaxScriptLibraries { get; set; } = 3;

        /// <summary>Highest score the composite can reach.</summary>
        public int Cap { get; set; } = 100;

        public static FormScoreOptions Default => new FormScoreOptions();

        /// <summary>Restores every weight/threshold to the shipped defaults in place.</summary>
        public void Reset()
        {
            var d = Default;
            WeightPerVisibleField = d.WeightPerVisibleField;
            WeightPerHiddenField = d.WeightPerHiddenField;
            WeightPerTab = d.WeightPerTab;
            WeightPerSection = d.WeightPerSection;
            WeightPerCustomControl = d.WeightPerCustomControl;
            WeightPerSubgrid = d.WeightPerSubgrid;
            WeightPerQuickView = d.WeightPerQuickView;
            WeightPerJsLibrary = d.WeightPerJsLibrary;
            WeightPerOnLoadHandler = d.WeightPerOnLoadHandler;
            WeightPerOnChangeHandler = d.WeightPerOnChangeHandler;
            WeightPerTabStateChangeHandler = d.WeightPerTabStateChangeHandler;
            WeightPerBusinessRule = d.WeightPerBusinessRule;
            ModerateThreshold = d.ModerateThreshold;
            HeavyThreshold = d.HeavyThreshold;
            CriticalThreshold = d.CriticalThreshold;
            MaxAboveFoldFields = d.MaxAboveFoldFields;
            MaxTabs = d.MaxTabs;
            MaxSubgrids = d.MaxSubgrids;
            MaxQuickViews = d.MaxQuickViews;
            MaxCustomControls = d.MaxCustomControls;
            MaxScriptLibraries = d.MaxScriptLibraries;
            Cap = d.Cap;
        }
    }
}
