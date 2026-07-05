using System;
using System.IO;
using System.Linq;
using System.Text;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Emits Markdown that embeds a fenced <c>mermaid</c> <c>erDiagram</c> block (renders natively on
    /// GitHub/GitLab) plus a tables summary and a relationships table. Pure string generation —
    /// SDK-free and deterministic.
    /// </summary>
    public static class ErdMarkdown
    {
        public static string Emit(ErdModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var sb = new StringBuilder();
            sb.AppendLine("# Dataverse ERD");
            sb.AppendLine();
            sb.AppendLine($"_{model.Tables.Count} table(s), {model.Relationships.Count} relationship(s) — generated {DateTime.Now:yyyy-MM-dd HH:mm}_");
            sb.AppendLine();

            sb.AppendLine("## Diagram");
            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.Append(MermaidErdEmitter.Emit(model, ColumnDisplay.KeysAndLookupsOnly).TrimEnd('\r', '\n'));
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("## Tables");
            sb.AppendLine();
            sb.AppendLine("| Table | Logical name | Custom | Managed | Primary key | Primary name |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var t in model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"| {Cell(t.DisplayName ?? t.LogicalName)} | `{Cell(t.LogicalName)}` | {(t.IsCustom ? "yes" : "no")} | " +
                              $"{(t.IsManaged ? "yes" : "no")} | `{Cell(t.PrimaryIdColumn)}` | `{Cell(t.PrimaryNameColumn)}` |");
            sb.AppendLine();

            sb.AppendLine("## Relationships");
            sb.AppendLine();
            if (model.Relationships.Count == 0)
            {
                sb.AppendLine("_No relationships between the selected tables._");
            }
            else
            {
                sb.AppendLine("| Schema name | Type | From | To | Lookup | Required | Cascade |");
                sb.AppendLine("|---|---|---|---|---|---|---|");
                foreach (var r in model.Relationships)
                    sb.AppendLine($"| `{Cell(r.SchemaName)}` | {Cell(r.RelationType)} | {Cell(r.FromTable)} | {Cell(r.ToTable)} | " +
                                  $"`{Cell(r.LookupColumn)}` | {Cell(r.RequiredLevel)} | {Cell(r.CascadeSummary)} |");
            }
            sb.AppendLine();

            if (model.Notes != null && model.Notes.Count > 0)
            {
                sb.AppendLine("## Notes");
                sb.AppendLine();
                foreach (var n in model.Notes) sb.AppendLine($"- {Cell(n)}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static void Export(ErdModel model, string path)
            => File.WriteAllText(path, Emit(model), Encoding.UTF8);

        // Escape pipes/newlines so a value never breaks the Markdown table grid.
        private static string Cell(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }
}
