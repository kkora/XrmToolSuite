using System;
using System.Linq;
using System.Xml.Linq;

namespace XrmToolSuite.Core.FetchXml
{
    /// <summary>
    /// Parses a FetchXML string into a <see cref="ParsedFetchXml"/> using <c>System.Xml.Linq</c> only
    /// (no Dataverse SDK). Pure and deterministic, so it is fully unit-testable off a live connection.
    /// Malformed XML or a missing <c>&lt;fetch&gt;</c>/<c>&lt;entity&gt;</c> yields a failed result with a
    /// clear <see cref="FetchXmlParseResult.Error"/> rather than throwing.
    /// </summary>
    public static class FetchXmlParser
    {
        public static FetchXmlParseResult Parse(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return FetchXmlParseResult.Fail("FetchXML is empty. Paste or load a query first.");

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                return FetchXmlParseResult.Fail("Malformed XML: " + ex.Message);
            }

            var fetch = doc.Root;
            if (fetch == null || !string.Equals(fetch.Name.LocalName, "fetch", StringComparison.OrdinalIgnoreCase))
                return FetchXmlParseResult.Fail("Root element must be <fetch>.");

            var entity = fetch.Elements().FirstOrDefault(e =>
                string.Equals(e.Name.LocalName, "entity", StringComparison.OrdinalIgnoreCase));
            if (entity == null)
                return FetchXmlParseResult.Fail("<fetch> must contain an <entity> element.");

            var q = new ParsedFetchXml
            {
                RootEntity = Attr(entity, "name"),
                HasAggregate = BoolAttr(fetch, "aggregate"),
                Distinct = BoolAttr(fetch, "distinct"),
                NoLock = BoolAttr(fetch, "no-lock"),
                Top = IntAttr(fetch, "top"),
                PageSize = IntAttr(fetch, "count"),
                HasRootFilter = HasRealFilter(entity)
            };

            // Root-entity attributes.
            foreach (var a in Children(entity, "attribute"))
            {
                var name = Attr(a, "name");
                if (!string.IsNullOrEmpty(name))
                    q.Attributes.Add(name);
            }

            // <all-attributes/> anywhere in the tree is a payload risk.
            q.AllAttributes = entity.Descendants().Any(e =>
                string.Equals(e.Name.LocalName, "all-attributes", StringComparison.OrdinalIgnoreCase));

            // Total <attribute> count across the whole tree (root + every link-entity).
            q.TotalAttributeCount = entity.Descendants().Count(e =>
                string.Equals(e.Name.LocalName, "attribute", StringComparison.OrdinalIgnoreCase));

            // Root-entity orders.
            foreach (var o in Children(entity, "order"))
                q.Orders.Add(ParseOrder(o, onLinkEntity: false));

            // Link-entities (recursive), collecting their orders too.
            foreach (var le in Children(entity, "link-entity"))
                q.Links.Add(ParseLink(le, q));

            return FetchXmlParseResult.Ok(q);
        }

        private static ParsedLink ParseLink(XElement le, ParsedFetchXml q)
        {
            var link = new ParsedLink
            {
                Entity = Attr(le, "name"),
                Alias = Attr(le, "alias"),
                LinkType = string.IsNullOrEmpty(Attr(le, "link-type")) ? "inner" : Attr(le, "link-type"),
                From = Attr(le, "from"),
                To = Attr(le, "to"),
                HasFilter = HasRealFilter(le)
            };

            foreach (var o in Children(le, "order"))
                q.Orders.Add(ParseOrder(o, onLinkEntity: true));

            foreach (var nested in Children(le, "link-entity"))
                link.Links.Add(ParseLink(nested, q));

            return link;
        }

        private static ParsedOrder ParseOrder(XElement o, bool onLinkEntity) => new ParsedOrder
        {
            Attribute = Attr(o, "attribute"),
            Descending = BoolAttr(o, "descending"),
            OnLinkEntity = onLinkEntity
        };

        /// <summary>True if <paramref name="parent"/> has a direct <c>&lt;filter&gt;</c> containing at least one condition.</summary>
        private static bool HasRealFilter(XElement parent) =>
            Children(parent, "filter").Any(f =>
                f.Descendants().Any(c => string.Equals(c.Name.LocalName, "condition", StringComparison.OrdinalIgnoreCase)));

        private static System.Collections.Generic.IEnumerable<XElement> Children(XElement parent, string local) =>
            parent.Elements().Where(e => string.Equals(e.Name.LocalName, local, StringComparison.OrdinalIgnoreCase));

        private static string Attr(XElement e, string name) =>
            e.Attributes().FirstOrDefault(a => string.Equals(a.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;

        private static bool BoolAttr(XElement e, string name)
        {
            var v = Attr(e, name);
            return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1";
        }

        private static int? IntAttr(XElement e, string name)
        {
            var v = Attr(e, name);
            return int.TryParse(v, out var n) ? n : (int?)null;
        }
    }
}
