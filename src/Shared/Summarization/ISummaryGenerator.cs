using System;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>Result of a summary-generation attempt.</summary>
    public sealed class SummaryResult
    {
        /// <summary>The summary prose to show and embed in reports.</summary>
        public string Text { get; set; }

        /// <summary>True if produced by the AI service; false if produced by the offline template.</summary>
        public bool FromAi { get; set; }

        /// <summary>Set when an AI attempt failed and we fell back to the offline template.</summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Produces an executive summary from a <see cref="ReportModel"/>. Implementations are UI-free so they
    /// stay liftable into a console/CI wrapper. <paramref name="progress"/> may be null. Generic across the
    /// suite: the same interface serves the risk, debt, complexity, and AI-review tools.
    /// </summary>
    public interface ISummaryGenerator
    {
        SummaryResult Generate(ReportModel result, SummaryOptions options, Action<string> progress);
    }
}
