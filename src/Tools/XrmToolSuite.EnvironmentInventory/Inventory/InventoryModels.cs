using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.EnvironmentInventory.Inventory
{
    /// <summary>
    /// A single normalized inventory row unifying every disparate Dataverse component type (tables,
    /// solutions, roles, plugins, workflows, web resources, environment variables, …) into one shape.
    /// Deliberately SDK-free and UI-free so it is unit-testable and the exporters can consume it without
    /// a Dataverse or WinForms dependency. NEVER carries a secret/credential value in <see cref="Details"/>.
    /// </summary>
    public sealed class InventoryItem
    {
        /// <summary>High-level bucket: Solutions, Tables, Security, Automation, Web/Dev, Configuration.</summary>
        public string Category { get; set; }

        /// <summary>Specific component type within the category (e.g. "Security Role", "Plugin Step").</summary>
        public string ComponentType { get; set; }

        public string Name { get; set; }

        public string SchemaName { get; set; }

        /// <summary>Owner / publisher / owning business unit, where applicable.</summary>
        public string Owner { get; set; }

        public bool? IsManaged { get; set; }

        public DateTime? ModifiedOn { get; set; }

        /// <summary>Source-specific extra attributes for the detail panel. No secrets, ever.</summary>
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// The full inventory captured for one environment: a flat list of normalized <see cref="InventoryItem"/>
    /// rows plus the list of sources that could not be read (permission gaps degrade to a name here rather
    /// than aborting the collection). Pure helpers here power the grid's client-side search/filter and the
    /// summary counts. SDK-free / UI-free.
    /// </summary>
    public sealed class InventorySnapshot
    {
        public string EnvironmentName { get; set; }

        public DateTime CollectedOnUtc { get; set; } = DateTime.UtcNow;

        public List<InventoryItem> Items { get; set; } = new List<InventoryItem>();

        /// <summary>Names of sources that failed to collect (e.g. "Security roles") — shown, never a hard error.</summary>
        public List<string> UnavailableSources { get; set; } = new List<string>();

        public int Total => Items?.Count ?? 0;

        /// <summary>Distinct categories present, in stable display order.</summary>
        public IEnumerable<string> Categories()
        {
            return (Items ?? Enumerable.Empty<InventoryItem>())
                .Select(i => i.Category ?? "")
                .Where(c => c.Length > 0)
                .Distinct()
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Count of items per category (for the summary metrics and export summary table).</summary>
        public Dictionary<string, int> CountByCategory()
        {
            return (Items ?? Enumerable.Empty<InventoryItem>())
                .GroupBy(i => i.Category ?? "(uncategorized)")
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Case-insensitive client-side filter used by the grid: <paramref name="text"/> matches a substring
        /// of Name/SchemaName/ComponentType; <paramref name="category"/> (when non-empty) pins the category;
        /// <paramref name="managed"/> (when non-null) pins the managed state.
        /// </summary>
        public IEnumerable<InventoryItem> Filter(string text, string category, bool? managed)
        {
            IEnumerable<InventoryItem> rows = Items ?? Enumerable.Empty<InventoryItem>();

            if (!string.IsNullOrWhiteSpace(category))
                rows = rows.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));

            if (managed.HasValue)
                rows = rows.Where(i => i.IsManaged == managed.Value);

            if (!string.IsNullOrWhiteSpace(text))
            {
                var t = text.Trim();
                rows = rows.Where(i =>
                    Contains(i.Name, t) ||
                    Contains(i.SchemaName, t) ||
                    Contains(i.ComponentType, t));
            }

            return rows;
        }

        private static bool Contains(string value, string term) =>
            !string.IsNullOrEmpty(value) &&
            value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
