using Xunit;
using XrmToolSuite.Core;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the shared display formatters (JsonFormat / HtmlFormat / XmlFormat) used by
    /// preview panes and fetchxml editors across the suite. TC-CORE-FMT-01..08.
    /// </summary>
    public class FormatTests
    {
        // TC-CORE-FMT-01: compact JSON is indented with one property per line.
        [Fact]
        public void Json_Compact_IsIndented()
        {
            var pretty = JsonFormat.Pretty("{\"a\":1,\"b\":[1,2],\"c\":{\"d\":\"x\"}}");
            Assert.Contains("\r\n", pretty);
            Assert.Contains("\"a\": 1", pretty);
            Assert.Contains("  \"d\": \"x\"", pretty);
        }

        // TC-CORE-FMT-02: braces/commas/colons INSIDE string literals are untouched.
        [Fact]
        public void Json_StringLiterals_NotBroken()
        {
            var pretty = JsonFormat.Pretty("{\"msg\":\"a,b:{c}[d]\",\"esc\":\"q\\\"q\"}");
            Assert.Contains("\"a,b:{c}[d]\"", pretty);
            Assert.Contains("\"q\\\"q\"", pretty);
        }

        // TC-CORE-FMT-03: already-indented JSON passes through unchanged (no double formatting).
        [Fact]
        public void Json_AlreadyMultiline_Unchanged()
        {
            var input = "{\r\n  \"a\": 1\r\n}";
            Assert.Equal(input, JsonFormat.Pretty(input));
        }

        // TC-CORE-FMT-04: null/empty input is fail-soft.
        [Fact]
        public void Json_Empty_FailSoft()
        {
            Assert.Equal("", JsonFormat.Pretty(null));
            Assert.Equal("", JsonFormat.Pretty(""));
        }

        // TC-CORE-FMT-05: adjacent HTML tags are broken onto lines and nested tags indent.
        [Fact]
        public void Html_BreaksAndIndents()
        {
            var pretty = HtmlFormat.Pretty("<div><p>hi</p><span>x</span></div>");
            var lines = pretty.TrimEnd().Split('\n');
            Assert.Equal(4, lines.Length);              // <div> / <p>hi</p> / <span>x</span> / </div>
            Assert.StartsWith("  <p>", lines[1].TrimEnd('\r').TrimStart('\r'));
        }

        // TC-CORE-FMT-06: void elements (meta, br, …) do not increase indentation.
        [Fact]
        public void Html_VoidElements_DontIndent()
        {
            var pretty = HtmlFormat.Pretty("<head><meta charset=\"utf-8\"><title>t</title></head>");
            Assert.Contains("  <meta", pretty);
            Assert.Contains("  <title>t</title>", pretty); // same depth as meta — meta didn't nest
        }

        // TC-CORE-FMT-07: XML one-liner (view fetchxml) is indented; garbage passes through unchanged.
        [Fact]
        public void Xml_Pretty_And_FailSoft()
        {
            var pretty = XmlFormat.Pretty("<fetch><entity name=\"account\"><attribute name=\"name\"/></entity></fetch>");
            Assert.Contains("\n", pretty);
            Assert.Contains("<entity name=\"account\">", pretty);
            Assert.Equal("not xml <", XmlFormat.Pretty("not xml <"));
        }

        // TC-CORE-FMT-08: JSON embedded-style content stays lexically valid (round-trip shape check).
        [Fact]
        public void Json_PrettyOutput_KeepsAllTokens()
        {
            var input = "{\"nodes\":[{\"id\":\"a\"},{\"id\":\"b\"}]}";
            var pretty = JsonFormat.Pretty(input);
            // Stripping ALL whitespace outside strings should reproduce the input exactly.
            var compact = pretty.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            Assert.Equal(input.Replace(" ", ""), compact);
        }
    }
}
