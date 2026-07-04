using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.AnalyzerTests.Fakes
{
    /// <summary>
    /// In-memory <see cref="IOrganizationService"/> test double for the DeploymentRiskAnalyzer analyzers.
    ///
    /// It is deliberately small: analyzers only ever read (RetrieveMultiple + a couple of metadata
    /// Execute requests), so writes/associates throw. RetrieveMultiple returns the rows staged for the
    /// queried table via <see cref="Seed"/>, applying top-level Equal/In conditions so target-side
    /// lookups (by schemaname / definition id) filter realistically. Link-entity criteria (used by
    /// AnalyzerContext.QuerySolutionRows, which filters on the LINKED solutioncomponent row) are ignored
    /// — the seeded rows already represent "rows in this solution".
    /// </summary>
    public class FakeOrganizationService : IOrganizationService
    {
        private readonly Dictionary<string, List<Entity>> _tables =
            new Dictionary<string, List<Entity>>(StringComparer.OrdinalIgnoreCase);

        private EntityMetadata[] _allEntities = Array.Empty<EntityMetadata>();
        private readonly Dictionary<string, EntityMetadata> _entityDetail =
            new Dictionary<string, EntityMetadata>(StringComparer.OrdinalIgnoreCase);
        private EntityCollection _missingDependencies = new EntityCollection();

        /// <summary>Stage the rows returned when the given table is queried.</summary>
        public FakeOrganizationService Seed(string logicalName, params Entity[] rows)
        {
            _tables[logicalName] = rows?.ToList() ?? new List<Entity>();
            return this;
        }

        /// <summary>Seed an empty table only if it hasn't been staged yet (keeps explicit seeds intact).</summary>
        public FakeOrganizationService SeedIfAbsent(string logicalName)
        {
            if (!_tables.ContainsKey(logicalName)) _tables[logicalName] = new List<Entity>();
            return this;
        }

        /// <summary>Metadata returned by RetrieveAllEntitiesRequest (Entity filter).</summary>
        public FakeOrganizationService SeedAllEntities(params EntityMetadata[] entities)
        {
            _allEntities = entities ?? Array.Empty<EntityMetadata>();
            return this;
        }

        /// <summary>Metadata returned by RetrieveEntityRequest for one table.</summary>
        public FakeOrganizationService SeedEntityDetail(string logicalName, EntityMetadata metadata)
        {
            _entityDetail[logicalName] = metadata;
            return this;
        }

        /// <summary>Rows returned by RetrieveMissingDependenciesRequest.</summary>
        public FakeOrganizationService SeedMissingDependencies(params Entity[] deps)
        {
            _missingDependencies = new EntityCollection((deps ?? Array.Empty<Entity>()).ToList());
            return this;
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            if (!(query is QueryExpression qe))
                throw new NotSupportedException("FakeOrganizationService only handles QueryExpression.");

            var rows = _tables.TryGetValue(qe.EntityName, out var seeded)
                ? seeded.Where(r => Matches(r, qe.Criteria))
                : Enumerable.Empty<Entity>();

            if (qe.TopCount.HasValue)
                rows = rows.Take(qe.TopCount.Value);

            return new EntityCollection(rows.ToList()) { EntityName = qe.EntityName, MoreRecords = false };
        }

        private static bool Matches(Entity row, FilterExpression criteria)
        {
            if (criteria == null || criteria.Conditions.Count == 0) return true;
            // Only top-level Equal/In on the queried table's own attributes are modeled (And semantics).
            foreach (var c in criteria.Conditions)
            {
                var actual = Normalize(row.Contains(c.AttributeName) ? row[c.AttributeName] : null);
                switch (c.Operator)
                {
                    case ConditionOperator.Equal:
                        if (!Equals(actual, Normalize(c.Values.FirstOrDefault()))) return false;
                        break;
                    case ConditionOperator.In:
                        if (!c.Values.Select(Normalize).Any(v => Equals(v, actual))) return false;
                        break;
                    default:
                        // Unmodeled operator: don't silently pass — surface it so tests stay honest.
                        throw new NotSupportedException($"FakeOrganizationService does not model operator {c.Operator}.");
                }
            }
            return true;
        }

        private static object Normalize(object value)
        {
            switch (value)
            {
                case EntityReference er: return er.Id;
                case OptionSetValue os: return os.Value;
                default: return value;
            }
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            switch (request)
            {
                case RetrieveAllEntitiesRequest _:
                    return new RetrieveAllEntitiesResponse { Results = { ["EntityMetadata"] = _allEntities } };

                case Microsoft.Crm.Sdk.Messages.RetrieveMissingDependenciesRequest _:
                    return new Microsoft.Crm.Sdk.Messages.RetrieveMissingDependenciesResponse
                    { Results = { ["EntityCollection"] = _missingDependencies } };

                case RetrieveEntityRequest re:
                    if (_entityDetail.TryGetValue(re.LogicalName, out var md) && md != null)
                        return new RetrieveEntityResponse { Results = { ["EntityMetadata"] = md } };
                    // Mirror Dataverse: unknown entity faults; GetEntityDetail catches and treats as "absent".
                    throw new InvalidOperationException($"Entity '{re.LogicalName}' does not exist.");

                default:
                    throw new NotSupportedException($"FakeOrganizationService does not model request {request.RequestName}.");
            }
        }

        // --- Unused write surface: analyzers are read-only, so fail loudly if one ever writes. ---
        public Guid Create(Entity entity) => throw new NotSupportedException();
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotSupportedException();
        public void Update(Entity entity) => throw new NotSupportedException();
        public void Delete(string entityName, Guid id) => throw new NotSupportedException();
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotSupportedException();
    }
}
