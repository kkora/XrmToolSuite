using System.Text;

namespace XrmToolSuite.Core
{
    /// <summary>
    /// Display helpers for JSON text (BCL-only — no serializer dependency, so Core stays dependency-free).
    /// Compact single-line JSON is unreadable in preview textboxes; pretty-print it for display.
    /// </summary>
    public static class JsonFormat
    {
        /// <summary>
        /// Indents JSON for display: newline + indent after '{', '[' and ',', newline before '}' and ']',
        /// leaving string literals (including escapes) untouched. Purely lexical — it does not validate the
        /// JSON; malformed input just comes back oddly wrapped rather than throwing.
        /// </summary>
        public static string Pretty(string json, int indentSize = 2)
        {
            if (string.IsNullOrWhiteSpace(json)) return json ?? "";
            // Already multi-line? Assume it was produced indented; don't reformat.
            if (json.IndexOf('\n') >= 0) return json;

            var sb = new StringBuilder(json.Length * 2);
            int depth = 0;
            bool inString = false, escaped = false;

            foreach (var ch in json)
            {
                if (inString)
                {
                    sb.Append(ch);
                    if (escaped) escaped = false;
                    else if (ch == '\\') escaped = true;
                    else if (ch == '"') inString = false;
                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inString = true;
                        sb.Append(ch);
                        break;
                    case '{':
                    case '[':
                        sb.Append(ch);
                        depth++;
                        NewLine(sb, depth, indentSize);
                        break;
                    case '}':
                    case ']':
                        depth = depth > 0 ? depth - 1 : 0;
                        NewLine(sb, depth, indentSize);
                        sb.Append(ch);
                        break;
                    case ',':
                        sb.Append(ch);
                        NewLine(sb, depth, indentSize);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        if (!char.IsWhiteSpace(ch)) sb.Append(ch); // drop insignificant whitespace
                        break;
                }
            }
            return sb.ToString();
        }

        private static void NewLine(StringBuilder sb, int depth, int indentSize)
        {
            sb.Append("\r\n").Append(new string(' ', depth * indentSize));
        }
    }

    /// <summary>
    /// Display helper for HTML source previews. Real HTML is not XML (DOCTYPE, void elements), so this is a
    /// lexical line-breaker/indenter for readability only — never used for what gets exported to disk.
    /// </summary>
    public static class HtmlFormat
    {
        // Void elements never get a closing tag; don't indent after them.
        private static readonly string[] Void = { "area","base","br","col","embed","hr","img","input","link","meta","param","source","track","wbr","!doctype" };

        /// <summary>Breaks between adjacent tags and applies simple tag-depth indentation (fail-soft).</summary>
        public static string Pretty(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return html ?? "";
            try
            {
                var broken = html.Replace("><", ">\r\n<");
                var sb = new StringBuilder(broken.Length + 512);
                int depth = 0;
                foreach (var raw in broken.Split('\n'))
                {
                    var line = raw.Trim('\r', ' ', '\t');
                    if (line.Length == 0) continue;
                    bool closes = line.StartsWith("</");
                    if (closes) depth = depth > 0 ? depth - 1 : 0;
                    sb.Append(new string(' ', depth * 2)).Append(line).Append("\r\n");
                    if (!closes && line.StartsWith("<") && !line.StartsWith("<!--"))
                    {
                        var name = TagName(line);
                        bool voidTag = System.Array.IndexOf(Void, name) >= 0;
                        bool selfClosed = line.EndsWith("/>");
                        bool closedInline = line.Contains("</"); // e.g. <td>x</td>
                        if (!voidTag && !selfClosed && !closedInline && name.Length > 0) depth++;
                    }
                }
                return sb.ToString();
            }
            catch { return html; }
        }

        private static string TagName(string line)
        {
            var sb = new StringBuilder();
            for (int i = 1; i < line.Length; i++)
            {
                var c = char.ToLowerInvariant(line[i]);
                if (char.IsLetterOrDigit(c) || c == '!') sb.Append(c);
                else break;
            }
            return sb.ToString();
        }
    }
}
