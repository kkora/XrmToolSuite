using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.AttributeAuditor.Audit
{
    /// <summary>
    /// Per-run state for an attribute audit: the (single) source connection and lazily-cached environment
    /// metadata (all entities with their attributes). Fail-soft — every retrieval returns empty rather than
    /// throwing so a permission gap degrades the audit instead of aborting it. UI-free so the collector
    /// stays liftable into a console/CI wrapper and testable against a fake IOrganizationService.
    /// </summary>
    public sealed class AttributeAuditContext
    {
        public IOrganizationService Source { get; }
        public string EnvironmentName { get; }

        private EntityMetadata[] _entities;

        public AttributeAuditContext(IOrganizationService source, string environmentName)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            EnvironmentName = environmentName;
        }

        /// <summary>All entity metadata WITH attributes (Attributes filter), cached for the run.</summary>
        public EntityMetadata[] Entities()
        {
            if (_entities == null)
            {
                try
                {
                    var resp = (RetrieveAllEntitiesResponse)Source.Execute(new RetrieveAllEntitiesRequest
                    {
                        EntityFilters = EntityFilters.Attributes,
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

        /// <summary>Retrieve-all that never throws; returns an empty list on failure.</summary>
        public List<Entity> SafeRetrieveAll(string table, params string[] columns)
        {
            try { return Source.RetrieveAll(new QueryExpression(table) { ColumnSet = new ColumnSet(columns) }); }
            catch { return new List<Entity>(); }
        }
    }
}
