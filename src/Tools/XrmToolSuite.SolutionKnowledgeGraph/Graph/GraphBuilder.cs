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
            {1,"Table"},{2,"Column"},{9,"Option Set"},{10,"Relationship"},{20,"Security Role"},
            {26,"View"},{29,"Workflow / Flow"},{59,"Chart"},{60,"Form"},{61,"Web Resource"},
            {80,"Model-driven App"},{90,"Plugin Type"},{91,"Plugin Assembly"},{92,"Plugin Step"},
            {300,"Canvas App"},{380,"Environment Variable"},{381,"Environment Variable Value"},
        };

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
            {80,("appmodule","appmoduleid","name")},
            {90,("plugintype","plugintypeid","typename")},
            {91,("pluginassembly","pluginassemblyid","name")},
            {92,("sdkmessageprocessingstep","sdkmessageprocessingstepid","name")},
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

            // Resolve entity (table) names from metadata once.
            var entityNames = ResolveEntityNames(service);

            foreach (var grp in byType)
            {
                string typeLabel = TypeLabels.TryGetValue(grp.Key, out var t) ? t : $"Component ({grp.Key})";
                var ids = grp.Select(x => x.Id).ToList();
                var names = ResolveNames(service, grp.Key, ids, entityNames, progress);
                foreach (var id in ids)
                {
                    names.TryGetValue(id, out var label);
                    g.AddNode(id.ToString(), typeLabel, string.IsNullOrEmpty(label) ? $"{typeLabel} {Short(id)}" : label);
                }
            }

            progress?.Invoke("Reading dependencies…");
            AddDependencyEdges(service, g, components.Select(c => c.GetAttributeValue<Guid>("objectid")).Where(id => id != Guid.Empty).ToList());

            return g;
        }

        private static Dictionary<Guid, string> ResolveEntityNames(IOrganizationService service)
        {
            var map = new Dictionary<Guid, string>();
            try
            {
                var resp = (RetrieveAllEntitiesResponse)service.Execute(new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                });
                foreach (var e in resp.EntityMetadata ?? new EntityMetadata[0])
                    if (e.MetadataId.HasValue)
                        map[e.MetadataId.Value] = e.DisplayName?.UserLocalizedLabel?.Label ?? e.LogicalName;
            }
            catch { /* fail-soft: tables just show as type + short id */ }
            return map;
        }

        private static Dictionary<Guid, string> ResolveNames(IOrganizationService service, int type,
            List<Guid> ids, Dictionary<Guid, string> entityNames, Action<string> progress)
        {
            if (type == 1) // Table
                return ids.Where(entityNames.ContainsKey).ToDictionary(id => id, id => entityNames[id]);

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

        private static void AddDependencyEdges(IOrganizationService service, GraphModel g, List<Guid> objectIds)
        {
            var idSet = new HashSet<Guid>(objectIds);
            foreach (var chunk in Chunk(objectIds, 300))
            {
                var qe = new QueryExpression("dependency")
                {
                    ColumnSet = new ColumnSet("dependentcomponentobjectid", "requiredcomponentobjectid"),
                    Criteria = { Conditions = { new ConditionExpression("dependentcomponentobjectid", ConditionOperator.In, chunk.Cast<object>().ToArray()) } }
                };
                foreach (var d in SafeRetrieve(service, qe).Entities)
                {
                    var from = d.GetAttributeValue<Guid?>("dependentcomponentobjectid");
                    var to = d.GetAttributeValue<Guid?>("requiredcomponentobjectid");
                    if (from.HasValue && to.HasValue && idSet.Contains(from.Value))
                        g.AddEdge(from.Value.ToString(), to.Value.ToString(), "requires");
                }
            }
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
