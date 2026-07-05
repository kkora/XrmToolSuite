using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.ManagedSolutionImpactChecker.Analysis
{
    /// <summary>
    /// The single Dataverse-touching piece: builds a <see cref="LayerAnalysisInput"/> for a selected
    /// managed solution from a live connection, then hands it to the pure <see cref="LayerImpactRules"/>.
    /// Read-only — it never imports, upgrades, or deletes anything. Every query is wrapped so a
    /// permission/query failure degrades to an informational <see cref="Finding"/> (surfaced via
    /// <see cref="ImpactCollectionResult.Notes"/>) instead of throwing. Deliberately excluded from the
    /// SDK-free unit-test set.
    /// </summary>
    public sealed class ImpactCollector
    {
        // Well-known solution component type codes (mirrors the Deployment Risk Analyzer).
        private const int CT_Entity = 1;
        private const int CT_Attribute = 2;

        /// <summary>Everything the control needs after a collection run.</summary>
        public sealed class ImpactCollectionResult
        {
            public LayerAnalysisInput Input { get; set; } = new LayerAnalysisInput();
            /// <summary>Informational findings from degraded queries (merged into the report).</summary>
            public List<Finding> Notes { get; } = new List<Finding>();
            public string SolutionFriendlyName { get; set; }
            public string SolutionUniqueName { get; set; }
            public string SolutionVersion { get; set; }
            public bool IsManaged { get; set; }
        }

        public ImpactCollectionResult Collect(
            IOrganizationService svc,
            Entity solution,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            if (solution == null) throw new ArgumentNullException(nameof(solution));

            var result = new ImpactCollectionResult
            {
                SolutionFriendlyName = solution.GetAttributeValue<string>("friendlyname"),
                SolutionUniqueName = solution.GetAttributeValue<string>("uniquename"),
                SolutionVersion = solution.GetAttributeValue<string>("version"),
                IsManaged = solution.GetAttributeValue<bool?>("ismanaged") ?? false
            };
            var input = result.Input;

            // ---- Component layers ---------------------------------------------------------------------
            progress?.Invoke("Loading solution components…");
            List<Entity> components;
            try
            {
                var qe = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("componenttype", "objectid"),
                    Criteria = { Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, solution.Id) } }
                };
                components = svc.RetrieveAll(qe, worker: worker);
            }
            catch (Exception ex)
            {
                result.Notes.Add(Info("Component layers unavailable",
                    "Could not read the solution's components: " + ex.Message,
                    "Re-run as a System Administrator / System Customizer."));
                components = new List<Entity>();
            }

            var entityIds = new HashSet<Guid>(components
                .Where(c => (c.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? -1) == CT_Entity)
                .Select(c => c.GetAttributeValue<Guid>("objectid")));

            // Best-effort friendly names for tables from metadata (cheap, cached in one call).
            Dictionary<Guid, string> entityNames = TryLoadEntityNames(svc, result);

            foreach (var c in components)
            {
                if (worker?.CancellationPending == true) break;
                var typeCode = c.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? -1;
                var objectId = c.GetAttributeValue<Guid>("objectid");
                var typeLabel = ComponentTypeLabel(typeCode);
                var name = entityNames != null && entityNames.TryGetValue(objectId, out var n) ? n : objectId.ToString();

                input.Layers.Add(new ComponentLayer
                {
                    ComponentType = typeLabel,
                    Name = name,
                    ObjectId = objectId,
                    OwningSolution = result.SolutionFriendlyName ?? result.SolutionUniqueName,
                    IsManaged = result.IsManaged,
                    HasUnmanagedLayerAbove = false,          // populated below (best-effort)
                    RestrictiveManagedProperties = false     // populated below (best-effort)
                });
            }

            // ---- Active unmanaged layers above managed (msdyn_componentlayer, best-effort) ------------
            PopulateUnmanagedLayers(svc, result, worker, progress);

            // ---- Restrictive managed properties (metadata, best-effort for tables) --------------------
            PopulateRestrictiveManagedProperties(svc, result, entityIds, worker, progress);

            // ---- Missing dependencies -----------------------------------------------------------------
            progress?.Invoke("Checking missing dependencies…");
            try
            {
                var resp = (RetrieveMissingDependenciesResponse)svc.Execute(
                    new RetrieveMissingDependenciesRequest { SolutionUniqueName = result.SolutionUniqueName });
                foreach (var dep in resp.EntityCollection?.Entities ?? Enumerable.Empty<Entity>())
                {
                    var reqType = dep.GetAttributeValue<OptionSetValue>("requiredcomponenttype")?.Value ?? 0;
                    var reqId = dep.GetAttributeValue<Guid?>("requiredcomponentobjectid") ?? Guid.Empty;
                    input.MissingDependencies.Add((ComponentTypeLabel(reqType), reqId.ToString()));
                }
            }
            catch (Exception ex)
            {
                result.Notes.Add(Info("Dependency check partially unavailable",
                    "RetrieveMissingDependencies failed: " + ex.Message,
                    "Re-run with a System Administrator / System Customizer role."));
            }

            // ---- Publisher prefix (source solution's publisher) ---------------------------------------
            progress?.Invoke("Resolving publisher prefix…");
            input.SourcePublisherPrefix = TryResolvePublisherPrefix(svc, solution, result);
            // Single-connection analysis: no distinct target publisher to compare against, so leave target
            // null (the rules only flag a mismatch when both prefixes are present).
            input.TargetPublisherPrefix = null;

            progress?.Invoke($"Analyzed {input.Layers.Count} component(s).");
            return result;
        }

        private static Dictionary<Guid, string> TryLoadEntityNames(IOrganizationService svc, ImpactCollectionResult result)
        {
            try
            {
                var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = false
                });
                return resp.EntityMetadata
                    .Where(m => m.MetadataId.HasValue)
                    .GroupBy(m => m.MetadataId.Value)
                    .ToDictionary(g => g.Key, g => g.First().LogicalName);
            }
            catch (Exception ex)
            {
                result.Notes.Add(Info("Table names unavailable",
                    "Could not read table metadata; components are shown by object id: " + ex.Message,
                    "No action required — names are cosmetic."));
                return null;
            }
        }

        private void PopulateUnmanagedLayers(
            IOrganizationService svc, ImpactCollectionResult result, BackgroundWorker worker, Action<string> progress)
        {
            progress?.Invoke("Detecting unmanaged layers above managed components…");
            try
            {
                // msdyn_componentlayer surfaces the ordered layers per component. An "Active" (unmanaged)
                // layer alongside a managed one for the same component means an admin override sits on top.
                var qe = new QueryExpression("msdyn_componentlayer")
                {
                    ColumnSet = new ColumnSet("msdyn_componentid", "msdyn_solutionname", "msdyn_order", "msdyn_ismanaged")
                };
                var rows = svc.RetrieveAll(qe, worker: worker);

                var overridden = new HashSet<Guid>(rows
                    .GroupBy(r => r.GetAttributeValue<Guid?>("msdyn_componentid") ?? Guid.Empty)
                    .Where(g => g.Key != Guid.Empty)
                    .Where(g => g.Any(r => r.GetAttributeValue<bool?>("msdyn_ismanaged") == false))
                    .Where(g => g.Any(r => r.GetAttributeValue<bool?>("msdyn_ismanaged") == true))
                    .Select(g => g.Key));

                foreach (var layer in result.Input.Layers)
                    if (overridden.Contains(layer.ObjectId))
                        layer.HasUnmanagedLayerAbove = true;
            }
            catch (Exception ex)
            {
                result.Notes.Add(Info("Layer detection unavailable",
                    "Could not read component layers (msdyn_componentlayer): " + ex.Message +
                    ". Unmanaged-over-managed overrides were not detected.",
                    "Re-run with sufficient privileges, or review active layers manually in the maker portal."));
            }
        }

        private void PopulateRestrictiveManagedProperties(
            IOrganizationService svc, ImpactCollectionResult result, HashSet<Guid> entityIds,
            BackgroundWorker worker, Action<string> progress)
        {
            if (entityIds.Count == 0) return;
            progress?.Invoke("Checking managed properties…");
            try
            {
                var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = false
                });
                var restrictive = new HashSet<Guid>(resp.EntityMetadata
                    .Where(m => m.MetadataId.HasValue && entityIds.Contains(m.MetadataId.Value))
                    .Where(m => m.IsCustomizable != null && m.IsCustomizable.Value == false)
                    .Select(m => m.MetadataId.Value));

                foreach (var layer in result.Input.Layers)
                    if (restrictive.Contains(layer.ObjectId))
                        layer.RestrictiveManagedProperties = true;
            }
            catch (Exception ex)
            {
                result.Notes.Add(Info("Managed-property check unavailable",
                    "Could not read managed properties from metadata: " + ex.Message,
                    "Review component managed properties manually if customization restrictions are a concern."));
            }
        }

        private static string TryResolvePublisherPrefix(IOrganizationService svc, Entity solution, ImpactCollectionResult result)
        {
            try
            {
                var publisherRef = solution.GetAttributeValue<EntityReference>("publisherid");
                if (publisherRef == null) return null;
                var pub = svc.Retrieve("publisher", publisherRef.Id, new ColumnSet("customizationprefix"));
                return pub.GetAttributeValue<string>("customizationprefix");
            }
            catch (Exception ex)
            {
                result.Notes.Add(Info("Publisher prefix unavailable",
                    "Could not resolve the solution's publisher prefix: " + ex.Message,
                    "No action required."));
                return null;
            }
        }

        private static Finding Info(string title, string description, string recommendation) =>
            new Finding(LayerImpactRules.Category, Severity.Info, title, description, component: null, recommendation: recommendation);

        private static string ComponentTypeLabel(int code)
        {
            switch (code)
            {
                case 1: return "Entity";
                case 2: return "Attribute";
                case 3: return "Relationship";
                case 9: return "Option Set";
                case 10: return "Entity Relationship";
                case 20: return "Security Role";
                case 24: return "Form";
                case 26: return "Saved Query";
                case 29: return "Workflow / Flow";
                case 60: return "System Form";
                case 61: return "Web Resource";
                case 62: return "Site Map";
                case 80: return "Model-driven App";
                case 90: return "Plugin Type";
                case 91: return "Plugin Assembly";
                case 92: return "SDK Message Processing Step";
                case 380: return "Environment Variable Definition";
                case 381: return "Environment Variable Value";
                default: return code >= 0 ? $"Component ({code})" : "Component";
            }
        }
    }
}
