using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Emits a Mermaid <c>erDiagram</c> (renders on GitHub, GitLab, wikis, mermaid.live). Pure string
    /// generation — SDK-free and deterministic, so it is unit-testable. Cardinality tokens:
    /// OneToMany <c>||--o{</c>, ManyToOne <c>}o--||</c>, ManyToMany <c>}o--o{</c>.
    /// </summary>
    public static class MermaidErdEmitter
    {
        public static string Emit(ErdModel model, ColumnDisplay display)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var sb = new StringBuilder();
            sb.AppendLine("erDiagram");

            foreach (var t in model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                var id = Ident(t.LogicalName);
                sb.AppendLine("    " + id + " {");
                foreach (var c in ErdModel.SelectColumns(t, display))
                {
                    var marker = c.IsPrimaryId ? " PK" : c.IsLookup ? " FK" : "";
                    // Mermaid attribute line: <type> <name> [PK|FK] "comment"
                    sb.AppendLine($"        {Token(c.Type)} {Ident(c.LogicalName)}{marker} \"{Comment(c)}\"");
                }
                sb.AppendLine("    }");
            }

            foreach (var r in model.Relationships)
            {
                var token = Cardinality(r.RelationType);
                var label = Label(r);
                sb.AppendLine($"    {Ident(r.FromTable)} {token} {Ident(r.ToTable)} : \"{label}\"");
            }

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

        private static string Label(ErdRelationship r)
        {
            var text = !string.IsNullOrEmpty(r.LookupColumn) ? r.LookupColumn : r.SchemaName ?? "";
            return Escape(text);
        }

        private static string Comment(ErdColumn c)
        {
            var bits = new List<string>();
            if (c.IsPrimaryName) bits.Add("name");
            if (c.IsLookup && c.Targets != null && c.Targets.Count > 0) bits.Add("→ " + string.Join("|", c.Targets));
            if (!string.IsNullOrEmpty(c.RequiredLevel) && c.RequiredLevel != "None") bits.Add(c.RequiredLevel);
            return Escape(string.Join(", ", bits));
        }

        // Mermaid identifiers must be a single token; keep [A-Za-z0-9_], everything else -> _.
        private static string Ident(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            var chars = s.Select(ch => (char.IsLetterOrDigit(ch) || ch == '_') ? ch : '_').ToArray();
            var id = new string(chars);
            return char.IsDigit(id[0]) ? "_" + id : id;
        }

        // Attribute type is a single unquoted token in Mermaid.
        private static string Token(string s) => string.IsNullOrEmpty(s) ? "unknown" : Ident(s);

        private static string Escape(string s) => (s ?? "").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ");
    }
}
