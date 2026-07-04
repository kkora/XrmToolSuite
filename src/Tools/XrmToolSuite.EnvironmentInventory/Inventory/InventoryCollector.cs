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

namespace XrmToolSuite.EnvironmentInventory.Inventory
{
    /// <summary>
    /// Which sources the inventory collection should pull. Plain serializable POCO (also persisted in the
    /// tool's settings). Default is everything on.
    /// </summary>
    public sealed class InventoryScope
    {
        public bool Solutions { get; set; } = true;
        public bool Tables { get; set; } = true;
        public bool SecurityRoles { get; set; } = true;
        public bool UsersTeamsBU { get; set; } = true;
        public bool Plugins { get; set; } = true;
        public bool Workflows { get; set; } = true;
        public bool WebResources { get; set; } = true;
        public bool CustomApis { get; set; } = true;
        public bool EnvVarsConnRefs { get; set; } = true;

        public static InventoryScope All() => new InventoryScope();
    }

    /// <summary>
    /// The Dataverse-facing half of the tool: turns a live environment into a normalized
    /// <see cref="InventorySnapshot"/>. Uses Microsoft.Xrm.Sdk and this suite's shared <c>RetrieveAll</c>,
    /// so it stays OUT of the SDK-free unit-test compile set. Every source runs in its own try/catch and,
    /// on failure, records the source in <see cref="InventorySnapshot.UnavailableSources"/> and continues —
    /// a permission gap degrades the inventory, it never aborts it. Read-only; environment-variable and
    /// connection-reference SECRET VALUES are never read or emitted.
    /// </summary>
    public static class InventoryCollector
    {
        // Category constants (kept in sync with InventorySummary/exporters via the snapshot's Category field).
        private const string CatSolutions = "Solutions";
        private const string CatTables = "Tables";
        private const string CatSecurity = "Security";
        private const string CatAutomation = "Automation";
        private const string CatWebDev = "Web/Dev";
        private const string CatConfig = "Configuration";

        public static InventorySnapshot Collect(
            IOrganizationService service,
            InventoryScope scope,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            scope = scope ?? InventoryScope.All();

            var snapshot = new InventorySnapshot { CollectedOnUtc = DateTime.UtcNow };

            if (scope.Solutions) CollectStep(snapshot, "Solutions & publishers", worker,
                () => CollectSolutions(service, snapshot, progress));

            if (scope.Tables) CollectStep(snapshot, "Tables", worker,
                () => CollectTables(service, snapshot, progress));

            if (scope.SecurityRoles) CollectStep(snapshot, "Security roles", worker,
                () => CollectRoles(service, snapshot, progress));

            if (scope.UsersTeamsBU) CollectStep(snapshot, "Users, teams & business units", worker,
                () => CollectUsersTeamsBu(service, snapshot, progress));

            if (scope.Plugins) CollectStep(snapshot, "Plugins & steps", worker,
                () => CollectPlugins(service, snapshot, progress));

            if (scope.Workflows) CollectStep(snapshot, "Workflows", worker,
                () => CollectWorkflows(service, snapshot, progress));

            if (scope.WebResources) CollectStep(snapshot, "Web resources & PCF", worker,
                () => CollectWebResources(service, snapshot, progress));

            if (scope.CustomApis) CollectStep(snapshot, "Custom APIs", worker,
                () => CollectCustomApis(service, snapshot, progress));

            if (scope.EnvVarsConnRefs) CollectStep(snapshot, "Environment variables & connection references", worker,
                () => CollectConfig(service, snapshot, progress));

            return snapshot;
        }

        /// <summary>Runs one source; a failure adds its name to UnavailableSources and continues.</summary>
        private static void CollectStep(InventorySnapshot snapshot, string sourceName,
            BackgroundWorker worker, Action work)
        {
            if (worker?.CancellationPending == true) return;
            try { work(); }
            catch { snapshot.UnavailableSources.Add(sourceName); }
        }

        // ---- Solutions (US-ADMIN7.1.1) ----

