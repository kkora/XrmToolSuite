using System;
using Microsoft.Xrm.Sdk;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.SolutionKnowledgeGraph.Graph;

namespace XrmToolSuite.CollectorTests
{
    /// <summary>
    /// Headless tests for the Solution Knowledge Graph builder. <see cref="GraphBuilder.Build"/> is a
    /// static, read-only, fail-soft entry over <see cref="IOrganizationService"/>, so it runs against the
    /// shared <see cref="FakeOrganizationService"/>. The graph algorithms (traces, impact, cycles) are
    /// covered SDK-free in testing/UnitTests; these cases verify node/edge construction from solution
    /// rows and the dependency table. Traces to US-SOLN09-1 (build graph). TC-SOLN09-COL-01..08.
    /// </summary>
    public class GraphBuilderTests
    {
        private static readonly Guid SolId = new Guid("6C000000-0000-0000-0000-000000000001");

        private static Entity Comp(int type, Guid objectId) => new Entity("solutioncomponent", Guid.NewGuid())
        {
            ["componenttype"] = new OptionSetValue(type),
            ["objectid"] = objectId,
            ["solutionid"] = SolId
        };

        // A name-source row must carry both its primary key attribute (for the In filter) and Entity.Id
        // (which the builder maps the resolved label back onto).
        private static Entity Wf(Guid id, string name) =>
            new Entity("workflow", id) { ["workflowid"] = id, ["name"] = name };

        private static Entity Dep(Guid dependent, Guid required) => new Entity("dependency", Guid.NewGuid())
        {
            ["dependentcomponentobjectid"] = dependent,
            ["requiredcomponentobjectid"] = required
        };

        private static Entity Dep(Guid dependent, Guid required, int requiredType)
        {
            var e = Dep(dependent, required);
            e["requiredcomponenttype"] = new OptionSetValue(requiredType);
            return e;
        }

        private static GraphModel Build(FakeOrganizationService fake) => GraphBuilder.Build(fake, SolId, _ => { });

        // TC-SOLN09-COL-01: a typed component resolves its friendly type + display name from the source table.
        [Fact]
        public void TypedComponent_ResolvesTypeAndName()
        {
            var wf = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("solutioncomponent", Comp(29, wf))
                .Seed("workflow", Wf(wf, "Nightly Sync"));

            var g = Build(fake);
            Assert.Equal(1, g.NodeCount);
            var n = g.Node(wf.ToString());
            Assert.Equal("Workflow / Flow", n.Type);
            Assert.Equal("Nightly Sync", n.Label);
        }

        // TC-SOLN09-COL-09: a Table (type 1) component is named from entity metadata (display name).
        [Fact]
        public void TableComponent_NamedFromMetadata()
        {
            var entId = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("solutioncomponent", Comp(1, entId))
                .SeedAllEntities(MetaBuilder.Entity("account", metadataId: entId, displayName: "Account"));

            var n = Build(fake).Node(entId.ToString());
            Assert.Equal("Table", n.Type);
            Assert.Equal("Account", n.Label);
        }

        // TC-SOLN09-COL-02: an unmapped component type falls back to a "Component (code)" label.
        [Fact]
        public void UnmappedType_FallsBackToComponentLabel()
        {
            var id = Guid.NewGuid();
            var fake = new FakeOrganizationService().Seed("solutioncomponent", Comp(999, id));

            var n = Build(fake).Node(id.ToString());
            Assert.Equal("Component (999)", n.Type);
            Assert.StartsWith("Component (999)", n.Label);
        }

        // TC-SOLN09-COL-03: a typed component whose name row is absent still produces a node (fail-soft label).
        [Fact]
        public void MissingNameRow_StillCreatesNodeWithFallbackLabel()
        {
            var id = Guid.NewGuid();
            var fake = new FakeOrganizationService().Seed("solutioncomponent", Comp(60, id)); // Form, no systemform seeded

            var n = Build(fake).Node(id.ToString());
            Assert.Equal("Form", n.Type);
            Assert.StartsWith("Form ", n.Label);
        }

