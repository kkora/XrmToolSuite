using System;
using System.Linq;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>Supported AI providers for summary generation.</summary>
    public enum AiProvider
    {
        Anthropic,
        OpenAI,
        Google,
        Ollama
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
            /// <summary>False for a LOCAL provider (Ollama) — no API key is required.</summary>
            public bool RequiresApiKey { get; set; } = true;
        }

        public static readonly Info[] All =
        {
            new Info { Provider = AiProvider.Anthropic, DisplayName = "Anthropic (Claude)", Host = "api.anthropic.com",
                       Low = "claude-haiku-4-5", Mid = "claude-sonnet-5", High = "claude-opus-4-8" },
            new Info { Provider = AiProvider.OpenAI, DisplayName = "OpenAI", Host = "api.openai.com",
                       Low = "gpt-4o-mini", Mid = "gpt-4o", High = "gpt-4.1" },
            new Info { Provider = AiProvider.Google, DisplayName = "Google Gemini", Host = "generativelanguage.googleapis.com",
                       Low = "gemini-2.0-flash", Mid = "gemini-2.5-flash", High = "gemini-2.5-pro" },
            // Local models via Ollama (http://localhost:11434). No API key; nothing leaves the machine.
            // Model ids are whatever you've `ollama pull`ed — these are suggested defaults from a typical set.
            new Info { Provider = AiProvider.Ollama, DisplayName = "Ollama (local)", Host = "localhost:11434",
                       Low = "llama3.2:3b", Mid = "qwen2.5:7b", High = "qwen2.5-coder:7b", RequiresApiKey = false },
        };

        public static Info Get(AiProvider p) => All.First(x => x.Provider == p);

        /// <summary>The grey hint shown under the API-key box, tailored to local vs cloud providers.</summary>
        public static string KeyHint(AiProvider p) => Get(p).RequiresApiKey
            ? "The API key is used this session only and is never saved. Only anonymized observations are sent."
            : "Local model via Ollama — no API key needed. Install from ollama.com, then in a terminal:\r\n" +
              "    ollama pull qwen2.5:7b     (download a model, once)\r\n" +
              "    ollama list                (models you have)      ollama ps  (models loaded now)\r\n" +
              "Put that model id in the Model box above. Only anonymized observations are sent — nothing leaves this machine.";

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
