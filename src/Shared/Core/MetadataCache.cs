using System.Collections.Concurrent;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace XrmToolSuite.Core
{
    /// <summary>
    /// Per-session cache of entity metadata, keyed by connection + entity + filter depth.
    /// Call Clear() when the connection changes (BaseToolControl consumers can do this
    /// in their UpdateConnection override).
    /// </summary>
    public static class MetadataCache
    {
        private static readonly ConcurrentDictionary<string, EntityMetadata> Cache =
            new ConcurrentDictionary<string, EntityMetadata>();

        public static EntityMetadata GetEntity(
            IOrganizationService service,
            string connectionKey,
            string entityLogicalName,
            EntityFilters filters = EntityFilters.Attributes)
        {
            var key = $"{connectionKey}|{entityLogicalName}|{(int)filters}";

            return Cache.GetOrAdd(key, _ =>
            {
                var response = (RetrieveEntityResponse)service.Execute(new RetrieveEntityRequest
                {
                    LogicalName = entityLogicalName,
                    EntityFilters = filters
                });
                return response.EntityMetadata;
            });
        }

        public static void Clear() => Cache.Clear();
    }
}
