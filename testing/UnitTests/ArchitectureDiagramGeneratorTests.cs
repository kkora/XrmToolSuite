using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using XrmToolSuite.ArchitectureDiagramGenerator.Diagram;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the Architecture Diagram Generator's SDK-free model + emitters
    /// (<see cref="ComponentCatalog"/>, <see cref="ArchDiagram"/>, <see cref="DiagramEmitters"/>).
    /// Traces to US-DOC01.2.x / 3.x / 5.x. The SDK collector (ArchCollector) is manual-tested (needs Dataverse).
    /// </summary>
    public class ArchitectureDiagramGeneratorTests
    {
        // A small solution: an app (Apps) that depends on a form (UI) that depends on a table (Data),
        // plus a plug-in assembly (Code) with no edges (an orphan).
        private static ArchDiagram Sample()
        {
            var d = new ArchDiagram
            {
                SolutionName = "Contoso Sales", UniqueName = "contoso_sales", Version = "1.2.0.0",
                Publisher = "Contoso", IsManaged = false
            };
            d.Nodes.Add(new ArchNode("app", "Sales Hub", "Model-driven App", ArchLayers.Apps));
            d.Nodes.Add(new ArchNode("form", "Account Main", "Form", ArchLayers.Ui));
            d.Nodes.Add(new ArchNode("table", "Account", "Table", ArchLayers.Data));
            d.Nodes.Add(new ArchNode("asm", "Contoso.Plugins", "Plugin Assembly", ArchLayers.Code)); // orphan
            d.Edges.Add(new ArchEdge("app", "form"));
            d.Edges.Add(new ArchEdge("form", "table"));
            return d;
        }

        // TC-DOC01-CAT-01 (US-DOC01.2.1): component types map to friendly labels + architectural layers.
        [Fact]
        public void ComponentCatalog_MapsTypeToLabelAndLayer()
        {
            Assert.Equal("Table", ComponentCatalog.Label(1));
            Assert.Equal(ArchLayers.Data, ComponentCatalog.Layer(1));
            Assert.Equal(ArchLayers.Ui, ComponentCatalog.Layer(60));       // Form
            Assert.Equal(ArchLayers.Apps, ComponentCatalog.Layer(80));     // Model-driven App
            // Unknown type degrades to a generic label in the Other layer.
            Assert.Equal(ArchLayers.Other, ComponentCatalog.Layer(99999));
            Assert.Contains("99999", ComponentCatalog.Label(99999));
        }

        // TC-DOC01-ORPHAN-02 (US-DOC01.3.2): HideOrphans drops nodes with no edge; default keeps them.
        [Fact]
        public void HideOrphans_DropsUnconnectedNodes()
        {
            var d = Sample();
            Assert.Equal(4, d.VisibleNodes(new DiagramOptions { HideOrphans = false }).Count);
            var visible = d.VisibleNodes(new DiagramOptions { HideOrphans = true });
            Assert.Equal(3, visible.Count);
            Assert.DoesNotContain(visible, n => n.Key == "asm");
        }

        // TC-DOC01-LAYER-03 (US-DOC01.3.1): layers come back in canonical Apps→…→Other order.
        [Fact]
        public void NodesByLayer_OrdersLayersCanonically()
        {
            var layers = Sample().NodesByLayer(DiagramOptions.Default()).Select(kv => kv.Key).ToList();
            Assert.Equal(new[] { ArchLayers.Apps, ArchLayers.Ui, ArchLayers.Code, ArchLayers.Data }, layers);
        }

        // TC-DOC01-MER-04 (US-DOC01.5.1): Mermaid emits layered subgraphs and the dependency edges.
        [Fact]
        public void Mermaid_LayeredHasSubgraphsAndEdges()
        {
            var mer = DiagramEmitters.Mermaid(Sample(), new DiagramOptions { Layout = DiagramLayout.Layered });
            Assert.StartsWith("graph LR", mer);
            Assert.Contains("subgraph", mer);
            Assert.Contains("\"Apps\"", mer);
            Assert.Equal(2, Regex.Matches(mer, "-->").Count);              // two edges
            Assert.Contains("Sales Hub (Model-driven App)", mer);
        }

        // TC-DOC01-PUML-05 (US-DOC01.5.1): PlantUML is a well-formed @startuml/@enduml doc with packages.
        [Fact]
        public void PlantUml_IsWellFormedWithPackages()
        {
            var puml = DiagramEmitters.PlantUml(Sample(), new DiagramOptions { Layout = DiagramLayout.Layered });
            Assert.StartsWith("@startuml", puml);
            Assert.Contains("@enduml", puml);
            Assert.Contains("package \"Data\"", puml);
            Assert.Contains("rectangle", puml);
            Assert.Contains("-->", puml);
        }

        // TC-DOC01-DOT-06 (US-DOC01.5.1): DOT is a digraph with clustered layers and directed edges.
        [Fact]
        public void Dot_IsDigraphWithClusters()
        {
            var dot = DiagramEmitters.Dot(Sample(), new DiagramOptions { Layout = DiagramLayout.Layered });
            Assert.StartsWith("digraph architecture {", dot);
            Assert.Contains("subgraph cluster_", dot);
            Assert.Contains("rankdir=LR", dot);
            Assert.Equal(2, Regex.Matches(dot, "->").Count);
        }

        // TC-DOC01-MD-07 (US-DOC01.5.2): Markdown embeds a fenced mermaid block + a per-layer legend.
        [Fact]
        public void Markdown_EmbedsMermaidAndLegend()
        {
            var md = DiagramEmitters.Markdown(Sample());
            Assert.StartsWith("# Contoso Sales — architecture", md);
            Assert.Contains("```mermaid", md);
            Assert.Contains("| Layer | Components |", md);
            Assert.Contains("| Apps | 1 |", md);
        }

        // TC-DOC01-HTML-08 (US-DOC01.5.2): HTML is self-contained, theme-aware, with an inline SVG diagram.
        [Fact]
        public void Html_IsSelfContainedWithInlineSvg()
        {
            var html = DiagramEmitters.Html(Sample(), new DiagramOptions { Layout = DiagramLayout.Layered });
            Assert.StartsWith("<!DOCTYPE html>", html);
            Assert.Contains("prefers-color-scheme:dark", html);
            Assert.Contains("<svg", html);                                 // inline SVG, renders offline
            Assert.DoesNotContain("http://", html.Replace("http://www.w3", "")); // no external fetches
            Assert.DoesNotContain("https://", html);
            Assert.Contains("Mermaid source", html);                       // source embedded for re-rendering
        }

        // TC-DOC01-SVG-09 (US-DOC01.3.2): SVG escapes node text so names with markup cannot break out.
        [Fact]
        public void Svg_EscapesNodeText()
        {
            var d = Sample();
            d.Nodes.Add(new ArchNode("x", "<b>Evil</b>", "Web Resource", ArchLayers.Code));
            var svg = DiagramEmitters.Svg(d);
            Assert.Contains("&lt;b&gt;", svg);
            Assert.DoesNotContain("<b>Evil</b>", svg);
        }

        // TC-DOC01-JSON-10 (US-DOC01.5.1): JSON carries nodes + edges honoring the orphan filter.
        [Fact]
        public void Json_CarriesNodesAndEdges()
        {
            var json = DiagramEmitters.Json(Sample(), new DiagramOptions { HideOrphans = true });
            Assert.Contains("\"uniqueName\":\"contoso_sales\"", json);
            Assert.Contains("\"layer\":\"Apps\"", json);
            Assert.DoesNotContain("Contoso.Plugins", json);                // orphan filtered out
            Assert.Equal(2, Regex.Matches(json, "\"from\":").Count);       // two edges
        }
    }
}
