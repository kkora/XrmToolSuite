namespace XrmToolSuite.Core
{
    /// <summary>
    /// Display helpers for arbitrary text shown in a WinForms <c>TextBox</c>, which only breaks lines on
    /// CRLF — LF-only text (common in Dataverse-origin values: JSON/XML returned by a Custom API, multi-line
    /// component details) would otherwise render as one run-on line. Display-only; callers keep the original
    /// bytes for export. (Named to avoid a clash with MigraDoc's <c>TextFormat</c> enum in the reporting chain.)
    /// </summary>
    public static class TextBoxFormat
    {
        /// <summary>Normalizes any newline convention (CRLF/CR/LF) to CRLF so a TextBox wraps it correctly.</summary>
        public static string CrLf(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }
    }
}
