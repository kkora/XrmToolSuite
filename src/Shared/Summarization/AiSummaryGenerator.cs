using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>
    /// Generates the summary via a chosen AI provider (Anthropic / OpenAI / Google) over raw HTTPS.
    /// Sends ONLY the anonymized <see cref="SummaryPayload"/> (finding metadata); falls back to the
    /// offline template on any failure. Uses <see cref="HttpClient"/> (net48 BCL) + Newtonsoft
    /// (compile-time only) — neither is shipped in the nupkg. The system prompt comes from
    /// <see cref="SummaryOptions.SystemPrompt"/> so each tool steers the model (release decision,
    /// architecture review, cleanup priorities, …).
    /// </summary>
    public sealed class AiSummaryGenerator : ISummaryGenerator
    {
        // No client-wide timeout: it's applied per request (local models can be much slower than cloud).
        private static readonly HttpClient Http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        /// <summary>Generic executive-summary prompt used when the tool does not supply its own.</summary>
        public const string DefaultSystemPrompt =
            "You are a Dynamics 365 / Dataverse solution architect. From the analysis JSON, write a concise " +
            "executive summary for a technical decision-maker. Call out the most important items by name.\n" +
            "FORMAT (strict): plain text only — no Markdown. Do not use '#', '*', '**', backticks, or a title " +
            "line. Write 2-4 short paragraphs separated by a single blank line. End with a final line that " +
            "begins exactly with 'RECOMMENDATION: ' and a one-sentence action. Do not invent findings that " +
            "are not present in the JSON.";

        private readonly ScoreCalculator _fallbackScorer;

        public AiSummaryGenerator(ScoreCalculator fallbackScorer = null)
        {
            _fallbackScorer = fallbackScorer ?? ScoreCalculator.RiskDefault;
        }

        public SummaryResult Generate(ReportModel r, SummaryOptions o, Action<string> progress)
        {
            if (o == null) return Fallback(r, "No AI options provided.");
            var info = AiProviderCatalog.Get(o.Provider);
            if (info.RequiresApiKey && string.IsNullOrWhiteSpace(o.ApiKey))
                return Fallback(r, "No API key provided.");

            try
            {
                progress?.Invoke(info.RequiresApiKey ? "Contacting AI service…" : "Contacting local Ollama…");
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // net48 default may omit TLS1.2

                var payloadJson = JsonConvert.SerializeObject(
                    SummaryPayloadBuilder.Build(r, o.IncludeComponents),
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                var model = string.IsNullOrWhiteSpace(o.ModelId) ? info.Mid : o.ModelId;
                var systemPrompt = string.IsNullOrWhiteSpace(o.SystemPrompt) ? DefaultSystemPrompt : o.SystemPrompt;

                // Local models can be slow to load/generate on first call — give them a generous timeout.
                var timeout = info.RequiresApiKey ? TimeSpan.FromSeconds(60) : TimeSpan.FromMinutes(5);
                using (var req = BuildRequest(o.Provider, model, o.ApiKey, systemPrompt, payloadJson, o.MaxTokens))
                using (var cts = new CancellationTokenSource(timeout))
                {
                    HttpResponseMessage resp;
                    try { resp = Http.SendAsync(req, cts.Token).GetAwaiter().GetResult(); }
                    catch (OperationCanceledException)
                    {
                        return Fallback(r, info.RequiresApiKey
                            ? "The AI request timed out."
                            : "The local model timed out — is Ollama running and the model pulled? (ollama serve / ollama pull " + model + ")");
                    }
                    catch (HttpRequestException hex) when (!info.RequiresApiKey)
                    {
                        return Fallback(r, "Could not reach Ollama at http://localhost:11434 — start it with 'ollama serve'. (" + hex.Message + ")");
                    }
                    using (resp)
                    {
                        var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (!resp.IsSuccessStatusCode)
                            return Fallback(r, $"{o.Provider} HTTP {(int)resp.StatusCode}: {ExtractError(json) ?? resp.ReasonPhrase}");

                        var text = ExtractText(o.Provider, json);
                        if (string.IsNullOrWhiteSpace(text))
                            return Fallback(r, $"{o.Provider} returned no text.");

                        return new SummaryResult { Text = text.Trim(), FromAi = true };
                    }
                }
            }
            catch (Exception ex)
            {
                return Fallback(r, ex.Message);
            }
        }

        private static HttpRequestMessage BuildRequest(AiProvider provider, string model, string key,
            string systemPrompt, string payloadJson, int maxTokens)
        {
            string url;
            object body;

            switch (provider)
            {
                case AiProvider.OpenAI:
                    url = "https://api.openai.com/v1/chat/completions";
                    body = new
                    {
                        model,
                        max_tokens = maxTokens,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = payloadJson }
                        }
                    };
                    break;

                case AiProvider.Google:
                    url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(key)}";
                    body = new
                    {
                        system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                        contents = new[] { new { role = "user", parts = new[] { new { text = payloadJson } } } },
                        generationConfig = new { maxOutputTokens = maxTokens }
                    };
                    break;

                case AiProvider.Ollama:
                    // Local Ollama chat API — plain HTTP, no auth. stream:false returns a single JSON object.
                    url = "http://localhost:11434/api/chat";
                    body = new
                    {
                        model,
                        stream = false,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = payloadJson }
                        },
                        options = new { num_predict = maxTokens }
                    };
                    break;

                default: // Anthropic
                    url = "https://api.anthropic.com/v1/messages";
                    body = new
                    {
                        model,
                        max_tokens = maxTokens,
                        system = systemPrompt,
                        messages = new[] { new { role = "user", content = payloadJson } }
                    };
                    break;
            }

            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };
            if (provider == AiProvider.Anthropic)
            {
                req.Headers.Add("x-api-key", key);
                req.Headers.Add("anthropic-version", "2023-06-01");
            }
            else if (provider == AiProvider.OpenAI)
            {
                req.Headers.Add("Authorization", "Bearer " + key);
            }
            // Google: key travels in the query string (added above).
            return req;
        }

        private static string ExtractText(AiProvider provider, string json)
        {
            var o = JObject.Parse(json);
            switch (provider)
            {
                case AiProvider.OpenAI: return (string)o.SelectToken("choices[0].message.content");
                case AiProvider.Google: return (string)o.SelectToken("candidates[0].content.parts[0].text");
                case AiProvider.Ollama: return (string)o.SelectToken("message.content");
                default: return (string)o.SelectToken("content[0].text");
            }
        }

        private static string ExtractError(string json)
        {
            try
            {
                var o = JObject.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
                return (string)(o.SelectToken("error.message") ?? o.SelectToken("error.status") ?? o.SelectToken("error"));
            }
            catch { return null; }
        }

        private SummaryResult Fallback(ReportModel r, string error)
        {
            var offline = new TemplatedSummaryGenerator(_fallbackScorer).Generate(r, null, null);
            offline.Error = error; // FromAi stays false
            return offline;
        }
    }
}
