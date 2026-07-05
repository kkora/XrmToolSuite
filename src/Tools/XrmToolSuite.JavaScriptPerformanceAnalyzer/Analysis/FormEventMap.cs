using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XrmToolSuite.JavaScriptPerformanceAnalyzer.Analysis
{
    /// <summary>One script-library → form → event link parsed from a form's FormXML <c>&lt;events&gt;</c>.</summary>
    public sealed class FormScriptUsage
    {
        /// <summary>The web-resource library name referenced by the handler (matches a web resource's name).</summary>
        public string ScriptLibrary { get; set; }

        public string FormName { get; set; }

        public string Entity { get; set; }

        /// <summary>Friendly event name: OnLoad / OnSave / OnChange (plus the attribute for OnChange).</summary>
        public string Event { get; set; }

        /// <summary>The JavaScript function invoked by the handler.</summary>
        public string FunctionName { get; set; }
    }

    /// <summary>
    /// Parses Dataverse FormXML <c>&lt;events&gt;/&lt;event&gt;/&lt;Handlers&gt;/&lt;Handler&gt;</c> to map
    /// script libraries to the forms and events that call them, and to count OnLoad handlers per form (a key
    /// form-load latency signal). Uses <c>System.Xml.Linq</c> only — no Dataverse SDK — so it is pure,
    /// deterministic, unit-testable, and tolerant of null / blank / malformed FormXML (those yield empty / 0).
    /// </summary>
    public static class FormEventMap
    {
        public static List<FormScriptUsage> Map(IEnumerable<(string formName, string entity, string formXml)> forms)
        {
            var result = new List<FormScriptUsage>();
            if (forms == null) return result;

            foreach (var form in forms)
            {
                foreach (var ev in Events(form.formXml))
                {
                    var friendly = FriendlyEvent(EventName(ev), Attr(ev, "attribute"));
                    foreach (var handler in Handlers(ev))
                    {
                        var library = Attr(handler, "libraryName");
                        if (string.IsNullOrWhiteSpace(library)) continue;
                        result.Add(new FormScriptUsage
                        {
                            ScriptLibrary = library,
                            FormName = form.formName,
                            Entity = form.entity,
                            Event = friendly,
                            FunctionName = Attr(handler, "functionName")
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>Number of OnLoad handlers on a form (the classic slow-form-load signal). 0 on bad input.</summary>
        public static int OnLoadHandlerCount(string formXml)
        {
            int count = 0;
            foreach (var ev in Events(formXml))
            {
                if (string.Equals(EventName(ev), "onload", StringComparison.OrdinalIgnoreCase))
                    count += Handlers(ev).Count();
            }
            return count;
        }

        private static IEnumerable<XElement> Events(string formXml)
        {
            if (string.IsNullOrWhiteSpace(formXml)) return Enumerable.Empty<XElement>();
            XDocument doc;
            try { doc = XDocument.Parse(formXml); }
            catch (Exception) { return Enumerable.Empty<XElement>(); }
            if (doc.Root == null) return Enumerable.Empty<XElement>();

            return doc.Descendants().Where(e =>
                string.Equals(e.Name.LocalName, "event", StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<XElement> Handlers(XElement ev) =>
            ev.Descendants().Where(e =>
                string.Equals(e.Name.LocalName, "Handler", StringComparison.OrdinalIgnoreCase));

        private static string EventName(XElement ev) => Attr(ev, "name");

        private static string FriendlyEvent(string name, string attribute)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Event";
            if (name.Equals("onload", StringComparison.OrdinalIgnoreCase)) return "OnLoad";
            if (name.Equals("onsave", StringComparison.OrdinalIgnoreCase)) return "OnSave";
            if (name.Equals("onchange", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(attribute) ? "OnChange" : $"OnChange({attribute})";
            // Some FormXML uses the attribute logical name directly as the event name for field changes.
            return name;
        }

        private static string Attr(XElement e, string name) =>
            e.Attributes().FirstOrDefault(a =>
                string.Equals(a.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
