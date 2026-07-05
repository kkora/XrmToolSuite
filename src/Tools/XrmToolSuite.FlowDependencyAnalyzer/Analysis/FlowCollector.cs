using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.FlowDependencyAnalyzer.Analysis
{
    /// <summary>
    /// The only Dataverse-touching piece: retrieves modern cloud flows (<c>workflow</c>, <c>category = 5</c>),
    /// connection references, environment-variable definitions and table metadata, parses each flow's
    /// <c>clientdata</c> via <see cref="FlowClientDataParser"/>, and runs <see cref="FlowRiskRules"/>. Deliberately
    /// kept out of the SDK-free unit-test set. Every query failure degrades to an informational finding rather
    /// than throwing, and paging / progress / cancellation flow through <see cref="QueryExtensions.RetrieveAll"/>.
    /// </summary>
    public sealed class FlowCollector
    {
        private readonly FlowRiskOptions _options;

        public FlowCollector(FlowRiskOptions options = null)
        {
            _options = options ?? FlowRiskOptions.Default;
        }

        public FlowAnalysis Collect(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));

            var infos = new List<Finding>();

            progress?.Invoke("Retrieving cloud flows...");
            var flowRows = SafeRetrieveAll(svc, BuildFlowQuery(), worker, infos, "cloud flows (workflow)");

            var knownConnRefs = RetrieveSet(svc, "connectionreference", "connectionreferencelogicalname",
                worker, infos, "connection references");
            var knownEnvVars = RetrieveSet(svc, "environmentvariabledefinition", "schemaname",
                worker, infos, "environment variables");
            var solutionByFlow = RetrieveSolutionMembership(svc, worker, infos);

            if (worker?.CancellationPending == true) return Finalize(new List<FlowDependencies>(), knownConnRefs, knownEnvVars, MissingLookup.Empty, infos);

            var flows = new List<FlowDependencies>();
            int i = 0;
            foreach (var row in flowRows)
            {
                if (worker?.CancellationPending == true) break;
                i++;
                var name = row.GetAttributeValue<string>("name") ?? "(unnamed flow)";
                progress?.Invoke($"Parsing flow {i}/{flowRows.Count}: {name}");

                FlowDependencies dep;
                try
                {
                    dep = FlowClientDataParser.Parse(name, row.GetAttributeValue<string>("clientdata"));
                }
                catch (Exception ex)
                {
                    dep = new FlowDependencies { FlowName = name, ParseNote = "Unexpected parse failure: " + ex.Message };
                }

                dep.WorkflowId = row.Id.ToString();
                dep.Owner = row.GetAttributeValue<EntityReference>("ownerid")?.Name;
                dep.State = (row.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? -1) == 1 ? "Activated" : "Draft";
                if (solutionByFlow != null && solutionByFlow.TryGetValue(row.Id, out var sols))
                    dep.Solution = string.Join("; ", sols.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
                flows.Add(dep);
            }

            progress?.Invoke("Resolving table metadata...");
            var missing = BuildMissingLookup(svc, flows, worker, infos);

            return Finalize(flows, knownConnRefs, knownEnvVars, missing, infos);
        }

        private FlowAnalysis Finalize(
            List<FlowDependencies> flows, ISet<string> knownConnRefs, ISet<string> knownEnvVars,
            MissingLookup missing, List<Finding> infos)
        {
            var analysis = FlowRiskRules.Analyze(flows, knownConnRefs, knownEnvVars, missing, _options);
            // Surface any collection-time degradations as informational findings (never thrown).
            analysis.Findings.InsertRange(0, infos);
            return analysis;
        }

        private static QueryExpression BuildFlowQuery()
        {
            var q = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet("name", "clientdata", "ownerid", "statecode", "category", "type")
            };
            q.Criteria.AddCondition("category", ConditionOperator.Equal, 5); // 5 = modern cloud flow
            q.Criteria.AddCondition("type", ConditionOperator.Equal, 1);     // 1 = definition (not activation/template)
            return q;
        }

        private static List<Entity> SafeRetrieveAll(
            IOrganizationService svc, QueryExpression query, BackgroundWorker worker,
            List<Finding> infos, string what)
        {
            try
            {
                return svc.RetrieveAll(query, worker: worker);
            }
            catch (Exception ex)
            {
                infos.Add(new Finding(FlowRiskRules.Category, Severity.Info,
                    $"Could not retrieve {what}",
                    $"The {what} query failed, so results may be incomplete: {ex.Message}",
                    recommendation: $"Verify the connected user can read {what}."));
                return new List<Entity>();
            }
        }

        /// <summary>Retrieves a set of string values for one column; returns null if the query fails (= unresolvable).</summary>
        private static ISet<string> RetrieveSet(
            IOrganizationService svc, string entity, string column, BackgroundWorker worker,
            List<Finding> infos, string what)
        {
            try
            {
                var rows = svc.RetrieveAll(new QueryExpression(entity) { ColumnSet = new ColumnSet(column) }, worker: worker);
                return new HashSet<string>(
                    rows.Select(r => r.GetAttributeValue<string>(column)).Where(s => !string.IsNullOrEmpty(s)),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                infos.Add(new Finding(FlowRiskRules.Category, Severity.Info,
                    $"Could not list {what}",
                    $"Listing {what} failed, so missing-{what} checks were skipped: {ex.Message}",
                    recommendation: $"Verify the connected user can read {entity}."));
                return null; // null = resolution unavailable → rules skip missing checks
            }
        }

        /// <summary>
        /// Best-effort map of workflow id → solution unique names (cloud flows are solutioncomponent
        /// type 29). Returns null if unavailable, so the solution filter simply shows no memberships.
        /// </summary>
        private static Dictionary<Guid, HashSet<string>> RetrieveSolutionMembership(
            IOrganizationService svc, BackgroundWorker worker, List<Finding> infos)
        {
            try
            {
                var q = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid", "solutionid")
                };
                q.Criteria.AddCondition("componenttype", ConditionOperator.Equal, 29); // 29 = Workflow
                var sol = q.AddLink("solution", "solutionid", "solutionid");
                sol.EntityAlias = "sol";
                sol.Columns = new ColumnSet("uniquename", "friendlyname", "ismanaged");

                var rows = svc.RetrieveAll(q, worker: worker);
                var map = new Dictionary<Guid, HashSet<string>>();
                foreach (var r in rows)
                {
                    var objId = r.GetAttributeValue<Guid>("objectid");
                    if (objId == Guid.Empty) continue;
                    var name = (r.GetAttributeValue<AliasedValue>("sol.friendlyname")?.Value as string)
                               ?? (r.GetAttributeValue<AliasedValue>("sol.uniquename")?.Value as string);
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("Active", StringComparison.OrdinalIgnoreCase)) continue; // noise
                    if (!map.TryGetValue(objId, out var set)) map[objId] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    set.Add(name);
                }
                return map;
            }
            catch (Exception ex)
            {
                infos.Add(new Finding(FlowRiskRules.Category, Severity.Info,
                    "Could not resolve solution membership",
                    "Solution membership could not be retrieved, so the solution filter will be empty: " + ex.Message,
                    recommendation: "Verify the connected user can read solutioncomponent."));
                return null;
            }
        }

        /// <summary>
        /// Resolves the set of table logical names (and their columns) referenced by the flows, to detect
        /// references to deleted/missing metadata. A metadata failure degrades to an unavailable lookup.
        /// </summary>
        private static MissingLookup BuildMissingLookup(
            IOrganizationService svc, List<FlowDependencies> flows, BackgroundWorker worker, List<Finding> infos)
        {
            var lookup = new MissingLookup();
            try
            {
                var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = false
                });

                var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in resp.EntityMetadata)
                {
                    if (!string.IsNullOrEmpty(m.LogicalName)) known.Add(m.LogicalName);
                    // Flow parameters often use the entity SET name (plural collection) — accept both.
                    if (!string.IsNullOrEmpty(m.EntitySetName)) known.Add(m.EntitySetName);
                    if (!string.IsNullOrEmpty(m.LogicalCollectionName)) known.Add(m.LogicalCollectionName);
                }
                lookup.KnownTables = known;
            }
            catch (Exception ex)
            {
                infos.Add(new Finding(FlowRiskRules.Category, Severity.Info,
                    "Could not resolve table metadata",
                    "Table metadata could not be retrieved, so missing-table checks were skipped: " + ex.Message,
                    recommendation: "Verify the connected user can read entity metadata."));
                lookup.KnownTables = null; // unavailable → rules skip
            }

            // Column resolution per-table is expensive and error-prone across environments; leave columns
            // unresolvable (null) so we never raise a false "missing column" finding. Table-level checks stand.
            lookup.KnownColumns = null;
            return lookup;
        }
    }
}
