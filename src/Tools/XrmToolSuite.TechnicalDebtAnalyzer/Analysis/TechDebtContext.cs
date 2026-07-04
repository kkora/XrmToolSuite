using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Analysis
{
    /// <summary>
    /// Shared per-run state for a technical-debt scan: the (single) source connection, its display name,
    /// and lazily-cached environment metadata (entities, web resources, processes, plugin registration,
    /// saved queries, roles). All retrieval is fail-soft — helpers return empty rather than throwing so a
    /// single permission gap degrades to an informational finding instead of aborting the scan.
    /// </summary>
    public sealed class TechDebtContext
    {
        // Well-known statecode values reused across analyzers.
        public const int StateInactive = 1;

        public IOrganizationService Source { get; }
        public string EnvironmentName { get; }

        /// <summary>Hard cap on per-entity row-count probes so a large org cannot stall the scan.</summary>
        public int MaxEntityProbes { get; set; } = 400;

        private EntityMetadata[] _entities;
        private readonly Dictionary<string, EntityMetadata> _entityDetail =
            new Dictionary<string, EntityMetadata>(StringComparer.OrdinalIgnoreCase);

        public TechDebtContext(IOrganizationService source, string environmentName)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            EnvironmentName = environmentName;
        }

        /// <summary>All entity metadata (Entity filter only), cached for the run.</summary>
        public EntityMetadata[] Entities()
        {
            if (_entities == null)
            {
                try
                {
                    var resp = (RetrieveAllEntitiesResponse)Source.Execute(new RetrieveAllEntitiesRequest
                    {
                        EntityFilters = EntityFilters.Entity,
                        RetrieveAsIfPublished = true
                    });
                    _entities = resp.EntityMetadata ?? new EntityMetadata[0];
                }
                catch
                {
                    _entities = new EntityMetadata[0];
                }
            }
            return _entities;
        }

        /// <summary>Custom entities only (where debt concentrates).</summary>
        public IEnumerable<EntityMetadata> CustomEntities() =>
            Entities().Where(e => e.IsCustomEntity == true && e.IsIntersect != true);

        /// <summary>Full metadata (attributes) for one entity, cached; null on failure.</summary>
        public EntityMetadata GetEntityDetail(string logicalName)
        {
            if (string.IsNullOrEmpty(logicalName)) return null;
            if (_entityDetail.TryGetValue(logicalName, out var cached)) return cached;
            try
            {
                var resp = (RetrieveEntityResponse)Source.Execute(new RetrieveEntityRequest
                {
                    LogicalName = logicalName,
                    EntityFilters = EntityFilters.Attributes,
                    RetrieveAsIfPublished = true
                });
                _entityDetail[logicalName] = resp.EntityMetadata;
                return resp.EntityMetadata;
            }
            catch
            {
                _entityDetail[logicalName] = null;
                return null;
            }
        }

        /// <summary>Row count for a table via aggregate fetch; -1 when it cannot be determined.</summary>
        public int RowCount(string logicalName)
        {
            try
            {
                var fetch =
                    $@"<fetch aggregate='true'><entity name='{logicalName}'>
                         <attribute name='{logicalName}id' alias='c' aggregate='count'/>
                       </entity></fetch>";
                var res = Source.RetrieveMultiple(new FetchExpression(fetch));
                if (res.Entities.Count == 0) return 0;
                var val = res.Entities[0].GetAttributeValue<AliasedValue>("c");
                return val != null ? Convert.ToInt32(val.Value) : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>Retrieve-all that never throws; returns an empty list on failure.</summary>
        public List<Entity> SafeRetrieveAll(QueryExpression qe)
        {
            try { return Source.RetrieveAll(qe); }
            catch { return new List<Entity>(); }
        }

        /// <summary>Retrieve-multiple that never throws; returns an empty collection on failure.</summary>
        public EntityCollection SafeRetrieve(QueryExpression qe)
        {
            try { return Source.RetrieveMultiple(qe); }
            catch { return new EntityCollection(); }
        }
    }
}
