using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.ManagedSolutionImpactChecker.Analysis
{
    /// <summary>
    /// Pure, deterministic, SDK-free rules that turn a <see cref="LayerAnalysisInput"/> and a chosen
    /// <see cref="DeploymentPath"/> into an <see cref="ImpactReport"/> (findings, banded score, pre-upgrade
    /// checklist, rollback guidance, metrics). Every judgement is path-aware: an <b>Upgrade</b> replaces the
    /// managed base and DELETES components missing from the incoming solution, while an <b>Update</b> or
    /// <b>Patch</b> never deletes — so deletion/overwrite risk is only surfaced on the delete-capable paths
    /// (Upgrade / Holding). The collector does every Dataverse read and hands a fully-populated input in;
    /// this class never touches a connection, so it is fully unit-testable.
    /// </summary>
    public static class LayerImpactRules
    {
        public const string Category = "Layering";

        public static ImpactReport Evaluate(LayerAnalysisInput input, DeploymentPath path, ImpactOptions opts = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            opts = opts ?? ImpactOptions.Default;

            var findings = new List<Finding>();
            var layers = input.Layers ?? new List<ComponentLayer>();
            bool pathDeletes = PathDeletes(path, opts);

            EvaluateUnmanagedLayers(findings, layers, path, pathDeletes);
            EvaluateRemovedComponents(findings, input.RemovedComponents ?? new List<string>(), path, pathDeletes, input.RemovedComponentsAssessed);
            EvaluateMissingDependencies(findings, input.MissingDependencies ?? new List<(string, string)>());
            EvaluatePublisherPrefix(findings, input.SourcePublisherPrefix, input.TargetPublisherPrefix);
            EvaluateRestrictiveManagedProperties(findings, layers);

            if (findings.Count == 0)
            {
                findings.Add(new Finding(Category, Severity.Info,
                    "No layering impact detected",
                    $"No unmanaged layers above managed components, removed components, missing dependencies, " +
                    $"publisher mismatch, or restrictive managed properties were found for the {path} path.",
                    component: null,
                    recommendation: "Proceed with standard change control."));
            }

            var calc = ScoreCalculator.RiskDefault;
            int score = calc.Score(findings);
            var band = calc.Band(findings, score);

            var report = new ImpactReport
            {
                Score = score,
                Band = band,
                Findings = findings.OrderByDescending(f => f.Severity).ToList(),
                Checklist = BuildChecklist(input, findings, path, pathDeletes),
                RollbackGuidance = BuildRollbackGuidance(input, findings, path, pathDeletes),
                Metrics = BuildMetrics(input, findings, path)
            };
            return report;
        }

        /// <summary>Upgrade always deletes; Holding deletes once applied (configurable). Update/Patch never delete.</summary>
        public static bool PathDeletes(DeploymentPath path, ImpactOptions opts = null)
        {
            opts = opts ?? ImpactOptions.Default;
            return path == DeploymentPath.Upgrade ||
                   (path == DeploymentPath.Holding && opts.TreatHoldingAsDeleting);
        }

        // ----------------------------------------------------------------- Rule: unmanaged layer above managed

        private static void EvaluateUnmanagedLayers(
            List<Finding> findings, List<ComponentLayer> layers, DeploymentPath path, bool pathDeletes)
        {
            foreach (var c in layers.Where(l => l != null && l.HasUnmanagedLayerAbove))
            {
                var component = ComponentLabel(c);

                // Detection finding — an active unmanaged customization exists above the managed layer. It
                // persists and masks the managed definition, so it is always worth surfacing (Medium).
                findings.Add(new Finding(Category, Severity.Medium,
                    "Unmanaged customization above managed layer",
                    $"'{component}' has an active unmanaged layer above its managed layer (owning layer: " +
                    $"{c.OwningSolution ?? "unknown"}). The unmanaged customization wins at runtime and will " +
                    "mask managed changes brought by this deployment.",
                    component,
                    "Review the active unmanaged customization; remove it if you want the managed definition to win."));

                // Escalation — on a delete-capable path (Upgrade/Holding) the managed base is replaced and the
                // active unmanaged customization may be reasserted/overwritten. Update/Patch leave it intact.
                if (pathDeletes)
                {
                    findings.Add(new Finding(Category, Severity.High,
                        "Component would be overwritten",
                        $"On the {path} path the managed base of '{component}' is replaced. The active unmanaged " +
                        "customization may be reasserted or overwritten, silently changing behaviour in production.",
                        component,
                        "Capture the current customization, then re-apply it after the upgrade if it is still required."));
                }
            }
        }

        // ----------------------------------------------------------------- Rule: removed components (deletion)

        private static void EvaluateRemovedComponents(
            List<Finding> findings, List<string> removed, DeploymentPath path, bool pathDeletes, bool assessed)
        {
            if (removed.Count == 0)
            {
                // Honesty guard: on a delete-capable path, an empty removed list only means "no data loss"
                // if removal was actually assessed. If it wasn't (e.g. a single-connection scan of just the
                // installed solution), say so plainly rather than implying the upgrade is deletion-safe.
                if (pathDeletes && !assessed)
                    findings.Add(new Finding(Category, Severity.Info,
                        "Deletion / data-loss impact not assessed",
                        $"The {path} path deletes components that the incoming solution removes, but this analysis " +
                        "did not receive removed-component data, so no deletion/data-loss risk could be evaluated. " +
                        "This is NOT a guarantee that the upgrade deletes nothing.",
                        component: null,
                        recommendation: "Compare the target against the incoming solution package (or the source " +
                        "environment) to enumerate removed components before upgrading."));
                return;
            }

            // Path-aware: Update and Patch NEVER delete, so removed components are not a data-loss risk on
            // those paths — surface a single informational note instead of per-component deletion findings.
            if (!pathDeletes)
            {
                findings.Add(new Finding(Category, Severity.Info,
                    "Removed components are not deleted on this path",
                    $"{removed.Count} component(s) are absent from the incoming solution, but the {path} path does " +
                    "NOT delete components — they remain in the target. Only a managed Upgrade (or an applied " +
                    "Holding upgrade) deletes them.",
                    component: null,
                    recommendation: "If the intent is to delete these components, stage a managed Upgrade instead."));
                return;
            }

            foreach (var entry in removed)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;
                var (type, name) = SplitRemoved(entry);
                var kind = ClassifyRemoved(type);
                var component = string.IsNullOrEmpty(type) ? name : $"{type}: {name}";

                switch (kind)
                {
                    case RemovedKind.Table:
                        findings.Add(new Finding(Category, Severity.Critical,
                            "Table would be deleted (data loss)",
                            $"Table '{name}' exists in the target but not in the incoming solution. The {path} path " +
                            "will delete it and permanently destroy all data it holds.",
                            component,
                            "Back up all rows for this table and confirm the deletion is intentional before upgrading."));
                        break;
                    case RemovedKind.Column:
                        findings.Add(new Finding(Category, Severity.High,
                            "Column would be deleted (data loss)",
                            $"Column '{name}' exists in the target but not in the incoming solution. The {path} path " +
                            "will delete it and the data stored in it.",
                            component,
                            "Back up the affected column's data and confirm the deletion is intentional before upgrading."));
                        break;
                    default:
                        findings.Add(new Finding(Category, Severity.Medium,
                            "Component would be deleted",
                            $"Component '{name}' exists in the target but not in the incoming solution. The {path} " +
                            "path will delete it.",
                            component,
                            "Confirm the removal is intentional; recreate the component if the deletion is unexpected."));
                        break;
                }
            }
        }

        // ----------------------------------------------------------------- Rule: missing dependencies

        private static void EvaluateMissingDependencies(
            List<Finding> findings, List<(string type, string name)> missing)
        {
            foreach (var dep in missing)
            {
                var typeLabel = string.IsNullOrEmpty(dep.type) ? "Component" : dep.type;
                var component = $"{typeLabel}: {dep.name}";
                findings.Add(new Finding(Category, Severity.High,
                    "Missing dependency",
                    $"The solution requires {typeLabel} '{dep.name}', which is neither contained in it nor present " +
                    "in the target. The import will fail until the dependency is satisfied.",
                    component,
                    $"Import the solution that provides '{dep.name}' first, or add it to this solution.",
                    "https://learn.microsoft.com/power-platform/alm/solution-concepts-alm"));
            }
        }

        // ----------------------------------------------------------------- Rule: publisher prefix mismatch

        private static void EvaluatePublisherPrefix(List<Finding> findings, string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) return;
            if (string.Equals(source.Trim(), target.Trim(), StringComparison.OrdinalIgnoreCase)) return;

            findings.Add(new Finding(Category, Severity.Medium,
                "Publisher prefix mismatch",
                $"The incoming solution's publisher prefix ('{source}') differs from the target's ('{target}'). " +
                "Mismatched publishers create parallel customization prefixes and can fragment ownership of components.",
                component: $"{source} ≠ {target}",
                recommendation: "Align the source and target publishers, or confirm the divergent prefix is intentional."));
        }

        // ----------------------------------------------------------------- Rule: restrictive managed properties

        private static void EvaluateRestrictiveManagedProperties(List<Finding> findings, List<ComponentLayer> layers)
        {
            var affected = layers
                .Where(l => l != null && l.RestrictiveManagedProperties)
                .Select(ComponentLabel)
                .ToList();
            if (affected.Count == 0) return;

            findings.Add(new Finding(Category, Severity.Medium,
                "Restrictive managed properties",
                $"{affected.Count} component(s) ship with managed properties that restrict customization after " +
                $"import: {string.Join(", ", affected.Take(20))}{(affected.Count > 20 ? ", …" : "")}. " +
                "You will not be able to change these components in the target once the solution is installed.",
                component: $"{affected.Count} component(s)",
                recommendation: "Confirm you do not need to customize these components post-import; request a less " +
                                "restrictive build from the publisher if you do."));
        }

        // ----------------------------------------------------------------- Checklist + rollback generation

        private static List<string> BuildChecklist(
            LayerAnalysisInput input, List<Finding> findings, DeploymentPath path, bool pathDeletes)
        {
            var list = new List<string>
            {
                $"Confirm the deployment path is '{path}' and record the current solution version before importing.",
                "Take a full backup / export of the target solution and its data before proceeding."
            };

            foreach (var f in findings.Where(f => f.Title == "Table would be deleted (data loss)"))
                list.Add($"Back up all data for {f.Component} — the upgrade will permanently delete it.");
            foreach (var f in findings.Where(f => f.Title == "Column would be deleted (data loss)"))
                list.Add($"Back up the data in {f.Component} before the upgrade deletes it.");

            if (findings.Any(f => f.Title == "Unmanaged customization above managed layer"))
                list.Add("Capture every active unmanaged customization so it can be reviewed/re-applied after import.");
            if (findings.Any(f => f.Title == "Missing dependency"))
                list.Add("Resolve all missing dependencies (import prerequisite solutions) before importing.");
            if (findings.Any(f => f.Title == "Publisher prefix mismatch"))
                list.Add("Reconcile the source and target publisher prefixes before importing.");
            if (findings.Any(f => f.Title == "Restrictive managed properties"))
                list.Add("Review the components with restrictive managed properties for post-import customization needs.");

            if (!pathDeletes && (input.RemovedComponents?.Count ?? 0) > 0)
                list.Add($"Note: the {path} path will not delete the absent components; stage an Upgrade if deletion is intended.");

            list.Add("Run this analysis in the actual target environment and attach the exported report to the change record.");
            return list;
        }

        private static List<string> BuildRollbackGuidance(
            LayerAnalysisInput input, List<Finding> findings, DeploymentPath path, bool pathDeletes)
        {
            var list = new List<string>
            {
                "Record the current managed solution version(s) and layer ownership so the pre-change state is documented."
            };

            if (pathDeletes)
            {
                list.Add("A managed Upgrade cannot be rolled back by uninstall alone — deleted components and their " +
                         "data are not restored. Keep a verified backup to recover them.");
                if (findings.Any(f => f.Title == "Table would be deleted (data loss)" ||
                                      f.Title == "Column would be deleted (data loss)"))
                    list.Add("To roll back a deletion, restore the affected table/column data from the pre-upgrade backup.");
            }
            else
            {
                list.Add($"The {path} path makes no deletions, so an uninstall / re-import of the prior solution " +
                         "version is the primary rollback path.");
            }

            if (findings.Any(f => f.Title == "Component would be overwritten"))
                list.Add("Keep the captured unmanaged customizations so any overwritten behaviour can be re-applied.");

            list.Add("Retain the previous solution package so it can be re-imported if the change must be reverted.");
            return list;
        }

        private static List<MetricRow> BuildMetrics(
            LayerAnalysisInput input, List<Finding> findings, DeploymentPath path)
        {
            var layers = input.Layers ?? new List<ComponentLayer>();
            int C(Severity s) => findings.Count(f => f.Severity == s);
            return new List<MetricRow>
            {
                new MetricRow("Deployment path", path.ToString()),
                new MetricRow("Components analyzed", layers.Count.ToString()),
                new MetricRow("Unmanaged layers above managed", layers.Count(l => l != null && l.HasUnmanagedLayerAbove).ToString()),
                new MetricRow("Removed components", (input.RemovedComponents?.Count ?? 0).ToString()),
                new MetricRow("Missing dependencies", (input.MissingDependencies?.Count ?? 0).ToString()),
                new MetricRow("Restrictive managed properties", layers.Count(l => l != null && l.RestrictiveManagedProperties).ToString()),
                new MetricRow("Critical findings", C(Severity.Critical).ToString()),
                new MetricRow("High findings", C(Severity.High).ToString()),
                new MetricRow("Medium findings", C(Severity.Medium).ToString()),
            };
        }

        // ----------------------------------------------------------------- Helpers

        private enum RemovedKind { Table, Column, Other }

        private static RemovedKind ClassifyRemoved(string type)
        {
            // Match the type token EXACTLY, not by substring: a substring test escalates related
            // multi-word types ("Entity Relationship"/"Entity Key" -> Table/Critical, "Field Security
            // Profile" -> Column/High), overstating data-loss risk. Only the bare table/column types
            // carry deletion data-loss severity.
            var t = (type ?? "").Trim().ToLowerInvariant();
            if (t == "entity" || t == "table") return RemovedKind.Table;
            if (t == "attribute" || t == "column" || t == "field") return RemovedKind.Column;
            return RemovedKind.Other;
        }

        /// <summary>Splits a "&lt;Type&gt;: &lt;Name&gt;" removed-component entry into its parts.</summary>
        private static (string type, string name) SplitRemoved(string entry)
        {
            var idx = entry.IndexOf(':');
            if (idx > 0)
                return (entry.Substring(0, idx).Trim(), entry.Substring(idx + 1).Trim());
            return ("", entry.Trim());
        }

        private static string ComponentLabel(ComponentLayer c)
        {
            if (c == null) return "(component)";
            if (!string.IsNullOrWhiteSpace(c.Name)) return c.Name;
            if (c.ObjectId != Guid.Empty) return c.ObjectId.ToString();
            return c.ComponentType ?? "(component)";
        }
    }
}
