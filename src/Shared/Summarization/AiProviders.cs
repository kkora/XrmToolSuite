using System;
using System.Linq;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>Supported AI providers for summary generation.</summary>
    public enum AiProvider
    {
        Anthropic,
        OpenAI,
        Google
    }

    /// <summary>
    /// Provider display names and suggested low/mid/high-quality models. Model ids are editable in the
    /// UI (free-text box) — these are just starting points that track each provider's tiers.
    /// </summary>
    public static class AiProviderCatalog
    {
        public sealed class Info
        {
            public AiProvider Provider { get; set; }
            public string DisplayName { get; set; }
            public string Host { get; set; }
            public string Low { get; set; }
            public string Mid { get; set; }
            public string High { get; set; }
        }

        public static readonly Info[] All =
        {
            new Info { Provider = AiProvider.Anthropic, DisplayName = "Anthropic (Claude)", Host = "api.anthropic.com",
                       Low = "claude-haiku-4-5", Mid = "claude-sonnet-5", High = "claude-opus-4-8" },
            new Info { Provider = AiProvider.OpenAI, DisplayName = "OpenAI", Host = "api.openai.com",
                       Low = "gpt-4o-mini", Mid = "gpt-4o", High = "gpt-4.1" },
            new Info { Provider = AiProvider.Google, DisplayName = "Google Gemini", Host = "generativelanguage.googleapis.com",
                       Low = "gemini-2.0-flash", Mid = "gemini-2.5-flash", High = "gemini-2.5-pro" },
        };

        public static Info Get(AiProvider p) => All.First(x => x.Provider == p);

        public static AiProvider Parse(string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                foreach (var i in All)
                    if (string.Equals(i.Provider.ToString(), s, StringComparison.OrdinalIgnoreCase))
                        return i.Provider;
            return AiProvider.Anthropic;
        }
    }
}