        // TC-SOLN09-COL-04: a dependency between two in-solution components becomes a "requires" edge.
        [Fact]
        public void Dependency_WithinSolution_AddsEdge()
        {
            Guid a = Guid.NewGuid(), b = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("solutioncomponent", Comp(29, a), Comp(29, b))
                .Seed("dependency", Dep(a, b));

            var g = Build(fake);
            Assert.Equal(1, g.EdgeCount);
            var e = g.Edges[0];
            Assert.Equal(a.ToString(), e.From);
            Assert.Equal(b.ToString(), e.To);
            Assert.Equal("requires", e.Kind);
        }

        // TC-SOLN09-COL-05: a required component outside the solution is auto-created as an Unknown endpoint.
        [Fact]
        public void Dependency_RequiredOutsideSolution_AutoCreatesEndpoint()
        {
            Guid a = Guid.NewGuid(), external = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("solutioncomponent", Comp(29, a))
                .Seed("dependency", Dep(a, external));

            var g = Build(fake);
            Assert.Equal(1, g.EdgeCount);
            var ext = g.Node(external.ToString());
            Assert.NotNull(ext);
            Assert.Equal("Unknown", ext.Type);
        }

        // TC-SOLN09-COL-10: an external required component is typed + named from its requiredcomponenttype
        // (a Table resolves via entity metadata) instead of showing as an "Unknown" GUID.
        [Fact]
        public void Dependency_ExternalRequired_TypedFromRequiredComponentType()
        {
            Guid a = Guid.NewGuid(), extTable = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("solutioncomponent", Comp(29, a))
                .Seed("dependency", Dep(a, extTable, 1)) // required is a Table (type 1) outside the solution
                .SeedAllEntities(MetaBuilder.Entity("contact", metadataId: extTable, displayName: "Contact"));

            var ext = Build(fake).Node(extTable.ToString());
            Assert.NotNull(ext);
            Assert.Equal("Table", ext.Type);
            Assert.Equal("Contact", ext.Label);
        }

        // TC-SOLN09-COL-11: a Column (type 2) component is named from attribute metadata as
        // "table.Column Display Name" instead of the type + short-guid fallback.
        [Fact]
        public void ColumnComponent_NamedFromAttributeMetadata()
        {
            var colId = Guid.NewGuid();
            var attr = MetaBuilder.Attribute("new_amount", displayName: "Amount");
            attr.MetadataId = colId;
            var fake = new FakeOrganizationService()
                .Seed("solutioncomponent", Comp(2, colId))
                .SeedAllEntities(MetaBuilder.Entity("new_widget", isCustom: true, attributes: new[] { attr }));

            var n = Build(fake).Node(colId.ToString());
            Assert.Equal("Column", n.Type);
            Assert.Equal("new_widget.Amount", n.Label);
        }

        // TC-SOLN09-COL-06: a dependency whose dependent is not in the solution produces no edge.
        [Fact]
        public void Dependency_DependentOutsideSolution_NoEdge()
        {
            Guid a = Guid.NewGuid(), other = Guid.NewGuid();
            var fake = new FakeOrganizationService()
                .Seed("solutioncomponent", Comp(29, a))
                .Seed("dependency", Dep(other, a)); // dependent 'other' isn't a solution component

            Assert.Equal(0, Build(fake).EdgeCount);
        }

        // TC-SOLN09-COL-07: an empty solution yields an empty graph.
        [Fact]
        public void EmptySolution_EmptyGraph()
        {
            var g = Build(new FakeOrganizationService());
            Assert.Equal(0, g.NodeCount);
            Assert.Equal(0, g.EdgeCount);
        }

        // TC-SOLN09-COL-08: a component with an empty object id is skipped.
        [Fact]
        public void EmptyObjectId_Skipped()
        {
            var fake = new FakeOrganizationService().Seed("solutioncomponent", Comp(29, Guid.Empty));
            Assert.Equal(0, Build(fake).NodeCount);
        }
    }
}
