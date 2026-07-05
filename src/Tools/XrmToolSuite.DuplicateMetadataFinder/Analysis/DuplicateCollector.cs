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

namespace XrmToolSuite.DuplicateMetadataFinder.Analysis
{
    /// <summary>
    /// The Dataverse-facing half of the tool: turns a live environment into a flat list of
    /// <see cref="MetadataComponent"/>s for the SDK-free <see cref="SimilarityEngine"/> to score and group.
    /// Uses Microsoft.Xrm.Sdk and this suite's shared <c>RetrieveAll</c>, so it stays OUT of the SDK-free
    /// unit-test compile set. Every kind runs in its own try/catch and, on failure, records a note and
    /// continues — a permission gap degrades the scan, it never aborts it. Read-only; never reads secrets.
    /// </summary>
    public static class DuplicateCollector
    {
        /// <summary>Collected components plus any degraded-scan notes.</summary>
        public sealed class CollectResult
        {
            public List<MetadataComponent> Components { get; } = new List<MetadataComponent>();
            public List<string> Notes { get; } = new List<string>();
        }

        public static CollectResult Collect(
            IOrganizationService service,
            DuplicateScanOptions options,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            options = options ?? DuplicateScanOptions.All();
            var result = new CollectResult();

            // Table metadata (one round-trip) feeds columns, tables, option sets and relationships.
            EntityMetadata[] entities = null;
            if (options.Columns || options.Tables || options.OptionSets || options.Relationships)
            {
                Step(result, "Table metadata", worker, () =>
                {
                    progress?.Invoke("Reading table metadata…");
                    var resp = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest
                    {
                        EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                        RetrieveAsIfPublished = true
                    });
                    entities = resp.EntityMetadata ?? new EntityMetadata[0];
                });
            }

            if (entities != null)
            {
                if (options.Columns) Step(result, "Columns", worker,
                    () => CollectColumns(entities, options, result, progress));
                if (options.Tables) Step(result, "Tables", worker,
                    () => CollectTables(entities, options, result, progress));
                if (options.OptionSets) Step(result, "Option sets", worker,
                    () => CollectOptionSets(service, entities, options, result, progress));
                if (options.Relationships) Step(result, "Relationships", worker,
                    () => CollectRelationships(entities, options, result, progress));
            }

            if (options.Forms) Step(result, "Forms", worker,
                () => CollectForms(service, options, result, progress));
            if (options.Views) Step(result, "Views", worker,
                () => CollectViews(service, options, result, progress));
            if (options.BusinessRules) Step(result, "Business rules", worker,
                () => CollectBusinessRules(service, options, result, progress));
            if (options.WebResources) Step(result, "Web resources", worker,
                () => CollectWebResources(service, options, result, progress));
            if (options.PluginSteps) Step(result, "Plugin steps", worker,
                () => CollectPluginSteps(service, options, result, progress));

            return result;
        }

        /// <summary>Runs one kind; a failure records a note and continues (analyzer convention).</summary>
        private static void Step(CollectResult result, string name, BackgroundWorker worker, Action work)
        {
            if (worker?.CancellationPending == true) return;
            try { work(); }
            catch (Exception ex) { result.Notes.Add($"{name}: skipped ({ex.Message})"); }
        }

        private static bool Keep(DuplicateScanOptions o, bool isManaged, bool isCustom) =>
            !o.CustomOnly || (isCustom && !isManaged);

        // ---- columns (US-ADMIN3.2.1) ----

        private static void CollectColumns(EntityMetadata[] entities, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning columns…");
            foreach (var e in entities)
            {
                foreach (var a in e.Attributes ?? new AttributeMetadata[0])
                {
                    if (string.IsNullOrEmpty(a.LogicalName)) continue;
                    if (a.AttributeOf != null) continue;                 // skip virtual/derived parts
                    var isManaged = a.IsManaged ?? false;
                    var isCustom = a.IsCustomAttribute ?? false;
                    if (!Keep(o, isManaged, isCustom)) continue;

                    result.Components.Add(new MetadataComponent
                    {
                        Kind = ComponentKind.Column,
                        Key = e.LogicalName + "." + a.LogicalName,
                        DisplayName = Label(a.DisplayName) ?? a.LogicalName,
                        SchemaName = a.LogicalName,
                        Container = e.LogicalName,
                        DataType = a.AttributeType?.ToString(),
                        Description = Label(a.Description),
                        IsManaged = isManaged,
                        IsCustom = isCustom,
                    });
                }
            }
        }

