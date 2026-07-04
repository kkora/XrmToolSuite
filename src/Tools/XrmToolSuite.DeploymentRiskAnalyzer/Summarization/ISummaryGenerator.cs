using System;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Summarization
{
    /// <summary>Result of a summary-generation attempt.</summary>
    public sealed class DeploymentSummary
    {
        /// <summary>The summary prose to show and embed in reports.</summary>
        public string Text { get; set; }

        /// <summary>True if produced by the AI service; false if produced by the offline template.</summary>
        public bool FromAi { get; set; }

        /// <summary>Set when an AI attempt failed and we fell back to the offline template.</summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// Produces an executive deployment summary from an analysis result. Implementations are UI-free so
    /// they stay liftable into a console/CI wrapper. <paramref name="progress"/> may be null.
    /// </summary>
    public interface ISummaryGenerator
    {
        DeploymentSummary Generate(AnalysisResult result, SummaryOptions options, Action<string> progress);
    }
}
