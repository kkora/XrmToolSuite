using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.ApiDocumentationBuilder.Api
{
    /// <summary>
    /// Builds an <see cref="ApiCatalog"/> from live metadata: <c>customapi</c> + <c>customapirequestparameter</c>
    /// + <c>customapiresponseproperty</c>, with backing plugin-type names resolved from <c>plugintype</c>.
    /// UI-free and fail-soft — a query gap degrades to a documented note rather than aborting. Read-only; never
    /// invokes an API.
    /// </summary>
    public sealed class ApiCollector
    {
        public ApiCatalog Build(IOrganizationService service, string environmentName, Action<string> progress)
        {
            var catalog = new ApiCatalog { EnvironmentName = environmentName };

            progress?.Invoke("Loading Custom APIs…");
            var apis = SafeRetrieveAll(service, new QueryExpression("customapi")
            {
                ColumnSet = new ColumnSet("customapiid", "uniquename", "name", "displayname", "description",
                    "bindingtype", "boundentitylogicalname", "isfunction", "isprivate", "ismanaged",
                    "executeprivilegename", "plugintypeid"),
                Orders = { new OrderExpression("uniquename", OrderType.Ascending) }
            }, out var apisFailed);

            if (apisFailed)
            {
                catalog.Notes.Add("Custom APIs could not be read in this environment (permission gap or unsupported).");
                return catalog;
            }
            if (apis.Count == 0)
                catalog.Notes.Add("No Custom APIs are present in this environment.");

            var byId = new Dictionary<Guid, ApiDoc>();
            foreach (var e in apis)
            {
                var doc = new ApiDoc
                {
                    Id = e.Id,
                    UniqueName = e.GetAttributeValue<string>("uniquename"),
                    DisplayName = e.GetAttributeValue<string>("displayname")
                                  ?? e.GetAttributeValue<string>("name"),
                    Description = e.GetAttributeValue<string>("description"),
                    BindingType = (ApiBindingType)(e.GetAttributeValue<OptionSetValue>("bindingtype")?.Value ?? 0),
                    BoundEntityLogicalName = e.GetAttributeValue<string>("boundentitylogicalname"),
                    IsFunction = e.GetAttributeValue<bool>("isfunction"),
                    IsPrivate = e.GetAttributeValue<bool>("isprivate"),
                    IsManaged = e.GetAttributeValue<bool>("ismanaged"),
                    ExecutePrivilegeName = e.GetAttributeValue<string>("executeprivilegename"),
                    PluginTypeName = e.GetAttributeValue<EntityReference>("plugintypeid")?.Name
                };
                byId[e.Id] = doc;
                catalog.Apis.Add(doc);
            }

            ResolvePluginTypeNames(service, catalog.Apis, apis);

            progress?.Invoke("Loading request parameters…");
            foreach (var rp in SafeRetrieve(service, new QueryExpression("customapirequestparameter")
            {
                ColumnSet = new ColumnSet("uniquename", "name", "displayname", "type", "isoptional",
                    "logicalentityname", "description", "customapiid")
            }).Entities)
            {
                var owner = rp.GetAttributeValue<EntityReference>("customapiid");
                if (owner == null || !byId.TryGetValue(owner.Id, out var doc)) continue;
                doc.Parameters.Add(new ApiParameter
                {
                    UniqueName = rp.GetAttributeValue<string>("uniquename"),
                    DisplayName = rp.GetAttributeValue<string>("displayname") ?? rp.GetAttributeValue<string>("name"),
                    LogicalName = rp.GetAttributeValue<string>("uniquename"),
                    Type = (ApiFieldType)(rp.GetAttributeValue<OptionSetValue>("type")?.Value ?? 10),
                    IsOptional = rp.GetAttributeValue<bool>("isoptional"),
                    LogicalEntityName = rp.GetAttributeValue<string>("logicalentityname"),
                    Description = rp.GetAttributeValue<string>("description")
                });
            }

            progress?.Invoke("Loading response properties…");
            foreach (var rp in SafeRetrieve(service, new QueryExpression("customapiresponseproperty")
            {
                ColumnSet = new ColumnSet("uniquename", "name", "displayname", "type",
                    "logicalentityname", "description", "customapiid")
            }).Entities)
            {
                var owner = rp.GetAttributeValue<EntityReference>("customapiid");
                if (owner == null || !byId.TryGetValue(owner.Id, out var doc)) continue;
                doc.ResponseProperties.Add(new ApiResponseProperty
                {
                    UniqueName = rp.GetAttributeValue<string>("uniquename"),
                    DisplayName = rp.GetAttributeValue<string>("displayname") ?? rp.GetAttributeValue<string>("name"),
                    LogicalName = rp.GetAttributeValue<string>("uniquename"),
                    Type = (ApiFieldType)(rp.GetAttributeValue<OptionSetValue>("type")?.Value ?? 10),
                    LogicalEntityName = rp.GetAttributeValue<string>("logicalentityname"),
                    Description = rp.GetAttributeValue<string>("description")
                });
            }

            // Stable ordering within each API.
            foreach (var doc in catalog.Apis)
            {
                doc.Parameters.Sort((a, b) => string.Compare(a.UniqueName, b.UniqueName, StringComparison.OrdinalIgnoreCase));
                doc.ResponseProperties.Sort((a, b) => string.Compare(a.UniqueName, b.UniqueName, StringComparison.OrdinalIgnoreCase));
            }

            return catalog;
        }

        // plugintypeid.Name is often unpopulated on the lookup; resolve the type names in one batched query.
        private static void ResolvePluginTypeNames(IOrganizationService service, List<ApiDoc> docs, List<Entity> apis)
        {
            var ids = apis
                .Select(e => e.GetAttributeValue<EntityReference>("plugintypeid")?.Id)
                .Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
            if (ids.Count == 0) return;

            var names = new Dictionary<Guid, string>();
            foreach (var chunk in Chunk(ids, 300))
            {
                var qe = new QueryExpression("plugintype")
                {
                    ColumnSet = new ColumnSet("plugintypeid", "typename", "name"),
                    Criteria = { Conditions = { new ConditionExpression("plugintypeid", ConditionOperator.In, chunk.Cast<object>().ToArray()) } }
                };
                foreach (var pt in SafeRetrieve(service, qe).Entities)
                    names[pt.Id] = pt.GetAttributeValue<string>("typename") ?? pt.GetAttributeValue<string>("name");
            }

            for (int i = 0; i < apis.Count; i++)
            {
                var id = apis[i].GetAttributeValue<EntityReference>("plugintypeid")?.Id;
                if (id.HasValue && names.TryGetValue(id.Value, out var name) && !string.IsNullOrEmpty(name))
                    docs[i].PluginTypeName = name;
            }
        }

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
}
