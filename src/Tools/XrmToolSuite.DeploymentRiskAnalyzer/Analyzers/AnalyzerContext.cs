using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>Contract implemented by every analyzer module.</summary>
    public interface IAnalyzer
    {
        string Name { get; }
        AnalyzerCategory Category { get; }
        /// <summary>True when the analyzer produces meaningfully better results with a target connection.</summary>
        bool BenefitsFromTarget { get; }
        List<RiskFinding> Analyze(AnalyzerContext context, Action<string> progress);
    }

    /// <summary>
    /// Shared state for one analysis run: connections, the selected solution,
    /// its components, and lazily-cached metadata for source and target.
    /// </summary>
    public class AnalyzerContext
    {
        // Well-known solution component type codes
        public const int CT_Entity = 1;
        public const int CT_Attribute = 2;
        public const int CT_OptionSet = 9;
        public const int CT_EntityRelationship = 10;
        public const int CT_Role = 20;
        public const int CT_Workflow = 29;
        public const int CT_PluginType = 90;
        public const int CT_PluginAssembly = 91;
        public const int CT_SdkMessageProcessingStep = 92;
        public const int CT_WebResource = 61;

        public IOrganizationService Source { get; }
        public IOrganizationService Target { get; }   // may be null
        public bool HasTarget => Target != null;

        public Guid SolutionId { get; }
        public string SolutionUniqueName { get; }
        public string SolutionVersion { get; }
        public bool SolutionIsManaged { get; }
        public Guid PublisherId { get; }

        public List<Entity> SolutionComponents { get; private set; }

        private EntityMetadata[] _sourceEntities;
        private EntityMetadata[] _targetEntities;
        private readonly Dictionary<string, EntityMetadata> _sourceEntityDetail =
            new Dictionary<string, EntityMetadata>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EntityMetadata> _targetEntityDetail =
            new Dictionary<string, EntityMetadata>(StringComparer.OrdinalIgnoreCase);

        public AnalyzerContext(IOrganizationService source, IOrganizationService target, Entity solution)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target;
            SolutionId = solution.Id;
            SolutionUniqueName = solution.GetAttributeValue<string>("uniquename");
            SolutionVersion = solution.GetAttributeValue<string>("version");
            SolutionIsManaged = solution.GetAttributeValue<bool?>("ismanaged") ?? false;
            PublisherId = solution.GetAttributeValue<EntityReference>("publisherid")?.Id ?? Guid.Empty;
        }

        /// <summary>Loads all solutioncomponent rows for the selected solution (paged via XrmToolSuite.Core).</summary>
        public void LoadComponents()
        {
            var qe = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("componenttype", "objectid", "rootcomponentbehavior"),
                Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, SolutionId) } }
            };
            SolutionComponents = Source.RetrieveAll(qe);
        }

        public IEnumerable<Guid> ComponentIds(int componentType) =>
            SolutionComponents
                .Where(c => c.GetAttributeValue<OptionSetValue>("componenttype")?.Value == componentType)
                .Select(c => c.GetAttributeValue<Guid>("objectid"));

        /// <summary>All entity metadata (Entity filter only) for the source, cached.</summary>
        public EntityMetadata[] SourceEntities()
        {
            if (_sourceEntities == null) _sourceEntities = RetrieveAllEntities(Source);
            return _sourceEntities;
        }

        /// <summary>All entity metadata (Entity filter only) for the target, cached. Null when no target.</summary>
        public EntityMetadata[] TargetEntities()
        {
            if (!HasTarget) return null;
            if (_targetEntities == null) _targetEntities = RetrieveAllEntities(Target);
            return _targetEntities;
        }

        private static EntityMetadata[] RetrieveAllEntities(IOrganizationService svc)
        {
            var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true
            });
            return resp.EntityMetadata;
        }

        /// <summary>Full metadata (attributes + relationships) for one entity, cached per side.</summary>
        public EntityMetadata GetEntityDetail(string logicalName, bool fromTarget = false)
        {
            var cache = fromTarget ? _targetEntityDetail : _sourceEntityDetail;
            var svc = fromTarget ? Target : Source;
            if (svc == null) return null;
            if (cache.TryGetValue(logicalName, out var cached)) return cached;

            try
            {
                var resp = (RetrieveEntityResponse)svc.Execute(new RetrieveEntityRequest
                {
                    LogicalName = logicalName,
                    EntityFilters = EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = true
                });
                cache[logicalName] = resp.EntityMetadata;
                return resp.EntityMetadata;
            }
            catch (Exception)
            {
                cache[logicalName] = null; // entity does not exist on that side
                return null;
            }
        }

        /// <summary>Logical names of entities that are root components of this solution.</summary>
        public List<string> SolutionEntityLogicalNames()
        {
            var ids = new HashSet<Guid>(ComponentIds(CT_Entity));
            return SourceEntities()
                .Where(e => e.MetadataId.HasValue && ids.Contains(e.MetadataId.Value))
                .Select(e => e.LogicalName)
                .ToList();
        }

        /// <summary>
        /// Query a solution-aware table restricted to rows that are components of the selected solution,
        /// without relying on hard-coded component type codes (they vary for late-bound types).
        /// </summary>
        public EntityCollection QuerySolutionRows(string tableLogicalName, string idAttribute, params string[] columns)
        {
            var qe = new QueryExpression(tableLogicalName) { ColumnSet = new ColumnSet(columns) };
            var link = qe.AddLink("solutioncomponent", idAttribute, "objectid");
            link.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, SolutionId);
            return Source.RetrieveMultiple(qe);
        }

        /// <summary>Retrieve-multiple that never throws; returns empty collection on failure.</summary>
        public static EntityCollection SafeRetrieve(IOrganizationService svc, QueryExpression qe)
        {
            try { return svc.RetrieveMultiple(qe); }
            catch { return new EntityCollection(); }
        }
    }

    /// <summary>Friendly labels for solution component type codes used in dependency reporting.</summary>
    public static class ComponentTypeLabels
    {
        private static readonly Dictionary<int, string> Map = new Dictionary<int, string>
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

        public static string Get(int code) => Map.TryGetValue(code, out var s) ? s : $"Component ({code})";
    }
}
