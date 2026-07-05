using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XrmToolSuite.FormPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Parses a model-driven main form's FormXML into a <see cref="FormModel"/> using
    /// <c>System.Xml.Linq</c> only — no Dataverse SDK. Counts tabs (visible vs hidden), sections,
    /// data-bound fields (visible vs hidden), PCF/custom controls, subgrids, quick-view controls, and the
    /// scripting load (distinct JS libraries + onload/onchange/tabstatechange handler bindings). Pure and
    /// deterministic, so it is fully unit-testable. Null / blank / malformed XML yields a
    /// <see cref="FormModel"/> flagged <see cref="FormModel.ParseFailed"/> — it never throws.
    /// </summary>
    public static class FormXmlParser
    {
        // Well-known control classids (upper-case, braces stripped). Everything else with a customControl
        // binding is treated as a PCF/custom control; unmatched classids are surfaced as "unknown control".
        internal const string SubgridClassId = "E7A81278-8635-4D9E-8D4D-59480B391C5B";
        internal const string QuickViewClassId = "5C5600E0-1D6E-4205-A272-BE48DA2CA630";

        public static FormModel Parse(string formXml)
        {
            var model = new FormModel();

            if (string.IsNullOrWhiteSpace(formXml))
            {
                model.ParseFailed = true;
                return model;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(formXml);
            }
            catch (Exception)
            {
                // Malformed FormXML is common on legacy/edited forms — degrade rather than throw.
                model.ParseFailed = true;
                return model;
            }

            if (doc.Root == null)
            {
                model.ParseFailed = true;
                return model;
            }

            // ---- tabs & sections ----
            var tabs = Descendants(doc, "tab").ToList();
            model.Tabs = tabs.Count;
            model.HiddenTabs = tabs.Count(IsHiddenByDefault);
            model.Sections = Descendants(doc, "section").Count();

            // ---- controls (fields, subgrids, quick views, PCF/custom) ----
            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var control in Descendants(doc, "control"))
            {
                var classId = NormalizeClassId(Attr(control, "classid"));
                if (!string.IsNullOrEmpty(classId))
                    model.ControlClassIds.Add(classId);

                bool isSubgrid = classId == SubgridClassId;
                bool isQuickView = classId == QuickViewClassId;
                bool isPcf = control.Descendants().Any(e =>
                    string.Equals(e.Name.LocalName, "customControl", StringComparison.OrdinalIgnoreCase));

                if (isSubgrid) model.Subgrids++;
                if (isQuickView) model.QuickViews++;
                if (isPcf) model.CustomControls++;

                var dataField = Attr(control, "datafieldname");
                if (!string.IsNullOrWhiteSpace(dataField) && !isSubgrid && !isQuickView)
                {
                    model.Fields++;
                    if (IsInHiddenCell(control))
                        model.HiddenFields++;
                }
            }

            // ---- scripting: form libraries + event handlers ----
            foreach (var lib in Descendants(doc, "Library"))
            {
                var name = Attr(lib, "name");
                if (!string.IsNullOrWhiteSpace(name)) libraries.Add(name);
            }

            foreach (var ev in Descendants(doc, "event"))
            {
                var eventName = (Attr(ev, "name") ?? "").Trim();
                var handlers = ev.Descendants()
                    .Where(e => string.Equals(e.Name.LocalName, "Handler", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var h in handlers)
                {
                    var libName = Attr(h, "libraryName");
                    if (!string.IsNullOrWhiteSpace(libName)) libraries.Add(libName);
                }

                if (string.Equals(eventName, "onload", StringComparison.OrdinalIgnoreCase))
                    model.OnLoadHandlers += handlers.Count;
                else if (string.Equals(eventName, "onchange", StringComparison.OrdinalIgnoreCase))
                    model.OnChangeHandlers += handlers.Count;
                else if (string.Equals(eventName, "tabstatechange", StringComparison.OrdinalIgnoreCase))
                    model.TabStateChangeHandlers += handlers.Count;
            }

            model.JsLibraries = libraries.Count;

            return model;
        }

        // ---- helpers ----

        private static IEnumerable<XElement> Descendants(XDocument doc, string localName) =>
            doc.Descendants().Where(e =>
                string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));

        /// <summary>A tab/section is hidden by default when it carries <c>visible="false"/"0"</c>.</summary>
        private static bool IsHiddenByDefault(XElement e)
        {
            var v = Attr(e, "visible");
            return string.Equals(v, "false", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(v, "0", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A field is not above the fold when its containing <c>&lt;cell&gt;</c> is <c>visible="false"</c>.</summary>
        private static bool IsInHiddenCell(XElement control)
        {
            var cell = control.Ancestors().FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, "cell", StringComparison.OrdinalIgnoreCase));
            return cell != null && IsHiddenByDefault(cell);
        }

        private static string Attr(XElement e, string name) =>
            e.Attributes().FirstOrDefault(a =>
                string.Equals(a.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;

        /// <summary>Upper-cases a classid and strips surrounding braces for stable comparison.</summary>
        private static string NormalizeClassId(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId)) return null;
            return classId.Trim().Trim('{', '}').ToUpperInvariant();
        }
    }
}
