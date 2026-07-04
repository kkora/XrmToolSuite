using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Analysis
{
    /// <summary>Web-resource type codes used across the web-resource analyzers.</summary>
    internal static class WebResourceType
    {
        public const int Html = 1;
        public const int Css = 2;
        public const int JScript = 3;
        public const int Xml = 4;
        public const int Png = 5;
        public const int Jpg = 6;
        public const int Gif = 7;
        public const int Xsl = 8;
        public const int Ico = 9;
    }

    /// <summary>Flags web resources that share a display name — a sign of copy-paste duplication.</summary>
    public sealed class DuplicateArtifactsAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Duplicate Artifacts";
        public string Category => "Duplicate Artifacts";

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Scanning web resources for duplicates…");

            var web = ctx.SafeRetrieveAll(new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "displayname", "webresourcetype")
            });

            var dupGroups = web
                .Select(w => new
                {
                    Name = w.GetAttributeValue<string>("name"),
                    Display = w.GetAttributeValue<string>("displayname")
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Display))
                .GroupBy(x => x.Display.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var g in dupGroups)
                findings.Add(new Finding(Category, Severity.Low, "Duplicate web-resource display name",
                    $"{g.Count()} web resources share the display name '{g.Key}': {string.Join(", ", g.Select(x => x.Name).Take(6))}.",
                    g.Key, "Consolidate duplicates into a single shared web resource to reduce maintenance surface."));

            return findings;
        }
    }

    /// <summary>Flags JavaScript web resources using deprecated Dynamics client APIs.</summary>
    public sealed class DeprecatedApiAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Deprecated APIs";
        public string Category => "Deprecated APIs";

        // token -> why it is deprecated
        private static readonly (string Token, string Why)[] Deprecated =
        {
            ("Xrm.Page", "Xrm.Page is deprecated — migrate to the executionContext formContext API."),
            ("crmForm", "crmForm is a legacy CRM 4.0 API and unsupported."),
            ("/2011/Organization.svc", "The 2011 SOAP endpoint is deprecated — use the Web API (/api/data)."),
            ("getServerUrl", "getServerUrl is deprecated — use getClientUrl."),
            ("XMLHttpRequest", "Synchronous XMLHttpRequest to CRM endpoints is discouraged — use Xrm.WebApi."),
        };

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Scanning JavaScript for deprecated APIs…");

            var scripts = ctx.SafeRetrieveAll(new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "content"),
                Criteria =
                {
                    Conditions = { new ConditionExpression("webresourcetype", ConditionOperator.Equal, WebResourceType.JScript) }
                }
            });

            foreach (var w in scripts)
            {
                var name = w.GetAttributeValue<string>("name");
                string code = DecodeContent(w.GetAttributeValue<string>("content"));
                if (string.IsNullOrEmpty(code)) continue;

                var hits = Deprecated.Where(d => code.IndexOf(d.Token, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                foreach (var d in hits)
                    findings.Add(new Finding(Category, Severity.Medium, $"Deprecated API: {d.Token}",
                        $"'{name}' references {d.Token}. {d.Why}",
                        name, "Refactor the script to the supported client API.",
                        "https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference"));
            }

            return findings;
        }

        private static string DecodeContent(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(base64)); }
            catch { return null; }
        }
    }
}
