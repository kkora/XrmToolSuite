using System.Xml.Linq;

namespace XrmToolSuite.Core
{
    /// <summary>
    /// Display helpers for XML text. Dataverse stores view/form XML as a single line; pretty-print it
    /// before showing it in a textbox so it is readable and editable.
    /// </summary>
    public static class XmlFormat
    {
        /// <summary>Indents XML for display; returns the input unchanged if it doesn't parse (fail-soft).</summary>
        public static string Pretty(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return xml ?? "";
            try { return XDocument.Parse(xml).ToString(); }
            catch { return xml; }
        }
    }
}
