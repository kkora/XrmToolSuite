using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.EnvironmentComparisonSuite.Analysis
{
    /// <summary>
    /// The only Dataverse-touching piece of the tool. For each enabled category it builds a normalized
    /// <see cref="ComponentSnapshot"/> list from BOTH environments (source + target
    /// <see cref="IOrganizationService"/>) and diffs them through <see cref="SnapshotComparer"/>. Every
    /// category is wrapped so a query/metadata failure degrades to an informational finding rather than
    /// aborting the whole run. Reports progress per category and honours cancellation. Read-only — it
    /// never writes to either environment. Deliberately kept out of the SDK-free unit-test set.
    /// </summary>
    public sealed class ComparisonCollector
    {
        private readonly CompareOptions _opts;

        public ComparisonCollector(CompareOptions opts = null)
        {
            _opts = opts ?? CompareOptions.Default;
        }

        public ComparisonReport Compare(
            IOrganizationService source,
            IOrganizationService target,
            ISet<string> enabledCategories,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            enabledCategories = enabledCategories ?? new HashSet<string>(ComparisonCategories.All);

            var diffs = new List<ComponentDiff>();
            var degraded = new List<Finding>();

            // Metadata categories share a single RetrieveAllEntities call per environment (bounded to 2
            // requests) rather than one request per table.
            bool needsAttributes = enabledCategories.Contains(ComparisonCategories.Columns);
            bool needsRelationships = enabledCategories.Contains(ComparisonCategories.Relationships);
            bool needsMetadata = enabledCategories.Contains(ComparisonCategories.Tables) ||
                                 enabledCategories.Contains(ComparisonCategories.Keys) ||
                                 needsAttributes || needsRelationships;

            EntityMetadata[] srcMeta = null, tgtMeta = null;
            if (needsMetadata)
            {
                var filters = EntityFilters.Entity;
                if (needsAttributes) filters |= EntityFilters.Attributes;
                if (needsRelationships) filters |= EntityFilters.Relationships;
                progress?.Invoke("Retrieving source metadata…");
                srcMeta = SafeMetadata(source, filters);
                if (Cancelled(worker)) return Roll(diffs, degraded);
                progress?.Invoke("Retrieving target metadata…");
                tgtMeta = SafeMetadata(target, filters);
            }

            foreach (var category in ComparisonCategories.All)
            {
                if (Cancelled(worker)) break;
                if (!enabledCategories.Contains(category)) continue;

                progress?.Invoke($"Comparing {category}…");
                try
                {
                    var s = Collect(category, source, srcMeta, worker);
                    var t = Collect(category, target, tgtMeta, worker);
                    diffs.AddRange(SnapshotComparer.Compare(category, s, t, _opts));
                }
                catch (Exception ex)
                {
                    // Degrade the whole category to an informational note; never fail the run.
                    degraded.Add(new Finding(category, Severity.Info,
                        $"{category} comparison skipped",
                        $"This category could not be compared: {ex.Message}",
                        category,
                        "Check permissions/connectivity on both environments and re-run this category."));
                }
            }

            return Roll(diffs, degraded);
        }

        private ComparisonReport Roll(List<ComponentDiff> diffs, List<Finding> degraded)
        {
            var report = SnapshotComparer.Roll(diffs, _opts);
            report.Findings.AddRange(degraded); // informational; weight 0, does not move the score
            return report;
        }

        // ---------------------------------------------------------------- category dispatch

        private List<ComponentSnapshot> Collect(
            string category, IOrganizationService svc, EntityMetadata[] meta, BackgroundWorker worker)
        {
            switch (category)
            {
                case ComparisonCategories.Solutions: return Solutions(svc, worker);
                case ComparisonCategories.Publishers: return Publishers(svc, worker);
                case ComparisonCategories.Tables: return Tables(meta);
                case ComparisonCategories.Columns: return Columns(meta);
                case ComparisonCategories.Relationships: return Relationships(meta);
                case ComparisonCategories.Keys: return Keys(meta);
                case ComparisonCategories.Forms: return Forms(svc, worker);
                case ComparisonCategories.Views: return Views(svc, worker);
                case ComparisonCategories.Charts: return Charts(svc, worker);
                case ComparisonCategories.Dashboards: return Dashboards(svc, worker);
                case ComparisonCategories.Roles: return Roles(svc, worker);
                case ComparisonCategories.Teams: return Teams(svc, worker);
                case ComparisonCategories.BusinessUnits: return BusinessUnits(svc, worker);
                case ComparisonCategories.PluginAssemblies: return PluginAssemblies(svc, worker);
                case ComparisonCategories.PluginSteps: return PluginSteps(svc, worker);
                case ComparisonCategories.PluginImages: return PluginImages(svc, worker);
                case ComparisonCategories.Workflows: return Workflows(svc, worker);
                case ComparisonCategories.CustomApis: return CustomApis(svc, worker);
                case ComparisonCategories.EnvironmentVariables: return EnvironmentVariables(svc, worker);
                case ComparisonCategories.ConnectionReferences: return ConnectionReferences(svc, worker);
                case ComparisonCategories.WebResources: return WebResources(svc, worker);
                default: return new List<ComponentSnapshot>();
            }
        }

        // ---------------------------------------------------------------- solutions / publishers

        private static List<ComponentSnapshot> Solutions(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename", "friendlyname", "version", "ismanaged"),
                Criteria = { Conditions = { new ConditionExpression("isvisible", ConditionOperator.Equal, true) } }
            };
            return svc.RetrieveAll(q, worker: worker)
                .Where(e => { var u = e.GetAttributeValue<string>("uniquename"); return u != "Default" && u != "Active"; })
                .Select(e => new ComponentSnapshot(
                        ComparisonCategories.Solutions,
                        e.GetAttributeValue<string>("uniquename"),
                        e.GetAttributeValue<string>("friendlyname") ?? e.GetAttributeValue<string>("uniquename"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false,
                        e.GetAttributeValue<string>("version"))
                    .With("friendlyname", e.GetAttributeValue<string>("friendlyname")))
                .ToList();
        }

        private static List<ComponentSnapshot> Publishers(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("publisher")
            {
                ColumnSet = new ColumnSet("uniquename", "friendlyname", "customizationprefix", "customizationoptionvalueprefix", "isreadonly")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(
                        ComparisonCategories.Publishers,
                        e.GetAttributeValue<string>("uniquename"),
                        e.GetAttributeValue<string>("friendlyname") ?? e.GetAttributeValue<string>("uniquename"))
                    .With("prefix", e.GetAttributeValue<string>("customizationprefix"))
                    .With("optionvalueprefix", e.GetAttributeValue<int?>("customizationoptionvalueprefix")?.ToString()))
                .ToList();
        }

        // ---------------------------------------------------------------- metadata (tables / columns / rels / keys)

        private static List<ComponentSnapshot> Tables(EntityMetadata[] meta)
        {
            return (meta ?? Array.Empty<EntityMetadata>())
                .Select(m => new ComponentSnapshot(
                        ComparisonCategories.Tables, m.LogicalName,
                        Label(m.DisplayName) ?? m.LogicalName, m.IsManaged ?? false)
                    .With("ownership", m.OwnershipType?.ToString())
                    .With("isactivity", m.IsActivity?.ToString())
                    .With("isauditenabled", m.IsAuditEnabled?.Value.ToString())
                    .With("primaryname", m.PrimaryNameAttribute))
                .ToList();
        }

        private static List<ComponentSnapshot> Columns(EntityMetadata[] meta)
        {
            var list = new List<ComponentSnapshot>();
            foreach (var m in meta ?? Array.Empty<EntityMetadata>())
            {
                foreach (var a in m.Attributes ?? Array.Empty<AttributeMetadata>())
                {
                    if (a.AttributeOf != null) continue; // skip virtual/child parts (e.g. lookup name shadow)
                    var snap = new ComponentSnapshot(
                            ComparisonCategories.Columns, $"{m.LogicalName}.{a.LogicalName}",
                            $"{m.LogicalName}.{a.LogicalName}", a.IsManaged ?? false)
                        .With("type", a.AttributeType?.ToString())
                        .With("requiredlevel", a.RequiredLevel?.Value.ToString());
                    if (a is StringAttributeMetadata s) snap.With("maxlength", s.MaxLength?.ToString());
                    if (a is DecimalAttributeMetadata d) snap.With("precision", d.Precision?.ToString());
                    if (a is MoneyAttributeMetadata mo) snap.With("precision", mo.Precision?.ToString());
                    list.Add(snap);
                }
            }
            return list;
        }

        private static List<ComponentSnapshot> Relationships(EntityMetadata[] meta)
        {
            var list = new List<ComponentSnapshot>();
            foreach (var m in meta ?? Array.Empty<EntityMetadata>())
            {
                foreach (var r in (m.OneToManyRelationships ?? Array.Empty<OneToManyRelationshipMetadata>()))
                {
                    if (r.SchemaName == null) continue;
                    list.Add(new ComponentSnapshot(ComparisonCategories.Relationships, r.SchemaName, r.SchemaName, r.IsManaged ?? false)
                        .With("type", "OneToMany")
                        .With("referencing", $"{r.ReferencingEntity}.{r.ReferencingAttribute}")
                        .With("referenced", r.ReferencedEntity)
                        .With("cascadedelete", r.CascadeConfiguration?.Delete?.ToString())
                        .With("cascadeassign", r.CascadeConfiguration?.Assign?.ToString()));
                }
                foreach (var r in (m.ManyToManyRelationships ?? Array.Empty<ManyToManyRelationshipMetadata>()))
                {
                    if (r.SchemaName == null) continue;
                    // N:N appears on both entities — first writer wins (Compare de-dupes by key anyway).
                    if (list.Any(x => string.Equals(x.Key, r.SchemaName, StringComparison.OrdinalIgnoreCase))) continue;
                    list.Add(new ComponentSnapshot(ComparisonCategories.Relationships, r.SchemaName, r.SchemaName, r.IsManaged ?? false)
                        .With("type", "ManyToMany")
                        .With("entity1", r.Entity1LogicalName)
                        .With("entity2", r.Entity2LogicalName));
                }
            }
            return list;
        }

        private static List<ComponentSnapshot> Keys(EntityMetadata[] meta)
        {
            var list = new List<ComponentSnapshot>();
            foreach (var m in meta ?? Array.Empty<EntityMetadata>())
            {
                foreach (var k in m.Keys ?? Array.Empty<EntityKeyMetadata>())
                {
                    var attrs = string.Join(",", (k.KeyAttributes ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
                    list.Add(new ComponentSnapshot(ComparisonCategories.Keys, $"{m.LogicalName}.{k.LogicalName}",
                            $"{m.LogicalName}.{k.LogicalName}", k.IsManaged ?? false)
                        .With("attributes", attrs));
                }
            }
            return list;
        }

        // ---------------------------------------------------------------- UI (forms / views / charts / dashboards)

        private static List<ComponentSnapshot> Forms(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("objecttypecode", "name", "type", "formxml", "ismanaged")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.Forms,
                        $"{e.GetAttributeValue<string>("objecttypecode")}|{e.GetAttributeValue<string>("name")}|{e.GetAttributeValue<OptionSetValue>("type")?.Value}",
                        $"{e.GetAttributeValue<string>("objecttypecode")}: {e.GetAttributeValue<string>("name")}",
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("formtype", e.GetAttributeValue<OptionSetValue>("type")?.Value.ToString())
                    .With("definitionhash", Hash(e.GetAttributeValue<string>("formxml"))))
                .ToList();
        }

        private static List<ComponentSnapshot> Views(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("savedquery")
            {
                ColumnSet = new ColumnSet("returnedtypecode", "name", "fetchxml", "layoutxml", "ismanaged")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.Views,
                        $"{e.GetAttributeValue<string>("returnedtypecode")}|{e.GetAttributeValue<string>("name")}",
                        $"{e.GetAttributeValue<string>("returnedtypecode")}: {e.GetAttributeValue<string>("name")}",
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("fetchhash", Hash(e.GetAttributeValue<string>("fetchxml")))
                    .With("layouthash", Hash(e.GetAttributeValue<string>("layoutxml"))))
                .ToList();
        }

        private static List<ComponentSnapshot> Charts(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("savedqueryvisualization")
            {
                ColumnSet = new ColumnSet("primaryentitytypecode", "name", "datadescription", "presentationdescription", "ismanaged")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.Charts,
                        $"{e.GetAttributeValue<string>("primaryentitytypecode")}|{e.GetAttributeValue<string>("name")}",
                        $"{e.GetAttributeValue<string>("primaryentitytypecode")}: {e.GetAttributeValue<string>("name")}",
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("datahash", Hash(e.GetAttributeValue<string>("datadescription")))
                    .With("presentationhash", Hash(e.GetAttributeValue<string>("presentationdescription"))))
                .ToList();
        }

        private static List<ComponentSnapshot> Dashboards(IOrganizationService svc, BackgroundWorker worker)
        {
            // Dashboards are systemforms of type 0.
            var q = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("name", "formxml", "ismanaged"),
                Criteria = { Conditions = { new ConditionExpression("type", ConditionOperator.Equal, 0) } }
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.Dashboards,
                        e.GetAttributeValue<string>("name"), e.GetAttributeValue<string>("name"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("definitionhash", Hash(e.GetAttributeValue<string>("formxml"))))
                .ToList();
        }

        // ---------------------------------------------------------------- security (roles / teams / BUs)

        private static List<ComponentSnapshot> Roles(IOrganizationService svc, BackgroundWorker worker)
        {
            var roles = svc.RetrieveAll(new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("roleid", "name", "ismanaged")
            }, worker: worker);

            // Privilege set per role, compared as a stable hash of the sorted (privilege, depth) pairs.
            var byRole = new Dictionary<Guid, List<string>>();
            try
            {
                var rp = svc.RetrieveAll(new QueryExpression("roleprivileges")
                {
                    ColumnSet = new ColumnSet("roleid", "privilegeid", "privilegedepthmask")
                }, worker: worker);
                foreach (var r in rp)
                {
                    var rid = r.GetAttributeValue<Guid>("roleid");
                    if (rid == Guid.Empty) continue;
                    if (!byRole.TryGetValue(rid, out var l)) byRole[rid] = l = new List<string>();
                    l.Add($"{r.GetAttributeValue<Guid>("privilegeid")}:{r.GetAttributeValue<int>("privilegedepthmask")}");
                }
            }
            catch { /* privilege set unavailable — roles still compared by name/managed */ }

            return roles.Select(e =>
            {
                var id = e.GetAttributeValue<Guid>("roleid");
                var snap = new ComponentSnapshot(ComparisonCategories.Roles,
                    e.GetAttributeValue<string>("name"), e.GetAttributeValue<string>("name"),
                    e.GetAttributeValue<bool?>("ismanaged") ?? false);
                if (byRole.TryGetValue(id, out var privs))
                    snap.With("privilegeset", Hash(string.Join("|", privs.OrderBy(x => x, StringComparer.Ordinal))))
                        .With("privilegecount", privs.Count.ToString());
                return snap;
            }).ToList();
        }

        private static List<ComponentSnapshot> Teams(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("team")
            {
                ColumnSet = new ColumnSet("name", "teamtype", "isdefault"),
                Criteria = { Conditions = { new ConditionExpression("isdefault", ConditionOperator.Equal, false) } }
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.Teams,
                        e.GetAttributeValue<string>("name"), e.GetAttributeValue<string>("name"))
                    .With("teamtype", e.GetAttributeValue<OptionSetValue>("teamtype")?.Value.ToString()))
                .ToList();
        }

        private static List<ComponentSnapshot> BusinessUnits(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("businessunit") { ColumnSet = new ColumnSet("name", "parentbusinessunitid") };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.BusinessUnits,
                        e.GetAttributeValue<string>("name"), e.GetAttributeValue<string>("name"))
                    .With("parent", e.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Name))
                .ToList();
        }

        // ---------------------------------------------------------------- code / automation

        private static List<ComponentSnapshot> PluginAssemblies(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("pluginassembly") { ColumnSet = new ColumnSet("name", "version", "ismanaged", "isolationmode") };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.PluginAssemblies,
                        e.GetAttributeValue<string>("name"), e.GetAttributeValue<string>("name"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false, e.GetAttributeValue<string>("version"))
                    .With("isolationmode", e.GetAttributeValue<OptionSetValue>("isolationmode")?.Value.ToString()))
                .ToList();
        }

        private static List<ComponentSnapshot> PluginSteps(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("name", "stage", "mode", "rank", "statecode", "ismanaged", "sdkmessageid")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.PluginSteps,
                        // Match by message + entity + stage (as the spec requires): the step name usually
                        // already encodes message: entity, so key on name + stage.
                        $"{e.GetAttributeValue<string>("name")}|{e.GetAttributeValue<OptionSetValue>("stage")?.Value}",
                        e.GetAttributeValue<string>("name"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("stage", e.GetAttributeValue<OptionSetValue>("stage")?.Value.ToString())
                    .With("mode", e.GetAttributeValue<OptionSetValue>("mode")?.Value.ToString())
                    .With("rank", e.GetAttributeValue<int?>("rank")?.ToString())
                    .With("state", e.GetAttributeValue<OptionSetValue>("statecode")?.Value.ToString()))
                .ToList();
        }

        private static List<ComponentSnapshot> PluginImages(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("sdkmessageprocessingstepimage")
            {
                ColumnSet = new ColumnSet("name", "imagetype", "attributes", "entityalias", "ismanaged")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.PluginImages,
                        $"{e.GetAttributeValue<string>("name")}|{e.GetAttributeValue<string>("entityalias")}",
                        e.GetAttributeValue<string>("name"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("imagetype", e.GetAttributeValue<OptionSetValue>("imagetype")?.Value.ToString())
                    .With("attributes", e.GetAttributeValue<string>("attributes")))
                .ToList();
        }

        private static List<ComponentSnapshot> Workflows(IOrganizationService svc, BackgroundWorker worker)
        {
            // category: 0=Workflow 2=BusinessRule 5=ModernFlow; clientdata often unavailable — degrade to
            // presence + type comparison when its content is not accessible.
            var q = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("name", "category", "type", "statecode", "ismanaged"),
                Criteria = { Conditions = { new ConditionExpression("type", ConditionOperator.Equal, 1) } } // definitions only
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.Workflows,
                        $"{e.GetAttributeValue<OptionSetValue>("category")?.Value}|{e.GetAttributeValue<string>("name")}",
                        e.GetAttributeValue<string>("name"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("category", e.GetAttributeValue<OptionSetValue>("category")?.Value.ToString())
                    .With("state", e.GetAttributeValue<OptionSetValue>("statecode")?.Value.ToString()))
                .ToList();
        }

        private static List<ComponentSnapshot> CustomApis(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("customapi")
            {
                ColumnSet = new ColumnSet("uniquename", "name", "isfunction", "bindingtype", "ismanaged")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.CustomApis,
                        e.GetAttributeValue<string>("uniquename"),
                        e.GetAttributeValue<string>("name") ?? e.GetAttributeValue<string>("uniquename"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("isfunction", e.GetAttributeValue<bool?>("isfunction")?.ToString())
                    .With("bindingtype", e.GetAttributeValue<OptionSetValue>("bindingtype")?.Value.ToString()))
                .ToList();
        }

        // ---------------------------------------------------------------- config (env vars / conn refs / web resources)

        private static List<ComponentSnapshot> EnvironmentVariables(IOrganizationService svc, BackgroundWorker worker)
        {
            var defs = svc.RetrieveAll(new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("environmentvariabledefinitionid", "schemaname", "displayname", "type", "defaultvalue", "ismanaged")
            }, worker: worker);

            // Values keyed by definition id (one current value per definition).
            var valueByDef = new Dictionary<Guid, string>();
            try
            {
                var vals = svc.RetrieveAll(new QueryExpression("environmentvariablevalue")
                {
                    ColumnSet = new ColumnSet("environmentvariabledefinitionid", "value")
                }, worker: worker);
                foreach (var v in vals)
                {
                    var def = v.GetAttributeValue<EntityReference>("environmentvariabledefinitionid");
                    if (def != null) valueByDef[def.Id] = v.GetAttributeValue<string>("value");
                }
            }
            catch { /* values unavailable — definitions still compared */ }

            return defs.Select(e =>
            {
                var id = e.GetAttributeValue<Guid>("environmentvariabledefinitionid");
                // Type 100000002 = Secret in the environmentvariabledefinition type optionset.
                int? type = e.GetAttributeValue<OptionSetValue>("type")?.Value;
                bool secret = type == 100000002;
                var snap = new ComponentSnapshot(ComparisonCategories.EnvironmentVariables,
                        e.GetAttributeValue<string>("schemaname"),
                        e.GetAttributeValue<string>("displayname") ?? e.GetAttributeValue<string>("schemaname"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("type", type?.ToString())
                    .With("defaultvalue", secret ? CompareOptions.Mask : e.GetAttributeValue<string>("defaultvalue"), secret);
                snap.With("currentvalue",
                    secret ? CompareOptions.Mask : (valueByDef.TryGetValue(id, out var val) ? val : null), secret);
                return snap;
            }).ToList();
        }

        private static List<ComponentSnapshot> ConnectionReferences(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("connectionreference")
            {
                ColumnSet = new ColumnSet("connectionreferencelogicalname", "connectionreferencedisplayname", "connectorid", "ismanaged")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.ConnectionReferences,
                        e.GetAttributeValue<string>("connectionreferencelogicalname"),
                        e.GetAttributeValue<string>("connectionreferencedisplayname") ?? e.GetAttributeValue<string>("connectionreferencelogicalname"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("connectorid", e.GetAttributeValue<string>("connectorid")))
                .ToList();
        }

        private static List<ComponentSnapshot> WebResources(IOrganizationService svc, BackgroundWorker worker)
        {
            var q = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "webresourcetype", "content", "ismanaged")
            };
            return svc.RetrieveAll(q, worker: worker)
                .Select(e => new ComponentSnapshot(ComparisonCategories.WebResources,
                        e.GetAttributeValue<string>("name"), e.GetAttributeValue<string>("name"),
                        e.GetAttributeValue<bool?>("ismanaged") ?? false)
                    .With("type", e.GetAttributeValue<OptionSetValue>("webresourcetype")?.Value.ToString())
                    .With("contenthash", Hash(e.GetAttributeValue<string>("content"))))
                .ToList();
        }

        // ---------------------------------------------------------------- helpers

        private static EntityMetadata[] SafeMetadata(IOrganizationService svc, EntityFilters filters)
        {
            var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = filters,
                RetrieveAsIfPublished = true
            });
            return resp.EntityMetadata ?? Array.Empty<EntityMetadata>();
        }

        private static string Label(Microsoft.Xrm.Sdk.Label label) =>
            label?.UserLocalizedLabel?.Label;

        private static bool Cancelled(BackgroundWorker worker) => worker?.CancellationPending == true;

        /// <summary>Short, stable content hash used to compare large XML/content blobs without storing them.</summary>
        private static string Hash(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++) sb.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }
    }
}
