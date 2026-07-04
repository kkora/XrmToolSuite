using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Summarization
{
    /// <summary>
    /// Generates the summary via a chosen AI provider (Anthropic / OpenAI / Google) over raw HTTPS.
    /// Sends ONLY the anonymized <see cref="SummaryPayload"/> (finding metadata); falls back to the
    /// offline template on any failure. Uses <see cref="HttpClient"/> (net48 BCL) + Newtonsoft
    /// (compile-time only) — neither is shipped in the nupkg.
    /// </summary>
    public sealed class AiSummaryGenerator : ISummaryGenerator
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };

        private const string SystemPrompt =
            "You are a Dynamics 365 / Dataverse release manager. From the deployment risk analysis JSON, " +
            "write a concise executive summary for a release decision. Call out the most important risks " +
            "by name.\n" +
            "FORMAT (strict): plain text only — no Markdown. Do not use '#', '*', '**', backticks, or a " +
            "title line. Write 2-3 short paragraphs separated by a single blank line. End with a final " +
            "line that begins exactly with 'RECOMMENDATION: ' followed by GO, GO WITH CAUTION, or NO-GO " +
            "and a one-sentence justification. Do not invent findings that are not present in the JSON.";

        public DeploymentSummary Generate(AnalysisResult r, SummaryOptions o, Action<string> progress)
        {
            if (o == null || string.IsNullOrWhiteSpace(o.ApiKey))
                return Fallback(r, "No API key provided.");

            try
            {
                progress?.Invoke("Contacting AI service…");
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; // net48 default may omit TLS1.2

                var payloadJson = JsonConvert.SerializeObject(
                    SummaryPayloadBuilder.Build(r, o.IncludeComponents),
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

                var model = string.IsNullOrWhiteSpace(o.ModelId) ? AiProviderCatalog.Get(o.Provider).Mid : o.ModelId;

                using (var req = BuildRequest(o.Provider, model, o.ApiKey, payloadJson))
                {
                    var resp = Http.SendAsync(req).GetAwaiter().GetResult();
                    var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    if (!resp.IsSuccessStatusCode)
                        return Fallback(r, $"{o.Provider} HTTP {(int)resp.StatusCode}: {ExtractError(json) ?? resp.ReasonPhrase}");

                    var text = ExtractText(o.Provider, json);
                    if (string.IsNullOrWhiteSpace(text))
                        return Fallback(r, $"{o.Provider} returned no text.");

                    return new DeploymentSummary { Text = text.Trim(), FromAi = true };
                }
            }
            catch (Exception ex)
            {
                return Fallback(r, ex.Message);
            }
        }

        private static HttpRequestMessage BuildRequest(AiProvider provider, string model, string key, string payloadJson)
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
                        max_tokens = 1024,
                        messages = new[]
                        {
                            new { role = "system", content = SystemPrompt },
                            new { role = "user", content = payloadJson }
                        }
                    };
                    break;

                case AiProvider.Google:
                    url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(key)}";
                    body = new
                    {
                        system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
                        contents = new[] { new { role = "user", parts = new[] { new { text = payloadJson } } } },
                        generationConfig = new { maxOutputTokens = 1024 }
                    };
                    break;

                default: // Anthropic
                    url = "https://api.anthropic.com/v1/messages";
                    body = new
                    {
                        model,
                        max_tokens = 1024,
                        system = SystemPrompt,
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

        private static DeploymentSummary Fallback(AnalysisResult r, string error)
        {
            var offline = new TemplatedSummaryGenerator().Generate(r, null, null);
            offline.Error = error; // FromAi stays false
            return offline;
        }
    }
}
