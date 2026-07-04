using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.AiSolutionReviewer.Analysis
{
    /// <summary>
    /// Per-run state for an AI-assisted solution review: the source connection and the selected solution.
    /// Provides a solution-scoped query helper (join to <c>solutioncomponent</c>) and stable type counts.
    /// Collectors gather structured facts; the AI layer turns them into recommendations. Fail-soft.
    /// </summary>
    public sealed class ReviewContext
    {
        public const int CT_Entity = 1;
        public const int CT_SdkMessageProcessingStep = 92;

        public IOrganizationService Source { get; }
        public Guid SolutionId { get; }
        public string SolutionUniqueName { get; }
        public string SolutionFriendlyName { get; }
        public string SolutionVersion { get; }
        public bool SolutionIsManaged { get; }

        public ReviewContext(IOrganizationService source, Entity solution)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            SolutionId = solution.Id;
            SolutionUniqueName = solution.GetAttributeValue<string>("uniquename");
            SolutionFriendlyName = solution.GetAttributeValue<string>("friendlyname") ?? SolutionUniqueName;
            SolutionVersion = solution.GetAttributeValue<string>("version");
            SolutionIsManaged = solution.GetAttributeValue<bool?>("ismanaged") ?? false;
        }

        /// <summary>Rows of a solution-aware table restricted to this solution's components. Empty on failure.</summary>
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

        /// <summary>Retrieve-multiple that never throws.</summary>
        public EntityCollection SafeRetrieve(QueryExpression qe)
        {
            try { return Source.RetrieveMultiple(qe); }
            catch { return new EntityCollection(); }
        }
    }
}
