using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.PortalHealthAnalyzer.Analysis
{
    /// <summary>
    /// The only Dataverse-touching piece of the tool. Detects which Power Pages schema (<c>adx_</c> vs
    /// <c>mspp_</c>) is provisioned, lists websites, and retrieves a selected website's configuration into
    /// a schema-normalized <see cref="PortalInventory"/> the SDK-free rules can score. Every table
    /// retrieval degrades to an informational note when the table is missing — it never throws — so a
    /// half-provisioned environment still yields a usable report. Deliberately excluded from the unit-test
    /// set (which covers only the SDK-free rules).
    /// </summary>
    public sealed class PortalCollector
    {
        /// <summary>
        /// Per-schema logical/attribute-name map. Every Power Pages table follows the same shape in both
        /// schemas apart from the prefix and a couple of table renames, so one descriptor per schema keeps
        /// the collector DRY.
        /// </summary>
        private sealed class SchemaMap
        {
            public PortalSchema Schema;
            public string P;                    // prefix: "adx_" or "mspp_"
            public string Website;
            public string WebPage;
            public string WebTemplate;
            public string PageTemplate;
            public string ContentSnippet;
            public string SiteSetting;
            public string WebRole;
            public string Permission;
            public string Form;                 // entity form (adx) / basic form (mspp)
            public string List;                 // entity list (adx) / list (mspp)
            public string WebFile;
            public string Redirect;

            // Attribute helpers.
            public string A(string tail) => P + tail;      // e.g. A("name") => "adx_name"
            public string WebsiteId => P + "websiteid";

            public static SchemaMap For(PortalSchema schema)
            {
                var p = schema == PortalSchema.Adx ? "adx_" : "mspp_";
                return new SchemaMap
                {
                    Schema = schema,
                    P = p,
                    Website = p + "website",
                    WebPage = p + "webpage",
                    WebTemplate = p + "webtemplate",
                    PageTemplate = p + "pagetemplate",
                    ContentSnippet = p + "contentsnippet",
                    SiteSetting = p + "sitesetting",
                    WebRole = p + "webrole",
                    Permission = schema == PortalSchema.Adx ? "adx_entitypermission" : "mspp_tablepermission",
                    Form = schema == PortalSchema.Adx ? "adx_entityform" : "mspp_basicform",
                    List = schema == PortalSchema.Adx ? "adx_entitylist" : "mspp_list",
                    WebFile = p + "webfile",
                    Redirect = p + "redirect",
                };
            }
        }

        private readonly Dictionary<string, bool> _entityExistsCache =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Lists every Power Pages website across both schemas. A schema whose <c>website</c> table does not
        /// exist (not provisioned) is silently skipped rather than throwing.
        /// </summary>
        public List<(Guid id, string name, PortalSchema schema)> ListWebsites(
            IOrganizationService svc, BackgroundWorker worker)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var results = new List<(Guid, string, PortalSchema)>();

            foreach (var schema in new[] { PortalSchema.Adx, PortalSchema.Mspp })
            {
                if (worker?.CancellationPending == true) break;
                var map = SchemaMap.For(schema);
                try
                {
                    var query = new QueryExpression(map.Website)
                    {
                        ColumnSet = new ColumnSet(map.A("name")),
                        NoLock = true
                    };
                    var sites = svc.RetrieveAll(query, worker: worker);
                    foreach (var s in sites)
                        results.Add((s.Id, s.GetAttributeValue<string>(map.A("name")) ?? "(unnamed)", schema));
                }
                catch (Exception)
                {
                    // Table not provisioned for this schema — nothing to list, keep going.
                }
            }

            return results.OrderBy(r => r.Item3).ThenBy(r => r.Item2, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Retrieves the selected website's configuration into a normalized inventory. Each table is fetched
        /// independently; a missing table adds an entry to <see cref="PortalInventory.UnavailableTables"/>
        /// and is skipped. Honors cancellation and reports progress.
        /// </summary>
        public PortalInventory Collect(
            IOrganizationService svc,
            Guid websiteId,
            PortalSchema schema,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var map = SchemaMap.For(schema);
            var inv = new PortalInventory { Schema = schema, WebsiteId = websiteId };

            // Website name (best-effort).
            try
            {
                var site = svc.Retrieve(map.Website, websiteId, new ColumnSet(map.A("name")));
                inv.WebsiteName = site.GetAttributeValue<string>(map.A("name"));
            }
            catch { inv.WebsiteName = websiteId.ToString(); }

            progress?.Invoke("Retrieving web pages…");
            inv.Pages = RetrieveTable(svc, worker, inv, map.WebPage,
                new ColumnSet(map.A("name"), map.A("parentpageid"), map.A("pagetemplateid"), "statecode"),
                map.WebsiteId, websiteId,
                e => new PortalRecord
                {
                    Id = e.Id,
                    Name = e.GetAttributeValue<string>(map.A("name")),
                    Active = IsActive(e),
                    ParentId = Ref(e, map.A("parentpageid")),
                    TemplateId = Ref(e, map.A("pagetemplateid"))
                });

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving web templates…");
            inv.Templates = RetrieveTable(svc, worker, inv, map.WebTemplate,
                new ColumnSet(map.A("name"), "statecode"), map.WebsiteId, websiteId,
                e => Simple(e, map));

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving page templates…");
            inv.PageTemplates = RetrieveTable(svc, worker, inv, map.PageTemplate,
                new ColumnSet(map.A("name"), map.A("webtemplateid"), "statecode"), map.WebsiteId, websiteId,
                e => new PortalRecord
                {
                    Id = e.Id,
                    Name = e.GetAttributeValue<string>(map.A("name")),
                    Active = IsActive(e),
                    TemplateId = Ref(e, map.A("webtemplateid"))
                });

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving content snippets…");
            inv.Snippets = RetrieveTable(svc, worker, inv, map.ContentSnippet,
                new ColumnSet(map.A("name"), "statecode"), map.WebsiteId, websiteId,
                e => Simple(e, map));

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving site settings…");
            inv.Settings = RetrieveTable(svc, worker, inv, map.SiteSetting,
                new ColumnSet(map.A("name"), map.A("value")), map.WebsiteId, websiteId,
                e => new PortalSetting
                {
                    Name = e.GetAttributeValue<string>(map.A("name")),
                    Value = e.GetAttributeValue<string>(map.A("value"))
                });

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving web roles…");
            inv.WebRoles = RetrieveTable(svc, worker, inv, map.WebRole,
                new ColumnSet(map.A("name")), map.WebsiteId, websiteId, e => Simple(e, map));

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving table permissions…");
            inv.Permissions = RetrieveTable(svc, worker, inv, map.Permission,
                new ColumnSet(map.A("name"), map.A("entitylogicalname"), map.A("scope")), map.WebsiteId, websiteId,
                e => new PortalPermission
                {
                    Name = e.GetAttributeValue<string>(map.A("name")),
                    EntityLogicalName = e.GetAttributeValue<string>(map.A("entitylogicalname")),
                    Scope = e.FormattedValues.Contains(map.A("scope")) ? e.FormattedValues[map.A("scope")] : null,
                    // Anonymous detection is a deep, per-web-role join — deferred to the Portal Security Scanner.
                    AnonymousReadWriteOrDelete = false
                });

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving forms…");
            inv.Forms = RetrieveTable(svc, worker, inv, map.Form,
                new ColumnSet(map.A("name"), map.A("entityname")), map.WebsiteId, websiteId,
                e => new PortalForm
                {
                    Name = e.GetAttributeValue<string>(map.A("name")),
                    EntityLogicalName = e.GetAttributeValue<string>(map.A("entityname")),
                    Kind = "Form",
                    EntityExists = EntityExists(svc, e.GetAttributeValue<string>(map.A("entityname")))
                });

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving lists…");
            var lists = RetrieveTable(svc, worker, inv, map.List,
                new ColumnSet(map.A("name"), map.A("entityname")), map.WebsiteId, websiteId,
                e => new PortalForm
                {
                    Name = e.GetAttributeValue<string>(map.A("name")),
                    EntityLogicalName = e.GetAttributeValue<string>(map.A("entityname")),
                    Kind = "List",
                    EntityExists = EntityExists(svc, e.GetAttributeValue<string>(map.A("entityname")))
                });
            // Lists feed the entity-binding rule (via Forms) and the count card (via Lists).
            inv.Forms.AddRange(lists);
            inv.Lists = lists.Select(l => new PortalRecord { Name = l.Name }).ToList();

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving web files…");
            inv.WebFiles = RetrieveTable(svc, worker, inv, map.WebFile,
                new ColumnSet(map.A("name"), map.A("parentpageid"), "statecode"), map.WebsiteId, websiteId,
                e => new PortalRecord
                {
                    Id = e.Id,
                    Name = e.GetAttributeValue<string>(map.A("name")),
                    Active = IsActive(e),
                    ParentId = Ref(e, map.A("parentpageid"))
                });

            if (Cancelled(worker)) return inv;
            progress?.Invoke("Retrieving redirects…");
            inv.Redirects = RetrieveTable(svc, worker, inv, map.Redirect,
                new ColumnSet(map.A("name")), map.WebsiteId, websiteId, e => Simple(e, map));

            progress?.Invoke($"Collected configuration for '{inv.WebsiteName}'.");
            return inv;
        }

        // --------------------------------------------------------------- Retrieval helpers

        /// <summary>
        /// Retrieves every record of <paramref name="logicalName"/> for the website and projects them via
        /// <paramref name="project"/>. A missing table (or any retrieval error) records the table as
        /// unavailable and returns an empty list rather than throwing.
        /// </summary>
        private List<T> RetrieveTable<T>(
            IOrganizationService svc, BackgroundWorker worker, PortalInventory inv,
            string logicalName, ColumnSet columns, string websiteIdAttr, Guid websiteId,
            Func<Entity, T> project)
        {
            try
            {
                var query = new QueryExpression(logicalName) { ColumnSet = columns, NoLock = true };
                query.Criteria.AddCondition(websiteIdAttr, ConditionOperator.Equal, websiteId);
                var rows = svc.RetrieveAll(query, worker: worker);
                return rows.Select(project).ToList();
            }
            catch (Exception)
            {
                inv.UnavailableTables.Add(logicalName);
                return new List<T>();
            }
        }

        private static PortalRecord Simple(Entity e, SchemaMap map) => new PortalRecord
        {
            Id = e.Id,
            Name = e.GetAttributeValue<string>(map.A("name")),
            Active = IsActive(e)
        };

        private static bool IsActive(Entity e)
        {
            var state = e.GetAttributeValue<OptionSetValue>("statecode");
            return state == null || state.Value == 0; // 0 == Active; absent = treat as active
        }

        private static Guid? Ref(Entity e, string attr)
        {
            var er = e.GetAttributeValue<EntityReference>(attr);
            return er?.Id;
        }

        private static bool Cancelled(BackgroundWorker worker) => worker?.CancellationPending == true;

        /// <summary>True when a Dataverse table exists and is enabled. Cached; missing tables return false.</summary>
        private bool EntityExists(IOrganizationService svc, string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName)) return false;
            if (_entityExistsCache.TryGetValue(logicalName, out var cached)) return cached;

            bool exists;
            try
            {
                var resp = (RetrieveEntityResponse)svc.Execute(new RetrieveEntityRequest
                {
                    LogicalName = logicalName,
                    EntityFilters = EntityFilters.Entity
                });
                exists = resp.EntityMetadata != null;
            }
            catch (Exception)
            {
                exists = false; // table does not exist / not accessible
            }

            _entityExistsCache[logicalName] = exists;
            return exists;
        }
    }
}
