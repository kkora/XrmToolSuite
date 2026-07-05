using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.ComponentUsageExplorer.Analysis
{
    /// <summary>
    /// Bridges Dataverse to the SDK-free usage model. <see cref="Search"/> finds components by
    /// display/schema name, GUID, or type (via <c>solutioncomponent</c> + metadata for tables/columns);
    /// <see cref="BuildFootprint"/> enumerates required + dependent components via the platform dependency
    /// APIs (<see cref="RetrieveDependentComponentsRequest"/>, <see cref="RetrieveRequiredComponentsRequest"/>,
    /// <see cref="RetrieveDependenciesForDeleteRequest"/>), resolves their type names + owning solutions, and
    /// tallies usage-by-type. Read-only. Every unsupported query degrades to a flag / skipped item rather
    /// than throwing. This is the only Dataverse-touching piece, so it is deliberately not unit-tested.
    /// </summary>
    public sealed class UsageCollector
    {
        // solutioncomponent type codes we can resolve a name for directly.
        private const int CT_Entity = 1;
        private const int CT_Attribute = 2;

        /// <summary>
        /// Finds components matching <paramref name="query"/> (display name, schema name, GUID, or blank for
        /// "browse by type"), optionally filtered to a single component-type code. Searches tables/columns
        /// via metadata and other components via <c>solutioncomponent</c> joined to their base tables.
        /// </summary>
        public List<ComponentRef> Search(
            IOrganizationService svc, string query, int? typeFilter, BackgroundWorker worker)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var results = new List<ComponentRef>();
            var q = (query ?? string.Empty).Trim();
            Guid.TryParse(q, out var asGuid);

            // Owning-solution map is resolved lazily and cached across the whole search.
            var solutionsByObject = new Dictionary<Guid, HashSet<string>>();
            var managedByObject = new Dictionary<Guid, bool>();

            // ---- tables + columns via metadata (fast, name-friendly) ----
            if (!typeFilter.HasValue || typeFilter == CT_Entity || typeFilter == CT_Attribute)
            {
                try
                {
                    var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
                    {
                        EntityFilters = EntityFilters.Entity,
                        RetrieveAsIfPublished = false
                    });

                    foreach (var m in resp.EntityMetadata)
                    {
                        if (worker?.CancellationPending == true) break;
                        if (typeFilter == CT_Attribute) break; // columns handled below by explicit lookups only
                        var display = m.DisplayName?.UserLocalizedLabel?.Label;
                        if (!Matches(q, asGuid, display, m.LogicalName, m.MetadataId))
                            continue;

                        results.Add(new ComponentRef
                        {
                            ComponentType = CT_Entity,
                            ComponentTypeName = "Entity",
                            ObjectId = m.MetadataId ?? Guid.Empty,
                            Name = display,
                            SchemaName = m.LogicalName,
                            IsManaged = m.IsManaged ?? false
                        });
                    }
                }
                catch (Exception)
                {
                    // Metadata read failed — other component classes below still run.
                }
            }

            // ---- other components via solutioncomponent (name resolved per type below) ----
            try
            {
                var scQuery = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid", "componenttype", "solutionid")
                };
                if (typeFilter.HasValue)
                    scQuery.Criteria.AddCondition("componenttype", ConditionOperator.Equal, typeFilter.Value);

                var solLink = scQuery.AddLink("solution", "solutionid", "solutionid");
                solLink.EntityAlias = "sol";
                solLink.Columns = new ColumnSet("uniquename", "ismanaged", "isvisible");
                solLink.LinkCriteria.AddCondition("isvisible", ConditionOperator.Equal, true);

                var rows = svc.RetrieveAll(scQuery, null, worker);

                // Aggregate owning solutions per object id (a component can belong to several solutions).
                foreach (var row in rows)
                {
                    var objectId = row.GetAttributeValue<Guid>("objectid");
                    var unique = AliasString(row, "sol.uniquename");
                    var managed = AliasBool(row, "sol.ismanaged");
                    if (objectId == Guid.Empty) continue;

                    if (!solutionsByObject.TryGetValue(objectId, out var set))
                        solutionsByObject[objectId] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrEmpty(unique) &&
                        !string.Equals(unique, "Active", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(unique, "Default", StringComparison.OrdinalIgnoreCase))
                        set.Add(unique);
                    if (managed) managedByObject[objectId] = true;
                    else if (!managedByObject.ContainsKey(objectId)) managedByObject[objectId] = false;
                }

                // Build candidate refs from the distinct (objectid, componenttype) pairs, resolving a name.
                var pairs = rows
                    .Select(r => (id: r.GetAttributeValue<Guid>("objectid"),
                                  type: r.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0))
                    .Where(p => p.id != Guid.Empty && (typeFilter == null || p.type != CT_Entity))
                    .Distinct()
                    .ToList();

                foreach (var group in pairs.GroupBy(p => p.type))
                {
                    if (worker?.CancellationPending == true) break;
                    var names = ResolveNames(svc, group.Key, group.Select(p => p.id).Distinct().ToList());
                    foreach (var p in group)
                    {
                        names.TryGetValue(p.id, out var nm);
                        if (!Matches(q, asGuid, nm.name, nm.schema, p.id))
                            continue;
                        results.Add(new ComponentRef
                        {
                            ComponentType = p.type,
                            ComponentTypeName = ComponentTypeName(p.type),
                            ObjectId = p.id,
                            Name = nm.name,
                            SchemaName = nm.schema,
                            IsManaged = managedByObject.TryGetValue(p.id, out var mg) && mg,
                            OwningSolutions = solutionsByObject.TryGetValue(p.id, out var s)
                                ? s.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                                : new List<string>()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // solutioncomponent read failed — return whatever the metadata pass produced.
            }

            // Attach owning-solution info to the table results too.
            foreach (var r in results.Where(x => x.ComponentType == CT_Entity))
            {
                if (solutionsByObject.TryGetValue(r.ObjectId, out var s))
                    r.OwningSolutions = s.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            }

            return results
                .GroupBy(r => (r.ObjectId, r.ComponentType))
                .Select(g => g.First())
                .OrderBy(r => r.ComponentTypeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Builds the full where-used footprint for <paramref name="c"/>: required + dependent components
        /// (deduplicated across the three dependency APIs), their type names + owning solutions, and the
        /// usage-by-type tally. Any unsupported query is degraded to <see cref="UsageFootprint.DependencyDataIncomplete"/>.
        /// </summary>
        public UsageFootprint BuildFootprint(
            IOrganizationService svc, ComponentRef c, BackgroundWorker worker, Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            if (c == null) throw new ArgumentNullException(nameof(c));

            var fp = new UsageFootprint { Component = c };

            progress?.Invoke("Retrieving required components...");
            var required = new List<(Guid id, int type)>();
            TryDependencyCall(fp, () =>
            {
                var resp = (RetrieveRequiredComponentsResponse)svc.Execute(new RetrieveRequiredComponentsRequest
                {
                    ComponentType = c.ComponentType,
                    ObjectId = c.ObjectId
                });
                required.AddRange(ExtractDependencies(resp.EntityCollection, requiredSide: true));
            });

            if (worker?.CancellationPending == true) return Finish(fp, svc, worker, required, new List<(Guid, int)>());

            progress?.Invoke("Retrieving dependent components...");
            var dependents = new List<(Guid id, int type)>();
            TryDependencyCall(fp, () =>
            {
                var resp = (RetrieveDependentComponentsResponse)svc.Execute(new RetrieveDependentComponentsRequest
                {
                    ComponentType = c.ComponentType,
                    ObjectId = c.ObjectId
                });
                dependents.AddRange(ExtractDependencies(resp.EntityCollection, requiredSide: false));
            });

            progress?.Invoke("Checking delete dependencies...");
            TryDependencyCall(fp, () =>
            {
                var resp = (RetrieveDependenciesForDeleteResponse)svc.Execute(new RetrieveDependenciesForDeleteRequest
                {
                    ComponentType = c.ComponentType,
                    ObjectId = c.ObjectId
                });
                // These are components that would block a delete = things that depend on this one.
                dependents.AddRange(ExtractDependencies(resp.EntityCollection, requiredSide: false));
            });

            return Finish(fp, svc, worker, required, dependents);
        }

        private UsageFootprint Finish(
            UsageFootprint fp, IOrganizationService svc, BackgroundWorker worker,
            List<(Guid id, int type)> required, List<(Guid id, int type)> dependents)
        {
            var self = fp.Component;
            fp.RequiredComponents = ResolveRefs(svc, Dedupe(required, self), worker);
            fp.DependentComponents = ResolveRefs(svc, Dedupe(dependents, self), worker);
            fp.UsageByType = UsageFootprint.BuildUsageByType(fp.DependentComponents);
            return fp;
        }

        private static List<(Guid id, int type)> Dedupe(
            IEnumerable<(Guid id, int type)> items, ComponentRef self)
        {
            return items
                .Where(x => x.id != Guid.Empty && !(x.id == self.ObjectId && x.type == self.ComponentType))
                .GroupBy(x => (x.id, x.type))
                .Select(g => g.Key)
                .ToList();
        }

        private static IEnumerable<(Guid id, int type)> ExtractDependencies(EntityCollection deps, bool requiredSide)
        {
            if (deps == null) yield break;
            foreach (var d in deps.Entities)
            {
                // dependency rows carry required*/dependent* attribute pairs; pick the "other" side.
                var idAttr = requiredSide ? "requiredcomponentobjectid" : "dependentcomponentobjectid";
                var typeAttr = requiredSide ? "requiredcomponenttype" : "dependentcomponenttype";
                var id = d.GetAttributeValue<Guid?>(idAttr) ?? Guid.Empty;
                var type = d.GetAttributeValue<OptionSetValue>(typeAttr)?.Value ?? 0;
                if (id != Guid.Empty) yield return (id, type);
            }
        }

        /// <summary>Runs a dependency API call, flagging the footprint incomplete on any failure.</summary>
        private static void TryDependencyCall(UsageFootprint fp, Action call)
        {
            try { call(); }
            catch (Exception) { fp.DependencyDataIncomplete = true; }
        }

        private List<ComponentRef> ResolveRefs(
            IOrganizationService svc, List<(Guid id, int type)> pairs, BackgroundWorker worker)
        {
            var refs = new List<ComponentRef>();
            if (pairs.Count == 0) return refs;

            var solutionMap = OwningSolutions(svc, pairs.Select(p => p.id).Distinct().ToList(), worker);

            foreach (var group in pairs.GroupBy(p => p.type))
            {
                var names = ResolveNames(svc, group.Key, group.Select(p => p.id).Distinct().ToList());
                foreach (var p in group)
                {
                    names.TryGetValue(p.id, out var nm);
                    solutionMap.TryGetValue(p.id, out var sol);
                    refs.Add(new ComponentRef
                    {
                        ComponentType = p.type,
                        ComponentTypeName = ComponentTypeName(p.type),
                        ObjectId = p.id,
                        Name = nm.name,
                        SchemaName = nm.schema,
                        IsManaged = sol.managed,
                        OwningSolutions = sol.solutions ?? new List<string>()
                    });
                }
            }

            return refs
                .OrderBy(r => r.ComponentTypeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Resolves owning solution unique-names + managed state for a set of object ids.</summary>
        private static Dictionary<Guid, (List<string> solutions, bool managed)> OwningSolutions(
            IOrganizationService svc, List<Guid> ids, BackgroundWorker worker)
        {
            var map = new Dictionary<Guid, (List<string>, bool)>();
            if (ids == null || ids.Count == 0) return map;

            var tmpSolutions = new Dictionary<Guid, HashSet<string>>();
            var tmpManaged = new Dictionary<Guid, bool>();
            try
            {
                // Chunk the IN clause to stay well under platform limits.
                foreach (var chunk in Chunk(ids, 300))
                {
                    var qe = new QueryExpression("solutioncomponent")
                    {
                        ColumnSet = new ColumnSet("objectid", "solutionid")
                    };
                    qe.Criteria.AddCondition("objectid", ConditionOperator.In, chunk.Cast<object>().ToArray());
                    var link = qe.AddLink("solution", "solutionid", "solutionid");
                    link.EntityAlias = "sol";
                    link.Columns = new ColumnSet("uniquename", "ismanaged", "isvisible");
                    link.LinkCriteria.AddCondition("isvisible", ConditionOperator.Equal, true);

                    foreach (var row in svc.RetrieveAll(qe, null, worker))
                    {
                        var objectId = row.GetAttributeValue<Guid>("objectid");
                        var unique = AliasString(row, "sol.uniquename");
                        var managed = AliasBool(row, "sol.ismanaged");
                        if (objectId == Guid.Empty) continue;
                        if (!tmpSolutions.TryGetValue(objectId, out var set))
                            tmpSolutions[objectId] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (!string.IsNullOrEmpty(unique) &&
                            !string.Equals(unique, "Active", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(unique, "Default", StringComparison.OrdinalIgnoreCase))
                            set.Add(unique);
                        if (managed) tmpManaged[objectId] = true;
                        else if (!tmpManaged.ContainsKey(objectId)) tmpManaged[objectId] = false;
                    }
                }
            }
            catch (Exception)
            {
                // owning-solution info is best-effort
            }

            foreach (var id in ids)
            {
                var solutions = tmpSolutions.TryGetValue(id, out var s)
                    ? s.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList()
                    : new List<string>();
                var managed = tmpManaged.TryGetValue(id, out var mg) && mg;
                map[id] = (solutions, managed);
            }
            return map;
        }

        /// <summary>Resolves display + schema names for a set of ids of a single component type.</summary>
        private static Dictionary<Guid, (string name, string schema)> ResolveNames(
            IOrganizationService svc, int componentType, List<Guid> ids)
        {
            var map = new Dictionary<Guid, (string, string)>();
            if (ids == null || ids.Count == 0) return map;

            // Tables/columns resolve from metadata; everything else from its base table's primary-name column.
            var (entity, nameAttr) = BaseTableFor(componentType);
            if (entity == null) return map;

            try
            {
                foreach (var chunk in Chunk(ids, 300))
                {
                    var qe = new QueryExpression(entity)
                    {
                        ColumnSet = new ColumnSet(nameAttr)
                    };
                    qe.Criteria.AddCondition(entity + "id", ConditionOperator.In, chunk.Cast<object>().ToArray());
                    foreach (var row in svc.RetrieveAll(qe))
                    {
                        var nm = row.GetAttributeValue<string>(nameAttr);
                        map[row.Id] = (nm, null);
                    }
                }
            }
            catch (Exception)
            {
                // name resolution is cosmetic — leave unresolved ids as GUID labels
            }
            return map;
        }

        /// <summary>Base table + primary-name column for a component type, or (null,null) if unknown.</summary>
        private static (string entity, string nameAttr) BaseTableFor(int componentType)
        {
            switch (componentType)
            {
                case 20: return ("role", "name");
                case 24: return ("systemform", "name");
                case 60: return ("systemform", "name");
                case 26: return ("savedquery", "name");
                case 59: return ("savedqueryvisualization", "name");
                case 29: return ("workflow", "name");
                case 31: return ("report", "name");
                case 36: return ("template", "title");
                case 61: return ("webresource", "name");
                case 80: return ("appmodule", "name");
                case 90: return ("plugintype", "friendlyname");
                case 91: return ("pluginassembly", "name");
                case 92: return ("sdkmessageprocessingstep", "name");
                case 300: return ("canvasapp", "displayname");
                case 380: return ("environmentvariabledefinition", "displayname");
                case 381: return ("environmentvariablevalue", "schemaname");
                default: return (null, null);
            }
        }

        // ---- component-type labels (self-contained; mirrors the suite's shared map) ----
        private static readonly Dictionary<int, string> TypeLabels = new Dictionary<int, string>
        {
            {1,"Entity"},{2,"Attribute"},{3,"Relationship"},{9,"Option Set"},{10,"Entity Relationship"},
            {11,"Relationship Role"},{20,"Security Role"},{21,"Role Privilege"},{22,"Display String"},
            {24,"Form"},{26,"Saved Query"},{29,"Workflow / Flow"},{31,"Report"},{36,"Email Template"},
            {38,"KB Article Template"},{39,"Contract Template"},{44,"Duplicate Rule"},{50,"Ribbon Customization"},
            {59,"Chart"},{60,"System Form"},{61,"Web Resource"},{62,"Site Map"},{63,"Connection Role"},
            {65,"Hierarchy Rule"},{66,"Custom Control"},{68,"Custom Control Default Config"},
            {70,"Field Security Profile"},{71,"Field Permission"},{80,"Model-driven App"},
            {90,"Plugin Type"},{91,"Plugin Assembly"},{92,"SDK Message Processing Step"},
            {93,"SDK Message Processing Step Image"},{95,"Service Endpoint"},{150,"Routing Rule"},
            {151,"Routing Rule Item"},{152,"SLA"},{153,"SLA Item"},{154,"Convert Rule"},
            {161,"Mobile Offline Profile"},{165,"Similarity Rule"},{166,"Data Source Mapping"},
            {201,"SDK Message"},{300,"Canvas App"},{371,"Connector"},{372,"Connector (v2)"},
            {380,"Environment Variable Definition"},{381,"Environment Variable Value"},
            {400,"AI Project Type"},{401,"AI Project"},{402,"AI Configuration"},{430,"Entity Analytics Config"}
        };

        private static string ComponentTypeName(int code) =>
            TypeLabels.TryGetValue(code, out var s) ? s : $"Component ({code})";

        // ---- small helpers ----

        private static bool Matches(string query, Guid asGuid, string display, string schema, Guid? id)
        {
            if (string.IsNullOrEmpty(query)) return true; // blank = browse-by-type
            if (asGuid != Guid.Empty && id.HasValue && id.Value == asGuid) return true;
            return (display != null && display.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                || (schema != null && schema.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static IEnumerable<List<Guid>> Chunk(List<Guid> ids, int size)
        {
            for (int i = 0; i < ids.Count; i += size)
                yield return ids.GetRange(i, Math.Min(size, ids.Count - i));
        }

        private static string AliasString(Entity e, string alias)
        {
            if (e.Contains(alias) && e[alias] is AliasedValue a) return a.Value?.ToString();
            return null;
        }

        private static bool AliasBool(Entity e, string alias)
        {
            if (e.Contains(alias) && e[alias] is AliasedValue a && a.Value is bool b) return b;
            return false;
        }
    }
}
