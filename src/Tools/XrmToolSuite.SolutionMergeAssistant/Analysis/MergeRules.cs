using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.SolutionMergeAssistant.Analysis
{
    /// <summary>
    /// Pure, deterministic, SDK-free comparison of two or more solutions. Detects overlapping components,
    /// publisher-prefix and version divergence, managed/unmanaged conflicts, and environment-variable /
    /// connection-reference conflicts, then rolls everything into a single <see cref="MergeVerdict"/>,
    /// a recommended merge strategy, and a merged-component checklist. Never touches Dataverse — the
    /// collector does the reads and hands fully-populated <see cref="SolutionInfo"/> lists in.
    /// </summary>
    public static class MergeRules
    {
        public const string Category = "Merge";

        /// <summary>
        /// Compares the selected solutions. <paramref name="configItems"/> carries the environment-variable
        /// and connection-reference items each solution packages (may be null/empty). Deterministic: given
        /// the same inputs, produces the same findings, verdict, strategy, and checklist.
        /// </summary>
        public static MergeReport Compare(
            IReadOnlyList<SolutionInfo> solutions,
            IReadOnlyList<ConfigItem> configItems = null,
            MergeOptions opts = null)
        {
            opts = opts ?? new MergeOptions();
            var report = new MergeReport();
            var findings = report.Findings;

            solutions = (solutions ?? new List<SolutionInfo>())
                .Where(s => s != null)
                .OrderBy(s => s.UniqueName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            configItems = (configItems ?? new List<ConfigItem>())
                .Where(c => c != null)
                .ToList();

            if (solutions.Count < 2)
            {
                findings.Add(new Finding(Category, Severity.Info,
                    "Select at least two solutions",
                    "A merge comparison needs two or more solutions from the same environment. " +
                    $"{solutions.Count} was provided.",
                    recommendation: "Pick two or more solutions to compare."));
                Finalize(report, solutions, opts, sharedCount: 0, standardPrefix: null, importOrder: solutions);
                return report;
            }

            // ---- 1. Duplicate / overlapping components + per-component managed-state conflict ----
            int sharedCount = 0;
            var overlaps = solutions
                .SelectMany(s => s.Components.Select(c => new { Sol = s, Comp = c }))
                .GroupBy(x => new ComponentKey(x.Comp.ComponentType, x.Comp.ObjectId))
                .Where(g => g.Select(x => x.Sol.UniqueName).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .ToList();

            foreach (var group in overlaps.OrderByDescending(g => ComponentTypes.DuplicateSeverity(g.Key.Type))
                                          .ThenBy(g => First(g).Comp.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                sharedCount++;
                var comp = First(group).Comp;
                var owners = group.Select(x => x.Sol)
                                  .GroupBy(s => s.UniqueName, StringComparer.OrdinalIgnoreCase)
                                  .Select(gg => gg.First())
                                  .OrderBy(s => s.UniqueName, StringComparer.OrdinalIgnoreCase)
                                  .ToList();
                var ownerLabels = string.Join(", ", owners.Select(s => s.Label));
                var category = ComponentTypes.Category(comp);
                var typeName = string.IsNullOrEmpty(comp.ComponentTypeName)
                    ? ComponentTypes.Name(comp.ComponentType)
                    : comp.ComponentTypeName;

                findings.Add(new Finding(
                    category, ComponentTypes.DuplicateSeverity(comp.ComponentType),
                    $"Duplicate {typeName}: {comp.DisplayName}",
                    $"{typeName} '{comp.DisplayName}' is packaged in {owners.Count} selected solutions " +
                    $"({ownerLabels}). On merge the last import wins, so the definitions must be reconciled first.",
                    component: comp.DisplayName,
                    recommendation: $"Decide the authoritative owner of this {typeName.ToLowerInvariant()} and " +
                                    "remove it from the other solution(s) before merging."));

                // Managed in one solution, unmanaged in another → layering conflict (High).
                var managedStates = group.Select(x => x.Comp.IsManaged).Distinct().ToList();
                if (managedStates.Count > 1)
                {
                    var managed = string.Join(", ", group.Where(x => x.Comp.IsManaged)
                        .Select(x => x.Sol.Label).Distinct());
                    var unmanaged = string.Join(", ", group.Where(x => !x.Comp.IsManaged)
                        .Select(x => x.Sol.Label).Distinct());
                    findings.Add(new Finding(
                        "Managed State", Severity.High,
                        $"Managed/unmanaged conflict: {comp.DisplayName}",
                        $"{typeName} '{comp.DisplayName}' is managed in {managed} but unmanaged in {unmanaged}. " +
                        "After merge the unmanaged layer sits on top and wins, which can silently override the " +
                        "managed definition and cannot be cleanly uninstalled.",
                        component: comp.DisplayName,
                        recommendation: "Standardize the managed state: ship the component from a single managed " +
                                        "solution, or keep it unmanaged in one place only."));
                }
            }

            // ---- 2. Publisher-prefix mismatch on shared components ----
            var prefixes = solutions
                .Select(s => s.PublisherPrefix)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            string standardPrefix = StandardPrefix(solutions);
            if (prefixes.Count > 1)
            {
                findings.Add(new Finding(
                    "Publisher", Severity.Medium,
                    "Publisher prefix mismatch",
                    $"The selected solutions use {prefixes.Count} different publisher prefixes " +
                    $"({string.Join(", ", prefixes.Select(p => "'" + p + "'"))}). Merging components from different " +
                    "publishers mixes prefixes and can cause naming/ownership collisions.",
                    component: string.Join("/", prefixes),
                    recommendation: $"Standardize on a single publisher (prefix '{standardPrefix}') and re-own " +
                                    "components created under the other prefixes."));
            }

            // ---- 3. Version divergence ----
            var versions = solutions
                .Select(s => s.Version)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (versions.Count > 1)
            {
                // A version split matters more when the solutions actually overlap (real layering order).
                var sev = sharedCount > 0 ? Severity.Medium : Severity.Low;
                findings.Add(new Finding(
                    "Version", sev,
                    "Solution version differences",
                    "The selected solutions are at different versions (" +
                    string.Join(", ", solutions.Select(s => $"{s.UniqueName} v{s.Version}")) +
                    "). Import order determines which version's shared components land last.",
                    component: string.Join(", ", versions),
                    recommendation: "Import in ascending version order so the newest definitions win, and bump the " +
                                    "target solution version after merge."));
            }

            // ---- 4. Config conflicts (environment variables / connection references) ----
            foreach (var group in configItems
                         .GroupBy(c => new ConfigKey(c.Kind, c.SchemaName))
                         .OrderBy(g => g.Key.Kind, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(g => g.Key.SchemaName, StringComparer.OrdinalIgnoreCase))
            {
                var owners = group.Select(c => c.OwningSolution)
                                  .Where(o => !string.IsNullOrEmpty(o))
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .ToList();
                if (owners.Count < 2) continue; // packaged in a single solution — merges cleanly

                var kindName = string.Equals(group.Key.Kind, "ConnRef", StringComparison.OrdinalIgnoreCase)
                    ? "Connection reference" : "Environment variable";
                var kindCategory = string.Equals(group.Key.Kind, "ConnRef", StringComparison.OrdinalIgnoreCase)
                    ? "Connection References" : "Environment Variables";

                var distinctValues = group
                    .Select(c => c.DefinitionOrValue ?? string.Empty)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (distinctValues.Count > 1)
                {
                    findings.Add(new Finding(
                        kindCategory, Severity.High,
                        $"{kindName} conflict: {group.Key.SchemaName}",
                        $"'{group.Key.SchemaName}' is packaged in {owners.Count} solutions with different " +
                        $"definitions/values ({string.Join(" | ", distinctValues.Select(Trim))}). After merge only " +
                        "one binding survives, so automation may bind to the wrong value.",
                        component: group.Key.SchemaName,
                        recommendation: $"Agree the correct {kindName.ToLowerInvariant()} value and ship it from one " +
                                        "solution only."));
                }
                else
                {
                    findings.Add(new Finding(
                        kindCategory, Severity.Medium,
                        $"Duplicate {kindName.ToLowerInvariant()}: {group.Key.SchemaName}",
                        $"'{group.Key.SchemaName}' is packaged identically in {owners.Count} solutions " +
                        $"({string.Join(", ", owners)}). The duplicate inclusion is harmless but should be owned once.",
                        component: group.Key.SchemaName,
                        recommendation: $"Keep this {kindName.ToLowerInvariant()} in a single solution to avoid " +
                                        "duplicate ownership."));
                }
            }

            // ---- Roll up: strategy, checklist, verdict ----
            var importOrder = ImportOrder(solutions);
            Finalize(report, solutions, opts, sharedCount, standardPrefix, importOrder);
            return report;
        }

        // ---------------------------------------------------------------------------------------

        private static void Finalize(
            MergeReport report,
            IReadOnlyList<SolutionInfo> solutions,
            MergeOptions opts,
            int sharedCount,
            string standardPrefix,
            IReadOnlyList<SolutionInfo> importOrder)
        {
            var findings = report.Findings;

            // Shared score/band via the suite's default risk weighting.
            var calc = ScoreCalculator.RiskDefault;
            report.Score = calc.Score(findings);
            report.Band = calc.Band(findings, report.Score);
            report.Verdict = VerdictFor(findings, opts);

            // Metrics.
            int Count(Severity s) => findings.Count(f => f.Severity == s);
            report.Metrics.Add(new MetricRow("Solutions compared", solutions.Count.ToString()));
            report.Metrics.Add(new MetricRow("Total components",
                solutions.Sum(s => s.Components?.Count ?? 0).ToString()));
            report.Metrics.Add(new MetricRow("Shared components", sharedCount.ToString()));
            report.Metrics.Add(new MetricRow("Conflicts (High)", Count(Severity.High).ToString()));
            report.Metrics.Add(new MetricRow("Conflicts (Medium)", Count(Severity.Medium).ToString()));
            report.Metrics.Add(new MetricRow("Conflicts (Low)", Count(Severity.Low).ToString()));
            report.Metrics.Add(new MetricRow("Verdict", report.VerdictText));

            // Recommended strategy.
            var strategy = new List<string>();
            if (solutions.Count >= 2)
            {
                strategy.Add("Import order: " + string.Join(" → ",
                    importOrder.Select(s => $"{s.UniqueName} v{s.Version}")) +
                    " (ascending version so the newest shared definitions land last).");
            }
            if (!string.IsNullOrEmpty(standardPrefix))
                strategy.Add($"Standardize on publisher prefix '{standardPrefix}' (owns the most components).");
            if (findings.Any(f => f.Category == "Managed State"))
                strategy.Add("Resolve managed/unmanaged conflicts first — ship each shared component from a single " +
                             "managed solution so the layering is deterministic.");
            if (findings.Any(f => f.Category == "Environment Variables" || f.Category == "Connection References"))
                strategy.Add("Reconcile environment-variable and connection-reference values, then package each " +
                             "binding in exactly one solution.");
            if (sharedCount > 0)
                strategy.Add($"Reconcile the {sharedCount} overlapping component(s) below before importing.");
            strategy.Add("Merge is read-only here — this tool recommends; it does not import or write solutions.");
            report.RecommendedStrategy = string.Join(Environment.NewLine, strategy.Select(s => "• " + s));

            // Merged-component checklist (traceable, ordered by severity).
            var checklist = report.Checklist;
            foreach (var group in findings
                         .Where(f => f.Severity >= Severity.Low && !string.IsNullOrEmpty(f.Recommendation))
                         .GroupBy(f => f.Category)
                         .OrderByDescending(g => g.Max(f => f.Severity))
                         .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                checklist.Add($"[{group.Key}] {group.Count()} item(s) to reconcile");
                foreach (var f in group.OrderByDescending(f => f.Severity))
                    checklist.Add($"    - {f.Component}: {f.Recommendation}");
            }
            if (checklist.Count == 0)
                checklist.Add("No conflicts — components can be merged as-is. Verify in a sandbox before production.");
        }

        /// <summary>Maps the finding mix to a single verdict, safest → riskiest.</summary>
        private static MergeVerdict VerdictFor(IReadOnlyList<Finding> findings, MergeOptions opts)
        {
            int crit = findings.Count(f => f.Severity == Severity.Critical);
            int high = findings.Count(f => f.Severity == Severity.High);
            int med = findings.Count(f => f.Severity == Severity.Medium);
            int low = findings.Count(f => f.Severity == Severity.Low);

            if (crit > 0 || high >= Math.Max(1, opts.DoNotMergeHighCount)) return MergeVerdict.DoNotMerge;
            if (high > 0) return MergeVerdict.HighRisk;
            if (med > 0) return MergeVerdict.ManualReview;
            if (low > 0) return MergeVerdict.MergeWithWarnings;
            return MergeVerdict.SafeToMerge;
        }

        /// <summary>The publisher prefix owning the most components across the selected solutions.</summary>
        private static string StandardPrefix(IReadOnlyList<SolutionInfo> solutions)
        {
            return solutions
                .Where(s => !string.IsNullOrWhiteSpace(s.PublisherPrefix))
                .GroupBy(s => s.PublisherPrefix, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Sum(s => s.Components?.Count ?? 0))
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Key)
                .FirstOrDefault();
        }

        /// <summary>Ascending version order (unparseable versions sort last, then by name) for import.</summary>
        private static List<SolutionInfo> ImportOrder(IReadOnlyList<SolutionInfo> solutions)
        {
            return solutions
                .OrderBy(s => ParseVersion(s.Version))
                .ThenBy(s => s.UniqueName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static Version ParseVersion(string v)
        {
            return System.Version.TryParse(v, out var parsed) ? parsed : new Version(0, 0);
        }

        private static string Trim(string s)
        {
            s = s ?? "(empty)";
            return s.Length <= 60 ? s : s.Substring(0, 57) + "...";
        }

        private static T First<T>(IEnumerable<T> items) => items.First();

        // ---- keys -----------------------------------------------------------------------------

        private struct ComponentKey : IEquatable<ComponentKey>
        {
            public readonly int Type;
            public readonly Guid ObjectId;
            public ComponentKey(int type, Guid objectId) { Type = type; ObjectId = objectId; }
            public bool Equals(ComponentKey other) => Type == other.Type && ObjectId == other.ObjectId;
            public override bool Equals(object obj) => obj is ComponentKey k && Equals(k);
            public override int GetHashCode() => unchecked((Type * 397) ^ ObjectId.GetHashCode());
        }

        private struct ConfigKey : IEquatable<ConfigKey>
        {
            public readonly string Kind;
            public readonly string SchemaName;
            public ConfigKey(string kind, string schemaName)
            {
                Kind = kind ?? string.Empty;
                SchemaName = schemaName ?? string.Empty;
            }
            public bool Equals(ConfigKey other) =>
                string.Equals(Kind, other.Kind, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(SchemaName, other.SchemaName, StringComparison.OrdinalIgnoreCase);
            public override bool Equals(object obj) => obj is ConfigKey k && Equals(k);
            public override int GetHashCode() =>
                unchecked((StringComparer.OrdinalIgnoreCase.GetHashCode(Kind) * 397) ^
                          StringComparer.OrdinalIgnoreCase.GetHashCode(SchemaName));
        }
    }
}
