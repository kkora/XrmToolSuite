using System;
using System.Collections.Generic;

namespace XrmToolSuite.SolutionComplexityScore.Analysis
{
    /// <summary>The per-dimension weighted contribution to the overall complexity, for the dashboard.</summary>
    public sealed class DimensionScore
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public double Points { get; set; }   // Count * weight
    }

    /// <summary>
    /// The computed complexity/effort model for a solution. All values are derived from
    /// <see cref="ComponentCounts"/> by <see cref="ComplexityMetrics"/> — no Dataverse access.
    /// </summary>
    public sealed class ComplexityResult
    {
        public double ComplexityPoints { get; set; }
        public int ComplexityScore { get; set; }       // 0–100 (higher = more complex)
        public int MaintainabilityScore { get; set; }  // 0–100 (higher = easier to maintain)
        public int UpgradeEffortDays { get; set; }
        public int MigrationEffortDays { get; set; }
        public int TestingEffortDays { get; set; }
        public int SupportCostPerYear { get; set; }    // rough annual $ estimate
        public List<DimensionScore> Dimensions { get; } = new List<DimensionScore>();
    }

    /// <summary>
    /// Transparent, documented complexity + effort model. Each component dimension contributes weighted
    /// "complexity points"; the total maps to a 0–100 score (saturating at <see cref="PointsForMax"/>).
    /// Effort/cost are simple, defensible linear functions of the same tallies so the numbers are
    /// explainable to stakeholders — they are estimates, not guarantees. Pure and fully unit-testable.
    /// </summary>
    public static class ComplexityMetrics
    {
        // Per-unit complexity weights. Higher = each instance adds more cognitive/maintenance load.
        public const double WTable = 3.0;
        public const double WColumn = 0.2;
        public const double WRelationship = 1.0;
        public const double WForm = 1.5;
        public const double WView = 0.5;
        public const double WChart = 0.4;
        public const double WPluginStep = 2.5;
        public const double WWorkflow = 2.0;
        public const double WFlow = 2.0;
        public const double WBusinessRule = 1.5;
        public const double WJavaScript = 1.5;
        public const double WPcf = 3.0;
        public const double WCustomApi = 2.5;
        public const double WDashboard = 1.0;
        public const double WApp = 2.0;

        /// <summary>Complexity points that saturate the 0–100 score at 100.</summary>
        public const double PointsForMax = 600.0;

        /// <summary>Assumed blended day rate for the support-cost estimate (documented assumption).</summary>
        public const int DayRate = 800;

        public static ComplexityResult Compute(ComponentCounts c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));

            var result = new ComplexityResult();
            void Add(string name, int count, double weight)
            {
                double pts = count * weight;
                result.Dimensions.Add(new DimensionScore { Name = name, Count = count, Points = Math.Round(pts, 1) });
                result.ComplexityPoints += pts;
            }

            Add("Tables", c.Tables, WTable);
            Add("Columns", c.Columns, WColumn);
            Add("Relationships", c.Relationships, WRelationship);
            Add("Forms", c.Forms, WForm);
            Add("Views", c.Views, WView);
            Add("Charts", c.Charts, WChart);
            Add("Plugin steps", c.PluginSteps, WPluginStep);
            Add("Workflows", c.Workflows, WWorkflow);
            Add("Flows", c.Flows, WFlow);
            Add("Business rules", c.BusinessRules, WBusinessRule);
            Add("JavaScript", c.JavaScriptWebResources, WJavaScript);
            Add("PCF controls", c.Pcfs, WPcf);
            Add("Custom APIs", c.CustomApis, WCustomApi);
            Add("Dashboards", c.Dashboards, WDashboard);
            Add("Apps", c.Apps, WApp);

            result.ComplexityScore = (int)Math.Round(Math.Min(100.0, result.ComplexityPoints / PointsForMax * 100.0));
            result.MaintainabilityScore = 100 - result.ComplexityScore;

            // Effort estimates (person-days), transparent linear functions of the tallies.
            result.TestingEffortDays = (int)Math.Round(
                c.Forms * 0.5 + c.Views * 0.1 + c.PluginSteps * 0.75 + c.Flows * 0.5 +
                c.Workflows * 0.5 + c.BusinessRules * 0.25 + c.CustomApis * 0.75 +
                c.JavaScriptWebResources * 0.5 + c.Pcfs * 1.0);
            result.UpgradeEffortDays = (int)Math.Round(result.ComplexityPoints * 0.05);
            result.MigrationEffortDays = (int)Math.Round(result.ComplexityPoints * 0.08 + c.Tables * 0.5);

            // Rough annual support cost: the recurring maintenance + regression-testing burden, in $.
            result.SupportCostPerYear =
                (int)Math.Round((result.TestingEffortDays + result.UpgradeEffortDays) * (double)DayRate * 2.0);

            return result;
        }
    }
}
