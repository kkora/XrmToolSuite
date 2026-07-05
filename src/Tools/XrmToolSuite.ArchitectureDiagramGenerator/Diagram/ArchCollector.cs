using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.ArchitectureDiagramGenerator.Diagram
{
    /// <summary>
    /// Builds an <see cref="ArchDiagram"/> from a live solution: nodes are the solution's components
    /// (classified into architectural layers via <see cref="ComponentCatalog"/>, with best-effort display
    /// names), edges come from the platform <c>dependency</c> table (dependent → required). UI-free and
    /// fail-soft — a query gap degrades the diagram to a documented note rather than aborting. Mirrors the
    /// Solution Knowledge Graph's extraction so the two tools stay consistent.
    /// </summary>
    public sealed class ArchCollector
    {
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

        public ArchDiagram Build(IOrganizationService service, Guid solutionId, DiagramSolutionInfo info,
            Action<string> progress)
        {
            var d = new ArchDiagram
            {
                SolutionName = info?.FriendlyName,
                UniqueName = info?.UniqueName,
                Version = info?.Version,
                Publisher = info?.Publisher,
                IsManaged = info?.IsManaged ?? false
            };

            progress?.Invoke("Loading solution components…");
            var components = SafeRetrieveAll(service, new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("componenttype", "objectid"),
                Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId) } }
            }, out var componentsFailed);

            if (componentsFailed)
                d.Notes.Add("Solution components could not be read in this environment (permission gap); the diagram is empty.");

            var byType = components
                .Select(c => new
                {
                    Type = c.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? -1,
                    Id = c.GetAttributeValue<Guid>("objectid")
                })
                .Where(x => x.Id != Guid.Empty)
                .GroupBy(x => x.Type)
                .ToList();

            var entityNames = ResolveEntityNames(service, d);

            foreach (var grp in byType)
            {
                var label = ComponentCatalog.Label(grp.Key);
                var layer = ComponentCatalog.Layer(grp.Key);
                var ids = grp.Select(x => x.Id).ToList();
                var names = ResolveNames(service, grp.Key, ids, entityNames);
                foreach (var id in ids)
                {
                    names.TryGetValue(id, out var name);
                    d.Nodes.Add(new ArchNode(
                        id.ToString(),
                        string.IsNullOrEmpty(name) ? $"{label} {Short(id)}" : name,
                        label, layer));
                }
            }

            progress?.Invoke("Reading dependencies…");
            AddDependencyEdges(service, d, components
                .Select(c => c.GetAttributeValue<Guid>("objectid"))
                .Where(id => id != Guid.Empty)
                .ToList());

            return d;
        }

        private static Dictionary<Guid, string> ResolveEntityNames(IOrganizationService service, ArchDiagram d)
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
            catch
            {
                d.Notes.Add("Table display names were not available (metadata read failed); tables show as type + short id.");
            }
            return map;
        }

        private static Dictionary<Guid, string> ResolveNames(IOrganizationService service, int type,
            List<Guid> ids, Dictionary<Guid, string> entityNames)
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
                {
                    var name = row.GetAttributeValue<string>(src.name);
                    if (!string.IsNullOrEmpty(name)) result[row.Id] = name;
                }
            }
            return result;
        }

        private static void AddDependencyEdges(IOrganizationService service, ArchDiagram d, List<Guid> objectIds)
        {
            var idSet = new HashSet<Guid>(objectIds);
            var seen = new HashSet<string>();
            foreach (var chunk in Chunk(objectIds, 300))
            {
                var qe = new QueryExpression("dependency")
                {
                    ColumnSet = new ColumnSet("dependentcomponentobjectid", "requiredcomponentobjectid"),
                    Criteria = { Conditions = { new ConditionExpression("dependentcomponentobjectid", ConditionOperator.In, chunk.Cast<object>().ToArray()) } }
                };
                foreach (var dep in SafeRetrieve(service, qe).Entities)
                {
                    var from = dep.GetAttributeValue<Guid?>("dependentcomponentobjectid");
                    var to = dep.GetAttributeValue<Guid?>("requiredcomponentobjectid");
                    if (from.HasValue && to.HasValue && idSet.Contains(from.Value) && idSet.Contains(to.Value)
                        && from.Value != to.Value)
                    {
                        var key = from.Value + "|" + to.Value;
                        if (seen.Add(key))
                            d.Edges.Add(new ArchEdge(from.Value.ToString(), to.Value.ToString()));
                    }
                }
            }
        }

        private static string Short(Guid id) => id.ToString().Substring(0, 8);

        private static IEnumerable<List<T>> Chunk<T>(List<T> list, int size)
        {
            for (int i = 0; i < list.Count; i += size)
                yield return list.GetRange(i, Math.Min(size, list.Count - i));
        }

        private static List<Entity> SafeRetrieveAll(IOrganizationService service, QueryExpression qe, out bool failed)
        {
            try { failed = false; return service.RetrieveAll(qe); }
            catch { failed = true; return new List<Entity>(); }
        }

        private static EntityCollection SafeRetrieve(IOrganizationService service, QueryExpression qe)
        {
            try { return service.RetrieveMultiple(qe); } catch { return new EntityCollection(); }
        }
    }

    /// <summary>Lightweight solution descriptor for the picker + diagram header (SDK-free POCO).</summary>
    public sealed class DiagramSolutionInfo
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public bool IsManaged { get; set; }

        public override string ToString() =>
            $"{FriendlyName} ({UniqueName}) {Version}" + (IsManaged ? " [managed]" : "");
    }
}
