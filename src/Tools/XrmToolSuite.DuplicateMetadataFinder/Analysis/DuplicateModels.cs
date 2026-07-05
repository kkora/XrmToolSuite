using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.DuplicateMetadataFinder.Analysis
{
    /// <summary>
    /// The component categories the finder groups. Duplicates are only ever compared within the same
    /// kind (a form is never "similar" to a column), which also blocks the O(n²) comparison per kind.
    /// </summary>
    public enum ComponentKind
    {
        Column,
        OptionSet,
        Table,
        Form,
        View,
        Chart,
        Dashboard,
        BusinessRule,
        WebResource,
        PluginStep,
        Relationship,
    }

    /// <summary>
    /// What to scan and how. Plain serializable POCO (also persisted in tool settings). SDK-free so the
    /// defaults and toggles are unit-testable. Default: the reliable metadata kinds on, threshold 80.
    /// </summary>
    public sealed class DuplicateScanOptions
    {
        public bool Columns { get; set; } = true;
        public bool OptionSets { get; set; } = true;
        public bool Tables { get; set; } = true;
        public bool Forms { get; set; } = true;
        public bool Views { get; set; } = true;
        public bool BusinessRules { get; set; } = true;
        public bool WebResources { get; set; } = true;
        public bool PluginSteps { get; set; } = true;
        public bool Relationships { get; set; } = true;

        /// <summary>Skip managed/system components so only the customer's own (removable) metadata is compared.</summary>
        public bool CustomOnly { get; set; }

        /// <summary>Similarity cut-off 0..100; only pairs at/above this group.</summary>
        public int Threshold { get; set; } = 80;

        public static DuplicateScanOptions All() => new DuplicateScanOptions();
    }

    /// <summary>
    /// A single metadata component reduced to the signals the similarity engine needs. Deliberately
    /// SDK-free: the collector translates Dataverse metadata into these POCOs, so the whole scoring /
    /// grouping / recommendation model is unit-testable without a connection.
    /// </summary>
    public sealed class MetadataComponent
    {
        public ComponentKind Kind { get; set; }

        /// <summary>Stable identity within its kind (e.g. <c>account.telephone1</c> or a component id).</summary>
        public string Key { get; set; }

        public string DisplayName { get; set; }

        /// <summary>Schema / logical name (columns, tables, relationships). May be null for UI assets.</summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Owning table (columns, forms, views, charts). Shown in comparisons; the engine still compares
        /// across containers because the same field created by two teams on two tables is the point.
        /// </summary>
        public string Container { get; set; }

        /// <summary>Attribute/option-set data type, or the plugin-step message+stage signature.</summary>
        public string DataType { get; set; }

        public string Description { get; set; }

        /// <summary>Option labels for option sets / picklist columns (order-independent).</summary>
        public IReadOnlyList<string> OptionValues { get; set; }

        /// <summary>Content hash for web resources / JavaScript — an exact-match strong signal.</summary>
        public string ContentHash { get; set; }

        /// <summary>Reference / dependency weight; the most-referenced member of a group is the recommended keep.</summary>
        public int UsageCount { get; set; }

        public bool IsManaged { get; set; }
        public bool IsCustom { get; set; }

        public override string ToString() => $"{Kind}:{Key}";
    }

    /// <summary>One contributing factor to a pair's similarity, kept for the side-by-side "why" display.</summary>
    public sealed class SimilarityFactor
    {
        public string Name { get; }
        /// <summary>Raw factor strength, 0..1.</summary>
        public double Value { get; }
        /// <summary>Weight applied to this factor for the pair's kind, 0..1.</summary>
        public double Weight { get; }

        public SimilarityFactor(string name, double value, double weight)
        {
            Name = name;
            Value = value;
            Weight = weight;
        }

        public override string ToString() =>
            $"{Name} {(int)Math.Round(Value * 100)}% (w{Weight:0.##})";
    }

    /// <summary>A scored pair of same-kind components.</summary>
    public sealed class DuplicatePair
    {
        public MetadataComponent A { get; }
        public MetadataComponent B { get; }
        /// <summary>0..100 similarity score.</summary>
        public int Score { get; }
        public IReadOnlyList<SimilarityFactor> Factors { get; }
        /// <summary>True when a definitive signal (identical content hash) drove the score — no false-positive disclaimer needed.</summary>
        public bool IsExactContentMatch { get; }

        public DuplicatePair(MetadataComponent a, MetadataComponent b, int score,
            IReadOnlyList<SimilarityFactor> factors, bool isExactContentMatch)
        {
            A = a;
            B = b;
            Score = score;
            Factors = factors ?? new List<SimilarityFactor>();
            IsExactContentMatch = isExactContentMatch;
        }
    }

    /// <summary>A cluster of mutually/transitively similar components plus a recommended primary to keep.</summary>
    public sealed class DuplicateGroup
    {
        public ComponentKind Kind { get; set; }
        public List<MetadataComponent> Members { get; } = new List<MetadataComponent>();
        public List<DuplicatePair> Pairs { get; } = new List<DuplicatePair>();

        /// <summary>Highest pair score in the group — used to rank groups worst-first.</summary>
        public int TopScore => Pairs.Count == 0 ? 0 : Pairs.Max(p => p.Score);

        /// <summary>
        /// The member recommended to keep: most-referenced wins; ties broken toward managed (harder to
        /// remove) then the lexicographically smaller key so the choice is deterministic. Read-only — the
        /// tool recommends, it never merges or deletes.
        /// </summary>
        public MetadataComponent RecommendedPrimary =>
            Members
                .OrderByDescending(m => m.UsageCount)
                .ThenByDescending(m => m.IsManaged)
                .ThenBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

        public string RecommendationReason()
        {
            var primary = RecommendedPrimary;
            if (primary == null) return "No members.";
            var others = Members.Where(m => !ReferenceEquals(m, primary)).ToList();
            if (primary.UsageCount > 0 && others.All(m => m.UsageCount < primary.UsageCount))
                return $"Keep '{primary.Key}' — most referenced ({primary.UsageCount} usages).";
            if (others.All(m => m.UsageCount == primary.UsageCount))
                return $"Keep '{primary.Key}' — usage is tied; picked deterministically" +
                       (primary.IsManaged ? " (managed, harder to remove)." : ".");
            return $"Keep '{primary.Key}' — highest reference weight.";
        }
    }

    /// <summary>The full finder run: groups (worst-first) plus roll-up counts and any degraded-scan notes.</summary>
    public sealed class DuplicateScanResult
    {
        public string EnvironmentName { get; set; }
        public DateTime ScannedOnUtc { get; set; } = DateTime.UtcNow;
        public int Threshold { get; set; }
        public List<DuplicateGroup> Groups { get; } = new List<DuplicateGroup>();

        /// <summary>Non-fatal issues (a query that failed and was skipped) — surfaced as info, never thrown.</summary>
        public List<string> Notes { get; } = new List<string>();

        public int GroupCount => Groups.Count;
        public int DuplicateComponentCount => Groups.Sum(g => g.Members.Count);

        public IEnumerable<DuplicateGroup> GroupsByKind(ComponentKind kind) =>
            Groups.Where(g => g.Kind == kind);

        /// <summary>Groups ordered worst-first for display: highest top score, then largest cluster.</summary>
        public IEnumerable<DuplicateGroup> Ranked() =>
            Groups.OrderByDescending(g => g.TopScore).ThenByDescending(g => g.Members.Count);
    }
}
