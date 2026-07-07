using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// How many columns an emitter renders per table.
    /// </summary>
    public enum ColumnDisplay
    {
        /// <summary>Only primary id, primary name, lookups and alternate-key member columns.</summary>
        KeysAndLookupsOnly,
        /// <summary>Keys/lookups plus required (business/system required) columns.</summary>
        Important,
        /// <summary>Every column on the table.</summary>
        All
    }

    /// <summary>A single Dataverse column projected into the ERD model. SDK-free (no Microsoft.Xrm.Sdk).</summary>
    public sealed class ErdColumn
    {
        public string LogicalName { get; set; }
        public string Type { get; set; }
        public string RequiredLevel { get; set; }
        public bool IsPrimaryId { get; set; }
        public bool IsPrimaryName { get; set; }
        public bool IsLookup { get; set; }
        public List<string> Targets { get; set; } = new List<string>();
    }

    /// <summary>An alternate (entity) key — a uniqueness constraint over one or more columns.</summary>
    public sealed class ErdKey
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
    }

    /// <summary>A table (entity) with its columns and alternate keys.</summary>
    public sealed class ErdTable
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string SchemaName { get; set; }
        public bool IsCustom { get; set; }
        public bool IsManaged { get; set; }
        public string PrimaryIdColumn { get; set; }
        public string PrimaryNameColumn { get; set; }
        public List<ErdColumn> Columns { get; set; } = new List<ErdColumn>();
        public List<ErdKey> AlternateKeys { get; set; } = new List<ErdKey>();
    }

    /// <summary>A relationship (edge) between two tables.</summary>
    public sealed class ErdRelationship
    {
        public string SchemaName { get; set; }
        /// <summary>OneToMany, ManyToOne or ManyToMany.</summary>
        public string RelationType { get; set; }
        public string FromTable { get; set; }
        public string ToTable { get; set; }
        public string LookupColumn { get; set; }
        public string CascadeSummary { get; set; }
        public string RequiredLevel { get; set; }
    }

    /// <summary>
    /// The whole ERD: tables and relationships. Pure data, fully deterministic and unit-testable;
    /// the SDK collector populates it and the emitters/exporters consume it.
    /// </summary>
    public sealed class ErdModel
    {
        public List<ErdTable> Tables { get; set; } = new List<ErdTable>();
        public List<ErdRelationship> Relationships { get; set; } = new List<ErdRelationship>();

        /// <summary>Diagnostic notes (missing metadata degraded rather than thrown). Optional.</summary>
        public List<string> Notes { get; set; } = new List<string>();

        /// <summary>
        /// Returns a trimmed copy of this model per the filter, WITHOUT re-querying:
        /// drops tables failing the custom-only / managed-only toggles, then drops relationships
        /// whose type is not allowed or whose endpoints no longer both survive.
        /// </summary>
        public ErdModel Apply(ErdFilter filter)
        {
            if (filter == null) return this;

            var tables = Tables.Where(t =>
                    (!filter.CustomOnly || t.IsCustom) &&
                    (!filter.ManagedOnly || t.IsManaged))
                .ToList();

            var kept = new HashSet<string>(tables.Select(t => t.LogicalName), StringComparer.OrdinalIgnoreCase);
            bool typeFilter = filter.RelationshipTypes != null && filter.RelationshipTypes.Count > 0;

            var rels = Relationships.Where(r =>
                    (!typeFilter || filter.RelationshipTypes.Contains(r.RelationType)) &&
                    kept.Contains(r.FromTable) && kept.Contains(r.ToTable))
                .ToList();

            return new ErdModel { Tables = tables, Relationships = rels, Notes = new List<string>(Notes) };
        }

        /// <summary>
        /// Selects which of a table's columns should render for the given display setting.
        /// Keys+lookups: primary id/name, lookups, alternate-key members. Important: adds required columns.
        /// All: everything. Order is stable (primary id, primary name, then declaration order).
        /// </summary>
        public static IReadOnlyList<ErdColumn> SelectColumns(ErdTable table, ColumnDisplay display)
        {
            if (table == null) return new List<ErdColumn>();
            if (display == ColumnDisplay.All) return Order(table, table.Columns);

            var keyMembers = new HashSet<string>(
                table.AlternateKeys.SelectMany(k => k.Columns ?? new List<string>()),
                StringComparer.OrdinalIgnoreCase);

            bool Include(ErdColumn c)
            {
                if (c.IsPrimaryId || c.IsPrimaryName || c.IsLookup) return true;
                if (keyMembers.Contains(c.LogicalName)) return true;
                if (display == ColumnDisplay.Important && IsRequired(c.RequiredLevel)) return true;
                return false;
            }

            return Order(table, table.Columns.Where(Include).ToList());
        }

        private static bool IsRequired(string requiredLevel) =>
            string.Equals(requiredLevel, "ApplicationRequired", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(requiredLevel, "SystemRequired", StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<ErdColumn> Order(ErdTable table, IEnumerable<ErdColumn> cols)
        {
            return cols
                .OrderByDescending(c => c.IsPrimaryId)
                .ThenByDescending(c => c.IsPrimaryName)
                .ThenBy(c => c.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    /// <summary>
    /// Pure, re-queryless model filter. <see cref="RelationshipTypes"/> empty/null = all types allowed.
    /// </summary>
    public sealed class ErdFilter
    {
        public bool CustomOnly { get; set; }
        public bool ManagedOnly { get; set; }
        public HashSet<string> RelationshipTypes { get; set; }
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
