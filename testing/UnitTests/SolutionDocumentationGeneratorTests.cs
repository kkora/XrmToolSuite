using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using XrmToolSuite.SolutionDocumentationGenerator.Doc;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the Solution Documentation Generator's SDK-free document pipeline: the
    /// <see cref="DocBuilder"/> (mode + sections gating, inventory counts, "not available" degradation)
    /// and the <see cref="DocRenderers"/> (Markdown / HTML / JSON). Traces to US-SOLN5.1.x / 5.2.x / 5.3.x
    /// / 5.5.x. The SDK collector and the Word/PDF/Excel exporters are excluded (manual-tested).
    /// </summary>
    public class SolutionDocumentationGeneratorTests
    {
        // A hand-built scan: 2 tables (one with a relationship), 1 global choice, and a spread of generic
        // components across every category, so section gating and inventory counts are exercised.
        private static SolutionScanData Sample()
        {
            var account = new DocEntity
            {
                LogicalName = "account", DisplayName = "Account", SchemaName = "Account",
                IsCustom = false, IsManaged = true, PrimaryIdColumn = "accountid", PrimaryNameColumn = "name",
                Columns =
                {
                    new DocColumn { LogicalName = "accountid", Type = "Uniqueidentifier", IsPrimaryId = true, RequiredLevel = "SystemRequired" },
                    new DocColumn { LogicalName = "name", Type = "String", IsPrimaryName = true, RequiredLevel = "ApplicationRequired" },
                    new DocColumn { LogicalName = "telephone1", Type = "String", RequiredLevel = "None" }
                },
                Relationships =
                {
                    new DocRelationship { SchemaName = "new_account_projects", RelationType = "OneToMany",
                        FromTable = "account", ToTable = "new_project", LookupColumn = "new_accountid" }
                }
            };
            var project = new DocEntity
            {
                LogicalName = "new_project", DisplayName = "Project", SchemaName = "new_Project",
                IsCustom = true, IsManaged = false, PrimaryIdColumn = "new_projectid", PrimaryNameColumn = "new_name",
                Columns =
                {
                    new DocColumn { LogicalName = "new_projectid", Type = "Uniqueidentifier", IsPrimaryId = true },
                    new DocColumn { LogicalName = "new_accountid", Type = "Lookup", IsCustom = true }
                }
            };

            var scan = new SolutionScanData
            {
                SolutionName = "Contoso Sales", UniqueName = "contoso_sales", Version = "1.2.0.0",
                Publisher = "Contoso", PublisherPrefix = "new", IsManaged = false,
                Description = "Core sales customizations.",
                Entities = { account, project },
                Choices = { new DocChoice { Name = "new_stage", DisplayName = "Stage", IsGlobal = true, Options = { "New", "Won" } } }
            };

            scan.Components.Add(new ScanComponent { Category = SectionKinds.Forms, ComponentType = "Main form", Name = "Account Main", SchemaName = "account", Details = { ["Entity"] = "account" } });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.Views, ComponentType = "View", Name = "Active Accounts", Details = { ["Entity"] = "account" } });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.Apps, ComponentType = "App Module", Name = "Sales Hub", SchemaName = "saleshub" });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.Automation, ComponentType = "Modern Flow", Name = "Notify Owner", Details = { ["Entity"] = "account", ["State"] = "Activated" } });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.Plugins, ComponentType = "Plug-in step", Name = "Account Create", Details = { ["Message"] = "Create", ["Stage"] = "PostOperation/Sync" } });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.WebResources, ComponentType = "JScript", Name = "account_form.js", SchemaName = "new_account_form" });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.CustomApis, ComponentType = "Custom API", Name = "Recalculate", SchemaName = "new_Recalculate", Details = { ["Kind"] = "Action", ["BoundEntity"] = "account" } });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.Config, ComponentType = "Environment Variable", Name = "API Base URL", SchemaName = "new_apibaseurl", Details = { ["Type"] = "String" } });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.Config, ComponentType = "Connection Reference", Name = "Dataverse conn", SchemaName = "new_dv", Details = { ["Connector"] = "shared_commondataserviceforapps" } });
            scan.Components.Add(new ScanComponent { Category = SectionKinds.Roles, ComponentType = "Security Role", Name = "Sales Rep", Details = { ["BusinessUnit"] = "Contoso" } });

            scan.Inventory = new System.Collections.Generic.List<InventoryCount>
            {
                new InventoryCount("Tables", 2),
                new InventoryCount("Forms", 1),
                new InventoryCount("Automation", 1),
                new InventoryCount("Plug-in registrations", 1)
            };
            return scan;
        }

        // TC-SOLN5-COUNT-01 (US-SOLN5.3.2): ComponentCount rolls up per category and overall.
        [Fact]
        public void ComponentCount_CountsPerCategoryAndTotal()
        {
            var scan = Sample();
            Assert.Equal(2, scan.ComponentCount(SectionKinds.Schema));   // two tables
            Assert.Equal(1, scan.ComponentCount(SectionKinds.Forms));
            Assert.Equal(2, scan.ComponentCount(SectionKinds.Config));   // env var + conn ref
            Assert.Equal(12, scan.ComponentCount());                     // 2 entities + 10 components
        }

        // TC-SOLN5-MODE-02 (US-SOLN5.1.1): Executive Summary keeps only the executive framing sections.
        [Fact]
        public void Build_ExecutiveSummary_OmitsDetailSections()
        {
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.ExecutiveSummary });
            var kinds = doc.Sections.Select(s => s.Kind).ToList();

            Assert.Contains(SectionKinds.Architecture, kinds);
            Assert.Contains(SectionKinds.Inventory, kinds);
            Assert.Contains(SectionKinds.ReleaseNotes, kinds);
            // Detail sections are omitted at this mode.
            Assert.DoesNotContain(SectionKinds.Schema, kinds);
            Assert.DoesNotContain(SectionKinds.Plugins, kinds);
            Assert.DoesNotContain(SectionKinds.Diagrams, kinds);
        }

        // TC-SOLN5-MODE-03 (US-SOLN5.1.1): Full Solution Reference includes every section, incl. diagrams.
        [Fact]
        public void Build_FullReference_IncludesAllSections()
        {
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.FullReference });
            var kinds = doc.Sections.Select(s => s.Kind).ToList();

            foreach (var kind in SectionKinds.All)
                Assert.Contains(kind, kinds);
            Assert.Equal("Full Solution Reference", doc.ModeLabel);
        }

        // TC-SOLN5-MODE-04 (US-SOLN5.1.1): Standard Reference drops only the heavy diagram section.
        [Fact]
        public void Build_StandardReference_OmitsDiagramsButKeepsDetail()
        {
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.StandardReference });
            var kinds = doc.Sections.Select(s => s.Kind).ToList();

            Assert.Contains(SectionKinds.Schema, kinds);
            Assert.Contains(SectionKinds.Config, kinds);
            Assert.DoesNotContain(SectionKinds.Diagrams, kinds);
        }

        // TC-SOLN5-SECT-05 (US-SOLN5.1.1): an unchecked section is excluded even when the mode allows it.
        [Fact]
        public void Build_UncheckedSection_IsExcluded()
        {
            var sections = DocSections.All();
            sections.Plugins = false;
            sections.WebResources = false;
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.FullReference, Sections = sections });
            var kinds = doc.Sections.Select(s => s.Kind).ToList();

            Assert.DoesNotContain(SectionKinds.Plugins, kinds);
            Assert.DoesNotContain(SectionKinds.WebResources, kinds);
            Assert.Contains(SectionKinds.Schema, kinds); // still checked
        }

        // TC-SOLN5-SCHEMA-06 (US-SOLN5.2.1): Full Reference emits a per-table column detail table.
        [Fact]
        public void Build_FullReference_Schema_HasPerTableColumnDetail()
        {
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.FullReference });
            var schema = doc.Section(SectionKinds.Schema);
            Assert.NotNull(schema);
            // A column-detail table names the table and lists a column row.
            Assert.Contains(schema.Tables, t => (t.Caption ?? "").Contains("account") &&
                                                t.Rows.Any(r => r.Any(c => c == "telephone1")));
        }

        // TC-SOLN5-NA-07 (US-SOLN5.2.3): a null typed list degrades to a documented "not available" note.
        [Fact]
        public void Build_NullSchema_RendersNotAvailableNote()
        {
            var scan = Sample();
            scan.Entities = null; // collector could not read table metadata
            var doc = DocBuilder.Build(scan, new DocOptions { Mode = DocMode.FullReference });
            var schema = doc.Section(SectionKinds.Schema);
            Assert.NotNull(schema);
            Assert.Contains(schema.Notes, n => n.Contains("not available"));
        }

        // TC-SOLN5-MD-08 (US-SOLN5.5.1): Markdown carries the title and section headers.
        [Fact]
        public void Markdown_ContainsTitleAndSectionHeaders()
        {
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.FullReference });
            var md = DocRenderers.Markdown(doc);

            Assert.StartsWith("# Contoso Sales", md);
            Assert.Contains("## " + SectionKinds.Title(SectionKinds.Inventory), md);
            Assert.Contains("## " + SectionKinds.Title(SectionKinds.Schema), md);
            Assert.Contains("```mermaid", md);        // diagram section fenced
            Assert.Contains("| Component type | Count |", md);
        }

        // TC-SOLN5-HTML-09 (US-SOLN5.5.1): HTML is self-contained + theme-aware and carries the sections.
        [Fact]
        public void Html_IsSelfContainedThemeAware()
        {
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.FullReference, BrandingHeader = "Contoso Ltd" });
            var html = DocRenderers.Html(doc);

            Assert.StartsWith("<!DOCTYPE html>", html);
            Assert.Contains("prefers-color-scheme:dark", html);            // theme-aware
            Assert.DoesNotContain("http://", html.Replace("http://www.w3", "")); // no external asset fetches
            Assert.Contains("Contoso Ltd", html);                          // branding header rendered
            Assert.Contains(">" + SectionKinds.Title(SectionKinds.Roles) + "<", html);
        }

        // TC-SOLN5-JSON-10 (US-SOLN5.5.1): JSON carries the structured inventory + sections.
        [Fact]
        public void Json_CarriesStructuredInventoryAndSections()
        {
            var doc = DocBuilder.Build(Sample(), new DocOptions { Mode = DocMode.FullReference });
            var json = DocRenderers.Json(doc);

            Assert.Contains("\"uniqueName\":\"contoso_sales\"", json);
            Assert.Contains("\"kind\":\"Inventory\"", json);
            Assert.Contains("\"kind\":\"Schema\"", json);
            // Every section object round-trips a kind; count matches the section count.
            Assert.Equal(doc.Sections.Count, Regex.Matches(json, "\"kind\":").Count);
        }
    }
}
