using System;
using System.Linq;
using System.Text;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Emits PlantUML entity/relationship text (renders in PlantUML, Confluence, VS Code plugins).
    /// Pure string generation — SDK-free and deterministic. Cardinality: OneToMany <c>||--o{</c>,
    /// ManyToOne <c>}o--||</c>, ManyToMany <c>}o--o{</c>.
    /// </summary>
    public static class PlantUmlEmitter
    {
        public static string Emit(ErdModel model, ColumnDisplay display)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var sb = new StringBuilder();
            sb.AppendLine("@startuml");
            sb.AppendLine("hide circle");
            sb.AppendLine("skinparam linetype ortho");

            foreach (var t in model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"entity \"{Escape(t.DisplayName ?? t.LogicalName)}\" as {Ident(t.LogicalName)} {{");
                var cols = ErdModel.SelectColumns(t, display).ToList();
                foreach (var c in cols.Where(x => x.IsPrimaryId))
                    sb.AppendLine($"  * {Ident(c.LogicalName)} : {c.Type} <<PK>>");
                if (cols.Any(c => !c.IsPrimaryId)) sb.AppendLine("  --");
                foreach (var c in cols.Where(x => !x.IsPrimaryId))
                {
                    var tag = c.IsLookup ? " <<FK>>" : c.IsPrimaryName ? " <<name>>" : "";
                    var req = IsRequired(c.RequiredLevel) ? "* " : "";
                    sb.AppendLine($"  {req}{Ident(c.LogicalName)} : {c.Type}{tag}");
                }
                sb.AppendLine("}");
            }

            foreach (var r in model.Relationships)
            {
                var label = !string.IsNullOrEmpty(r.LookupColumn) ? r.LookupColumn : r.SchemaName;
                sb.AppendLine($"{Ident(r.FromTable)} {Cardinality(r.RelationType)} {Ident(r.ToTable)} : {Escape(label)}");
            }

            sb.AppendLine("@enduml");
            return sb.ToString();
        }

        private static string Cardinality(string relationType)
        {
            switch (relationType)
            {
                case "OneToMany": return "||--o{";
                case "ManyToOne": return "}o--||";
                case "ManyToMany": return "}o--o{";
                default: return "||--o{";
            }
        }

        private static bool IsRequired(string level) =>
            level == "ApplicationRequired" || level == "SystemRequired";

        private static string Ident(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            var chars = s.Select(ch => (char.IsLetterOrDigit(ch) || ch == '_') ? ch : '_').ToArray();
            var id = new string(chars);
            return char.IsDigit(id[0]) ? "_" + id : id;
        }

        private static string Escape(string s) => (s ?? "").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ");
    }
}
