using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.SolutionDocumentationGenerator.Doc
{
    /// <summary>Lightweight solution descriptor for the picker (SDK-derived, no SDK types leak out).</summary>
    public sealed class DocSolutionInfo
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
        public bool IsManaged { get; set; }
    }

    /// <summary>
    /// The Dataverse-facing half of the tool: scans one solution into a plain, SDK-free
    /// <see cref="SolutionScanData"/> DTO. Uses Microsoft.Xrm.Sdk / Sdk.Metadata, so it is NOT part of the
    /// SDK-free unit-test compile set. Read-only. Every source runs in its own try/catch and degrades to a
    /// name in <see cref="SolutionScanData.UnavailableSources"/> rather than aborting the whole scan.
    /// Environment-variable current values and secrets are NEVER read.
    /// </summary>
    public sealed class DocCollector
    {
        // ---- component-type → category classification (SectionKinds values) ----

        private const string CatOther = "Other";

        /// <summary>Solutions available to document (visible, excluding the system defaults).</summary>
        public List<DocSolutionInfo> ListSolutions(IOrganizationService svc)
        {
            var qe = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "uniquename", "friendlyname", "version", "ismanaged"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                        new ConditionExpression("uniquename", ConditionOperator.NotIn, "Default", "Active", "Basic")
                    }
                },
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
            };
            return svc.RetrieveMultiple(qe).Entities.Select(s => new DocSolutionInfo
            {
                Id = s.Id,
                UniqueName = s.GetAttributeValue<string>("uniquename"),
                FriendlyName = s.GetAttributeValue<string>("friendlyname"),
                Version = s.GetAttributeValue<string>("version"),
                IsManaged = s.GetAttributeValue<bool?>("ismanaged") ?? false
            }).ToList();
        }

        public SolutionScanData Scan(IOrganizationService svc, Guid solutionId, DocOptions opts,
            BackgroundWorker worker, Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var scan = new SolutionScanData { GeneratedUtc = DateTime.UtcNow };

            // ---- header ----
            progress?.Invoke("Reading solution header…");
            var entityIds = new List<Guid>();
            var byId = new Dictionary<Guid, ScanComponent>();
            try
            {
                var sol = svc.Retrieve("solution", solutionId, new ColumnSet(
                    "friendlyname", "uniquename", "version", "ismanaged", "publisherid",
                    "description", "installedon", "modifiedon"));
                scan.SolutionName = sol.GetAttributeValue<string>("friendlyname");
                scan.UniqueName = sol.GetAttributeValue<string>("uniquename");
                scan.Version = sol.GetAttributeValue<string>("version");
                scan.IsManaged = sol.GetAttributeValue<bool?>("ismanaged") ?? false;
                scan.Description = sol.GetAttributeValue<string>("description");
                scan.InstalledOn = sol.GetAttributeValue<DateTime?>("installedon");
                scan.ModifiedOn = sol.GetAttributeValue<DateTime?>("modifiedon");

                var pubRef = sol.GetAttributeValue<EntityReference>("publisherid");
                if (pubRef != null)
                {
                    try
                    {
                        var pub = svc.Retrieve("publisher", pubRef.Id,
                            new ColumnSet("friendlyname", "customizationprefix"));
                        scan.Publisher = pub.GetAttributeValue<string>("friendlyname") ?? pubRef.Name;
                        scan.PublisherPrefix = pub.GetAttributeValue<string>("customizationprefix");
                    }
                    catch { scan.Publisher = pubRef.Name; }
                }
            }
            catch (Exception ex)
            {
                scan.UnavailableSources.Add("Solution header: " + ex.Message);
            }

            // ---- component inventory (summary-first, solutioncomponent fallback) ----
            CollectComponents(svc, solutionId, scan, worker, progress, entityIds, byId);

            // ---- schema (deep metadata for tables) ----
            if (WantsSchema(opts))
                CollectSchema(svc, entityIds, scan, worker, progress);
            else
                scan.Entities = new List<DocEntity>();

            // ---- detail enrichment (best-effort; degrades per source) ----
            EnrichDetails(svc, scan, worker, progress, byId);

            // ---- inventory rollup ----
            scan.Inventory = BuildInventory(scan);

            return scan;
        }

        private static bool WantsSchema(DocOptions opts) =>
            opts?.Sections == null || opts.Sections.Schema || opts.Sections.Diagrams;

        // =====================================================================================
        // Components (msdyn_solutioncomponentsummary → generic rows + entity ids)
        // =====================================================================================

        private void CollectComponents(IOrganizationService svc, Guid solutionId, SolutionScanData scan,
            BackgroundWorker worker, Action<string> progress, List<Guid> entityIds,
            Dictionary<Guid, ScanComponent> byId)
        {
            progress?.Invoke("Enumerating solution components…");
            var optionSetIds = new List<Guid>();

            bool summaryOk = false;
            try
            {
                var qe = new QueryExpression("msdyn_solutioncomponentsummary")
                {
                    ColumnSet = new ColumnSet(
                        "msdyn_name", "msdyn_displayname", "msdyn_componenttype", "msdyn_componenttypename",
                        "msdyn_objectid", "msdyn_ismanaged", "msdyn_modifiedon", "msdyn_schemaname"),
                    Criteria =
                    {
                        Conditions = { new ConditionExpression("msdyn_solutionid", ConditionOperator.Equal, solutionId) }
                    }
                };
                var rows = svc.RetrieveAll(qe, worker: worker);
                summaryOk = true;

                foreach (var r in rows)
                {
                    if (worker?.CancellationPending == true) break;
                    int? code = r.GetAttributeValue<OptionSetValue>("msdyn_componenttype")?.Value;
                    string typeName = r.GetAttributeValue<string>("msdyn_componenttypename");
                    var objectId = ParseGuid(r, "msdyn_objectid");

                    AddComponent(scan, entityIds, optionSetIds, byId, code, typeName,
                        name: r.GetAttributeValue<string>("msdyn_displayname") ?? r.GetAttributeValue<string>("msdyn_name"),
                        schema: r.GetAttributeValue<string>("msdyn_schemaname") ?? r.GetAttributeValue<string>("msdyn_name"),
                        managed: r.GetAttributeValue<bool?>("msdyn_ismanaged"),
                        modified: r.GetAttributeValue<DateTime?>("msdyn_modifiedon"),
                        objectId: objectId);
                }
            }
            catch
            {
                scan.UnavailableSources.Add("Component summary (msdyn_solutioncomponentsummary)");
            }

            if (!summaryOk)
            {
                // Fallback: raw solutioncomponent (type code only; names come from enrichment where possible).
                try
                {
                    var qe = new QueryExpression("solutioncomponent")
                    {
                        ColumnSet = new ColumnSet("objectid", "componenttype", "ismanaged"),
                        Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId) } }
                    };
                    foreach (var r in svc.RetrieveAll(qe, worker: worker))
                    {
                        if (worker?.CancellationPending == true) break;
                        int? code = r.GetAttributeValue<OptionSetValue>("componenttype")?.Value;
                        var objectId = r.GetAttributeValue<Guid>("objectid");
                        AddComponent(scan, entityIds, optionSetIds, byId, code, ComponentTypeName(code),
                            name: null, schema: null,
                            managed: r.GetAttributeValue<bool?>("ismanaged"), modified: null, objectId: objectId);
                    }
                }
                catch (Exception ex)
                {
                    scan.UnavailableSources.Add("Solution components: " + ex.Message);
                }
            }

            // Global choices (best-effort).
            CollectChoices(svc, optionSetIds, scan, worker, progress);
        }

        private void AddComponent(SolutionScanData scan, List<Guid> entityIds, List<Guid> optionSetIds,
            Dictionary<Guid, ScanComponent> byId, int? code, string typeName,
            string name, string schema, bool? managed, DateTime? modified, Guid objectId)
        {
            // Entities and attributes/optionsets are handled via metadata, not the generic component list.
            if (code == 1) // Entity
            {
                if (objectId != Guid.Empty) entityIds.Add(objectId);
                return;
            }
            if (code == 2) return;            // Attribute — covered by its entity
            if (code == 9)                    // OptionSet (global choice)
            {
                if (objectId != Guid.Empty) optionSetIds.Add(objectId);
                return;
            }

            var category = Classify(code, typeName);
            if (category == null) return;     // ignored component types (relationships, keys, ribbons, …)

            var comp = new ScanComponent
            {
                Category = category,
                ComponentType = string.IsNullOrEmpty(typeName) ? ComponentTypeName(code) : typeName,
                Name = name,
                SchemaName = schema,
                IsManaged = managed,
                ModifiedOn = modified
            };
            scan.Components.Add(comp);
            if (objectId != Guid.Empty && !byId.ContainsKey(objectId)) byId[objectId] = comp;
        }

        /// <summary>Maps a component (by type code, falling back to the type-name string) to a section category.</summary>
        private static string Classify(int? code, string typeName)
        {
            switch (code)
            {
                case 20: return SectionKinds.Roles;                  // Role
                case 24: return SectionKinds.Forms;                  // SystemForm
                case 26: case 59: return SectionKinds.Views;         // SavedQuery / Chart
                case 29: return SectionKinds.Automation;             // Workflow
                case 61: case 66: case 68: case 150: return SectionKinds.WebResources; // WebResource / PCF
                case 62: case 80: case 300: return SectionKinds.Apps; // SiteMap / AppModule / CanvasApp
                case 90: case 91: case 92: case 93: return SectionKinds.Plugins; // Plugin type/assembly/step/image
                case 380: case 381: return SectionKinds.Config;      // Env var definition/value
            }

            var t = (typeName ?? "").ToLowerInvariant();
            if (t.Length == 0) return code.HasValue ? CatOther : null;
            if (t.Contains("security role")) return SectionKinds.Roles;
            if (t.Contains("form")) return SectionKinds.Forms;
            if (t.Contains("view") || t.Contains("saved query") || t.Contains("chart") || t.Contains("dashboard")) return SectionKinds.Views;
            if (t.Contains("app") || t.Contains("site map") || t.Contains("sitemap")) return SectionKinds.Apps;
            if (t.Contains("process") || t.Contains("workflow") || t.Contains("business rule") || t.Contains("flow")) return SectionKinds.Automation;
            if (t.Contains("plugin") || t.Contains("plug-in") || t.Contains("sdk message") || t.Contains("assembly")) return SectionKinds.Plugins;
            if (t.Contains("custom api")) return SectionKinds.CustomApis;
            if (t.Contains("web resource") || t.Contains("custom control")) return SectionKinds.WebResources;
            if (t.Contains("environment variable") || t.Contains("connection reference")) return SectionKinds.Config;
            return CatOther;
        }

        private static string ComponentTypeName(int? code)
        {
            switch (code)
            {
                case 1: return "Table";
                case 2: return "Column";
                case 9: return "Choice";
                case 20: return "Security Role";
                case 24: return "System Form";
                case 26: return "View";
                case 29: return "Process";
                case 59: return "Chart";
                case 61: return "Web Resource";
                case 62: return "Site Map";
                case 80: return "App Module";
                case 90: return "Plug-in Type";
                case 91: return "Plug-in Assembly";
                case 92: return "Plug-in Step";
                case 93: return "Plug-in Step Image";
                case 150: return "Custom Control";
                case 300: return "Canvas App";
                case 380: return "Environment Variable";
                case 381: return "Environment Variable Value";
                default: return code.HasValue ? ("Component type " + code.Value.ToString(CultureInfo.InvariantCulture)) : "Component";
            }
        }

        // =====================================================================================
        // Schema (entity metadata)
        // =====================================================================================

        private void CollectSchema(IOrganizationService svc, List<Guid> entityIds, SolutionScanData scan,
            BackgroundWorker worker, Action<string> progress)
        {
            var ids = entityIds.Distinct().ToList();
            if (ids.Count == 0) { scan.Entities = new List<DocEntity>(); return; }

            var entities = new List<DocEntity>();
            int done = 0;
            bool anyFailed = false;
            foreach (var id in ids)
            {
                if (worker?.CancellationPending == true) break;
                progress?.Invoke($"Reading table metadata ({++done}/{ids.Count})…");
                worker?.ReportProgress(done * 100 / Math.Max(1, ids.Count), $"Table {done}/{ids.Count}");
                try
                {
                    var resp = (RetrieveEntityResponse)svc.Execute(new RetrieveEntityRequest
                    {
                        MetadataId = id,
                        EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                        RetrieveAsIfPublished = true
                    });
                    entities.Add(BuildEntity(resp.EntityMetadata));
                }
                catch
                {
                    anyFailed = true;
                }
            }

            scan.Entities = entities;
            if (anyFailed) scan.UnavailableSources.Add("Some table (Schema) metadata");
        }

        private static DocEntity BuildEntity(EntityMetadata em)
        {
            var e = new DocEntity
            {
                LogicalName = em.LogicalName,
                DisplayName = Label(em.DisplayName) ?? em.LogicalName,
                SchemaName = em.SchemaName,
                IsCustom = em.IsCustomEntity ?? false,
                IsManaged = em.IsManaged ?? false,
                Description = Label(em.Description),
                PrimaryIdColumn = em.PrimaryIdAttribute,
                PrimaryNameColumn = em.PrimaryNameAttribute
            };

            foreach (var a in (em.Attributes ?? new AttributeMetadata[0])
                         .Where(a => a.LogicalName != null && a.AttributeOf == null)
                         .OrderBy(a => a.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                e.Columns.Add(new DocColumn
                {
                    LogicalName = a.LogicalName,
                    DisplayName = Label(a.DisplayName) ?? a.LogicalName,
                    Type = a.AttributeType?.ToString() ?? "Unknown",
                    RequiredLevel = a.RequiredLevel?.Value.ToString(),
                    Description = Label(a.Description),
                    IsPrimaryId = a.IsPrimaryId ?? false,
                    IsPrimaryName = a.IsPrimaryName ?? false,
                    IsCustom = a.IsCustomAttribute ?? false
                });
            }

            var selected = new HashSet<string>(new[] { em.LogicalName }, StringComparer.OrdinalIgnoreCase);
            foreach (var r in em.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
            {
                e.Relationships.Add(new DocRelationship
                {
                    SchemaName = r.SchemaName,
                    RelationType = "OneToMany",
                    FromTable = r.ReferencedEntity,
                    ToTable = r.ReferencingEntity,
                    LookupColumn = r.ReferencingAttribute
                });
            }
            foreach (var r in em.ManyToOneRelationships ?? new OneToManyRelationshipMetadata[0])
            {
                e.Relationships.Add(new DocRelationship
                {
                    SchemaName = r.SchemaName,
                    RelationType = "ManyToOne",
                    FromTable = r.ReferencingEntity,
                    ToTable = r.ReferencedEntity,
                    LookupColumn = r.ReferencingAttribute
                });
            }
            foreach (var r in em.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0])
            {
                e.Relationships.Add(new DocRelationship
                {
                    SchemaName = r.SchemaName,
                    RelationType = "ManyToMany",
                    FromTable = r.Entity1LogicalName,
                    ToTable = r.Entity2LogicalName,
                    LookupColumn = r.IntersectEntityName
                });
            }

            foreach (var k in em.Keys ?? new EntityKeyMetadata[0])
            {
                e.Keys.Add(new DocKey
                {
                    Name = Label(k.DisplayName) ?? k.LogicalName ?? k.SchemaName,
                    Columns = (k.KeyAttributes ?? new string[0]).ToList()
                });
            }

            return e;
        }

        private void CollectChoices(IOrganizationService svc, List<Guid> optionSetIds, SolutionScanData scan,
            BackgroundWorker worker, Action<string> progress)
        {
            var ids = optionSetIds.Distinct().ToList();
            if (ids.Count == 0) return;

            progress?.Invoke("Reading global choices…");
            bool anyFailed = false;
            foreach (var id in ids)
            {
                if (worker?.CancellationPending == true) break;
                try
                {
                    var resp = (RetrieveOptionSetResponse)svc.Execute(new RetrieveOptionSetRequest { MetadataId = id });
                    if (resp.OptionSetMetadata is OptionSetMetadata os)
                    {
                        scan.Choices.Add(new DocChoice
                        {
                            Name = os.Name,
                            DisplayName = Label(os.DisplayName) ?? os.Name,
                            IsGlobal = os.IsGlobal ?? true,
                            Options = (os.Options ?? new OptionMetadataCollection())
                                .Select(o => Label(o.Label) ?? o.Value?.ToString(CultureInfo.InvariantCulture) ?? "")
                                .ToList()
                        });
                    }
                }
                catch { anyFailed = true; }
            }
            if (anyFailed) scan.UnavailableSources.Add("Some global choices");
        }

        // =====================================================================================
        // Detail enrichment (targeted queries merged back onto the generic components by id)
        // =====================================================================================

        private void EnrichDetails(IOrganizationService svc, SolutionScanData scan, BackgroundWorker worker,
            Action<string> progress, Dictionary<Guid, ScanComponent> byId)
        {
            if (byId.Count == 0) return;

            Enrich(svc, worker, progress, scan, byId, "Automation processes", "workflow", "workflowid",
                new[] { "name", "category", "statecode", "type", "primaryentity" }, (r, c) =>
                {
                    c.ComponentType = WorkflowCategoryLabel(r.GetAttributeValue<OptionSetValue>("category")?.Value);
                    c.Details["Entity"] = r.GetAttributeValue<string>("primaryentity") ?? "";
                    c.Details["State"] = Formatted(r, "statecode");
                });

            Enrich(svc, worker, progress, scan, byId, "Plug-in steps", "sdkmessageprocessingstep", "sdkmessageprocessingstepid",
                new[] { "name", "stage", "mode" }, (r, c) =>
                {
                    c.ComponentType = "Plug-in step";
                    c.Details["Message"] = r.GetAttributeValue<string>("name") ?? "";
                    c.Details["Stage"] = StageLabel(r.GetAttributeValue<OptionSetValue>("stage")?.Value)
                        + "/" + ModeLabel(r.GetAttributeValue<OptionSetValue>("mode")?.Value);
                });

            Enrich(svc, worker, progress, scan, byId, "Forms", "systemform", "formid",
                new[] { "name", "type", "objecttypecode" }, (r, c) =>
                {
                    c.ComponentType = FormTypeLabel(r.GetAttributeValue<OptionSetValue>("type")?.Value);
                    c.Details["Entity"] = r.GetAttributeValue<string>("objecttypecode") ?? "";
                });

            Enrich(svc, worker, progress, scan, byId, "Views", "savedquery", "savedqueryid",
                new[] { "name", "returnedtypecode", "querytype" }, (r, c) =>
                {
                    c.Details["Entity"] = r.GetAttributeValue<string>("returnedtypecode") ?? "";
                });

            Enrich(svc, worker, progress, scan, byId, "Environment variables", "environmentvariabledefinition", "environmentvariabledefinitionid",
                new[] { "schemaname", "displayname", "type" }, (r, c) =>
                {
                    c.ComponentType = "Environment Variable";
                    // SECURITY: only the DECLARED TYPE — never a value/secret.
                    c.Details["Type"] = EnvVarTypeLabel(r.GetAttributeValue<OptionSetValue>("type")?.Value);
                });

            Enrich(svc, worker, progress, scan, byId, "Connection references", "connectionreference", "connectionreferenceid",
                new[] { "connectionreferencedisplayname", "connectionreferencelogicalname", "connectorid" }, (r, c) =>
                {
                    c.ComponentType = "Connection Reference";
                    c.Details["Connector"] = r.GetAttributeValue<string>("connectorid") ?? "";
                });

            Enrich(svc, worker, progress, scan, byId, "Custom APIs", "customapi", "customapiid",
                new[] { "uniquename", "displayname", "isfunction", "boundentitylogicalname" }, (r, c) =>
                {
                    c.ComponentType = "Custom API";
                    c.Details["Kind"] = (r.GetAttributeValue<bool?>("isfunction") ?? false) ? "Function" : "Action";
                    c.Details["BoundEntity"] = r.GetAttributeValue<string>("boundentitylogicalname") ?? "(unbound)";
                });

            Enrich(svc, worker, progress, scan, byId, "Security roles", "role", "roleid",
                new[] { "name", "businessunitid" }, (r, c) =>
                {
                    c.Details["BusinessUnit"] = r.GetAttributeValue<EntityReference>("businessunitid")?.Name ?? "";
                });

            Enrich(svc, worker, progress, scan, byId, "Apps", "appmodule", "appmoduleid",
                new[] { "name", "uniquename" }, (r, c) =>
                {
                    c.SchemaName = r.GetAttributeValue<string>("uniquename") ?? c.SchemaName;
                });
        }

        /// <summary>
        /// Enriches the components whose object ids are known by querying <paramref name="entityName"/> where
        /// its primary key is one of those ids (chunked), applying <paramref name="apply"/> to the matching
        /// <see cref="ScanComponent"/>. A failure records the source name and continues.
        /// </summary>
        private void Enrich(IOrganizationService svc, BackgroundWorker worker, Action<string> progress,
            SolutionScanData scan, Dictionary<Guid, ScanComponent> byId, string sourceName,
            string entityName, string pkAttr, string[] columns, Action<Entity, ScanComponent> apply)
        {
            if (worker?.CancellationPending == true) return;

            // Only query ids that map to a component whose id we still hold.
            var ids = byId.Keys.ToList();
            if (ids.Count == 0) return;

            progress?.Invoke($"Reading detail: {sourceName}…");
            try
            {
                foreach (var chunk in Chunk(ids, 300))
                {
                    if (worker?.CancellationPending == true) return;
                    var qe = new QueryExpression(entityName)
                    {
                        ColumnSet = new ColumnSet(columns),
                        Criteria = { Conditions = { new ConditionExpression(pkAttr, ConditionOperator.In, chunk.Cast<object>().ToArray()) } }
                    };
                    foreach (var r in svc.RetrieveMultiple(qe).Entities)
                    {
                        if (byId.TryGetValue(r.Id, out var comp))
                        {
                            try { apply(r, comp); } catch { /* one bad row never aborts a source */ }
                        }
                    }
                }
            }
            catch
            {
                scan.UnavailableSources.Add(sourceName + " detail");
            }
        }

        // =====================================================================================
        // Inventory rollup
        // =====================================================================================

        private static List<InventoryCount> BuildInventory(SolutionScanData scan)
        {
            var counts = new List<InventoryCount>();
            if ((scan.Entities?.Count ?? 0) > 0)
                counts.Add(new InventoryCount("Tables", scan.Entities.Count));
            if ((scan.Choices?.Count ?? 0) > 0)
                counts.Add(new InventoryCount("Choices", scan.Choices.Count));

            foreach (var g in (scan.Components ?? new List<ScanComponent>())
                         .GroupBy(c => CategoryLabel(c.Category))
                         .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                counts.Add(new InventoryCount(g.Key, g.Count()));
            }
            return counts;
        }

        private static string CategoryLabel(string category)
        {
            switch (category)
            {
                case SectionKinds.Forms: return "Forms";
                case SectionKinds.Views: return "Views & charts";
                case SectionKinds.Apps: return "Apps";
                case SectionKinds.Automation: return "Automation";
                case SectionKinds.Plugins: return "Plug-in registrations";
                case SectionKinds.WebResources: return "Web resources";
                case SectionKinds.CustomApis: return "Custom APIs";
                case SectionKinds.Config: return "Configuration";
                case SectionKinds.Roles: return "Security roles";
                default: return category ?? "Other";
            }
        }

        // ---- helpers ----

        private static IEnumerable<List<Guid>> Chunk(List<Guid> ids, int size)
        {
            for (int i = 0; i < ids.Count; i += size)
                yield return ids.GetRange(i, Math.Min(size, ids.Count - i));
        }

        private static Guid ParseGuid(Entity e, string attr)
        {
            var v = e.GetAttributeValue<object>(attr);
            if (v is Guid g) return g;
            if (v is EntityReference er) return er.Id;
            if (v is string s && Guid.TryParse(s, out var parsed)) return parsed;
            return Guid.Empty;
        }

        private static string Formatted(Entity e, string attr)
        {
            if (e.FormattedValues != null && e.FormattedValues.Contains(attr)) return e.FormattedValues[attr];
            var osv = e.GetAttributeValue<OptionSetValue>(attr);
            return osv != null ? osv.Value.ToString(CultureInfo.InvariantCulture) : "";
        }

        private static string Label(Microsoft.Xrm.Sdk.Label l) => l?.UserLocalizedLabel?.Label;

        private static string WorkflowCategoryLabel(int? c)
        {
            switch (c)
            {
                case 0: return "Workflow";
                case 1: return "Dialog";
                case 2: return "Business Rule";
                case 3: return "Action";
                case 4: return "Business Process Flow";
                case 5: return "Modern Flow";
                default: return "Process";
            }
        }

        private static string StageLabel(int? s)
        {
            switch (s)
            {
                case 10: return "PreValidation";
                case 20: return "PreOperation";
                case 40: return "PostOperation";
                default: return s?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }

        private static string ModeLabel(int? m)
        {
            switch (m)
            {
                case 0: return "Sync";
                case 1: return "Async";
                default: return m?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }

        private static string FormTypeLabel(int? t)
        {
            switch (t)
            {
                case 2: return "Main form";
                case 6: return "Quick view form";
                case 7: return "Quick create form";
                case 8: return "Dialog";
                case 11: return "Card form";
                case 12: return "Main - Interactive experience";
                default: return "Form";
            }
        }

        private static string EnvVarTypeLabel(int? t)
        {
            switch (t)
            {
                case 100000000: return "String";
                case 100000001: return "Number";
                case 100000002: return "Boolean";
                case 100000003: return "JSON";
                case 100000004: return "Data Source";
                case 100000005: return "Secret";
                default: return t?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }
    }
}
