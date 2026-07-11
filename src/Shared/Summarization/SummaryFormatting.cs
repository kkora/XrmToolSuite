using System.Text.RegularExpressions;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>
    /// Normalizes model output to clean plain text for the viewer and report embedding: strips
    /// Markdown emphasis/headings/backticks, tidies bullets, and puts the final RECOMMENDATION line
    /// on its own with a blank line before it. Pure/UI-free.
    /// </summary>
    public static class SummaryFormatting
    {
        public static string ToPlainText(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;

            var text = s.Replace("\r\n", "\n").Replace("\r", "\n");
            text = text.Replace("**", "").Replace("__", "").Replace("`", "");   // emphasis / inline code
            text = Regex.Replace(text, @"(?m)^\s*#{1,6}\s*", "");               // heading markers
            text = Regex.Replace(text, @"(?m)^\s*[-*]\s+", "  • ");             // bullets
            // Ensure the recommendation stands alone with a blank line before it.
            text = Regex.Replace(text, @"\s*(RECOMMENDATION:)", "\n\n$1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\n{3,}", "\n\n");                      // collapse blank runs
            return text.Trim();
        }

        /// <summary>
        /// Plain text with Windows (CRLF) newlines — for display in a WinForms TextBox, which only breaks
        /// lines on CRLF (LF-only text would otherwise render as one run-on paragraph). The model/report
        /// copy keeps LF; this is display-only.
        /// </summary>
        public static string ForTextBox(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }
    }
}
