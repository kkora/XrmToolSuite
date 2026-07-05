using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.EnvironmentComparisonSuite.Analysis
{
    /// <summary>
    /// The suite's UI-free, SDK-free comparison engine. Given normalized <see cref="ComponentSnapshot"/>
    /// lists from a source and a target environment it classifies each component
    /// Missing / Extra / Changed / ManagedVsUnmanaged / Identical, assigns a severity per category+class,
    /// and rolls the diffs into a weighted difference score, band, findings, metrics, and a count matrix.
    /// Pure and deterministic (same inputs → identical output), so it is fully unit-testable and is meant
    /// to be shared with the ADMIN8 Configuration Drift Monitor rather than duplicated.
    /// </summary>
    public static class SnapshotComparer
    {
        // Categories whose absence/structure carries the highest release risk. A Missing here is High
        // (a release must add it); a Missing UI/soft component is Medium.
        private static readonly HashSet<string> StructuralCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ComparisonCategories.Solutions, ComparisonCategories.Tables, ComparisonCategories.Columns,
            ComparisonCategories.Relationships, ComparisonCategories.Keys, ComparisonCategories.Roles,
            ComparisonCategories.PluginAssemblies, ComparisonCategories.PluginSteps, ComparisonCategories.PluginImages,
            ComparisonCategories.Workflows, ComparisonCategories.CustomApis, ComparisonCategories.EnvironmentVariables,
            ComparisonCategories.ConnectionReferences
        };

        /// <summary>
        /// Compares one category's source and target snapshots, matching by <see cref="ComponentSnapshot.Key"/>
        /// (case-insensitive). Deterministic: the returned list is ordered by name then key. Secret-typed
        /// values are masked in every <see cref="ComponentDiff.ChangedProperties"/> entry.
        /// </summary>
        public static List<ComponentDiff> Compare(
            string category,
            IEnumerable<ComponentSnapshot> source,
            IEnumerable<ComponentSnapshot> target,
            CompareOptions opts = null)
        {
            opts = opts ?? CompareOptions.Default;

            // First snapshot wins on a duplicate key (defensive; keys are expected unique per category).
            var src = ToMap(source);
            var tgt = ToMap(target);

            var diffs = new List<ComponentDiff>();

            foreach (var s in src.Values)
            {
                if (!tgt.TryGetValue(s.Key, out var t))
                {
                    diffs.Add(Classify(category, s, null, DiffClass.Missing, new List<ChangedProperty>(), opts));
                    continue;
                }

                var changed = ChangedProperties(s, t, opts);

                if (s.IsManaged != t.IsManaged)
                {
                    // Managed/unmanaged layering drift takes precedence over ordinary property changes,
                    // but any other differing properties are still surfaced in the detail list.
                    diffs.Add(Classify(category, s, t, DiffClass.ManagedVsUnmanaged, changed, opts));
                }
                else if (changed.Count > 0)
                {
                    diffs.Add(Classify(category, s, t, DiffClass.Changed, changed, opts));
                }
                else
                {
                    diffs.Add(Classify(category, s, t, DiffClass.Identical, changed, opts));
                }
            }

            foreach (var t in tgt.Values)
            {
                if (!src.ContainsKey(t.Key))
                    diffs.Add(Classify(category, null, t, DiffClass.Extra, new List<ChangedProperty>(), opts));
            }

            return diffs
                .OrderBy(d => d.Name ?? d.Key, StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Rolls a set of diffs into a <see cref="ComparisonReport"/>: a weighted difference score/band
        /// (reusing the suite's <see cref="ScoreCalculator.RiskDefault"/> weighting), one finding per
        /// non-identical diff, headline metrics, and a per-category × class count matrix. Deterministic.
        /// </summary>
        public static ComparisonReport Roll(IEnumerable<ComponentDiff> diffs, CompareOptions opts = null)
        {
            var report = new ComparisonReport();
            report.Diffs.AddRange((diffs ?? Enumerable.Empty<ComponentDiff>())
                .OrderBy(d => d.Category, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(d => d.Severity)
                .ThenBy(d => d.Name ?? d.Key, StringComparer.OrdinalIgnoreCase));

            // Count matrix (every category that appears gets a full row so summary cards line up).
            foreach (var d in report.Diffs)
            {
                if (!report.CountsByCategoryAndClass.TryGetValue(d.Category, out var row))
                {
                    row = NewClassRow();
                    report.CountsByCategoryAndClass[d.Category] = row;
                }
                row[d.Class]++;
            }

            // Findings: everything that is an actual difference (Identical contributes nothing).
            foreach (var d in report.Diffs.Where(x => x.Class != DiffClass.Identical))
                report.Findings.Add(ToFinding(d));

            report.Score = ScoreCalculator.RiskDefault.Score(report.Findings);
            report.Band = ScoreCalculator.RiskDefault.Band(report.Findings, report.Score);

            int Count(DiffClass c) => report.Diffs.Count(d => d.Class == c);
            report.Metrics.Add(new MetricRow("Differences",
                (report.Diffs.Count - Count(DiffClass.Identical)).ToString(), "components that differ"));
            report.Metrics.Add(new MetricRow("Missing in target", Count(DiffClass.Missing).ToString(), "present in source only"));
            report.Metrics.Add(new MetricRow("Extra in target", Count(DiffClass.Extra).ToString(), "present in target only"));
            report.Metrics.Add(new MetricRow("Changed", Count(DiffClass.Changed).ToString(), "property differences"));
            report.Metrics.Add(new MetricRow("Managed vs unmanaged", Count(DiffClass.ManagedVsUnmanaged).ToString(), "layering drift"));
            report.Metrics.Add(new MetricRow("Identical", Count(DiffClass.Identical).ToString(), "in sync"));

            return report;
        }

        // ---------------------------------------------------------------- internals

        private static Dictionary<string, ComponentSnapshot> ToMap(IEnumerable<ComponentSnapshot> items)
        {
            var map = new Dictionary<string, ComponentSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in items ?? Enumerable.Empty<ComponentSnapshot>())
            {
                if (s?.Key == null) continue;
                if (!map.ContainsKey(s.Key)) map[s.Key] = s;
            }
            return map;
        }

        /// <summary>Builds the ordinal property delta between two snapshots, masking any secret-typed value.</summary>
        private static List<ChangedProperty> ChangedProperties(ComponentSnapshot s, ComponentSnapshot t, CompareOptions opts)
        {
            var changed = new List<ChangedProperty>();

            if (!string.Equals(s.Version, t.Version, StringComparison.Ordinal) &&
                !(string.IsNullOrEmpty(s.Version) && string.IsNullOrEmpty(t.Version)))
                changed.Add(new ChangedProperty("version", s.Version ?? "", t.Version ?? ""));

            var keys = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in s.Properties.Keys) keys.Add(k);
            foreach (var k in t.Properties.Keys) keys.Add(k);

            foreach (var key in keys)
            {
                s.Properties.TryGetValue(key, out var sv);
                t.Properties.TryGetValue(key, out var tv);
                if (string.Equals(sv, tv, StringComparison.Ordinal)) continue;

                if (IsSecret(key, s, t, opts))
                    changed.Add(new ChangedProperty(key, CompareOptions.Mask, CompareOptions.Mask));
                else
                    changed.Add(new ChangedProperty(key, sv ?? "", tv ?? ""));
            }

            return changed;
        }

        private static bool IsSecret(string key, ComponentSnapshot s, ComponentSnapshot t, CompareOptions opts) =>
            (s != null && s.SecretKeys.Contains(key)) ||
            (t != null && t.SecretKeys.Contains(key)) ||
            opts.SecretProperties.Contains(key);

        private static ComponentDiff Classify(
            string category, ComponentSnapshot s, ComponentSnapshot t,
            DiffClass cls, List<ChangedProperty> changed, CompareOptions opts)
        {
            var present = s ?? t;
            var diff = new ComponentDiff
            {
                Category = category,
                Key = present?.Key,
                Name = present?.Name ?? present?.Key,
                Class = cls,
                SourceManaged = s?.IsManaged ?? false,
                TargetManaged = t?.IsManaged ?? false,
                Severity = SeverityFor(category, cls, opts)
            };
            diff.ChangedProperties.AddRange(changed);
            return diff;
        }

        /// <summary>Severity per category + class. Callers may override via <see cref="CompareOptions.SeverityResolver"/>.</summary>
        public static Severity SeverityFor(string category, DiffClass cls, CompareOptions opts = null)
        {
            var over = opts?.SeverityResolver?.Invoke(category, cls);
            if (over.HasValue) return over.Value;

            bool structural = category != null && StructuralCategories.Contains(category);
            switch (cls)
            {
                case DiffClass.Identical: return Severity.Info;
                case DiffClass.ManagedVsUnmanaged: return Severity.High; // import-blocking layering drift
                case DiffClass.Missing: return structural ? Severity.High : Severity.Medium;
                case DiffClass.Extra: return structural ? Severity.Medium : Severity.Low;
                case DiffClass.Changed: return Severity.Medium;
                default: return Severity.Info;
            }
        }

        private static Finding ToFinding(ComponentDiff d)
        {
            string title;
            string recommendation;
            switch (d.Class)
            {
                case DiffClass.Missing:
                    title = "Missing in target";
                    recommendation = "Add this component to the target (include it in the release solution).";
                    break;
                case DiffClass.Extra:
                    title = "Extra in target";
                    recommendation = "Confirm this component is expected in the target; it is not present in the source.";
                    break;
                case DiffClass.ManagedVsUnmanaged:
                    title = "Managed/unmanaged mismatch";
                    recommendation = "Align the managed state across environments — never flip layering for an installed component.";
                    break;
                default: // Changed
                    title = "Changed";
                    recommendation = "Review the property differences and reconcile source and target.";
                    break;
            }

            var desc = DescribeChange(d);
            return new Finding(d.Category, d.Severity, $"{title}: {d.Name}", desc, d.Name, recommendation);
        }

        private static string DescribeChange(ComponentDiff d)
        {
            switch (d.Class)
            {
                case DiffClass.Missing:
                    return $"'{d.Name}' exists in the source environment but not the target.";
                case DiffClass.Extra:
                    return $"'{d.Name}' exists in the target environment but not the source.";
                case DiffClass.ManagedVsUnmanaged:
                    return $"'{d.Name}' is {(d.SourceManaged ? "managed" : "unmanaged")} in source but " +
                           $"{(d.TargetManaged ? "managed" : "unmanaged")} in target." +
                           (d.ChangedProperties.Count > 0 ? " " + JoinChanges(d) : "");
                default:
                    return $"'{d.Name}' differs: {JoinChanges(d)}";
            }
        }

        private static string JoinChanges(ComponentDiff d) =>
            string.Join("; ", d.ChangedProperties.Select(c => $"{c.Prop} '{c.Source}' → '{c.Target}'"));

        private static Dictionary<DiffClass, int> NewClassRow() =>
            new Dictionary<DiffClass, int>
            {
                { DiffClass.Missing, 0 }, { DiffClass.Extra, 0 }, { DiffClass.Changed, 0 },
                { DiffClass.ManagedVsUnmanaged, 0 }, { DiffClass.Identical, 0 }
            };
    }

    /// <summary>Canonical category names shared by the collector, the comparer, and the UI selector.</summary>
    public static class ComparisonCategories
    {
        public const string Solutions = "Solutions";
        public const string Publishers = "Publishers";
        public const string Tables = "Tables";
        public const string Columns = "Columns";
        public const string Relationships = "Relationships";
        public const string Keys = "Alternate Keys";
        public const string Forms = "Forms";
        public const string Views = "Views";
        public const string Charts = "Charts";
        public const string Dashboards = "Dashboards";
        public const string Roles = "Security Roles";
        public const string Teams = "Teams";
        public const string BusinessUnits = "Business Units";
        public const string PluginAssemblies = "Plugin Assemblies";
        public const string PluginSteps = "Plugin Steps";
        public const string PluginImages = "Plugin Images";
        public const string Workflows = "Processes";
        public const string CustomApis = "Custom APIs";
        public const string EnvironmentVariables = "Environment Variables";
        public const string ConnectionReferences = "Connection References";
        public const string WebResources = "Web Resources";

        /// <summary>Every category, in the order the UI selector presents them.</summary>
        public static readonly string[] All =
        {
            Solutions, Publishers, Tables, Columns, Relationships, Keys,
            Forms, Views, Charts, Dashboards,
            Roles, Teams, BusinessUnits,
            PluginAssemblies, PluginSteps, PluginImages, Workflows, CustomApis,
            EnvironmentVariables, ConnectionReferences, WebResources
        };
    }
}
