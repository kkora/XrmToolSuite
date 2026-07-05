using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace XrmToolSuite.ApiDocumentationBuilder.Api
{
    /// <summary>
    /// Redaction-safety engine (SDK-free, unit-testable). Enforces the DOC06 safety requirement: never
    /// expose secrets, tokens, keys, or full HTTP-trigger URLs in generated documentation. A parameter or
    /// property whose <b>name</b> looks sensitive gets a masked sample value; free-text values have bearer
    /// tokens / URL query strings stripped. The operator can add their own terms (user-controlled redaction).
    /// </summary>
    public sealed class Redactor
    {
        public const string Mask = "***REDACTED***";

        // Built-in secret-name fragments (case-insensitive substring match on the parameter/property name).
        private static readonly string[] DefaultTerms =
        {
            "secret", "token", "password", "pwd", "apikey", "api_key", "clientsecret",
            "connectionstring", "connstr", "sastoken", "sas", "bearer", "credential",
            "privatekey", "accesskey", "authorization", "auth"
        };

        private readonly List<string> _terms;

        public Redactor(IEnumerable<string> additionalTerms = null)
        {
            _terms = DefaultTerms.ToList();
            if (additionalTerms != null)
                foreach (var t in additionalTerms)
                    if (!string.IsNullOrWhiteSpace(t))
                        _terms.Add(t.Trim());
        }

        /// <summary>True when a parameter/property name looks like it carries a secret.</summary>
        public bool IsSensitiveName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var lower = name.ToLowerInvariant();
            return _terms.Any(t => lower.IndexOf(t, StringComparison.Ordinal) >= 0);
        }

        /// <summary>A sample value for a parameter — masked if its name is sensitive.</summary>
        public string SampleFor(ApiParameter p)
        {
            if (p == null) return "null";
            if (IsSensitiveName(p.UniqueName) || IsSensitiveName(p.DisplayName) || IsSensitiveName(p.LogicalName))
                return "\"" + Mask + "\"";
            return FieldTypes.SampleJson(p.Type);
        }

        // Bearer/OAuth tokens in free text.
        private static readonly Regex BearerRx =
            new Regex(@"(?i)\bBearer\s+[A-Za-z0-9\-\._~\+\/]+=*", RegexOptions.Compiled);

        // A URL with a query string — the query often carries a SAS/trigger secret; strip it.
        private static readonly Regex UrlQueryRx =
            new Regex(@"(?i)\bhttps?://[^\s""']+\?[^\s""']*", RegexOptions.Compiled);

        /// <summary>
        /// Strip likely-sensitive fragments from free text: bearer tokens and URL query strings (which carry
        /// SAS / HTTP-trigger secrets). Returns the text with those fragments replaced by the mask.
        /// </summary>
        public string RedactText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = BearerRx.Replace(text, "Bearer " + Mask);
            text = UrlQueryRx.Replace(text, m =>
            {
                var q = m.Value.IndexOf('?');
                return q > 0 ? m.Value.Substring(0, q) + "?" + Mask : m.Value;
            });
            return text;
        }
    }
}
