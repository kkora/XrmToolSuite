using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace XrmToolSuite.AttributeAuditor.Audit
{
    /// <summary>
    /// Pure, SDK-free scanners that extract the column (attribute) logical names referenced by the
    /// XML/text blobs Dataverse stores for forms, views, and processes. Kept free of any Dataverse SDK
    /// dependency so the reference-detection logic is unit-testable without a connection. Matching is
    /// deliberately permissive (case-insensitive, over-detect rather than under-detect) so a used column
    /// is never mis-flagged as an unused retirement candidate.
    /// </summary>
    public static class UsageScanners
    {
        private static readonly RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        // Attribute values may be single- or double-quoted (FetchXML is commonly single-quoted in code).
        // Form controls carry the bound column in datafieldname="...".
        private static readonly Regex FormDataField = new Regex("datafieldname\\s*=\\s*[\"']([^\"']+)[\"']", Opts);

        // FetchXML: <attribute name="x"/>, <condition attribute="x" .../>, <order attribute="x"/>.
        private static readonly Regex FetchAttribute = new Regex("<attribute\\b[^>]*\\bname\\s*=\\s*[\"']([^\"']+)[\"']", Opts);
        private static readonly Regex FetchConditionOrOrder = new Regex("<(?:condition|order)\\b[^>]*\\battribute\\s*=\\s*[\"']([^\"']+)[\"']", Opts);

        // Grid layout: <cell name="x" .../>.
        private static readonly Regex LayoutCell = new Regex("<cell\\b[^>]*\\bname\\s*=\\s*[\"']([^\"']+)[\"']", Opts);

        /// <summary>Column logical names bound to controls in a form's formxml.</summary>
        public static ISet<string> FormColumns(string formXml) => Matches(formXml, FormDataField);

        /// <summary>Column logical names referenced by a view's fetchxml (attributes, conditions, orders).</summary>
        public static ISet<string> FetchColumns(string fetchXml)
        {
            var set = NewSet();
            AddMatches(set, fetchXml, FetchAttribute);
            AddMatches(set, fetchXml, FetchConditionOrOrder);
            return set;
        }

        /// <summary>Column logical names shown as columns in a view's layoutxml.</summary>
        public static ISet<string> LayoutColumns(string layoutXml) => Matches(layoutXml, LayoutCell);

        /// <summary>
        /// True if <paramref name="body"/> references <paramref name="logicalName"/> as a whole token
        /// (case-insensitive). Used to scan workflow XAML / business-rule / cloud-flow definitions, whose
        /// formats vary but always embed the column's logical name as a delimited token.
        /// </summary>
        public static bool ReferencesToken(string body, string logicalName)
        {
            if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(logicalName)) return false;
            // Word boundaries won't work for names with underscores (\w includes _), so require a
            // non-alphanumeric (or string edge) on each side of the name.
            var pattern = "(?<![a-z0-9_])" + Regex.Escape(logicalName) + "(?![a-z0-9_])";
            return Regex.IsMatch(body, pattern, RegexOptions.IgnoreCase);
        }

        private static ISet<string> Matches(string xml, Regex rx)
        {
            var set = NewSet();
            AddMatches(set, xml, rx);
            return set;
        }

        private static void AddMatches(ISet<string> set, string xml, Regex rx)
        {
            if (string.IsNullOrEmpty(xml)) return;
            foreach (Match m in rx.Matches(xml))
                if (m.Groups[1].Success) set.Add(m.Groups[1].Value);
        }

        private static ISet<string> NewSet() => new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
