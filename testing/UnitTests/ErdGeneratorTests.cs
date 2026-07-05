using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using XrmToolSuite.ErdGenerator.Erd;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the ERD Generator's SDK-free model + emitters (Mermaid, PlantUML, JSON, SVG)
    /// and the re-queryless <see cref="ErdModel.Apply"/> filter. Traces to US-DOC2.2.x/2.3.x/2.4.x/2.5.x.
    /// The Dataverse collector and the System.Drawing/MigraDoc exporters are excluded (manual-tested).
    /// </summary>
    public class ErdGeneratorTests
    {
        // account = standard + managed; new_project = custom + unmanaged. One 1:N (account→new_project
        // via the new_accountid lookup) and one N:N between them.
        private static ErdModel Sample()
        {
            var account = new ErdTable
            {
                LogicalName = "account", DisplayName = "Account", SchemaName = "Account",
                IsCustom = false, IsManaged = true,
                PrimaryIdColumn = "accountid", PrimaryNameColumn = "name",
                Columns =
                {
                    new ErdColumn { LogicalName = "accountid", Type = "Uniqueidentifier", IsPrimaryId = true, RequiredLevel = "SystemRequired" },
                    new ErdColumn { LogicalName = "name", Type = "String", IsPrimaryName = true, RequiredLevel = "ApplicationRequired" },
                    new ErdColumn { LogicalName = "telephone1", Type = "String", RequiredLevel = "None" }
                },
                AlternateKeys = { new ErdKey { Name = "AK Account Number", Columns = { "accountnumber" } } }
            };
            var project = new ErdTable
            {
                LogicalName = "new_project", DisplayName = "Project", SchemaName = "new_Project",
                IsCustom = true, IsManaged = false,
                PrimaryIdColumn = "new_projectid", PrimaryNameColumn = "new_name",
                Columns =
                {
                    new ErdColumn { LogicalName = "new_projectid", Type = "Uniqueidentifier", IsPrimaryId = true },
                    new ErdColumn { LogicalName = "new_name", Type = "String", IsPrimaryName = true },
                    new ErdColumn { LogicalName = "new_accountid", Type = "Lookup", IsLookup = true, Targets = { "account" }, RequiredLevel = "ApplicationRequired" }
                }
            };

            return new ErdModel
            {
                Tables = { account, project },
                Relationships =
                {
                    new ErdRelationship { SchemaName = "new_account_projects", RelationType = "OneToMany",
                        FromTable = "account", ToTable = "new_project", LookupColumn = "new_accountid",
                        CascadeSummary = "Delete=Cascade", RequiredLevel = "ApplicationRequired" },
                    new ErdRelationship { SchemaName = "new_account_project_mm", RelationType = "ManyToMany",
                        FromTable = "account", ToTable = "new_project", LookupColumn = "new_account_project" }
                }
            };
        }

        // TC-ERD-MERMAID-01 (US-DOC2.5.1): Mermaid output is an erDiagram with correct cardinality tokens and PK markers.
        [Fact]
        public void Mermaid_EmitsErDiagram_WithCardinalityAndPk()
        {
            var mmd = MermaidErdEmitter.Emit(Sample(), ColumnDisplay.KeysAndLookupsOnly);
            Assert.StartsWith("erDiagram", mmd);
            Assert.Contains("||--o{", mmd);   // one-to-many
            Assert.Contains("}o--o{", mmd);   // many-to-many
            Assert.Contains("accountid PK", mmd);
            Assert.Contains("new_accountid FK", mmd);
        }

        // TC-ERD-MERMAID-02 (US-DOC2.2.2): "keys + lookups" hides non-key columns; "all" shows them.
        [Fact]
        public void Mermaid_ColumnDisplay_ControlsColumns()
        {
            var keysOnly = MermaidErdEmitter.Emit(Sample(), ColumnDisplay.KeysAndLookupsOnly);
            var all = MermaidErdEmitter.Emit(Sample(), ColumnDisplay.All);
            Assert.DoesNotContain("telephone1", keysOnly); // non-key, non-lookup, optional -> hidden
            Assert.Contains("telephone1", all);
        }

        // TC-ERD-PLANTUML-03 (US-DOC2.5.1): PlantUML emits entities and a relationship line.
        [Fact]
        public void PlantUml_EmitsEntitiesAndRelationships()
        {
            var puml = PlantUmlEmitter.Emit(Sample(), ColumnDisplay.Important);
            Assert.Contains("@startuml", puml);
            Assert.Contains("@enduml", puml);
            Assert.Equal(2, Regex.Matches(puml, @"entity ").Count);
            Assert.Contains("||--o{", puml);
            Assert.Contains("<<PK>>", puml);
        }

        // TC-ERD-JSON-04 (US-DOC2.5.2): JSON carries the table/relationship counts and names.
        [Fact]
        public void Json_CarriesCountsAndNames()
        {
            var json = ErdJson.Emit(Sample());
            Assert.Contains("\"tableCount\":2", json);
            Assert.Contains("\"relationshipCount\":2", json);
            Assert.Contains("\"logicalName\":\"account\"", json);
            Assert.Contains("\"relationType\":\"OneToMany\"", json);
            // Every emitted table object round-trips a logicalName.
            Assert.Equal(2, Regex.Matches(json, "\"schemaName\":\"Account\"|\"schemaName\":\"new_Project\"").Count);
        }

        // TC-ERD-FILTER-05 (US-DOC2.4.1): custom-only keeps custom tables and drops now-dangling relationships.
        [Fact]
        public void Filter_CustomOnly_TrimsTablesAndRelationships()
        {
            var trimmed = Sample().Apply(new ErdFilter { CustomOnly = true });
            Assert.Single(trimmed.Tables);
            Assert.Equal("new_project", trimmed.Tables[0].LogicalName);
            Assert.Empty(trimmed.Relationships); // account removed => both-ends rule drops every edge
        }

        // TC-ERD-FILTER-06 (US-DOC2.4.1): managed-only keeps managed tables only.
        [Fact]
        public void Filter_ManagedOnly_KeepsManaged()
        {
            var trimmed = Sample().Apply(new ErdFilter { ManagedOnly = true });
            Assert.Single(trimmed.Tables);
            Assert.Equal("account", trimmed.Tables[0].LogicalName);
        }

        // TC-ERD-FILTER-07 (US-DOC2.4.1): relationship-type filter keeps only the allowed types.
        [Fact]
        public void Filter_RelationshipType_KeepsOnlyAllowed()
        {
            var trimmed = Sample().Apply(new ErdFilter
            {
                RelationshipTypes = new HashSet<string> { "OneToMany" }
            });
            Assert.Equal(2, trimmed.Tables.Count);            // both tables survive
            Assert.Single(trimmed.Relationships);
            Assert.Equal("OneToMany", trimmed.Relationships[0].RelationType);
        }

        // TC-ERD-SVG-08 (US-DOC2.5.1): SVG is well-formed and draws a box (rect) per table.
        [Fact]
        public void Svg_IsWellFormed_WithBoxPerTable()
        {
            var svg = ErdSvg.Emit(Sample(), ColumnDisplay.KeysAndLookupsOnly);
            Assert.StartsWith("<svg", svg);
            Assert.Contains("</svg>", svg);
            // One header rect per table box (header fill is unique to box headers).
            Assert.Equal(2, Regex.Matches(svg, @"height=""26""").Count);
        }

        // Regression: a control character in a table display name (illegal in XML 1.0) is stripped so the
        // emitted SVG stays well-formed and parseable as XML.
        [Fact]
        public void Svg_WithControlCharDisplayName_IsWellFormedXml()
        {
            var model = Sample();
            model.Tables.First().DisplayName = "AccountName"; // 0x0B vertical tab is not a valid XML char
            var svg = ErdSvg.Emit(model, ColumnDisplay.KeysAndLookupsOnly);
            var ex = Record.Exception(() => System.Xml.Linq.XDocument.Parse(svg));
            Assert.Null(ex);
        }

        // TC-ERD-COLSELECT-09 (US-DOC2.2.2): SelectColumns includes alternate-key members even when optional.
        [Fact]
        public void SelectColumns_KeysAndLookups_IncludesAlternateKeyMembers()
        {
            var account = Sample().Tables.First(t => t.LogicalName == "account");
            account.Columns.Add(new ErdColumn { LogicalName = "accountnumber", Type = "String", RequiredLevel = "None" });
            var shown = ErdModel.SelectColumns(account, ColumnDisplay.KeysAndLookupsOnly).Select(c => c.LogicalName).ToList();
            Assert.Contains("accountnumber", shown); // member of an alternate key
            Assert.DoesNotContain("telephone1", shown);
        }
    }
}
