using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Analysis
{
    /// <summary>
    /// Flags metadata that appears unused: custom tables holding no rows (candidate for removal) and
    /// tables that have grown unusually wide (a maintainability smell). Probing is capped so a large
    /// environment cannot stall the scan; the cap is reported as an informational finding when hit.
    /// </summary>
    public sealed class UnusedMetadataAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Unused Metadata";
        public string Category => "Unused Metadata";

        private const int WideTableAttributeCount = 200;

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            var custom = ctx.CustomEntities().ToList();

            int probed = 0;
            foreach (var e in custom)
            {
                if (probed >= ctx.MaxEntityProbes)
                {
                    findings.Add(new Finding(Category, Severity.Info, "Row-count probing capped",
                        $"Only the first {ctx.MaxEntityProbes} custom tables were probed for row counts.",
                        "(scan limit)", "Re-run against a narrower scope or raise the probe cap to cover the rest."));
                    break;
                }

                progress?.Invoke($"Checking usage of {e.LogicalName}…");
                int rows = ctx.RowCount(e.LogicalName);
                probed++;
                if (rows == 0)
                    findings.Add(new Finding(Category, Severity.Medium, "Custom table has no data",
                        $"'{e.DisplayName?.UserLocalizedLabel?.Label ?? e.LogicalName}' contains 0 rows and may be unused.",
                        e.LogicalName, "Confirm the table is still needed; delete it (and its forms/views) if obsolete."));

                var detail = ctx.GetEntityDetail(e.LogicalName);
                int attrs = detail?.Attributes?.Count(a => a.IsCustomAttribute == true) ?? 0;
                if (attrs >= WideTableAttributeCount)
                    findings.Add(new Finding(Category, Severity.Low, "Very wide custom table",
                        $"'{e.LogicalName}' has {attrs} custom columns — hard to maintain and slow to render.",
                        e.LogicalName, "Split rarely-used columns into a related table or remove unused columns."));
            }

            return findings;
        }
    }
}