        // ---- tables (US-ADMIN3.3.1) ----

        private static void CollectTables(EntityMetadata[] entities, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning tables…");
            foreach (var e in entities)
            {
                var isManaged = e.IsManaged ?? false;
                var isCustom = e.IsCustomEntity ?? false;
                if (!Keep(o, isManaged, isCustom)) continue;

                result.Components.Add(new MetadataComponent
                {
                    Kind = ComponentKind.Table,
                    Key = e.LogicalName,
                    DisplayName = Label(e.DisplayName) ?? e.LogicalName,
                    SchemaName = e.LogicalName,
                    Description = Label(e.Description),
                    IsManaged = isManaged,
                    IsCustom = isCustom,
                });
            }
        }

        // ---- option sets (US-ADMIN3.2.2) ----

        private static void CollectOptionSets(IOrganizationService service, EntityMetadata[] entities,
            DuplicateScanOptions o, CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning option sets…");
            var resp = (RetrieveAllOptionSetsResponse)service.Execute(new RetrieveAllOptionSetsRequest
            {
                RetrieveAsIfPublished = true
            });
            foreach (var os in resp.OptionSetMetadata.OfType<OptionSetMetadata>())
            {
                var isManaged = os.IsManaged ?? false;
                var isCustom = os.IsCustomOptionSet ?? false;
                if (!Keep(o, isManaged, isCustom)) continue;

                result.Components.Add(new MetadataComponent
                {
                    Kind = ComponentKind.OptionSet,
                    Key = os.Name,
                    DisplayName = Label(os.DisplayName) ?? os.Name,
                    SchemaName = os.Name,
                    DataType = "Picklist",
                    OptionValues = (os.Options ?? Enumerable.Empty<OptionMetadata>())
                        .Select(op => Label(op.Label) ?? op.Value?.ToString(CultureInfo.InvariantCulture))
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList(),
                    IsManaged = isManaged,
                    IsCustom = isCustom,
                });
            }
        }

        // ---- relationships (US-ADMIN3.3.2) ----

        private static void CollectRelationships(EntityMetadata[] entities, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning relationships…");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entities)
            {
                foreach (var rel in (e.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
                    .Cast<RelationshipMetadataBase>()
                    .Concat(e.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0]))
                {
                    var name = rel.SchemaName;
                    if (string.IsNullOrEmpty(name) || !seen.Add(name)) continue;
                    var isManaged = rel.IsManaged ?? false;
                    var isCustom = rel.IsCustomRelationship ?? false;
                    if (!Keep(o, isManaged, isCustom)) continue;

                    result.Components.Add(new MetadataComponent
                    {
                        Kind = ComponentKind.Relationship,
                        Key = name,
                        DisplayName = name,
                        SchemaName = name,
                        Container = e.LogicalName,
                        DataType = rel.RelationshipType.ToString(),
                        IsManaged = isManaged,
                        IsCustom = isCustom,
                    });
                }
            }
        }

        // ---- forms (US-ADMIN3.3.1) ----

