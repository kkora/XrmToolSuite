namespace XrmToolSuite.DeploymentRiskAnalyzer.Summarization
{
    /// <summary>
    /// Runtime options for AI summary generation. The API key is session-only and must NEVER be
    /// persisted to settings (per suite policy: never store credentials).
    /// </summary>
    public sealed class SummaryOptions
    {
        /// <summary>Which AI provider to call.</summary>
        public AiProvider Provider { get; set; } = AiProvider.Anthropic;

        /// <summary>API key for the selected provider (session-only; from env var or a prompt).</summary>
        public string ApiKey { get; set; }

        /// <summary>Model id for the selected provider (editable; defaults to the provider's mid tier).</summary>
        public string ModelId { get; set; } = "claude-haiku-4-5";

        /// <summary>When false, component/schema names are redacted from the payload (Mode C).</summary>
        public bool IncludeComponents { get; set; } = true;
    }
}
