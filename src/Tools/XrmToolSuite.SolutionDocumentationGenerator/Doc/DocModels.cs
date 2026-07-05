using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.SolutionDocumentationGenerator.Doc
{
    // =====================================================================================
    // SDK-free document model. NOTHING in this file references Microsoft.Xrm.Sdk so the whole
    // document-assembly pipeline (DocModels + DocBuilder + DocRenderers) is unit-testable with
    // the plain .NET SDK — no Dataverse, no WinForms. The SDK collector (DocCollector) populates
    // the plain SolutionScanData DTO below; DocBuilder turns it into a SolutionDoc; the renderers
    // and exporters consume SolutionDoc.
    // =====================================================================================

    /// <summary>How much of the solution the generated document covers.</summary>
    public enum DocMode
    {
        /// <summary>Executive-level framing only: architecture summary, component inventory, release notes.</summary>
        ExecutiveSummary,

        /// <summary>The common technical reference: every component section, schema at table-summary depth.</summary>
        StandardReference,

        /// <summary>Everything StandardReference has plus per-table column detail and the diagram section.</summary>
        FullReference
    }

    /// <summary>Stable section-kind identifiers (also used as <see cref="DocSection.Kind"/> values).</summary>
    public static class SectionKinds
    {
        public const string Architecture = "Architecture";
        public const string Inventory = "Inventory";
        public const string Schema = "Schema";
        public const string Forms = "Forms";
        public const string Views = "Views";
        public const string Apps = "Apps";
        public const string Automation = "Automation";
        public const string Plugins = "Plugins";
        public const string WebResources = "WebResources";
        public const string CustomApis = "CustomApis";
        public const string Config = "Config";
        public const string Roles = "Roles";
        public const string Diagrams = "Diagrams";
        public const string ReleaseNotes = "ReleaseNotes";

        /// <summary>All kinds in canonical document order.</summary>
        public static readonly string[] All =
        {
            Architecture, Inventory, Schema, Forms, Views, Apps, Automation,
            Plugins, WebResources, CustomApis, Config, Roles, Diagrams, ReleaseNotes
        };

        /// <summary>Human-readable heading for a section kind.</summary>
        public static string Title(string kind)
        {
            switch (kind)
            {
                case Architecture: return "Architecture summary";
                case Inventory: return "Component inventory";
                case Schema: return "Tables, columns & relationships";
                case Forms: return "Forms";
                case Views: return "Views, charts & dashboards";
                case Apps: return "Apps";
                case Automation: return "Automation (workflows, business rules & flows)";
                case Plugins: return "Plug-ins & custom APIs registration";
                case WebResources: return "Web resources";
                case CustomApis: return "Custom APIs";
                case Config: return "Configuration (environment variables & connection references)";
                case Roles: return "Security roles";
                case Diagrams: return "Diagrams";
                case ReleaseNotes: return "Release notes";
                default: return kind;
            }
        }
    }

    /// <summary>Per-section on/off checklist. Default = every section on. Plain serializable POCO.</summary>
    public sealed class DocSections
    {
        public bool Architecture { get; set; } = true;
        public bool Inventory { get; set; } = true;
        public bool Schema { get; set; } = true;
        public bool Forms { get; set; } = true;
        public bool Views { get; set; } = true;
        public bool Apps { get; set; } = true;
        public bool Automation { get; set; } = true;
        public bool Plugins { get; set; } = true;
        public bool WebResources { get; set; } = true;
        public bool CustomApis { get; set; } = true;
        public bool Config { get; set; } = true;
        public bool Roles { get; set; } = true;
        public bool Diagrams { get; set; } = true;
        public bool ReleaseNotes { get; set; } = true;

        public static DocSections All() => new DocSections();

        /// <summary>True when the checklist has the given <see cref="SectionKinds"/> value ticked.</summary>
        public bool IsEnabled(string kind)
        {
            switch (kind)
            {
                case SectionKinds.Architecture: return Architecture;
                case SectionKinds.Inventory: return Inventory;
                case SectionKinds.Schema: return Schema;
                case SectionKinds.Forms: return Forms;
                case SectionKinds.Views: return Views;
                case SectionKinds.Apps: return Apps;
                case SectionKinds.Automation: return Automation;
                case SectionKinds.Plugins: return Plugins;
                case SectionKinds.WebResources: return WebResources;
                case SectionKinds.CustomApis: return CustomApis;
                case SectionKinds.Config: return Config;
                case SectionKinds.Roles: return Roles;
                case SectionKinds.Diagrams: return Diagrams;
                case SectionKinds.ReleaseNotes: return ReleaseNotes;
                default: return false;
            }
        }
    }

    /// <summary>
    /// Document generation options: the documentation <see cref="Mode"/>, the <see cref="Sections"/>
    /// checklist, and branding fields for the header. Plain serializable POCO — never carries credentials.
    /// </summary>
    public sealed class DocOptions
    {
        public DocMode Mode { get; set; } = DocMode.StandardReference;
        public DocSections Sections { get; set; } = new DocSections();

        // ---- branding (rendered into the document header) ----
        public string BrandingHeader { get; set; }
        public string LogoUrl { get; set; }
        public string Publisher { get; set; }

        public static DocOptions Default() => new DocOptions();
    }

    /// <summary>A titled data table inside a section: a header row plus zero or more value rows.</summary>
    public sealed class DocTable
    {
        public string Caption { get; set; }
        public List<string> Headers { get; set; } = new List<string>();
        public List<List<string>> Rows { get; set; } = new List<List<string>>();

        public DocTable() { }
        public DocTable(string caption, params string[] headers)
        {
            Caption = caption;
            Headers = (headers ?? new string[0]).ToList();
        }

        public DocTable AddRow(params string[] cells)
        {
            Rows.Add((cells ?? new string[0]).Select(c => c ?? "").ToList());
            return this;
        }

        public int RowCount => Rows?.Count ?? 0;
    }

    /// <summary>One rendered section of the document: a heading, optional prose body, tables and notes.</summary>
    public sealed class DocSection
    {
        public string Title { get; set; }

        /// <summary>One of the <see cref="SectionKinds"/> constants.</summary>
        public string Kind { get; set; }

        public List<DocTable> Tables { get; set; } = new List<DocTable>();

        /// <summary>Advisory lines (e.g. "not available" degradations) shown under the heading.</summary>
        public List<string> Notes { get; set; } = new List<string>();

        /// <summary>Free-text prose (release notes, architecture narrative, diagram source).</summary>
        public string Body { get; set; }

        public DocSection() { }
        public DocSection(string kind, string title) { Kind = kind; Title = title; }

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(Body) &&
            (Notes == null || Notes.Count == 0) &&
            (Tables == null || Tables.All(t => t.RowCount == 0));
    }

    /// <summary>
    /// The finished, render-agnostic document. Every renderer/exporter (Markdown, HTML, JSON, Word, PDF,
    /// Excel) consumes exactly this shape. SDK-free.
    /// </summary>
    public sealed class SolutionDoc
    {
        public string SolutionName { get; set; }
        public string UniqueName { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public bool IsManaged { get; set; }

        /// <summary>Branding header line carried through from <see cref="DocOptions.BrandingHeader"/>.</summary>
        public string BrandingHeader { get; set; }

        /// <summary>Branding logo URL carried through from <see cref="DocOptions.LogoUrl"/> (rendered in HTML).</summary>
        public string LogoUrl { get; set; }

        public string ModeLabel { get; set; }

        public List<DocSection> Sections { get; set; } = new List<DocSection>();

        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>The section of the given kind, or null.</summary>
        public DocSection Section(string kind) =>
            (Sections ?? Enumerable.Empty<DocSection>())
                .FirstOrDefault(s => string.Equals(s.Kind, kind, StringComparison.OrdinalIgnoreCase));
    }

    // =====================================================================================
    // SolutionScanData DTO + the plain nested component models the SDK collector fills in.
    // All SDK-free so DocBuilder is testable without Dataverse.
    // =====================================================================================

    /// <summary>A component-type → count row for the inventory section.</summary>
    public sealed class InventoryCount
    {
        public string ComponentType { get; set; }
        public int Count { get; set; }

        public InventoryCount() { }
        public InventoryCount(string componentType, int count) { ComponentType = componentType; Count = count; }
    }

    /// <summary>A column projected from entity metadata for the schema section.</summary>
    public sealed class DocColumn
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public string RequiredLevel { get; set; }
        public string Description { get; set; }
        public bool IsPrimaryId { get; set; }
        public bool IsPrimaryName { get; set; }
        public bool IsCustom { get; set; }
    }

    /// <summary>A relationship edge for the schema section.</summary>
    public sealed class DocRelationship
    {
        public string SchemaName { get; set; }
        public string RelationType { get; set; }
        public string FromTable { get; set; }
        public string ToTable { get; set; }
        public string LookupColumn { get; set; }
    }

    /// <summary>An alternate key (uniqueness constraint over one or more columns).</summary>
    public sealed class DocKey
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
    }

    /// <summary>A table (entity) with its columns, relationships and keys.</summary>
    public sealed class DocEntity
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string SchemaName { get; set; }
        public bool IsCustom { get; set; }
        public bool IsManaged { get; set; }
        public string Description { get; set; }
        public string PrimaryIdColumn { get; set; }
        public string PrimaryNameColumn { get; set; }
        public List<DocColumn> Columns { get; set; } = new List<DocColumn>();
        public List<DocRelationship> Relationships { get; set; } = new List<DocRelationship>();
        public List<DocKey> Keys { get; set; } = new List<DocKey>();
    }

    /// <summary>A global or local choice (option set) with its labels.</summary>
    public sealed class DocChoice
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsGlobal { get; set; }
        public List<string> Options { get; set; } = new List<string>();
    }

    /// <summary>
    /// A generic, normalized solution-component row (mirrors the EnvironmentInventory pattern). Every
    /// non-schema component — forms, views, apps, workflows, plug-ins, web resources, custom APIs,
    /// environment variables, connection references, roles — lands here with a <see cref="Category"/>
    /// matching a <see cref="SectionKinds"/> value and source-specific <see cref="Details"/>. NEVER
    /// carries a secret/credential value.
    /// </summary>
    public sealed class ScanComponent
    {
        /// <summary>Section bucket = one of the <see cref="SectionKinds"/> constants.</summary>
        public string Category { get; set; }

        /// <summary>Specific component type label (e.g. "Main form", "Modern Flow", "Plug-in step").</summary>
        public string ComponentType { get; set; }

        public string Name { get; set; }
        public string SchemaName { get; set; }
        public bool? IsManaged { get; set; }
        public DateTime? ModifiedOn { get; set; }

        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// The plain DTO the SDK collector produces and <see cref="DocBuilder"/> consumes. SDK-free / UI-free.
    /// A null typed list means "the source could not be read" (degraded → a documented note); an empty
    /// list means "read successfully, nothing present".
    /// </summary>
    public sealed class SolutionScanData
    {
        // ---- header / branding ----
        public string SolutionName { get; set; }
        public string UniqueName { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string PublisherPrefix { get; set; }
        public bool IsManaged { get; set; }
        public string Description { get; set; }
        public DateTime? InstalledOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        // ---- inventory ----
        public List<InventoryCount> Inventory { get; set; } = new List<InventoryCount>();

        // ---- schema (deep metadata) ----
        public List<DocEntity> Entities { get; set; } = new List<DocEntity>();
        public List<DocChoice> Choices { get; set; } = new List<DocChoice>();

        // ---- all other components (generic rows, keyed by Category) ----
        public List<ScanComponent> Components { get; set; } = new List<ScanComponent>();

        // ---- degradations ----
        /// <summary>Sources that could not be read (permission gaps / unsupported) — a note, never a hard error.</summary>
        public List<string> UnavailableSources { get; set; } = new List<string>();

        /// <summary>
        /// True when the primary solution-component enumeration itself failed, so EVERY component section is
        /// empty because the data could not be read — NOT because the solution has no such components. When
        /// set, sections must render the "not available" note instead of a misleading "No X components".
        /// </summary>
        public bool ComponentScanFailed { get; set; }

        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Inventory helper: the number of components in the given <paramref name="category"/>
        /// (one of the <see cref="SectionKinds"/> values). Schema components come from
        /// <see cref="Entities"/>; everything else from <see cref="Components"/>. Pass null for the
        /// grand total across all categories.
        /// </summary>
        public int ComponentCount(string category = null)
        {
            var entities = Entities ?? new List<DocEntity>();
            var components = Components ?? new List<ScanComponent>();

            if (category == null)
                return entities.Count + components.Count;

            if (string.Equals(category, SectionKinds.Schema, StringComparison.OrdinalIgnoreCase))
                return entities.Count;

            return components.Count(c =>
                string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Generic components in a category, in stable name order.</summary>
        public IEnumerable<ScanComponent> InCategory(string category)
        {
            return (Components ?? Enumerable.Empty<ScanComponent>())
                .Where(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name ?? c.SchemaName ?? "", StringComparer.OrdinalIgnoreCase);
        }
    }
}
