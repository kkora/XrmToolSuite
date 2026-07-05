using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XrmToolSuite.ViewPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Parses a saved query / user query <c>LayoutXML</c> (<c>&lt;grid&gt;&lt;row&gt;&lt;cell name="..."/&gt;</c>)
    /// using <c>System.Xml.Linq</c> only — no Dataverse SDK. Returns the displayed grid columns so the view
    /// analyzer can flag over-wide layouts. Pure and deterministic (fully unit-testable), and tolerant of
    /// null / blank / malformed input: those yield an empty result (count 0) rather than throwing.
    /// </summary>
    public static class LayoutXmlParser
    {
        /// <summary>Number of displayed grid columns (visible <c>&lt;cell&gt;</c> elements with a name).</summary>
        public static int CountColumns(string layoutXml) => Columns(layoutXml).Count;

        /// <summary>
        /// The displayed grid column logical names, in document order. A cell is counted when it has a
        /// non-empty <c>name</c> and is not hidden (<c>ishidden="1"/"true"</c>). Null / blank / malformed
        /// LayoutXML returns an empty list.
        /// </summary>
        public static List<string> Columns(string layoutXml)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(layoutXml))
                return result;

            XDocument doc;
            try
            {
                doc = XDocument.Parse(layoutXml);
            }
            catch (Exception)
            {
                // Malformed LayoutXML is common on legacy views — degrade to "no columns" rather than throw.
                return result;
            }

            if (doc.Root == null)
                return result;

            foreach (var cell in doc.Descendants().Where(e =>
                         string.Equals(e.Name.LocalName, "cell", StringComparison.OrdinalIgnoreCase)))
            {
                var name = Attr(cell, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (IsHidden(cell))
                    continue;
                result.Add(name);
            }

            return result;
        }

        private static bool IsHidden(XElement cell)
        {
            var v = Attr(cell, "ishidden");
            return string.Equals(v, "1", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string Attr(XElement e, string name) =>
            e.Attributes().FirstOrDefault(a =>
                string.Equals(a.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
