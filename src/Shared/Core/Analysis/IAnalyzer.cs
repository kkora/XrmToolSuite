using System;
using System.Collections.Generic;

namespace XrmToolSuite.Core.Analysis
{
    /// <summary>
    /// Contract implemented by every analyzer module across the suite. Generic over the tool's own
    /// context type (<typeparamref name="TContext"/>), which carries the Dataverse connection(s),
    /// the subject solution/environment, and any cached metadata the analyzer needs.
    /// <para>
    /// Implementations MUST stay UI-free and MUST NOT throw on query failures — degrade to an
    /// informational <see cref="Finding"/> instead. <see cref="AnalyzerRunner"/> also wraps each call
    /// in a try/catch as a backstop, but analyzers should fail soft on their own.
    /// </para>
    /// </summary>
    public interface IAnalyzer<in TContext>
    {
        /// <summary>Human-readable name shown in the analyzer picker and traceability output.</summary>
        string Name { get; }

        /// <summary>Display category applied to findings this analyzer produces.</summary>
        string Category { get; }

        /// <summary>
        /// Runs the analysis. <paramref name="progress"/> reports a status message (wire it to
        /// <c>worker.ReportProgress</c>). Return an empty list when there is nothing to report.
        /// </summary>
        List<Finding> Analyze(TContext context, Action<string> progress);
    }
}
