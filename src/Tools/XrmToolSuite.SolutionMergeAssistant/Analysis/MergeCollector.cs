using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.SolutionMergeAssistant.Analysis
{
    /// <summary>
    /// Bridges Dataverse to the SDK-free <see cref="MergeRules"/>: loads each selected solution, enumerates
    /// its <c>solutioncomponent</c> rows once, resolves friendly names/types for the high-churn component
    /// classes, and gathers the environment-variable / connection-reference items each solution packages.
    /// This is the only Dataverse-touching piece, so it is deliberately kept out of the SDK-free unit-test
    /// set. Every read is via <see cref="QueryExtensions.RetrieveAll"/>; per-source failures degrade to a
    /// progress note and partial data rather than throwing, so one bad query can't fail the whole compare.
    /// Managed-state per component degrades to the owning solution's <c>ismanaged</c> flag.
    /// </summary>
    public sealed class MergeCollector
    {
        /// <summary>Component types whose friendly names are cheap and worth resolving for the grid/report.</summary>
        private static readonly int[] NamedTypes =
        {
            ComponentTypes.WebResource, ComponentTypes.PluginAssembly, ComponentTypes.PluginType,
            ComponentTypes.SdkMessageProcessingStep, ComponentTypes.SystemForm, ComponentTypes.SavedQuery,
            ComponentTypes.Workflow, ComponentTypes.Role, ComponentTypes.SavedQueryVisualization,
            ComponentTypes.EnvironmentVariableDefinition, ComponentTypes.ConnectionReference
        };

        /// <summary>Logical name of the table backing a named component type (for name resolution).</summary>
        private static readonly Dictionary<int, (string entity, string idAttr, string nameAttr)> NameSources =
            new Dictionary<int, (string, string, string)>
            {
                { ComponentTypes.WebResource, ("webresource", "webresourceid", "name") },
                { ComponentTypes.PluginAssembly, ("pluginassembly", "pluginassemblyid", "name") },
                { ComponentTypes.PluginType, ("plugintype", "plugintypeid", "typename") },
                { ComponentTypes.SdkMessageProcessingStep, ("sdkmessageprocessingstep", "sdkmessageprocessingstepid", "name") },
                { ComponentTypes.SystemForm, ("systemform", "formid", "name") },
                { ComponentTypes.SavedQuery, ("savedquery", "savedqueryid", "name") },
                { ComponentTypes.SavedQueryVisualization, ("savedqueryvisualization", "savedqueryvisualizationid", "name") },
                { ComponentTypes.Role, ("role", "roleid", "name") },
                { ComponentTypes.EnvironmentVariableDefinition, ("environmentvariabledefinition", "environmentvariabledefinitionid", "schemaname") },
                { ComponentTypes.ConnectionReference, ("connectionreference", "connectionreferenceid", "connectionreferencelogicalname") },
            };

        /// <summary>Loads each selected solution with its components (names/types resolved where cheap).</summary>
        public List<SolutionInfo> LoadSolutions(
            IOrganizationService svc,
            IEnumerable<Guid> solutionIds,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var ids = (solutionIds ?? Enumerable.Empty<Guid>()).Where(g => g != Guid.Empty).Distinct().ToList();
            var solutions = new List<SolutionInfo>();
            if (ids.Count == 0) return solutions;

            // ---- solution rows + publisher prefixes ----
            progress?.Invoke("Loading solution headers...");
            var prefixes = LoadPublisherPrefixes(svc, worker, progress);
            var solutionRows = RetrieveSolutions(svc, ids, worker, progress);

            foreach (var row in solutionRows)
            {
                if (worker?.CancellationPending == true) return solutions;
                var info = new SolutionInfo
                {
                    UniqueName = row.GetAttributeValue<string>("uniquename"),
                    FriendlyName = row.GetAttributeValue<string>("friendlyname"),
                    Version = row.GetAttributeValue<string>("version"),
                    IsManaged = row.GetAttributeValue<bool>("ismanaged"),
                };
                var pub = row.GetAttributeValue<EntityReference>("publisherid");
                if (pub != null && prefixes.TryGetValue(pub.Id, out var prefix))
                    info.PublisherPrefix = prefix;

                progress?.Invoke($"Enumerating components of '{info.UniqueName}'...");
                info.Components = LoadComponents(svc, row.Id, info.IsManaged, worker, progress);
                solutions.Add(info);
            }

            // ---- resolve friendly names for the high-churn types across all solutions ----
            ResolveNames(svc, solutions, worker, progress);

            return solutions;
        }

        /// <summary>
        /// Gathers the environment-variable and connection-reference items each solution packages, keyed by
        /// schema name, for the config-conflict class. Uses the already-loaded component object ids so it
        /// only resolves the definitions/values actually shipped.
        /// </summary>
        public List<ConfigItem> LoadConfigItems(
            IOrganizationService svc,
            IReadOnlyList<SolutionInfo> solutions,
            BackgroundWorker worker,
            Action<string> progress)
        {
            var items = new List<ConfigItem>();
            if (svc == null || solutions == null || solutions.Count == 0) return items;

            progress?.Invoke("Loading environment variables and connection references...");
            var envDefs = LoadEnvDefinitions(svc, worker, progress);      // id -> (schema, value)
            var envVals = LoadEnvValues(svc, envDefs, worker, progress);  // valueId -> (schema, value)
            var connRefs = LoadConnectionReferences(svc, worker, progress); // id -> (logicalname, connector)

            foreach (var sol in solutions)
            {
                if (worker?.CancellationPending == true) break;
                foreach (var comp in sol.Components)
                {
                    switch (comp.ComponentType)
                    {
                        case ComponentTypes.EnvironmentVariableDefinition:
                            if (envDefs.TryGetValue(comp.ObjectId, out var def))
                                items.Add(new ConfigItem { Kind = "EnvVar", SchemaName = def.schema, DefinitionOrValue = def.value, OwningSolution = sol.UniqueName });
                            break;
                        case ComponentTypes.EnvironmentVariableValue:
                            if (envVals.TryGetValue(comp.ObjectId, out var val))
                                items.Add(new ConfigItem { Kind = "EnvVar", SchemaName = val.schema, DefinitionOrValue = val.value, OwningSolution = sol.UniqueName });
                            break;
                        case ComponentTypes.ConnectionReference:
                            if (connRefs.TryGetValue(comp.ObjectId, out var cr))
                                items.Add(new ConfigItem { Kind = "ConnRef", SchemaName = cr.schema, DefinitionOrValue = cr.connector, OwningSolution = sol.UniqueName });
                            break;
                    }
                }
            }
            return items;
        }

        // ---- reads ---------------------------------------------------------------------------------

        private static List<Entity> RetrieveSolutions(
            IOrganizationService svc, List<Guid> ids, BackgroundWorker worker, Action<string> progress)
        {
            try
            {
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("uniquename", "friendlyname", "version", "ismanaged", "publisherid")
                };
                query.Criteria.AddCondition("solutionid", ConditionOperator.In, ids.Cast<object>().ToArray());
                return svc.RetrieveAll(query, null, worker).ToList();
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not load solutions: {ex.Message}");
                return new List<Entity>();
            }
        }

        private static Dictionary<Guid, string> LoadPublisherPrefixes(
            IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            var map = new Dictionary<Guid, string>();
            try
            {
                var query = new QueryExpression("publisher")
                {
                    ColumnSet = new ColumnSet("customizationprefix")
                };
                foreach (var row in svc.RetrieveAll(query, null, worker))
                    map[row.Id] = row.GetAttributeValue<string>("customizationprefix");
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not load publisher prefixes: {ex.Message}");
            }
            return map;
        }

        private static List<SolutionComponentRef> LoadComponents(
            IOrganizationService svc, Guid solutionId, bool solutionManaged,
            BackgroundWorker worker, Action<string> progress)
        {
            var components = new List<SolutionComponentRef>();
            try
            {
                var query = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("componenttype", "objectid", "ismetadata")
                };
                query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
                foreach (var row in svc.RetrieveAll(query, null, worker))
                {
                    var type = row.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? -1;
                    var objectId = row.GetAttributeValue<Guid>("objectid");
                    if (type < 0 || objectId == Guid.Empty) continue;
                    components.Add(new SolutionComponentRef
                    {
                        ComponentType = type,
                        ObjectId = objectId,
                        // Layering data (msdyn_componentlayer) is not queried per-row for cost; degrade to
                        // the owning solution's managed flag, which is what the merge rules compare on.
                        IsManaged = solutionManaged
                    });
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not enumerate components for a solution: {ex.Message}");
            }
            return components;
        }

        /// <summary>Resolves friendly names + type labels for the high-churn component classes in one batch per type.</summary>
        private static void ResolveNames(
            IOrganizationService svc, List<SolutionInfo> solutions, BackgroundWorker worker, Action<string> progress)
        {
            var byType = solutions
                .SelectMany(s => s.Components)
                .Where(c => NamedTypes.Contains(c.ComponentType))
                .GroupBy(c => c.ComponentType);

            foreach (var group in byType)
            {
                if (worker?.CancellationPending == true) return;
                if (!NameSources.TryGetValue(group.Key, out var src)) continue;

                var ids = group.Select(c => c.ObjectId).Distinct().ToList();
                var names = new Dictionary<Guid, string>();
                var isBusinessRule = new HashSet<Guid>();
                try
                {
                    var cols = group.Key == ComponentTypes.Workflow
                        ? new ColumnSet("workflowid", "name", "category")
                        : new ColumnSet(src.idAttr, src.nameAttr);
                    var entity = group.Key == ComponentTypes.Workflow ? "workflow" : src.entity;

                    var query = new QueryExpression(entity) { ColumnSet = cols };
                    query.Criteria.AddCondition(
                        group.Key == ComponentTypes.Workflow ? "workflowid" : src.idAttr,
                        ConditionOperator.In, ids.Cast<object>().ToArray());

                    foreach (var row in svc.RetrieveAll(query, null, worker))
                    {
                        names[row.Id] = row.GetAttributeValue<string>(
                            group.Key == ComponentTypes.Workflow ? "name" : src.nameAttr);
                        if (group.Key == ComponentTypes.Workflow &&
                            row.GetAttributeValue<OptionSetValue>("category")?.Value == 2) // 2 = Business Rule
                            isBusinessRule.Add(row.Id);
                    }
                }
                catch (Exception ex)
                {
                    progress?.Invoke($"Could not resolve names for {ComponentTypes.Name(group.Key)}: {ex.Message}");
                }

                foreach (var comp in group)
                {
                    if (names.TryGetValue(comp.ObjectId, out var name) && !string.IsNullOrEmpty(name))
                        comp.Name = name;
                    comp.ComponentTypeName = isBusinessRule.Contains(comp.ObjectId)
                        ? "Business Rule"
                        : ComponentTypes.Name(comp.ComponentType);
                }
            }

            // Everything else gets a type label (name stays the object id).
            foreach (var comp in solutions.SelectMany(s => s.Components)
                         .Where(c => string.IsNullOrEmpty(c.ComponentTypeName)))
                comp.ComponentTypeName = ComponentTypes.Name(comp.ComponentType);
        }

        private static Dictionary<Guid, (string schema, string value)> LoadEnvDefinitions(
            IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            var map = new Dictionary<Guid, (string, string)>();
            try
            {
                var query = new QueryExpression("environmentvariabledefinition")
                {
                    ColumnSet = new ColumnSet("schemaname", "defaultvalue")
                };
                foreach (var row in svc.RetrieveAll(query, null, worker))
                    map[row.Id] = (row.GetAttributeValue<string>("schemaname"),
                                   row.GetAttributeValue<string>("defaultvalue"));
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not load environment variable definitions: {ex.Message}");
            }
            return map;
        }

        private static Dictionary<Guid, (string schema, string value)> LoadEnvValues(
            IOrganizationService svc, Dictionary<Guid, (string schema, string value)> defs,
            BackgroundWorker worker, Action<string> progress)
        {
            var map = new Dictionary<Guid, (string, string)>();
            try
            {
                var query = new QueryExpression("environmentvariablevalue")
                {
                    ColumnSet = new ColumnSet("environmentvariabledefinitionid", "value")
                };
                foreach (var row in svc.RetrieveAll(query, null, worker))
                {
                    var defRef = row.GetAttributeValue<EntityReference>("environmentvariabledefinitionid");
                    var schema = defRef != null && defs.TryGetValue(defRef.Id, out var d) ? d.schema : null;
                    map[row.Id] = (schema ?? defRef?.Id.ToString(), row.GetAttributeValue<string>("value"));
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not load environment variable values: {ex.Message}");
            }
            return map;
        }

        private static Dictionary<Guid, (string schema, string connector)> LoadConnectionReferences(
            IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            var map = new Dictionary<Guid, (string, string)>();
            try
            {
                var query = new QueryExpression("connectionreference")
                {
                    ColumnSet = new ColumnSet("connectionreferencelogicalname", "connectorid")
                };
                foreach (var row in svc.RetrieveAll(query, null, worker))
                    map[row.Id] = (row.GetAttributeValue<string>("connectionreferencelogicalname"),
                                   row.GetAttributeValue<string>("connectorid"));
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not load connection references: {ex.Message}");
            }
            return map;
        }
    }
}
