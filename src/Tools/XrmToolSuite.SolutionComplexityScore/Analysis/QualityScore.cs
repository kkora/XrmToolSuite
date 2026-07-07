using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.SolutionComplexityScore.Analysis
{
    /// <summary>One best-practice violation that lowered the quality score, kept for the "why" breakdown.</summary>
    public sealed class QualityDeduction
    {
        public string Signal { get; }
        public int Points { get; }
        public string Why { get; }
        public QualityDeduction(string signal, int points, string why)
        {
            Signal = signal;
            Points = points;
            Why = why;
        }
    }

    /// <summary>
    /// The computed quality model for a solution: a 0–100 score (higher = better-built) with a Low/Med/High
    /// band and the list of deductions that lowered it.
    /// </summary>
    public sealed class QualityResult
    {
        public int QualityScore { get; set; }   // 0–100, higher = better
        public ScoreBand Band { get; set; }      // High = high quality (good)
        public List<QualityDeduction> Deductions { get; } = new List<QualityDeduction>();

        public string BandLabel =>
            Band == ScoreBand.High ? "High" : Band == ScoreBand.Medium ? "Medium" : "Low";
    }

    /// <summary>
    /// A sibling projection to <see cref="ComplexityMetrics"/>: complexity says "how big/hard", quality says
    /// "how well-built". Starts at 100 and deducts for best-practice violations derived from the SAME
    /// <see cref="ComponentCounts"/> (plus the computed <see cref="ComplexityResult"/>) — no new Dataverse
    /// data. Higher score = better. Pure and fully unit-testable.
    /// </summary>
    public static class QualityScore
    {
        /// <summary>Score at/above which quality is "High" (good); below <see cref="FairBand"/> is "Low".</summary>
        public const int GoodBand = 80;
        public const int FairBand = 60;

        public static ScoreBand BandFor(int score) =>
            score >= GoodBand ? ScoreBand.High : score >= FairBand ? ScoreBand.Medium : ScoreBand.Low;

        public static QualityResult Compute(ComponentCounts c, ComplexityResult complexity)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (complexity == null) throw new ArgumentNullException(nameof(complexity));

            var result = new QualityResult();
            void Deduct(string signal, int points, string why) =>
                result.Deductions.Add(new QualityDeduction(signal, points, why));

            var ci = CultureInfo.InvariantCulture;

            // 1) Oversized forms hurt load time and usability.
            if (c.WidestForm >= 150)
                Deduct("Oversized form", 15,
                    $"Form '{c.WidestFormName}' has ~{c.WidestForm} controls — very wide (slow load, poor UX).");
            else if (c.WidestForm >= 100)
                Deduct("Wide form", 8,
                    $"Form '{c.WidestFormName}' has ~{c.WidestForm} controls — wide; consider tabs or related tables.");

            // 2) Plugin-step density — a lot of hidden server logic per table.
            if (c.Tables > 0)
            {
                double density = (double)c.PluginSteps / c.Tables;
                if (density >= 3.0)
                    Deduct("Plugin-step density", 12,
                        $"{c.PluginSteps} plugin steps across {c.Tables} tables (~{density.ToString("0.#", ci)}/table) — heavy hidden logic.");
                else if (density >= 1.5)
                    Deduct("Plugin-step density", 6,
                        $"{c.PluginSteps} plugin steps across {c.Tables} tables (~{density.ToString("0.#", ci)}/table).");
            }

            // 3) Automation sprawl.
            int automation = c.Flows + c.Workflows + c.BusinessRules;
            if (automation >= 60)
                Deduct("Automation sprawl", 12,
                    $"{automation} processes (flows/workflows/business rules) — large, hard-to-test automation surface.");
            else if (automation >= 40)
                Deduct("Automation sprawl", 6,
                    $"{automation} processes (flows/workflows/business rules) — sizeable automation surface.");

            // 4) Client-side scripting weight.
            if (c.JavaScriptWebResources >= 40)
                Deduct("Client-script weight", 12,
                    $"{c.JavaScriptWebResources} JavaScript web resources — high client-side maintenance/upgrade cost.");
            else if (c.JavaScriptWebResources >= 25)
                Deduct("Client-script weight", 6,
                    $"{c.JavaScriptWebResources} JavaScript web resources — notable client-side footprint.");

            // 5) Legacy workflow reliance (modernization signal).
            if (c.Workflows >= 10 && c.Workflows > c.Flows)
                Deduct("Legacy automation", 8,
                    $"{c.Workflows} classic workflows outnumber {c.Flows} modern flows — plan migration to Power Automate.");
            else if (c.Workflows >= 5)
                Deduct("Legacy automation", 4,
                    $"{c.Workflows} classic workflows — consider migrating to modern flows.");

            // 6) Data-model sprawl and very wide tables.
            if (c.Tables >= 80)
                Deduct("Schema sprawl", 10, $"{c.Tables} tables — large schema, harder to learn and migrate.");
            else if (c.Tables >= 50)
                Deduct("Schema sprawl", 5, $"{c.Tables} tables — sizeable schema.");

            if (c.Tables > 0)
            {
                double cpt = (double)c.Columns / c.Tables;
                if (cpt >= 60)
                    Deduct("Wide tables", 8, $"~{cpt.ToString("0", ci)} columns/table on average — very wide tables.");
                else if (cpt >= 40)
                    Deduct("Wide tables", 4, $"~{cpt.ToString("0", ci)} columns/table on average — wide tables.");
            }

            // 7) Overall complexity dragging maintainability down (ties to the complexity model).
            if (complexity.MaintainabilityScore < 40)
                Deduct("Low maintainability", 10,
                    $"Overall complexity is high (maintainability {complexity.MaintainabilityScore}/100).");
            else if (complexity.MaintainabilityScore < 60)
                Deduct("Low maintainability", 5,
                    $"Overall complexity is elevated (maintainability {complexity.MaintainabilityScore}/100).");

            int total = result.Deductions.Sum(d => d.Points);
            result.QualityScore = Math.Max(0, 100 - total);
            result.Band = BandFor(result.QualityScore);
            return result;
        }
    }
}
