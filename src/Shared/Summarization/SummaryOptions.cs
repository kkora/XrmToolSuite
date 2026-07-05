namespace XrmToolSuite.Core.Summarization
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

        /// <summary>
        /// Model id for the selected provider (editable). Left null by default so the generator resolves the
        /// selected provider's mid tier (<see cref="AiProviderCatalog"/>) — a hardcoded default would send a
        /// Claude id to OpenAI/Google when only <see cref="Provider"/> is changed.
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>When false, component/schema names are redacted from the payload.</summary>
        public bool IncludeComponents { get; set; } = true;

        /// <summary>
        /// System prompt steering the model. When null the generator uses a generic executive-summary
        /// prompt; tools override it (e.g. the AI Solution Reviewer asks for architecture recommendations).
        /// </summary>
        public string SystemPrompt { get; set; }

        /// <summary>Max output tokens requested from the provider.</summary>
        public int MaxTokens { get; set; } = 1024;
    }
}
