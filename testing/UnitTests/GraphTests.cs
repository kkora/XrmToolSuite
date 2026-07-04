using System.Linq;
using Xunit;
using XrmToolSuite.SolutionKnowledgeGraph.Graph;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the Solution Knowledge Graph's SDK-free model and exporters: dependency
    /// tracing (forward reachability), impact analysis (reverse reachability), circular-dependency
    /// detection (Tarjan SCC), and GraphML/SVG/HTML output. Traces to US-KG-2..4.
    /// </summary>
    public class GraphTests
    {
        // Edge semantics: From depends on To. Graph: A→B→C→A (a cycle) plus D→B.
        private static GraphModel Sample()
        {
            var g = new GraphModel();
            g.AddNode("A", "Table", "Account");
            g.AddNode("B", "Form", "Account Main");
            g.AddNode("C", "Web Resource", "acct.js");
            g.AddNode("D", "Plugin Step", "PostCreate");
            g.AddEdge("A", "B");
            g.AddEdge("B", "C");
            g.AddEdge("C", "A");
            g.AddEdge("D", "B");
            return g;
        }

        // TC-KG-MODEL-01: nodes/edges are counted and duplicate edges are ignored.
        [Fact]
        public void AddEdge_DedupsAndCounts()
        {
            var g = Sample();
            g.AddEdge("A", "B"); // duplicate
            Assert.Equal(4, g.NodeCount);
            Assert.Equal(4, g.EdgeCount);
        }

        // TC-KG-MODEL-02: AddEdge auto-creates missing endpoints.
        [Fact]
        public void AddEdge_AutoCreatesNodes()
        {
            var g = new GraphModel();
            g.AddEdge("X", "Y");
            Assert.NotNull(g.Node("X"));
            Assert.NotNull(g.Node("Y"));
        }

        // TC-KG-TRACE-03: dependency trace is forward transitive reachability, excluding the node itself.
        [Fact]
        public void DependencyTrace_IsForwardReachable()
        {
            var trace = Sample().DependencyTrace("A");
            Assert.Equal(2, trace.Count);
            Assert.Contains("B", trace);
            Assert.Contains("C", trace);
            Assert.DoesNotContain("A", trace);
        }

        // TC-KG-IMPACT-04: deletion impact is reverse transitive reachability (who depends on this).
        [Fact]
        public void Impact_IsReverseReachable()
        {
            var impact = Sample().Impact("B");
            Assert.Equal(3, impact.Count);          // A (direct), D (direct), C (via A)
            Assert.Contains("A", impact);
            Assert.Contains("C", impact);
            Assert.Contains("D", impact);
        }

        // TC-KG-CYCLE-05: the A→B→C→A cycle is detected as one strongly-connected component.
        [Fact]
        public void Cycles_DetectsStronglyConnectedComponent()
        {
            var cycles = Sample().Cycles();
            Assert.Single(cycles);
            var cycle = cycles[0];
            Assert.Equal(3, cycle.Count);
            Assert.Contains("A", cycle);
            Assert.Contains("B", cycle);
            Assert.Contains("C", cycle);
            Assert.DoesNotContain("D", cycle);
        }

        // TC-KG-CYCLE-06: an acyclic graph reports no cycles.
        [Fact]
        public void Cycles_AcyclicGraph_None()
        {
            var g = new GraphModel();
            g.AddEdge("A", "B");
            g.AddEdge("B", "C");
            Assert.Empty(g.Cycles());
        }

        // TC-KG-EXPORT-07: GraphML is well-formed and includes every node and edge.
        [Fact]
        public void GraphMl_IncludesNodesAndEdges()
        {
            var xml = GraphMlExporter.Build(Sample());
            Assert.Contains("<graphml", xml);
            Assert.Contains("edgedefault=\"directed\"", xml);
            Assert.Equal(4, System.Text.RegularExpressions.Regex.Matches(xml, "<node ").Count);
            Assert.Equal(4, System.Text.RegularExpressions.Regex.Matches(xml, "<edge ").Count);
            Assert.Contains("Account", xml);
        }

        // TC-KG-EXPORT-08: SVG renders a circle per node and is self-contained.
        [Fact]
        public void Svg_RendersNodes()
        {
            var svg = SvgExporter.Build(Sample());
            Assert.Contains("<svg", svg);
            Assert.Equal(4, System.Text.RegularExpressions.Regex.Matches(svg, "<circle ").Count);
        }

        // TC-KG-EXPORT-09: the interactive HTML embeds the data and is self-contained (no external refs).
        [Fact]
        public void Html_IsSelfContained_WithData()
        {
            var html = HtmlGraphBuilder.Build(Sample(), "Sales");
            Assert.Contains("const DATA=", html);
            Assert.Contains("\"label\":\"Account\"", html);
            Assert.DoesNotContain("http://", html.Replace("http://www.w3.org", "")); // no external hosts (w3 ns only)
            Assert.DoesNotContain("cdn", html.ToLowerInvariant());
        }
    }
}