        private static void CollectSolutions(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading publishers…");
            var publishers = service.RetrieveAll(new QueryExpression("publisher")
            {
                ColumnSet = new ColumnSet("publisherid", "friendlyname", "uniquename", "customizationprefix")
            });
            var publisherNames = publishers.ToDictionary(
                p => p.Id,
                p => p.GetAttributeValue<string>("friendlyname") ?? p.GetAttributeValue<string>("uniquename") ?? "");

            foreach (var p in publishers)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatSolutions,
                    ComponentType = "Publisher",
                    Name = p.GetAttributeValue<string>("friendlyname"),
                    SchemaName = p.GetAttributeValue<string>("uniquename"),
                    Details =
                    {
                        ["Prefix"] = p.GetAttributeValue<string>("customizationprefix") ?? ""
                    }
                });
            }

            progress?.Invoke("Reading solutions…");
            var solutions = service.RetrieveAll(new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("friendlyname", "uniquename", "version", "ismanaged", "publisherid", "modifiedon", "isvisible")
            });
            foreach (var s in solutions)
            {
                var pubRef = s.GetAttributeValue<EntityReference>("publisherid");
                var owner = pubRef != null && publisherNames.TryGetValue(pubRef.Id, out var pn) ? pn : pubRef?.Name;
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatSolutions,
                    ComponentType = "Solution",
                    Name = s.GetAttributeValue<string>("friendlyname"),
                    SchemaName = s.GetAttributeValue<string>("uniquename"),
                    Owner = owner,
                    IsManaged = s.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = s.GetAttributeValue<DateTime?>("modifiedon"),
                    Details =
                    {
                        ["Version"] = s.GetAttributeValue<string>("version") ?? "",
                        ["Visible"] = (s.GetAttributeValue<bool?>("isvisible") ?? true) ? "yes" : "no"
                    }
                });
            }
        }

        // ---- Tables (US-ADMIN7.1.2) ----

        private static void CollectTables(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading table metadata…");
            var resp = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes,
                RetrieveAsIfPublished = true
            });

            foreach (var e in resp.EntityMetadata ?? new EntityMetadata[0])
            {
                int columnCount = (e.Attributes ?? new AttributeMetadata[0])
                    .Count(a => !string.IsNullOrEmpty(a.LogicalName));
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatTables,
                    ComponentType = e.IsCustomEntity == true ? "Custom Table" : "Table",
                    Name = e.DisplayName?.UserLocalizedLabel?.Label ?? e.LogicalName,
                    SchemaName = e.LogicalName,
                    IsManaged = e.IsManaged,
                    Details =
                    {
                        ["Columns"] = columnCount.ToString(CultureInfo.InvariantCulture),
                        ["ObjectTypeCode"] = e.ObjectTypeCode?.ToString(CultureInfo.InvariantCulture) ?? "",
                        ["Custom"] = (e.IsCustomEntity == true) ? "yes" : "no"
                    }
                });
            }
        }

        // ---- Security roles (US-ADMIN7.2.1) ----

        private static void CollectRoles(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading security roles…");
            var roles = service.RetrieveAll(new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("name", "ismanaged", "businessunitid", "modifiedon")
            });
            foreach (var r in roles)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatSecurity,
                    ComponentType = "Security Role",
                    Name = r.GetAttributeValue<string>("name"),
                    Owner = r.GetAttributeValue<EntityReference>("businessunitid")?.Name,
                    IsManaged = r.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = r.GetAttributeValue<DateTime?>("modifiedon")
                });
            }
        }

        // ---- Users, teams, business units (US-ADMIN7.2.1) ----

        private static void CollectUsersTeamsBu(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading business units…");
            var bus = service.RetrieveAll(new QueryExpression("businessunit")
            {
                ColumnSet = new ColumnSet("name", "isdisabled", "modifiedon")
            });
            foreach (var b in bus)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatSecurity,
                    ComponentType = "Business Unit",
                    Name = b.GetAttributeValue<string>("name"),
                    ModifiedOn = b.GetAttributeValue<DateTime?>("modifiedon"),
                    Details = { ["Disabled"] = (b.GetAttributeValue<bool?>("isdisabled") ?? false) ? "yes" : "no" }
                });
            }

            progress?.Invoke("Reading teams…");
            var teams = service.RetrieveAll(new QueryExpression("team")
            {
                ColumnSet = new ColumnSet("name", "teamtype", "businessunitid", "modifiedon")
            });
            foreach (var t in teams)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatSecurity,
                    ComponentType = "Team",
                    Name = t.GetAttributeValue<string>("name"),
                    Owner = t.GetAttributeValue<EntityReference>("businessunitid")?.Name,
                    ModifiedOn = t.GetAttributeValue<DateTime?>("modifiedon"),
                    Details = { ["TeamType"] = OptionSetLabel(t, "teamtype") }
                });
            }

            progress?.Invoke("Reading users…");
            var users = service.RetrieveAll(new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("fullname", "domainname", "isdisabled", "modifiedon")
            });
            foreach (var u in users)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatSecurity,
                    ComponentType = "User",
                    Name = u.GetAttributeValue<string>("fullname"),
                    SchemaName = u.GetAttributeValue<string>("domainname"),
                    ModifiedOn = u.GetAttributeValue<DateTime?>("modifiedon"),
                    Details = { ["Disabled"] = (u.GetAttributeValue<bool?>("isdisabled") ?? false) ? "yes" : "no" }
                });
            }
        }

        // ---- Plugins & steps (US-ADMIN7.2.2) ----

        private static void CollectPlugins(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading plugin assemblies…");
            var assemblies = service.RetrieveAll(new QueryExpression("pluginassembly")
            {
                ColumnSet = new ColumnSet("name", "version", "ismanaged", "modifiedon")
            });
            foreach (var a in assemblies)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatAutomation,
                    ComponentType = "Plugin Assembly",
                    Name = a.GetAttributeValue<string>("name"),
                    IsManaged = a.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = a.GetAttributeValue<DateTime?>("modifiedon"),
                    Details = { ["Version"] = a.GetAttributeValue<string>("version") ?? "" }
                });
            }

            progress?.Invoke("Reading plugin steps…");
            var steps = service.RetrieveAll(new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("name", "stage", "mode", "ismanaged", "statecode", "modifiedon")
            });
            foreach (var s in steps)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatAutomation,
                    ComponentType = "Plugin Step",
                    Name = s.GetAttributeValue<string>("name"),
                    IsManaged = s.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = s.GetAttributeValue<DateTime?>("modifiedon"),
                    Details =
                    {
                        ["Stage"] = StageLabel(s.GetAttributeValue<OptionSetValue>("stage")?.Value),
                        ["Mode"] = ModeLabel(s.GetAttributeValue<OptionSetValue>("mode")?.Value),
                        ["State"] = OptionSetLabel(s, "statecode")
                    }
                });
            }
        }

        // ---- Workflows (US-ADMIN7.2.2) ----

        private static void CollectWorkflows(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading workflows & flows…");
            var workflows = service.RetrieveAll(new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("name", "category", "statecode", "type", "primaryentity", "ismanaged", "modifiedon")
            });
            foreach (var w in workflows)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatAutomation,
                    ComponentType = WorkflowCategoryLabel(w.GetAttributeValue<OptionSetValue>("category")?.Value),
                    Name = w.GetAttributeValue<string>("name"),
                    SchemaName = w.GetAttributeValue<string>("primaryentity"),
                    IsManaged = w.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = w.GetAttributeValue<DateTime?>("modifiedon"),
                    Details =
                    {
                        ["State"] = OptionSetLabel(w, "statecode"),
                        ["Type"] = OptionSetLabel(w, "type")
                    }
                });
            }
        }

        // ---- Web resources & PCF (US-ADMIN7.3.1) ----

        private static void CollectWebResources(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading web resources…");
            var webresources = service.RetrieveAll(new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "displayname", "webresourcetype", "ismanaged", "modifiedon")
            });
            foreach (var w in webresources)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatWebDev,
                    ComponentType = "Web Resource",
                    Name = w.GetAttributeValue<string>("displayname") ?? w.GetAttributeValue<string>("name"),
                    SchemaName = w.GetAttributeValue<string>("name"),
                    IsManaged = w.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = w.GetAttributeValue<DateTime?>("modifiedon"),
                    Details = { ["Type"] = WebResourceTypeLabel(w.GetAttributeValue<OptionSetValue>("webresourcetype")?.Value) }
                });
            }

            // PCF controls — the customcontrol table isn't present in every environment; degrade quietly.
            try
            {
                progress?.Invoke("Reading PCF controls…");
                var controls = service.RetrieveAll(new QueryExpression("customcontrol")
                {
                    ColumnSet = new ColumnSet("name", "ismanaged", "modifiedon")
                });
                foreach (var c in controls)
                {
                    snapshot.Items.Add(new InventoryItem
                    {
                        Category = CatWebDev,
                        ComponentType = "PCF Control",
                        Name = c.GetAttributeValue<string>("name"),
                        SchemaName = c.GetAttributeValue<string>("name"),
                        IsManaged = c.GetAttributeValue<bool?>("ismanaged"),
                        ModifiedOn = c.GetAttributeValue<DateTime?>("modifiedon")
                    });
                }
            }
            catch
            {
                snapshot.UnavailableSources.Add("PCF controls");
            }
        }

        // ---- Custom APIs (US-ADMIN7.3.1) ----

        private static void CollectCustomApis(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading custom APIs…");
            var apis = service.RetrieveAll(new QueryExpression("customapi")
            {
                ColumnSet = new ColumnSet("name", "displayname", "uniquename", "isfunction", "ismanaged", "modifiedon")
            });
            foreach (var a in apis)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatWebDev,
                    ComponentType = "Custom API",
                    Name = a.GetAttributeValue<string>("displayname") ?? a.GetAttributeValue<string>("name"),
                    SchemaName = a.GetAttributeValue<string>("uniquename"),
                    IsManaged = a.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = a.GetAttributeValue<DateTime?>("modifiedon"),
                    Details = { ["IsFunction"] = (a.GetAttributeValue<bool?>("isfunction") ?? false) ? "yes" : "no" }
                });
            }
        }

        // ---- Environment variables & connection references (US-ADMIN7.3.2) ----
        // SECURITY: only DEFINITIONS are read — never environmentvariablevalue / secret content.

        private static void CollectConfig(IOrganizationService service, InventorySnapshot snapshot, Action<string> progress)
        {
            progress?.Invoke("Reading environment variable definitions…");
            var envvars = service.RetrieveAll(new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("schemaname", "displayname", "type", "ismanaged", "modifiedon")
            });
            foreach (var v in envvars)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatConfig,
                    ComponentType = "Environment Variable",
                    Name = v.GetAttributeValue<string>("displayname") ?? v.GetAttributeValue<string>("schemaname"),
                    SchemaName = v.GetAttributeValue<string>("schemaname"),
                    IsManaged = v.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = v.GetAttributeValue<DateTime?>("modifiedon"),
                    // NOTE: deliberately NO value/secret — only the variable's declared type.
                    Details = { ["Type"] = EnvVarTypeLabel(v.GetAttributeValue<OptionSetValue>("type")?.Value) }
                });
            }

            progress?.Invoke("Reading connection references…");
            var connrefs = service.RetrieveAll(new QueryExpression("connectionreference")
            {
                ColumnSet = new ColumnSet("connectionreferencedisplayname", "connectionreferencelogicalname", "connectorid", "ismanaged", "modifiedon")
            });
            foreach (var c in connrefs)
            {
                snapshot.Items.Add(new InventoryItem
                {
                    Category = CatConfig,
                    ComponentType = "Connection Reference",
                    Name = c.GetAttributeValue<string>("connectionreferencedisplayname")
                           ?? c.GetAttributeValue<string>("connectionreferencelogicalname"),
                    SchemaName = c.GetAttributeValue<string>("connectionreferencelogicalname"),
                    IsManaged = c.GetAttributeValue<bool?>("ismanaged"),
                    ModifiedOn = c.GetAttributeValue<DateTime?>("modifiedon"),
                    Details = { ["ConnectorId"] = c.GetAttributeValue<string>("connectorid") ?? "" }
                });
            }
        }

        // ---- option-set label helpers ----

        private static string OptionSetLabel(Entity e, string attribute)
        {
            var fv = e.FormattedValues != null && e.FormattedValues.Contains(attribute) ? e.FormattedValues[attribute] : null;
            if (!string.IsNullOrEmpty(fv)) return fv;
            var osv = e.GetAttributeValue<OptionSetValue>(attribute);
            return osv != null ? osv.Value.ToString(CultureInfo.InvariantCulture) : "";
        }

        private static string WorkflowCategoryLabel(int? category)
        {
            switch (category)
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

        private static string StageLabel(int? stage)
        {
            switch (stage)
            {
                case 10: return "PreValidation";
                case 20: return "PreOperation";
                case 40: return "PostOperation";
                default: return stage?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }

        private static string ModeLabel(int? mode)
        {
            switch (mode)
            {
                case 0: return "Synchronous";
                case 1: return "Asynchronous";
                default: return mode?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }

        private static string WebResourceTypeLabel(int? type)
        {
            switch (type)
            {
                case 1: return "HTML";
                case 2: return "CSS";
                case 3: return "JScript";
                case 4: return "XML";
                case 5: return "PNG";
                case 6: return "JPG";
                case 7: return "GIF";
                case 8: return "XAP (Silverlight)";
                case 9: return "XSL";
                case 10: return "ICO";
                case 11: return "SVG";
                case 12: return "RESX";
                default: return type?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }

        private static string EnvVarTypeLabel(int? type)
        {
            switch (type)
            {
                case 100000000: return "String";
                case 100000001: return "Number";
                case 100000002: return "Boolean";
                case 100000003: return "JSON";
                case 100000004: return "Data Source";
                case 100000005: return "Secret";
                default: return type?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }
    }
}
