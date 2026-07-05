using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace XrmToolSuite.SolutionDocumentationGenerator.Doc
{
    /// <summary>
    /// Turns a collected <see cref="SolutionScanData"/> into a render-agnostic <see cref="SolutionDoc"/>,
    /// honoring the documentation <see cref="DocMode"/> and the <see cref="DocSections"/> checklist. This is
    /// the heart of the tool's "template engine" and is deliberately SDK-free / UI-free so it is fully
    /// unit-testable. A section is emitted only when BOTH the mode permits it AND the checklist ticks it;
    /// sources the collector could not read degrade to a documented "not available" note.
    /// </summary>
    public static class DocBuilder
    {
        private const string NotAvailable =
            "This information was not available in the source environment (permission gap or unsupported component type); it is omitted from this document.";

        public static SolutionDoc Build(SolutionScanData scan, DocOptions options)
        {
            scan = scan ?? new SolutionScanData();
            options = options ?? DocOptions.Default();
            var sections = options.Sections ?? DocSections.All();

            var doc = new SolutionDoc
            {
                SolutionName = scan.SolutionName,
                UniqueName = scan.UniqueName,
                Version = scan.Version,
                Publisher = string.IsNullOrEmpty(options.Publisher) ? scan.Publisher : options.Publisher,
                IsManaged = scan.IsManaged,
                BrandingHeader = options.BrandingHeader,
                LogoUrl = options.LogoUrl,
                ModeLabel = ModeLabel(options.Mode),
                GeneratedUtc = scan.GeneratedUtc == default(DateTime) ? DateTime.UtcNow : scan.GeneratedUtc
            };

            foreach (var kind in SectionKinds.All)
            {
                if (!ModeAllows(options.Mode, kind)) continue;
                if (!sections.IsEnabled(kind)) continue;

                var section = BuildSection(kind, scan, options);
                if (section != null) doc.Sections.Add(section);
            }

            return doc;
        }

        // ---- mode / section gating ----

        /// <summary>
        /// Which section kinds a mode is willing to emit. ExecutiveSummary keeps only the executive framing;
        /// StandardReference adds every component detail section but not the (heavy) diagram; FullReference
        /// adds diagrams and per-table column detail (the latter handled inside <see cref="BuildSchema"/>).
        /// </summary>
        public static bool ModeAllows(DocMode mode, string kind)
        {
            switch (mode)
            {
                case DocMode.ExecutiveSummary:
                    return kind == SectionKinds.Architecture
                        || kind == SectionKinds.Inventory
                        || kind == SectionKinds.ReleaseNotes;

                case DocMode.StandardReference:
                    return kind != SectionKinds.Diagrams;

                case DocMode.FullReference:
                default:
                    return true;
            }
        }

        private static DocSection BuildSection(string kind, SolutionScanData scan, DocOptions options)
        {
            switch (kind)
            {
                case SectionKinds.Architecture: return BuildArchitecture(scan);
                case SectionKinds.Inventory: return BuildInventory(scan);
                case SectionKinds.Schema: return BuildSchema(scan, options.Mode);
                case SectionKinds.Forms: return BuildComponentSection(kind, scan, "form",
                    new[] { "Table", "Name", "Type", "Managed" }, FormRow);
                case SectionKinds.Views: return BuildComponentSection(kind, scan, "view",
                    new[] { "Table", "Name", "Type", "Managed" }, ViewRow);
                case SectionKinds.Apps: return BuildComponentSection(kind, scan, "app",
                    new[] { "Name", "Unique name", "Type", "Managed" }, AppRow);
                case SectionKinds.Automation: return BuildComponentSection(kind, scan, "process",
                    new[] { "Name", "Category", "Primary table", "State", "Managed" }, AutomationRow);
                case SectionKinds.Plugins: return BuildComponentSection(kind, scan, "registration",
                    new[] { "Name", "Type", "Message / entity", "Stage / mode", "Managed" }, PluginRow);
                case SectionKinds.WebResources: return BuildComponentSection(kind, scan, "web resource",
                    new[] { "Name", "Display name", "Type", "Managed" }, WebResourceRow);
                case SectionKinds.CustomApis: return BuildComponentSection(kind, scan, "custom API",
                    new[] { "Name", "Unique name", "Kind", "Bound entity", "Managed" }, CustomApiRow);
                case SectionKinds.Config: return BuildConfig(scan);
                case SectionKinds.Roles: return BuildComponentSection(kind, scan, "role",
                    new[] { "Name", "Business unit", "Managed" }, RoleRow);
                case SectionKinds.Diagrams: return BuildDiagrams(scan);
                case SectionKinds.ReleaseNotes: return BuildReleaseNotes(scan, options);
                default: return null;
            }
        }

        // ---- Architecture ----

        private static DocSection BuildArchitecture(SolutionScanData scan)
        {
            var s = new DocSection(SectionKinds.Architecture, SectionKinds.Title(SectionKinds.Architecture));

            var body = new StringBuilder();
            body.Append(Text(scan.SolutionName, scan.UniqueName))
                .Append(" is a ")
                .Append(scan.IsManaged ? "managed" : "unmanaged")
                .Append(" solution");
            if (!string.IsNullOrWhiteSpace(scan.Version)) body.Append(" at version ").Append(scan.Version);
            if (!string.IsNullOrWhiteSpace(scan.Publisher)) body.Append(", published by ").Append(scan.Publisher);
            body.Append(". It contains ").Append(scan.ComponentCount()).Append(" documented component(s)");
            var cats = ArchitectureHighlights(scan);
            if (cats.Count > 0) body.Append(" — ").Append(string.Join(", ", cats));
            body.Append(".");

            if (!string.IsNullOrWhiteSpace(scan.Description))
                body.Append("\n\n").Append(scan.Description.Trim());

            s.Body = body.ToString();

            var table = new DocTable("Solution at a glance", "Attribute", "Value")
                .AddRow("Display name", scan.SolutionName)
                .AddRow("Unique name", scan.UniqueName)
                .AddRow("Version", scan.Version)
                .AddRow("Publisher", scan.Publisher)
                .AddRow("Managed", scan.IsManaged ? "Yes" : "No")
                .AddRow("Total components", scan.ComponentCount().ToString(CultureInfo.InvariantCulture));
            if (scan.ModifiedOn.HasValue)
                table.AddRow("Modified (UTC)", scan.ModifiedOn.Value.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            s.Tables.Add(table);

            AddDegradationNotes(s, scan);
            return s;
        }

        private static List<string> ArchitectureHighlights(SolutionScanData scan)
        {
            var parts = new List<string>();
            void Add(int n, string singular, string plural)
            {
                if (n > 0) parts.Add($"{n} {(n == 1 ? singular : plural)}");
            }
            Add(scan.ComponentCount(SectionKinds.Schema), "table", "tables");
            Add(scan.ComponentCount(SectionKinds.Automation), "automation", "automations");
            Add(scan.ComponentCount(SectionKinds.Plugins), "plug-in registration", "plug-in registrations");
            Add(scan.ComponentCount(SectionKinds.Apps), "app", "apps");
            Add(scan.ComponentCount(SectionKinds.Roles), "security role", "security roles");
            return parts;
        }

        // ---- Inventory ----

        private static DocSection BuildInventory(SolutionScanData scan)
        {
            var s = new DocSection(SectionKinds.Inventory, SectionKinds.Title(SectionKinds.Inventory));

            var table = new DocTable("Components by type", "Component type", "Count");
            var inventory = (scan.Inventory ?? new List<InventoryCount>())
                .Where(i => i != null)
                .OrderByDescending(i => i.Count)
                .ThenBy(i => i.ComponentType, StringComparer.OrdinalIgnoreCase);
            int total = 0;
            foreach (var i in inventory)
            {
                table.AddRow(i.ComponentType ?? "(unknown)", i.Count.ToString(CultureInfo.InvariantCulture));
                total += i.Count;
            }
            table.AddRow("Total", total.ToString(CultureInfo.InvariantCulture));
            s.Tables.Add(table);

            AddDegradationNotes(s, scan);
            return s;
        }

        // ---- Schema ----

        private static DocSection BuildSchema(SolutionScanData scan, DocMode mode)
        {
            var s = new DocSection(SectionKinds.Schema, SectionKinds.Title(SectionKinds.Schema));

            if (scan.Entities == null)
            {
                s.Notes.Add(NotAvailable);
                return s;
            }

            var entities = scan.Entities.OrderBy(e => e.LogicalName, StringComparer.OrdinalIgnoreCase).ToList();

            var summary = new DocTable("Tables", "Display name", "Logical name", "Custom", "Managed", "Columns", "Relationships");
            foreach (var e in entities)
            {
                summary.AddRow(
                    e.DisplayName ?? e.LogicalName,
                    e.LogicalName,
                    e.IsCustom ? "Yes" : "No",
                    e.IsManaged ? "Yes" : "No",
                    (e.Columns?.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                    (e.Relationships?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            }
            s.Tables.Add(summary);

            // FullReference: per-table column detail.
            if (mode == DocMode.FullReference)
            {
                foreach (var e in entities)
                {
                    var cols = new DocTable(
                        $"{e.DisplayName ?? e.LogicalName} — columns ({e.LogicalName})",
                        "Column", "Type", "Required", "Flags", "Description");
                    foreach (var c in (e.Columns ?? new List<DocColumn>())
                                 .OrderByDescending(c => c.IsPrimaryId)
                                 .ThenByDescending(c => c.IsPrimaryName)
                                 .ThenBy(c => c.LogicalName, StringComparer.OrdinalIgnoreCase))
                    {
                        var flags = string.Join(" ", new[]
                        {
                            c.IsPrimaryId ? "PK" : null,
                            c.IsPrimaryName ? "name" : null,
                            c.IsCustom ? "custom" : null
                        }.Where(f => f != null));
                        cols.AddRow(
                            c.DisplayName != null && c.DisplayName != c.LogicalName
                                ? $"{c.DisplayName} ({c.LogicalName})"
                                : c.LogicalName,
                            c.Type,
                            c.RequiredLevel,
                            flags,
                            c.Description);
                    }
                    s.Tables.Add(cols);
                }
            }

            // Relationships (aggregated across all tables, de-duplicated by schema name).
            var rels = entities.SelectMany(e => e.Relationships ?? new List<DocRelationship>())
                .Where(r => r != null)
                .GroupBy(r => r.SchemaName ?? Guid.NewGuid().ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(r => r.FromTable, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.ToTable, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (rels.Count > 0)
            {
                var rt = new DocTable("Relationships", "Schema name", "Type", "From", "To", "Lookup");
                foreach (var r in rels)
                    rt.AddRow(r.SchemaName, r.RelationType, r.FromTable, r.ToTable, r.LookupColumn);
                s.Tables.Add(rt);
            }

            // Choices / option sets.
            if (scan.Choices != null && scan.Choices.Count > 0)
            {
                var ct = new DocTable("Choices (option sets)", "Name", "Scope", "Options");
                foreach (var c in scan.Choices.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                    ct.AddRow(
                        c.DisplayName ?? c.Name,
                        c.IsGlobal ? "Global" : "Local",
                        string.Join(", ", c.Options ?? new List<string>()));
                s.Tables.Add(ct);
            }

            return s;
        }

        // ---- generic component section ----

        private static DocSection BuildComponentSection(
            string kind, SolutionScanData scan, string noun, string[] headers,
            Func<ScanComponent, string[]> rowSelector)
        {
            var s = new DocSection(kind, SectionKinds.Title(kind));

            // Distinguish "read but empty" from "could not read": if the collector recorded this category as
            // unavailable, add the note; otherwise an empty list is a legitimate "no components".
            var rows = scan.InCategory(kind).ToList();
            var table = new DocTable("", headers);
            foreach (var c in rows) table.AddRow(rowSelector(c));

            if (rows.Count == 0)
            {
                if (SourceUnavailable(scan, kind))
                    s.Notes.Add(NotAvailable);
                else
                    s.Notes.Add($"No {noun} components are included in this solution.");
                return s;
            }

            s.Tables.Add(table);
            return s;
        }

        // ---- Config (env vars + connection refs) ----

        private static DocSection BuildConfig(SolutionScanData scan)
        {
            var s = new DocSection(SectionKinds.Config, SectionKinds.Title(SectionKinds.Config));

            var envVars = scan.InCategory(SectionKinds.Config)
                .Where(c => string.Equals(c.ComponentType, "Environment Variable", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var connRefs = scan.InCategory(SectionKinds.Config)
                .Where(c => string.Equals(c.ComponentType, "Connection Reference", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (envVars.Count > 0)
            {
                var t = new DocTable("Environment variables", "Display name", "Schema name", "Type", "Managed");
                foreach (var v in envVars)
                    t.AddRow(v.Name, v.SchemaName, Detail(v, "Type"), Managed(v.IsManaged));
                s.Tables.Add(t);
            }

            if (connRefs.Count > 0)
            {
                var t = new DocTable("Connection references", "Display name", "Logical name", "Connector", "Managed");
                foreach (var c in connRefs)
                    t.AddRow(c.Name, c.SchemaName, Detail(c, "Connector"), Managed(c.IsManaged));
                s.Tables.Add(t);
            }

            // NOTE: environment-variable VALUES / secrets are never read or emitted — definitions only.
            if (envVars.Count == 0 && connRefs.Count == 0)
            {
                if (SourceUnavailable(scan, SectionKinds.Config))
                    s.Notes.Add(NotAvailable);
                else
                    s.Notes.Add("No environment variables or connection references are included in this solution.");
            }
            else
            {
                s.Notes.Add("Environment-variable current values and secrets are intentionally not read or documented.");
            }

            return s;
        }

        // ---- Diagrams ----

        private static DocSection BuildDiagrams(SolutionScanData scan)
        {
            var s = new DocSection(SectionKinds.Diagrams, SectionKinds.Title(SectionKinds.Diagrams));

            if (scan.Entities == null || scan.Entities.Count == 0)
            {
                s.Notes.Add("No tables are available to diagram in this solution.");
                return s;
            }

            s.Body = MermaidErd(scan.Entities);

            var rels = scan.Entities.SelectMany(e => e.Relationships ?? new List<DocRelationship>())
                .Where(r => r != null)
                .GroupBy(r => r.SchemaName ?? Guid.NewGuid().ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(r => r.FromTable, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (rels.Count > 0)
            {
                var rt = new DocTable("Relationships shown", "From", "Type", "To", "Lookup");
                foreach (var r in rels) rt.AddRow(r.FromTable, r.RelationType, r.ToTable, r.LookupColumn);
                s.Tables.Add(rt);
            }
            else
            {
                s.Notes.Add("No relationships were found between the documented tables.");
            }

            return s;
        }

        /// <summary>Minimal, deterministic Mermaid <c>erDiagram</c> source for the documented tables.</summary>
        private static string MermaidErd(List<DocEntity> entities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("erDiagram");

            var names = new HashSet<string>(entities.Select(e => e.LogicalName), StringComparer.OrdinalIgnoreCase);
            foreach (var e in entities.OrderBy(e => e.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"  {Safe(e.LogicalName)} {{");
                if (!string.IsNullOrEmpty(e.PrimaryIdColumn))
                    sb.AppendLine($"    guid {Safe(e.PrimaryIdColumn)} PK");
                if (!string.IsNullOrEmpty(e.PrimaryNameColumn))
                    sb.AppendLine($"    string {Safe(e.PrimaryNameColumn)}");
                foreach (var c in (e.Columns ?? new List<DocColumn>()).Where(c => c.Type != null))
                {
                    if (c.IsPrimaryId || c.IsPrimaryName) continue;
                    sb.AppendLine($"    {Safe(c.Type)} {Safe(c.LogicalName)}");
                }
                sb.AppendLine("  }");
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in entities.SelectMany(e => e.Relationships ?? new List<DocRelationship>())
                         .Where(r => r != null && r.FromTable != null && r.ToTable != null)
                         .OrderBy(r => r.SchemaName, StringComparer.OrdinalIgnoreCase))
            {
                if (!names.Contains(r.FromTable) || !names.Contains(r.ToTable)) continue;
                if (r.SchemaName != null && !seen.Add(r.SchemaName)) continue;
                var token = string.Equals(r.RelationType, "ManyToMany", StringComparison.OrdinalIgnoreCase)
                    ? "}o--o{" : "||--o{";
                sb.AppendLine($"  {Safe(r.FromTable)} {token} {Safe(r.ToTable)} : \"{Safe(r.SchemaName ?? r.RelationType)}\"");
            }

            return sb.ToString();
        }

        private static string Safe(string s) =>
            string.IsNullOrEmpty(s) ? "_" : new string(s.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray());

        // ---- Release notes ----

        private static DocSection BuildReleaseNotes(SolutionScanData scan, DocOptions options)
        {
            var s = new DocSection(SectionKinds.ReleaseNotes, SectionKinds.Title(SectionKinds.ReleaseNotes));

            var sb = new StringBuilder();
            sb.Append(Text(scan.SolutionName, scan.UniqueName));
            if (!string.IsNullOrWhiteSpace(scan.Version)) sb.Append(" v").Append(scan.Version);
            sb.Append(" — ").Append(scan.IsManaged ? "managed" : "unmanaged").Append(" solution")
              .Append(" containing ").Append(scan.ComponentCount()).Append(" component(s).");
            sb.AppendLine().AppendLine();
            sb.AppendLine("Contents:");
            foreach (var i in (scan.Inventory ?? new List<InventoryCount>())
                         .Where(i => i != null && i.Count > 0)
                         .OrderByDescending(i => i.Count))
            {
                sb.Append("- ").Append(i.Count).Append(' ').AppendLine(i.ComponentType);
            }
            if (scan.UnavailableSources != null && scan.UnavailableSources.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Not documented (unavailable at scan time): " +
                    string.Join(", ", scan.UnavailableSources));
            }

            s.Body = sb.ToString().TrimEnd();
            return s;
        }

        // ---- generic row selectors ----

        private static string[] FormRow(ScanComponent c) => new[]
        {
            Detail(c, "Entity"), c.Name, c.ComponentType, Managed(c.IsManaged)
        };

        private static string[] ViewRow(ScanComponent c) => new[]
        {
            Detail(c, "Entity"), c.Name, c.ComponentType, Managed(c.IsManaged)
        };

        private static string[] AppRow(ScanComponent c) => new[]
        {
            c.Name, c.SchemaName, c.ComponentType, Managed(c.IsManaged)
        };

        private static string[] AutomationRow(ScanComponent c) => new[]
        {
            c.Name, c.ComponentType, Detail(c, "Entity"), Detail(c, "State"), Managed(c.IsManaged)
        };

        private static string[] PluginRow(ScanComponent c) => new[]
        {
            c.Name, c.ComponentType, Detail(c, "Message"), Detail(c, "Stage"), Managed(c.IsManaged)
        };

        private static string[] WebResourceRow(ScanComponent c) => new[]
        {
            c.SchemaName ?? c.Name, c.Name, Detail(c, "Type"), Managed(c.IsManaged)
        };

        private static string[] CustomApiRow(ScanComponent c) => new[]
        {
            c.Name, c.SchemaName, Detail(c, "Kind"), Detail(c, "BoundEntity"), Managed(c.IsManaged)
        };

        private static string[] RoleRow(ScanComponent c) => new[]
        {
            c.Name, Detail(c, "BusinessUnit"), Managed(c.IsManaged)
        };

        // ---- helpers ----

        private static void AddDegradationNotes(DocSection s, SolutionScanData scan)
        {
            if (scan.UnavailableSources != null && scan.UnavailableSources.Count > 0)
                s.Notes.Add("Some sources could not be read at scan time: " +
                            string.Join(", ", scan.UnavailableSources) + ". " + NotAvailable);
        }

        private static bool SourceUnavailable(SolutionScanData scan, string kind)
        {
            return (scan.UnavailableSources ?? new List<string>())
                .Any(u => u != null && u.IndexOf(kind, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string Detail(ScanComponent c, string key)
        {
            if (c?.Details != null && c.Details.TryGetValue(key, out var v)) return v ?? "";
            return "";
        }

        private static string Managed(bool? m) => m.HasValue ? (m.Value ? "Managed" : "Unmanaged") : "";

        private static string Text(string primary, string fallback) =>
            !string.IsNullOrWhiteSpace(primary) ? primary
            : !string.IsNullOrWhiteSpace(fallback) ? fallback
            : "This solution";

        public static string ModeLabel(DocMode mode)
        {
            switch (mode)
            {
                case DocMode.ExecutiveSummary: return "Executive Summary";
                case DocMode.FullReference: return "Full Solution Reference";
                default: return "Standard Reference";
            }
        }
    }
}
