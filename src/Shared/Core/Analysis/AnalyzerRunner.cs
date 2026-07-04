using System;
using System.Collections.Generic;

namespace XrmToolSuite.Core.Analysis
{
    /// <summary>Aggregated output of an analysis run: findings plus which analyzers ran vs. were skipped.</summary>
    public sealed class AnalysisRun
    {
        public List<Finding> Findings { get; } = new List<Finding>();
        public List<string> AnalyzersRun { get; } = new List<string>();
        public List<string> AnalyzersSkipped { get; } = new List<string>();
    }

    /// <summary>
    /// Runs a set of analyzers with uniform, defensive error handling: an analyzer that throws is
    /// recorded as skipped and degraded to a single informational <see cref="Finding"/> rather than
    /// aborting the whole run. This is the run loop the Deployment Risk Analyzer previously hand-rolled
    /// in its control, hoisted here so every tool degrades failures identically.
    /// </summary>
    public static class AnalyzerRunner
    {
        /// <summary>
        /// Runs each analyzer in order against <paramref name="context"/>. <paramref name="progress"/>
        /// receives status messages. If <paramref name="isCancelled"/> returns true, the loop stops
        /// early (already-collected findings are returned).
        /// </summary>
        public static AnalysisRun Run<TContext>(
            IEnumerable<IAnalyzer<TContext>> analyzers,
            TContext context,
            Action<string> progress = null,
            Func<bool> isCancelled = null)
        {
            var run = new AnalysisRun();
            if (analyzers == null) return run;

            foreach (var analyzer in analyzers)
            {
                if (isCancelled != null && isCancelled()) break;
                progress?.Invoke($"Running {analyzer.Name}…");
                try
                {
                    var findings = analyzer.Analyze(context, progress);
                    if (findings != null) run.Findings.AddRange(findings);
                    run.AnalyzersRun.Add(analyzer.Name);
                }
                catch (Exception ex)
                {
                    run.AnalyzersSkipped.Add(analyzer.Name);
                    run.Findings.Add(new Finding(
                        analyzer.Category,
                        Severity.Info,
                        $"{analyzer.Name} failed",
                        ex.Message,
                        analyzer.Name,
                        "Check permissions/connectivity and re-run this analyzer."));
                }
            }

            return run;
        }
    }
}
