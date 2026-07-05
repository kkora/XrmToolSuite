using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.CustomApiExplorer.Analysis
{
    /// <summary>
    /// The Dataverse-facing half of the tool: builds a <see cref="CustomApiCatalog"/> from the
    /// <c>customapi</c> / <c>customapirequestparameter</c> / <c>customapiresponseproperty</c> tables plus
    /// backing plugin type and SDK message. Read-only. Uses Microsoft.Xrm.Sdk and the shared <c>RetrieveAll</c>,
    /// so it stays OUT of the SDK-free unit-test set. Dependency lookups degrade to notes, never exceptions.
    /// </summary>
    public static class CustomApiCollector
    {
        public static CustomApiCatalog Collect(
            IOrganizationService service, BackgroundWorker worker, Action<string> progress)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var catalog = new CustomApiCatalog { CollectedOnUtc = DateTime.UtcNow };

            progress?.Invoke("Reading Custom APIs…");
            var apis = service.RetrieveAll(new QueryExpression("customapi")
            {
                ColumnSet = new ColumnSet("customapiid", "uniquename", "name", "displayname", "description",
                    "bindingtype", "boundentitylogicalname", "isfunction", "isprivate", "ismanaged", "plugintypeid")
            });

            var byId = new Dictionary<Guid, CustomApiInfo>();
            var pluginTypeIds = new HashSet<Guid>();
            foreach (var a in apis)
            {
                if (worker?.CancellationPending == true) return catalog;
                var pluginRef = a.GetAttributeValue<EntityReference>("plugintypeid");
                if (pluginRef != null) pluginTypeIds.Add(pluginRef.Id);

                var info = new CustomApiInfo
                {
                    Id = a.Id,
                    UniqueName = a.GetAttributeValue<string>("uniquename"),
                    DisplayName = a.GetAttributeValue<string>("displayname") ?? a.GetAttributeValue<string>("name"),
                    Description = a.GetAttributeValue<string>("description"),
                    BindingType = (CustomApiBindingType)(a.GetAttributeValue<OptionSetValue>("bindingtype")?.Value ?? 0),
                    BoundEntityLogicalName = a.GetAttributeValue<string>("boundentitylogicalname"),
                    IsFunction = a.GetAttributeValue<bool?>("isfunction") ?? false,
                    IsPrivate = a.GetAttributeValue<bool?>("isprivate") ?? false,
                    IsManaged = a.GetAttributeValue<bool?>("ismanaged") ?? false,
                    // Placeholder; resolved to a friendly name below.
                    PluginTypeName = pluginRef?.Id.ToString(),
                };
                info.Callers.Clear();
                byId[a.Id] = info;
                catalog.Apis.Add(info);
            }

            // Resolve plugin type names.
            try
            {
                var pluginNames = ResolvePluginTypeNames(service, pluginTypeIds);
                foreach (var info in catalog.Apis)
                {
                    if (Guid.TryParse(info.PluginTypeName, out var id) && pluginNames.TryGetValue(id, out var name))
                        info.PluginTypeName = name;
                    else if (Guid.TryParse(info.PluginTypeName, out _))
                        info.PluginTypeName = null; // an id we couldn't resolve → treat as none
                }
            }
            catch (Exception ex) { catalog.Notes.Add($"Plugin types: skipped ({ex.Message})"); }

            // Request parameters.
            try
            {
                progress?.Invoke("Reading request parameters…");
                var parameters = service.RetrieveAll(new QueryExpression("customapirequestparameter")
                {
                    ColumnSet = new ColumnSet("customapiid", "uniquename", "name", "displayname",
                        "type", "isoptional", "logicalentityname")
                });
                foreach (var p in parameters)
                {
                    var owner = p.GetAttributeValue<EntityReference>("customapiid");
                    if (owner == null || !byId.TryGetValue(owner.Id, out var info)) continue;
                    info.Parameters.Add(new CustomApiParameter
                    {
                        UniqueName = p.GetAttributeValue<string>("uniquename"),
                        DisplayName = p.GetAttributeValue<string>("displayname") ?? p.GetAttributeValue<string>("name"),
                        LogicalName = p.GetAttributeValue<string>("uniquename"),
                        Type = (CustomApiFieldType)(p.GetAttributeValue<OptionSetValue>("type")?.Value ?? 10),
                        IsOptional = p.GetAttributeValue<bool?>("isoptional") ?? false,
                        LogicalEntityName = p.GetAttributeValue<string>("logicalentityname"),
                    });
                }
            }
            catch (Exception ex) { catalog.Notes.Add($"Request parameters: skipped ({ex.Message})"); }

            // Response properties.
            try
            {
                progress?.Invoke("Reading response properties…");
                var responses = service.RetrieveAll(new QueryExpression("customapiresponseproperty")
                {
                    ColumnSet = new ColumnSet("customapiid", "uniquename", "name", "displayname",
                        "type", "logicalentityname")
                });
                foreach (var r in responses)
                {
                    var owner = r.GetAttributeValue<EntityReference>("customapiid");
                    if (owner == null || !byId.TryGetValue(owner.Id, out var info)) continue;
                    info.ResponseProperties.Add(new CustomApiResponseProperty
                    {
                        UniqueName = r.GetAttributeValue<string>("uniquename"),
                        DisplayName = r.GetAttributeValue<string>("displayname") ?? r.GetAttributeValue<string>("name"),
                        LogicalName = r.GetAttributeValue<string>("uniquename"),
                        Type = (CustomApiFieldType)(r.GetAttributeValue<OptionSetValue>("type")?.Value ?? 10),
                        LogicalEntityName = r.GetAttributeValue<string>("logicalentityname"),
                    });
                }
            }
            catch (Exception ex) { catalog.Notes.Add($"Response properties: skipped ({ex.Message})"); }

            // Order members for a stable catalog.
            foreach (var info in catalog.Apis)
            {
                info.Parameters.Sort((x, y) => string.CompareOrdinal(x.LogicalName, y.LogicalName));
                info.ResponseProperties.Sort((x, y) => string.CompareOrdinal(x.LogicalName, y.LogicalName));
            }

            return catalog;
        }

        private static Dictionary<Guid, string> ResolvePluginTypeNames(IOrganizationService service, HashSet<Guid> ids)
        {
            var map = new Dictionary<Guid, string>();
            if (ids.Count == 0) return map;
            var types = service.RetrieveAll(new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("plugintypeid", "typename", "friendlyname")
            });
            foreach (var t in types)
            {
                if (!ids.Contains(t.Id)) continue;
                map[t.Id] = t.GetAttributeValue<string>("typename")
                            ?? t.GetAttributeValue<string>("friendlyname");
            }
            return map;
        }
    }
}