        private static void CollectForms(IOrganizationService service, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning forms…");
            var forms = service.RetrieveAll(new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("name", "objecttypecode", "type", "ismanaged", "description")
            });
            foreach (var f in forms)
            {
                var isManaged = f.GetAttributeValue<bool?>("ismanaged") ?? false;
                if (!Keep(o, isManaged, !isManaged)) continue;
                result.Components.Add(new MetadataComponent
                {
                    Kind = ComponentKind.Form,
                    Key = "form:" + f.Id,
                    DisplayName = f.GetAttributeValue<string>("name"),
                    Container = f.GetAttributeValue<string>("objecttypecode"),
                    DataType = f.FormattedValues.Contains("type") ? f.FormattedValues["type"] : null,
                    Description = f.GetAttributeValue<string>("description"),
                    IsManaged = isManaged,
                    IsCustom = !isManaged,
                });
            }
        }

        // ---- views (US-ADMIN3.3.1) ----

        private static void CollectViews(IOrganizationService service, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning views…");
            var views = service.RetrieveAll(new QueryExpression("savedquery")
            {
                ColumnSet = new ColumnSet("name", "returnedtypecode", "ismanaged", "description")
            });
            foreach (var v in views)
            {
                var isManaged = v.GetAttributeValue<bool?>("ismanaged") ?? false;
                if (!Keep(o, isManaged, !isManaged)) continue;
                result.Components.Add(new MetadataComponent
                {
                    Kind = ComponentKind.View,
                    Key = "view:" + v.Id,
                    DisplayName = v.GetAttributeValue<string>("name"),
                    Container = v.GetAttributeValue<string>("returnedtypecode"),
                    Description = v.GetAttributeValue<string>("description"),
                    IsManaged = isManaged,
                    IsCustom = !isManaged,
                });
            }
        }

        // ---- business rules (US-ADMIN3.3.2) ----

        private static void CollectBusinessRules(IOrganizationService service, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning business rules…");
            // category 2 == Business Rule
            var rules = service.RetrieveAll(new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("name", "primaryentity", "ismanaged"),
                Criteria =
                {
                    Conditions = { new ConditionExpression("category", ConditionOperator.Equal, 2) }
                }
            });
            foreach (var r in rules)
            {
                var isManaged = r.GetAttributeValue<bool?>("ismanaged") ?? false;
                if (!Keep(o, isManaged, !isManaged)) continue;
                result.Components.Add(new MetadataComponent
                {
                    Kind = ComponentKind.BusinessRule,
                    Key = "rule:" + r.Id,
                    DisplayName = r.GetAttributeValue<string>("name"),
                    Container = r.GetAttributeValue<string>("primaryentity"),
                    IsManaged = isManaged,
                    IsCustom = !isManaged,
                });
            }
        }

        // ---- web resources / JavaScript (US-ADMIN3.3.2) ----

        private static void CollectWebResources(IOrganizationService service, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning web resources…");
            var wrs = service.RetrieveAll(new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "displayname", "webresourcetype", "content", "ismanaged")
            });
            foreach (var w in wrs)
            {
                var isManaged = w.GetAttributeValue<bool?>("ismanaged") ?? false;
                if (!Keep(o, isManaged, !isManaged)) continue;
                var type = w.GetAttributeValue<OptionSetValue>("webresourcetype")?.Value;
                var content = w.GetAttributeValue<string>("content"); // base64; hashed as-is for exact-match
                result.Components.Add(new MetadataComponent
                {
                    Kind = ComponentKind.WebResource,
                    Key = "wr:" + (w.GetAttributeValue<string>("name") ?? w.Id.ToString()),
                    DisplayName = w.GetAttributeValue<string>("displayname") ?? w.GetAttributeValue<string>("name"),
                    SchemaName = w.GetAttributeValue<string>("name"),
                    DataType = WebResourceTypeLabel(type),
                    ContentHash = string.IsNullOrEmpty(content) ? null : TextSimilarity.ContentHash(content),
                    IsManaged = isManaged,
                    IsCustom = !isManaged,
                });
            }
        }

        // ---- plugin steps (US-ADMIN3.3.2) ----

        private static void CollectPluginSteps(IOrganizationService service, DuplicateScanOptions o,
            CollectResult result, Action<string> progress)
        {
            progress?.Invoke("Scanning plugin steps…");
            var steps = service.RetrieveAll(new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("name", "stage", "ismanaged", "sdkmessageid", "primaryobjecttypecode")
            });
            foreach (var s in steps)
            {
                var isManaged = s.GetAttributeValue<bool?>("ismanaged") ?? false;
                if (!Keep(o, isManaged, !isManaged)) continue;
                var message = s.GetAttributeValue<EntityReference>("sdkmessageid")?.Name;
                var entity = s.GetAttributeValue<string>("primaryobjecttypecode");
                var stage = s.GetAttributeValue<OptionSetValue>("stage")?.Value;
                result.Components.Add(new MetadataComponent
                {
                    Kind = ComponentKind.PluginStep,
                    Key = "step:" + s.Id,
                    DisplayName = s.GetAttributeValue<string>("name"),
                    Container = entity,
                    // The message+entity+stage signature is the duplicate key for steps.
                    DataType = $"{message}/{entity}/{stage}",
                    IsManaged = isManaged,
                    IsCustom = !isManaged,
                });
            }
        }

        private static string Label(Microsoft.Xrm.Sdk.Label label) =>
            label?.UserLocalizedLabel?.Label;

        private static string WebResourceTypeLabel(int? type)
        {
            switch (type)
            {
                case 1: return "HTML";
                case 2: return "CSS";
                case 3: return "JScript";
                case 4: return "XML";
                case 9: return "XSL";
                default: return type?.ToString(CultureInfo.InvariantCulture) ?? "";
            }
        }
    }
}
