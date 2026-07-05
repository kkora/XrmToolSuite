using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.ComponentUsageExplorer.Analysis
{
    /// <summary>
    /// Pure, deterministic, SDK-free rules that turn a <see cref="UsageFootprint"/> into a
    /// <see cref="UsageReport"/>: a change-safety verdict, a banded impact score, findings per dependent
    /// class, and a plain-language explanation. Category is always "Usage". Never touches Dataverse — the
    /// collector does the reads and hands a fully-populated footprint in. Unit-tested with hand-built
    /// footprints.
    /// </summary>
    public static class UsageVerdictRules
    {
        public const string Category = "Usage";

        /// <summary>solutioncomponent type = Entity (a table). A table with many dependents is DoNotDelete.</summary>
        private const int CT_Entity = 1;

        /// <summary>
        /// High-value dependent types whose presence alone raises the blast radius to High impact: forms
        /// (24, 60), workflows/flows (29), model-driven apps (80), plugin type/assembly/step/image (90–93),
        /// and canvas apps (300).
        /// </summary>
        private static readonly HashSet<int> HighValueTypes = new HashSet<int> { 24, 60, 29, 80, 90, 91, 92, 93, 300 };

        public static UsageReport Evaluate(UsageFootprint fp, UsageVerdictOptions opts = null)
        {
            if (fp == null) throw new ArgumentNullException(nameof(fp));
            opts = opts ?? UsageVerdictOptions.Default;

            var report = new UsageReport();
            var component = fp.Component;
            var componentLabel = component?.Label ?? "(component)";

            var dependents = (fp.DependentComponents ?? new List<ComponentRef>())
                .Where(d => d != null).ToList();
            var required = (fp.RequiredComponents ?? new List<ComponentRef>())
                .Where(r => r != null).ToList();

            int depCount = dependents.Count;
            bool isTable = component != null && component.ComponentType == CT_Entity;

            // ---- classify dependents ----
            var componentSolutions = new HashSet<string>(
                component?.OwningSolutions ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            var managed = dependents.Where(d => d.IsManaged).ToList();
            var crossSolution = dependents.Where(d => IsCrossSolution(d, componentSolutions)).ToList();
            var highValue = dependents.Where(d => HighValueTypes.Contains(d.ComponentType)).ToList();

            // ---- findings per dependent class ----
            var findings = report.Findings;

            if (depCount == 0)
            {
                findings.Add(new Finding(Category, Severity.Info,
                    "No dependent components found",
                    $"No component was found that depends on '{componentLabel}'. Changing it is unlikely to break " +
                    "anything else, subject to any usage the platform dependency APIs do not track.",
                    componentLabel));
            }
            else
            {
                foreach (var group in dependents
                             .GroupBy(d => string.IsNullOrWhiteSpace(d.ComponentTypeName) ? "(unknown)" : d.ComponentTypeName)
                             .OrderByDescending(g => g.Count()))
                {
                    bool highValueGroup = group.Any(d => HighValueTypes.Contains(d.ComponentType));
                    var severity = highValueGroup ? Severity.High : Severity.Medium;
                    findings.Add(new Finding(Category, severity,
                        $"Used by {group.Count()} {group.Key}",
                        $"'{componentLabel}' is depended on by {group.Count()} {group.Key} component(s): " +
                        Sample(group) + ". A change to '" + componentLabel + "' can affect them.",
                        componentLabel,
                        "Review each dependent " + group.Key + " before changing or deleting the component."));
                }
            }

            if (managed.Count > 0)
            {
                findings.Add(new Finding(Category, Severity.High,
                    $"{managed.Count} managed dependent(s)",
                    $"'{componentLabel}' is depended on by {managed.Count} component(s) in a managed layer " +
                    $"({Sample(managed)}). Changing it can conflict with the managed solution's own updates.",
                    componentLabel,
                    "Coordinate the change with the managed solution's ALM process / publisher."));
            }

            if (crossSolution.Count > 0)
            {
                findings.Add(new Finding(Category, Severity.High,
                    $"{crossSolution.Count} cross-solution dependent(s)",
                    $"'{componentLabel}' is used by {crossSolution.Count} component(s) that ship in other " +
                    $"solution(s) ({Sample(crossSolution)}). The change lands outside this component's own solution.",
                    componentLabel,
                    "Confirm all owning solutions are re-exported / re-deployed together after the change."));
            }

            if (required.Count > 0)
            {
                findings.Add(new Finding(Category, Severity.Info,
                    $"Requires {required.Count} component(s)",
                    $"'{componentLabel}' itself depends on {required.Count} other component(s): {Sample(required)}. " +
                    "These must exist wherever the component is deployed.",
                    componentLabel));
            }

            if (fp.DependencyDataIncomplete)
            {
                findings.Add(new Finding(Category, Severity.Medium,
                    "Dependency data incomplete",
                    "The platform dependency APIs could not fully resolve usage for this component " +
                    "(unsupported type, Power Pages, or a query limitation). Treat the footprint as a lower bound.",
                    componentLabel,
                    "Verify usage manually (forms, flows, plugins, apps) before relying on this verdict."));
            }

            // ---- verdict (most-severe wins) ----
            ChangeSafety verdict;
            if (isTable && depCount >= opts.DoNotDeleteDependentThreshold)
                verdict = ChangeSafety.DoNotDelete;
            else if (managed.Count > 0 || crossSolution.Count > 0)
                verdict = ChangeSafety.RequiresAlmReview;
            else if (highValue.Count > 0 || depCount >= opts.HighImpactDependentThreshold)
                verdict = ChangeSafety.HighImpact;
            else if (fp.DependencyDataIncomplete)
                verdict = ChangeSafety.RequiresDependencyReview;
            else if (depCount > 0)
                verdict = ChangeSafety.ChangeWithCaution;
            else
                verdict = ChangeSafety.SafeToChange;

            report.Verdict = verdict;

            // ---- score + band (weighted from findings, then floored to the verdict's severity) ----
            int score = ScoreCalculator.RiskDefault.Score(findings);
            score = Math.Max(score, VerdictFloor(verdict));
            score = Math.Min(100, score);
            report.Score = score;
            report.Band = BandFor(verdict, score);

            // ---- metrics ----
            report.Metrics.Add(new MetricRow("Dependent components", depCount.ToString()));
            report.Metrics.Add(new MetricRow("Required components", required.Count.ToString()));
            report.Metrics.Add(new MetricRow("Managed dependents", managed.Count.ToString()));
            report.Metrics.Add(new MetricRow("Cross-solution dependents", crossSolution.Count.ToString()));
            report.Metrics.Add(new MetricRow("High-value dependents", highValue.Count.ToString()));

            // Ensure UsageByType is populated (idempotent with the collector's tally).
            if (fp.UsageByType == null || fp.UsageByType.Count == 0)
                fp.UsageByType = UsageFootprint.BuildUsageByType(dependents);

            report.Explanation = BuildExplanation(componentLabel, verdict, depCount, required.Count,
                managed, crossSolution, highValue, fp.DependencyDataIncomplete, isTable);

            return report;
        }

        private static bool IsCrossSolution(ComponentRef dep, HashSet<string> componentSolutions)
        {
            if (dep.OwningSolutions == null || dep.OwningSolutions.Count == 0) return false;
            if (componentSolutions.Count == 0) return false;
            // Cross-solution when the dependent shares no owning solution with the component.
            return !dep.OwningSolutions.Any(s => componentSolutions.Contains(s));
        }

        private static int VerdictFloor(ChangeSafety v)
        {
            switch (v)
            {
                case ChangeSafety.DoNotDelete: return 90;
                case ChangeSafety.RequiresAlmReview: return 70;
                case ChangeSafety.HighImpact: return 55;
                case ChangeSafety.RequiresDependencyReview: return 25;
                case ChangeSafety.ChangeWithCaution: return 15;
                default: return 0;
            }
        }

        private static ScoreBand BandFor(ChangeSafety v, int score)
        {
            switch (v)
            {
                case ChangeSafety.DoNotDelete:
                case ChangeSafety.HighImpact:
                case ChangeSafety.RequiresAlmReview:
                    return ScoreBand.High;
                case ChangeSafety.ChangeWithCaution:
                case ChangeSafety.RequiresDependencyReview:
                    return ScoreBand.Medium;
                default:
                    return ScoreBand.Low;
            }
        }

        private static string BuildExplanation(
            string label, ChangeSafety verdict, int depCount, int reqCount,
            List<ComponentRef> managed, List<ComponentRef> crossSolution, List<ComponentRef> highValue,
            bool incomplete, bool isTable)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Verdict: {UsageReport.VerdictText(verdict)}. ");

            switch (verdict)
            {
                case ChangeSafety.SafeToChange:
                    sb.Append($"Nothing was found to depend on '{label}', so a change has a small blast radius. ");
                    break;
                case ChangeSafety.ChangeWithCaution:
                    sb.Append($"'{label}' has {depCount} dependent(s). Review them before changing it. ");
                    break;
                case ChangeSafety.HighImpact:
                    sb.Append($"'{label}' has {depCount} dependent(s)");
                    if (highValue.Count > 0)
                        sb.Append($", including high-value components ({TopNames(highValue)})");
                    sb.Append(". A change has a large blast radius; test the affected areas thoroughly. ");
                    break;
                case ChangeSafety.DoNotDelete:
                    sb.Append($"'{label}' is a table with {depCount} dependent(s) and typically holds data. " +
                              "Deleting it is destructive and usually irreversible. ");
                    break;
                case ChangeSafety.RequiresDependencyReview:
                    sb.Append("The platform could not fully enumerate this component's usage, so the footprint is a " +
                              "lower bound. Do not assume it is safe. ");
                    break;
                case ChangeSafety.RequiresAlmReview:
                    if (managed.Count > 0)
                        sb.Append($"{managed.Count} dependent(s) live in a managed layer ({TopNames(managed)}). ");
                    if (crossSolution.Count > 0)
                        sb.Append($"{crossSolution.Count} dependent(s) ship in other solutions ({TopNames(crossSolution)}). ");
                    sb.Append("The change crosses solution/ALM boundaries and needs sign-off before deployment. ");
                    break;
            }

            if (reqCount > 0)
                sb.Append($"It also requires {reqCount} other component(s) that must exist wherever it is deployed. ");

            if (incomplete && verdict != ChangeSafety.RequiresDependencyReview)
                sb.Append("Note: dependency data was incomplete, so verify usage manually. ");

            switch (verdict)
            {
                case ChangeSafety.DoNotDelete:
                    sb.Append("Next: do not delete; if a change is unavoidable, run a full dependency review and back up data first.");
                    break;
                case ChangeSafety.RequiresAlmReview:
                    sb.Append("Next: coordinate with the owning solutions' ALM process and re-deploy them together.");
                    break;
                case ChangeSafety.HighImpact:
                    sb.Append("Next: perform a dependency review and regression-test every dependent before shipping.");
                    break;
                case ChangeSafety.RequiresDependencyReview:
                    sb.Append("Next: manually confirm forms, flows, plugins and apps that may use it.");
                    break;
                case ChangeSafety.ChangeWithCaution:
                    sb.Append("Next: review the listed dependents, then proceed.");
                    break;
                default:
                    sb.Append("Next: proceed; re-check after any future dependency is added.");
                    break;
            }

            return sb.ToString();
        }

        private static string Sample(IEnumerable<ComponentRef> comps)
        {
            var names = comps.Select(c => c.Label).Where(n => !string.IsNullOrWhiteSpace(n)).Take(5).ToList();
            var text = string.Join(", ", names);
            return string.IsNullOrEmpty(text) ? "(unnamed)" : text;
        }

        private static string TopNames(IEnumerable<ComponentRef> comps) => Sample(comps);
    }
}
