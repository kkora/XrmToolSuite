using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.EnvironmentInventory.Inventory
{
    /// <summary>
    /// SDK-free projection of an <see cref="InventorySnapshot"/> into the shared reporting primitives:
    /// a list of <see cref="MetricRow"/> (counts per category + total) and a full <see cref="ReportModel"/>
    /// so the inventory can reuse the suite's score-oriented JSON/HTML exporters. The inventory has no risk
    /// score, so Score is 0 and there are no findings — this is a catalog, not an assessment.
    /// </summary>
    public static class InventorySummary
    {
        public static List<MetricRow> ToMetrics(InventorySnapshot snapshot)
        {
            snapshot = snapshot ?? new InventorySnapshot();
            var metrics = new List<MetricRow>
            {
                new MetricRow("Total components", snapshot.Total.ToString(), "across all categories")
            };
            foreach (var kv in snapshot.CountByCategory())
                metrics.Add(new MetricRow(kv.Key, kv.Value.ToString(), "components"));

            if (snapshot.UnavailableSources != null && snapshot.UnavailableSources.Count > 0)
                metrics.Add(new MetricRow("Unavailable sources",
                    string.Join(", ", snapshot.UnavailableSources), "not collected"));

            return metrics;
        }

        public static ReportModel ToReportModel(InventorySnapshot snapshot)
        {
            snapshot = snapshot ?? new InventorySnapshot();
            var model = new ReportModel
            {
                ToolName = "Environment Inventory",
                ReportTitle = "Environment Inventory",
                ScoreWord = "components",
                SubjectName = snapshot.EnvironmentName,
                SourceEnvironment = snapshot.EnvironmentName,
                AnalyzedOnUtc = snapshot.CollectedOnUtc,
                Score = 0,
                Band = ScoreBand.Low,
                LeadIn = $"{snapshot.Total} component(s) inventoried across " +
                         $"{snapshot.CountByCategory().Count} categor(ies)."
            };

            foreach (var m in ToMetrics(snapshot))
                model.Metrics.Add(m);

            foreach (var c in snapshot.Categories())
                model.AnalyzersRun.Add(c);

            if (snapshot.UnavailableSources != null)
                foreach (var s in snapshot.UnavailableSources)
                    model.AnalyzersSkipped.Add(s);

            return model;
        }
    }
}
