using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.Core.FetchXml
{
    /// <summary>
    /// A parsed, structural view of a FetchXML query — entities, attributes, filters, joins, orders,
    /// and paging flags. Deliberately UI-free and SDK-free (built from <c>System.Xml.Linq</c> only, no
    /// <c>Microsoft.Xrm.Sdk</c>) so the parser + rule engine are unit-testable and reusable by other
    /// performance tools (View / Dashboard analyzers).
    /// </summary>
    public sealed class ParsedFetchXml
    {
        /// <summary>Logical name of the root <c>&lt;entity&gt;</c>.</summary>
        public string RootEntity { get; set; }

        /// <summary>True if an <c>&lt;all-attributes/&gt;</c> element appears anywhere in the query.</summary>
        public bool AllAttributes { get; set; }

        /// <summary>Attribute logical names selected directly on the root entity.</summary>
        public List<string> Attributes { get; } = new List<string>();

        /// <summary>Count of <c>&lt;attribute&gt;</c> elements across the root entity and every link-entity.</summary>
        public int TotalAttributeCount { get; set; }

        /// <summary>True if the root entity carries a <c>&lt;filter&gt;</c> with at least one condition.</summary>
        public bool HasRootFilter { get; set; }

        /// <summary>Top-level link-entities (each may nest further links).</summary>
        public List<ParsedLink> Links { get; } = new List<ParsedLink>();

        /// <summary>All orders declared on the root entity and any link-entity.</summary>
        public List<ParsedOrder> Orders { get; } = new List<ParsedOrder>();

        /// <summary>True if <c>fetch aggregate="true"</c>.</summary>
        public bool HasAggregate { get; set; }

        /// <summary>True if <c>fetch distinct="true"</c>.</summary>
        public bool Distinct { get; set; }

        /// <summary>True if <c>fetch no-lock="true"</c>.</summary>
        public bool NoLock { get; set; }

        /// <summary><c>fetch top="N"</c>, when present.</summary>
        public int? Top { get; set; }

        /// <summary><c>fetch count="N"</c> (page size), when present.</summary>
        public int? PageSize { get; set; }

        /// <summary>Flattened enumeration of every link-entity (root's links plus all nested links).</summary>
        public IEnumerable<ParsedLink> AllLinks()
        {
            foreach (var link in Links)
                foreach (var l in Flatten(link))
                    yield return l;
        }

        /// <summary>Total number of link-entities (joins) at every depth.</summary>
        public int LinkCount => AllLinks().Count();

        private static IEnumerable<ParsedLink> Flatten(ParsedLink link)
        {
            yield return link;
            foreach (var child in link.Links)
                foreach (var l in Flatten(child))
                    yield return l;
        }
    }

    /// <summary>A parsed <c>&lt;link-entity&gt;</c> (join) within a FetchXML query.</summary>
    public sealed class ParsedLink
    {
        public string Entity { get; set; }
        public string Alias { get; set; }
        /// <summary>"inner" or "outer" (defaults to "inner" when unspecified, matching Dataverse).</summary>
        public string LinkType { get; set; } = "inner";
        public string From { get; set; }
        public string To { get; set; }
        /// <summary>True if this link carries a <c>&lt;filter&gt;</c> with at least one condition.</summary>
        public bool HasFilter { get; set; }
        /// <summary>Nested link-entities joined off this one.</summary>
        public List<ParsedLink> Links { get; } = new List<ParsedLink>();

        public bool IsOuter =>
            string.Equals(LinkType, "outer", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A parsed <c>&lt;order&gt;</c> element.</summary>
    public sealed class ParsedOrder
    {
        public string Attribute { get; set; }
        public bool Descending { get; set; }
        /// <summary>True when the order is declared on a link-entity rather than the root entity.</summary>
        public bool OnLinkEntity { get; set; }
    }

    /// <summary>Result of <see cref="FetchXmlParser.Parse"/>: either a populated query or an error.</summary>
    public sealed class FetchXmlParseResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public ParsedFetchXml Query { get; set; }

        public static FetchXmlParseResult Fail(string error) =>
            new FetchXmlParseResult { Success = false, Error = error };

        public static FetchXmlParseResult Ok(ParsedFetchXml query) =>
            new FetchXmlParseResult { Success = true, Query = query };
    }
}
