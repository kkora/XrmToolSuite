using System.IO;
using System.Text;
using XrmToolSuite.AttributeAuditor.Audit;

namespace XrmToolSuite.AttributeAuditor.Reporting
{
    /// <summary>
    /// Writes the full audit grid (every audited column, used and unused) to CSV — the natural export for
    /// a column inventory, complementing the shared findings/dashboard exporters. SDK-free and RFC-4180
    /// quoted, so it is unit-tested directly.
    /// </summary>
    public static class AuditCsvExporter
    {
        private static readonly string[] Header =
            { "Table", "Column", "DisplayName", "Type", "Custom", "Managed", "Used", "Usage" };

        public static string ToCsv(AuditResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", Header));
            foreach (var c in r.Columns)
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Escape(c.Table),
                    Escape(c.LogicalName),
                    Escape(c.DisplayName),
                    Escape(c.AttributeType),
                    c.IsCustom ? "yes" : "no",
                    c.IsManaged ? "yes" : "no",
                    c.IsUsed ? "yes" : "no",
                    Escape(c.UsageSummary()),
                }));
            }
            return sb.ToString();
        }

        public static void Export(AuditResult r, string path) =>
            File.WriteAllText(path, ToCsv(r), new UTF8Encoding(true)); // BOM so Excel reads UTF-8

        /// <summary>
        /// RFC-4180: quote fields containing comma, quote, CR or LF; double embedded quotes. Also neutralizes
        /// CSV formula injection: a value starting with =, +, -, @ (or tab) is executed as a formula when the
        /// file is opened in Excel/Sheets (this exporter even writes a BOM for Excel), so prefix it with an
        /// apostrophe to force it to be read as text.
        /// </summary>
        private static string Escape(string value)
        {
            value = value ?? "";
            if (value.Length > 0 && "=+-@\t".IndexOf(value[0]) >= 0)
                value = "'" + value;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
