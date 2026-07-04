using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.SolutionComplexityScore.Analysis
{
    /// <summary>
    /// Per-run state for a complexity scan: the source connection and the selected solution. Provides a
    /// solution-scoped query helper (join to <c>solutioncomponent</c>) and stable component-type counts.
    /// Fail-soft — helpers return empty rather than throwing.
    /// </summary>
    public sealed class ComplexityContext
    {
        // Stable solution component type codes.
        public const int CT_Entity = 1;
        public const int CT_Attribute = 2;
        public const int CT_EntityRelationship = 10;
        public const int CT_SdkMessageProcessingStep = 92;
        public const int CT_CustomControl = 66; // PCF

        public IOrganizationService Source { get; }
        public Guid SolutionId { get; }
        public string SolutionUniqueName { get; }
        public string SolutionFriendlyName { get; }
        public string SolutionVersion { get; }
        public bool SolutionIsManaged { get; }

        private List<Entity> _components;

        public ComplexityContext(IOrganizationService source, Entity solution)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            SolutionId = solution.Id;
            SolutionUniqueName = solution.GetAttributeValue<string>("uniquename");
            SolutionFriendlyName = solution.GetAttributeValue<string>("friendlyname") ?? SolutionUniqueName;
            SolutionVersion = solution.GetAttributeValue<string>("version");
            SolutionIsManaged = solution.GetAttributeValue<bool?>("ismanaged") ?? false;
        }

        public void LoadComponents()
        {
            var qe = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("componenttype", "objectid"),
                Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, SolutionId) } }
            };
            try { _components = Source.RetrieveAll(qe); }
            catch { _components = new List<Entity>(); }
        }

        /// <summary>Number of solution components of the given type code.</summary>
        public int CountOfType(int componentType) =>
            (_components ?? new List<Entity>())
            .Count(c => c.GetAttributeValue<OptionSetValue>("componenttype")?.Value == componentType);

        /// <summary>
        /// Rows of a solution-aware table restricted to this solution's components (join on objectid),
        /// without relying on a component-type code. Returns an empty collection on failure.
        /// </summary>
        public EntityCollection QuerySolutionRows(string tableLogicalName, string idAttribute, params string[] columns)
        {
            try
            {
                var qe = new QueryExpression(tableLogicalName) { ColumnSet = new ColumnSet(columns) };
                var link = qe.AddLink("solutioncomponent", idAttribute, "objectid");
                link.LinkCriteria.AddCondition("solutionid", ConditionOperator.Equal, SolutionId);
                return Source.RetrieveMultiple(qe);
            }
            catch
            {
                return new EntityCollection();
            }
        }
    }
}
