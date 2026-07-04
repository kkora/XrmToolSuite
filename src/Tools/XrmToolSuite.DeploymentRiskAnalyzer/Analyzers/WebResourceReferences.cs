using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Shared helpers for the Form and Ribbon analyzers: pull web-resource names out of form/ribbon XML
    /// and list the web resources that exist in the source environment.
    /// </summary>
    internal static class WebResourceReferences
    {
        private static readonly Regex Token =
            new Regex(@"\$webresource:([^""'\s\)\}<]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LibraryName =
            new Regex(@"<Library\b[^>]*\bname\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HandlerLib =
            new Regex(@"\blibraryName\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Web-resource names referenced by the XML. <paramref name="includeFormLibraries"/>
        /// also mines formLibraries/Library@name and Handler@libraryName (form-only constructs).</summary>
        public static IEnumerable<string> Extract(string xml, bool includeFormLibraries)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(xml)) return set;

            foreach (Match m in Token.Matches(xml)) Add(set, m.Groups[1].Value);
            if (includeFormLibraries)
            {
                foreach (Match m in LibraryName.Matches(xml)) Add(set, m.Groups[1].Value);
                foreach (Match m in HandlerLib.Matches(xml)) Add(set, m.Groups[1].Value);
            }
            return set;
        }

        private static void Add(HashSet<string> set, string name)
        {
            var n = (name ?? "").Trim();
            // A $webresource: token may carry a suffix like ".js"/"/content"; keep the bare resource name.
            if (n.Length > 0) set.Add(n);
        }

        /// <summary>Names of all web resources in the source environment (for existence checks).</summary>
        public static HashSet<string> SourceNames(AnalyzerContext ctx)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var qe = new QueryExpression("webresource") { ColumnSet = new ColumnSet("name") };
            foreach (var wr in AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities)
            {
                var n = wr.GetAttributeValue<string>("name");
                if (!string.IsNullOrEmpty(n)) set.Add(n);
            }
            return set;
        }
    }
}
