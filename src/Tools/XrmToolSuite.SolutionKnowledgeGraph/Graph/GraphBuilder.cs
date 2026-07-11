using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.SolutionKnowledgeGraph.Graph
{
    /// <summary>
    /// Builds a <see cref="GraphModel"/> from a live solution: nodes are the solution's components (with
    /// friendly type + best-effort display name), edges come from the platform <c>dependency</c> table
    /// (dependent → required). UI-free and fail-soft — a query gap degrades the graph rather than aborting.
    /// </summary>
    public static class GraphBuilder
    {
        private static readonly Dictionary<int, string> TypeLabels = new Dictionary<int, string>
        {
            {1,"Table"},{2,"Column"},{3,"Relationship"},{9,"Option Set"},{10,"Relationship"},
            {20,"Security Role"},{24,"Form"},{26,"View"},{29,"Workflow / Flow"},{31,"Report"},
            {59,"Chart"},{60,"Form"},{61,"Web Resource"},{62,"Site Map"},{63,"Connection Role"},
            {66,"Custom Control"},{68,"Custom Control Config"},{70,"Field Security Profile"},
            {71,"Field Permission"},{80,"Model-driven App"},{90,"Plugin Type"},{91,"Plugin Assembly"},
            {92,"Plugin Step"},{93,"Plugin Step Image"},{95,"Service Endpoint"},{300,"Canvas App"},
            {380,"Environment Variable"},{381,"Environment Variable Value"},
        };

        // Component types whose instance names come from METADATA (MetadataId-keyed): tables, columns,
        // relationships (3 = legacy, 10 = entity relationship), global option sets.
        private static readonly HashSet<int> MetadataNamedTypes = new HashSet<int> { 1, 2, 3, 9, 10 };

        // component type -> (table, id attribute, name attribute) for display-name resolution.
        private static readonly Dictionary<int, (string table, string id, string name)> NameSources =
            new Dictionary<int, (string, string, string)>
        {
            {20,("role","roleid","name")},
            {26,("savedquery","savedqueryid","name")},
            {29,("workflow","workflowid","name")},
            {59,("savedqueryvisualization","savedqueryvisualizationid","name")},
            {60,("systemform","formid","name")},
            {61,("webresource","webresourceid","name")},
            {62,("sitemap","sitemapid","sitemapname")},
            {80,("appmodule","appmoduleid","name")},
            {90,("plugintype","plugintypeid","typename")},
            {91,("pluginassembly","pluginassemblyid","name")},
            {92,("sdkmessageprocessingstep","sdkmessageprocessingstepid","name")},
            {380,("environmentvariabledefinition","environmentvariabledefinitionid","schemaname")},
            // 381 (Environment Variable Value) is intentionally NOT name-resolved: its only readable
            // column is the value itself, which can carry secrets — never put those in exports.
        };

        public static GraphModel Build(IOrganizationService service, Guid solutionId, Action<string> progress)
        {
            var g = new GraphModel();

            progress?.Invoke("Loading solution components…");
            var components = SafeRetrieveAll(service, new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("componenttype", "objectid"),
                Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId) } }
            });

            var byType = components
                .Select(c => new
                {
                    Type = c.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? -1,
                    Id = c.GetAttributeValue<Guid>("objectid")
                })
                .Where(x => x.Id != Guid.Empty)
                .GroupBy(x => x.Type)
                .ToList();

            // Resolve readable names once: tables/columns/relationships/option sets from metadata, and
            // org-specific component-type labels (codes ≥ 10000) from solutioncomponentdefinition.
            var metaNames = ResolveMetadataNames(service, progress);
            var customTypes = ResolveCustomTypeNames(service);

            foreach (var grp in byType)
            {
                var ids = grp.Select(x => x.Id).ToList();
                AddTypedNodes(service, g, grp.Key, ids, metaNames, customTypes, progress);
            }

            progress?.Invoke("Reading dependencies…");
            var solutionIds = new HashSet<Guid>(
                components.Select(c => c.GetAttributeValue<Guid>("objectid")).Where(id => id != Guid.Empty));
            var deps = ReadDependencies(service, solutionIds);

            // Type + name the required components that live OUTSIDE the solution, using the dependency row's
            // requiredcomponenttype — so they render as e.g. "Table Contact" instead of an "Unknown" GUID.
            foreach (var grp in deps.Where(d => !solutionIds.Contains(d.Required)).GroupBy(d => d.RequiredType))
            {
                var ids = grp.Select(d => d.Required).Distinct().Where(id => g.Node(id.ToString()) == null).ToList();
                AddTypedNodes(service, g, grp.Key, ids, metaNames, customTypes, progress);
            }

            foreach (var d in deps)
                if (solutionIds.Contains(d.Dependent))
                    g.AddEdge(d.Dependent.ToString(), d.Required.ToString(), "requires");

            return g;
        }

        /// <summary>Adds nodes of one component type with their best-effort display names (fail-soft label).</summary>
        private static void AddTypedNodes(IOrganizationService service, GraphModel g, int type,
            List<Guid> ids, Dictionary<Guid, string> metaNames, Dictionary<int, string> customTypes, Action<string> progress)
        {
            if (ids.Count == 0) return;
            string typeLabel = type < 0 ? "Unknown"
                : TypeLabels.TryGetValue(type, out var t) ? t
                : customTypes.TryGetValue(type, out var c) ? c
                : $"Component ({type})";
            var names = ResolveNames(service, type, ids, metaNames, progress);
            foreach (var id in ids)
            {
                names.TryGetValue(id, out var label);
                g.AddNode(id.ToString(), typeLabel, string.IsNullOrEmpty(label) ? $"{typeLabel} {Short(id)}" : label);
            }
        }

        /// <summary>
        /// MetadataId → readable name for every metadata-named component: tables ("Account"), columns
        /// ("account.Account Number"), relationships (schema name), and global option sets. One bulk
        /// metadata call; fail-soft (a gap just leaves those nodes on the type + short-id fallback).
        /// </summary>
        private static Dictionary<Guid, string> ResolveMetadataNames(IOrganizationService service, Action<string> progress)
        {
            var map = new Dictionary<Guid, string>();
            try
            {
                progress?.Invoke("Loading metadata names (tables, columns, relationships)…");
                var resp = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = true
                });
                foreach (var e in resp.EntityMetadata ?? new EntityMetadata[0])
                {
                    if (e.MetadataId.HasValue)
                        map[e.MetadataId.Value] = e.DisplayName?.UserLocalizedLabel?.Label ?? e.LogicalName;
                    foreach (var a in e.Attributes ?? new AttributeMetadata[0])
                        if (a.MetadataId.HasValue)
                            map[a.MetadataId.Value] = $"{e.LogicalName}.{a.DisplayName?.UserLocalizedLabel?.Label ?? a.LogicalName}";
                    foreach (var r in e.OneToManyRelationships ?? new OneToManyRelationshipMetadata[0])
                        if (r.MetadataId.HasValue) map[r.MetadataId.Value] = r.SchemaName;
                    foreach (var r in e.ManyToOneRelationships ?? new OneToManyRelationshipMetadata[0])
                        if (r.MetadataId.HasValue) map[r.MetadataId.Value] = r.SchemaName;
                    foreach (var r in e.ManyToManyRelationships ?? new ManyToManyRelationshipMetadata[0])
                        if (r.MetadataId.HasValue) map[r.MetadataId.Value] = r.SchemaName;
                }
            }
            catch { /* fail-soft */ }
            try
            {
                var resp = (RetrieveAllOptionSetsResponse)service.Execute(new RetrieveAllOptionSetsRequest
                {
                    RetrieveAsIfPublished = true
                });
                foreach (var os in resp.OptionSetMetadata ?? new OptionSetMetadataBase[0])
                    if (os.MetadataId.HasValue)
                        map[os.MetadataId.Value] = os.DisplayName?.UserLocalizedLabel?.Label ?? os.Name;
            }
            catch { /* fail-soft */ }
            return map;
        }

        /// <summary>
        /// Org-specific component-type names (codes ≥ 10000, e.g. Connection Reference) from the
        /// solutioncomponentdefinition table, so custom types don't show as "Component (10047)".
        /// </summary>
        private static Dictionary<int, string> ResolveCustomTypeNames(IOrganizationService service)
        {
            var map = new Dictionary<int, string>();
            foreach (var row in SafeRetrieveAll(service, new QueryExpression("solutioncomponentdefinition")
            {
                ColumnSet = new ColumnSet("solutioncomponenttype", "name")
            }))
            {
                var code = row.GetAttributeValue<int?>("solutioncomponenttype");
                var name = row.GetAttributeValue<string>("name");
                if (code.HasValue && !string.IsNullOrEmpty(name) && !map.ContainsKey(code.Value))
                    map[code.Value] = name;
            }
            return map;
        }

        private static Dictionary<Guid, string> ResolveNames(IOrganizationService service, int type,
            List<Guid> ids, Dictionary<Guid, string> metaNames, Action<string> progress)
        {
            if (MetadataNamedTypes.Contains(type)) // table / column / relationship / option set
                return ids.Where(metaNames.ContainsKey).ToDictionary(id => id, id => metaNames[id]);

            if (!NameSources.TryGetValue(type, out var src)) return new Dictionary<Guid, string>();

            var result = new Dictionary<Guid, string>();
            foreach (var chunk in Chunk(ids, 300))
            {
                var qe = new QueryExpression(src.table)
                {
                    ColumnSet = new ColumnSet(src.name),
                    Criteria = { Conditions = { new ConditionExpression(src.id, ConditionOperator.In, chunk.Cast<object>().ToArray()) } }
                };
                foreach (var row in SafeRetrieve(service, qe).Entities)
                    result[row.Id] = row.GetAttributeValue<string>(src.name);
            }
            return result;
        }

        /// <summary>One row of the platform <c>dependency</c> table: dependent → required, with the
        /// required component's type code (so external required components can be typed and named).</summary>
        private struct Dependency { public Guid Dependent; public Guid Required; public int RequiredType; }

        private static List<Dependency> ReadDependencies(IOrganizationService service, HashSet<Guid> dependentIds)
        {
            var list = new List<Dependency>();
            foreach (var chunk in Chunk(dependentIds.ToList(), 300))
            {
                var qe = new QueryExpression("dependency")
                {
                    ColumnSet = new ColumnSet("dependentcomponentobjectid", "requiredcomponentobjectid", "requiredcomponenttype"),
                    Criteria = { Conditions = { new ConditionExpression("dependentcomponentobjectid", ConditionOperator.In, chunk.Cast<object>().ToArray()) } }
                };
                foreach (var d in SafeRetrieve(service, qe).Entities)
                {
                    var from = d.GetAttributeValue<Guid?>("dependentcomponentobjectid");
                    var to = d.GetAttributeValue<Guid?>("requiredcomponentobjectid");
                    if (!from.HasValue || !to.HasValue) continue;
                    list.Add(new Dependency { Dependent = from.Value, Required = to.Value, RequiredType = OptionValue(d, "requiredcomponenttype") });
                }
            }
            return list;
        }

        // A component-type field is an OptionSetValue in Dataverse; tolerate a raw int too. -1 = unknown.
        private static int OptionValue(Entity e, string attr)
        {
            if (!e.Contains(attr) || e[attr] == null) return -1;
            if (e[attr] is OptionSetValue osv) return osv.Value;
            if (e[attr] is int i) return i;
            return -1;
        }

        private static string Short(Guid id) => id.ToString().Substring(0, 8);

        private static IEnumerable<List<T>> Chunk<T>(List<T> list, int size)
        {
            for (int i = 0; i < list.Count; i += size)
                yield return list.GetRange(i, Math.Min(size, list.Count - i));
        }

        private static List<Entity> SafeRetrieveAll(IOrganizationService service, QueryExpression qe)
        {
            try { return service.RetrieveAll(qe); } catch { return new List<Entity>(); }
        }

        private static EntityCollection SafeRetrieve(IOrganizationService service, QueryExpression qe)
        {
            try { return service.RetrieveMultiple(qe); } catch { return new EntityCollection(); }
        }
    }
}
