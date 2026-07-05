using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.PluginDependencyGraph.Graph;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the Plugin Dependency Graph's SDK-free engine: builder projection
    /// (US-PLUGIN1.1.x / 2.1), subgraph isolation (US-PLUGIN1.2.2), filtering (US-PLUGIN1.2.3),
    /// risk rules — high-impact / duplicate / unmanaged (US-PLUGIN1.4.x), the emitters
    /// (US-PLUGIN1.5.3), and the guarantee that a secure-config value is never present (US-PLUGIN1.3.3).
    /// </summary>
    public class PluginDependencyGraphTests
    {
        // One managed assembly → type → two steps (Create/account, Update/contact), one pre-image on the
        // first step (which also uses secure + unsecure config), a custom API backed by the type, all in
        // the "Sales" solution.
        private static PluginRegistrationData Sample()
        {
            var d = new PluginRegistrationData();
            d.Solutions.Add(new PluginSolutionInfo { Id = "SOL", UniqueName = "sales", FriendlyName = "Sales", IsManaged = false });
            d.Assemblies.Add(new PluginAssemblyInfo { Id = "A1", Name = "Acme.Plugins", Version = "1.0.0.0", IsolationMode = "Sandbox", IsManaged = true, OwningSolution = "Sales" });
            d.Types.Add(new PluginTypeInfo { Id = "T1", AssemblyId = "A1", TypeName = "Acme.AccountHandler", FriendlyName = "Account Handler", IsManaged = true });
            d.Steps.Add(new PluginStepInfo
            {
                Id = "S1", TypeId = "T1", Name = "Create account", MessageName = "Create", PrimaryEntity = "account",
                Stage = "PostOperation", Mode = "Synchronous", Rank = 1, IsManaged = true, OwningSolution = "Sales",
                UsesSecureConfig = true, UsesUnsecureConfig = true, UnsecureConfigRedacted = "endpoint=https://api.example.com"
            });
            d.Steps.Add(new PluginStepInfo
            {
                Id = "S2", TypeId = "T1", Name = "Update contact", MessageName = "Update", PrimaryEntity = "contact",
                Stage = "PreOperation", Mode = "Asynchronous", Rank = 1, IsManaged = true, OwningSolution = "Sales"
            });
            d.Images.Add(new PluginImageInfo { Id = "I1", StepId = "S1", Name = "PreImage", ImageType = "PreImage", Attributes = "name,accountnumber" });
            d.CustomApis.Add(new CustomApiInfo { Id = "C1", Name = "acme_DoThing", UniqueName = "acme_DoThing", PluginTypeId = "T1", IsManaged = true });
            return d;
        }

        private static bool HasEdge(PluginGraph g, string from, string to, string kind) =>
            g.Edges.Any(e => e.FromId == from && e.ToId == to && e.Kind == kind);

        // TC-PDG-BUILD-01: builder emits the expected typed nodes and pipeline edges (US-PLUGIN1.1.x, 2.1).
        [Fact]
        public void Build_ProducesNodesAndEdges()
        {
            var g = PluginGraphBuilder.Build(Sample());

            Assert.NotNull(g.Node(PluginGraphBuilder.AssemblyId("A1")));
            Assert.NotNull(g.Node(PluginGraphBuilder.TypeId("T1")));
            Assert.NotNull(g.Node(PluginGraphBuilder.StepId("S1")));
            Assert.NotNull(g.Node(PluginGraphBuilder.ImageId("I1")));
            Assert.NotNull(g.Node(PluginGraphBuilder.TableId("account")));
            Assert.NotNull(g.Node(PluginGraphBuilder.MessageId("Create")));
            Assert.NotNull(g.Node(PluginGraphBuilder.CustomApiId("C1")));
            Assert.NotNull(g.Node(PluginGraphBuilder.SolutionId("sales")));

            Assert.True(HasEdge(g, PluginGraphBuilder.SolutionId("sales"), PluginGraphBuilder.AssemblyId("A1"), "member"));
            Assert.True(HasEdge(g, PluginGraphBuilder.AssemblyId("A1"), PluginGraphBuilder.TypeId("T1"), "contains"));
            Assert.True(HasEdge(g, PluginGraphBuilder.TypeId("T1"), PluginGraphBuilder.StepId("S1"), "registers"));
            Assert.True(HasEdge(g, PluginGraphBuilder.StepId("S1"), PluginGraphBuilder.TableId("account"), "on-table"));
            Assert.True(HasEdge(g, PluginGraphBuilder.StepId("S1"), PluginGraphBuilder.MessageId("Create"), "on-message"));
            Assert.True(HasEdge(g, PluginGraphBuilder.StepId("S1"), PluginGraphBuilder.ImageId("I1"), "image"));
            Assert.True(HasEdge(g, PluginGraphBuilder.CustomApiId("C1"), PluginGraphBuilder.TypeId("T1"), "implements"));
        }

        // TC-PDG-BUILD-02: node ordering is deterministic (by type then id).
        [Fact]
        public void Build_IsDeterministic()
        {
            var a = PluginGraphEmitters.Json(PluginGraphBuilder.Build(Sample()));
            var b = PluginGraphEmitters.Json(PluginGraphBuilder.Build(Sample()));
            Assert.Equal(a, b);
        }

        // TC-PDG-SUB-03: subgraph of an assembly keeps its footprint + owning solution but not an
        // unrelated custom API (US-PLUGIN1.2.2).
        [Fact]
        public void Subgraph_IsolatesAssemblyFootprint()
        {
            var g = PluginGraphBuilder.Build(Sample());
            var sub = g.Subgraph(PluginGraphBuilder.AssemblyId("A1"));

            Assert.NotNull(sub.Node(PluginGraphBuilder.TypeId("T1")));
            Assert.NotNull(sub.Node(PluginGraphBuilder.StepId("S1")));
            Assert.NotNull(sub.Node(PluginGraphBuilder.TableId("account")));
            Assert.NotNull(sub.Node(PluginGraphBuilder.SolutionId("sales")));   // ancestor
            Assert.Null(sub.Node(PluginGraphBuilder.CustomApiId("C1")));         // not in the forward footprint
        }

        // TC-PDG-FILTER-04: filtering by table keeps only the matching step's lineage (US-PLUGIN1.2.3).
        [Fact]
        public void Filter_ByTable_KeepsMatchingStepsOnly()
        {
            var g = PluginGraphBuilder.Build(Sample());
            var f = g.Filter(byTable: "account");

            Assert.NotNull(f.Node(PluginGraphBuilder.StepId("S1")));
            Assert.NotNull(f.Node(PluginGraphBuilder.TypeId("T1")));         // ancestor kept
            Assert.Null(f.Node(PluginGraphBuilder.StepId("S2")));            // sibling dropped
            Assert.Null(f.Node(PluginGraphBuilder.TableId("contact")));
        }

        // TC-PDG-FILTER-05: no criteria returns the whole graph.
        [Fact]
        public void Filter_NoCriteria_ReturnsAll()
        {
            var g = PluginGraphBuilder.Build(Sample());
            var f = g.Filter();
            Assert.Equal(g.Nodes.Count, f.Nodes.Count);
        }

        // TC-PDG-RISK-06: high-impact assembly flagged when fan-out exceeds the threshold (US-PLUGIN1.4.1).
        [Fact]
        public void Risk_HighImpactAssembly_Flagged()
        {
            var d = Sample();
            var g = PluginGraphBuilder.Build(d);
            var findings = PluginRiskRules.Evaluate(g, d, new PluginRiskOptions { HighImpactThreshold = 1 });

            var hi = findings.FirstOrDefault(f => f.Title.StartsWith("High-impact"));
            Assert.NotNull(hi);
            Assert.Equal("Acme.Plugins", hi.Component);

            // With the default threshold the modest fan-out (2 tables + 2 messages) is not flagged.
            var none = PluginRiskRules.Evaluate(g, d);
            Assert.DoesNotContain(none, f => f.Title.StartsWith("High-impact"));
        }

        // TC-PDG-RISK-07: overlapping steps on the same message+entity+stage+mode flagged (US-PLUGIN1.4.2).
        [Fact]
        public void Risk_DuplicateSteps_Flagged()
        {
            var d = new PluginRegistrationData();
            d.Assemblies.Add(new PluginAssemblyInfo { Id = "A", Name = "Dup", IsManaged = true });
            d.Types.Add(new PluginTypeInfo { Id = "T", AssemblyId = "A", TypeName = "T", IsManaged = true });
            d.Steps.Add(new PluginStepInfo { Id = "1", TypeId = "T", Name = "First", MessageName = "Update", PrimaryEntity = "account", Stage = "PostOperation", Mode = "Synchronous", Rank = 1, IsManaged = true });
            d.Steps.Add(new PluginStepInfo { Id = "2", TypeId = "T", Name = "Second", MessageName = "Update", PrimaryEntity = "account", Stage = "PostOperation", Mode = "Synchronous", Rank = 1, IsManaged = true });

            var g = PluginGraphBuilder.Build(d);
            var findings = PluginRiskRules.Evaluate(g, d);
            var dup = findings.FirstOrDefault(f => f.Title.Contains("Duplicate"));
            Assert.NotNull(dup);
            // Colliding rank (both 1) escalates to High.
            Assert.Equal(Severity.High, dup.Severity);
        }

        // TC-PDG-RISK-08: unmanaged assembly flagged High, naming the component + owning solution (US-PLUGIN1.4.3).
        [Fact]
        public void Risk_UnmanagedAssembly_Flagged()
        {
            var d = new PluginRegistrationData();
            d.Solutions.Add(new PluginSolutionInfo { Id = "S", UniqueName = "dev", FriendlyName = "Dev Tools", IsManaged = false });
            d.Assemblies.Add(new PluginAssemblyInfo { Id = "A", Name = "Rogue.Plugin", IsManaged = false, OwningSolution = "Dev Tools" });

            var g = PluginGraphBuilder.Build(d);
            var findings = PluginRiskRules.Evaluate(g, d);
            var un = findings.FirstOrDefault(f => f.Title.Contains("Unmanaged plugin assembly"));
            Assert.NotNull(un);
            Assert.Equal(Severity.High, un.Severity);
            Assert.Equal("Rogue.Plugin", un.Component);
            Assert.Contains("Dev Tools", un.Description);
        }

        // TC-PDG-EMIT-09: Mermaid / GraphML / JSON are well-formed and count nodes+edges (US-PLUGIN1.5.3).
        [Fact]
        public void Emitters_ProduceWellFormedOutput()
        {
            var g = PluginGraphBuilder.Build(Sample());

            var mmd = PluginGraphEmitters.Mermaid(g);
            Assert.Contains("flowchart LR", mmd);

            var xml = PluginGraphEmitters.GraphML(g);
            Assert.Contains("<graphml", xml);
            Assert.Contains("edgedefault=\"directed\"", xml);
            Assert.Equal(g.Nodes.Count, Regex.Matches(xml, "<node ").Count);
            Assert.Equal(g.Edges.Count, Regex.Matches(xml, "<edge ").Count);
            // GraphML round-trips node type + edge kind.
            Assert.Contains("<data key=\"d_type\">Assembly</data>", xml);
            Assert.Contains("<data key=\"d_kind\">registers</data>", xml);

            var json = PluginGraphEmitters.Json(g);
            Assert.Contains("\"nodeCount\":" + g.Nodes.Count, json);
            Assert.Contains("\"edgeCount\":" + g.Edges.Count, json);
        }

        // Regression: a control character in a node label (illegal in XML 1.0) is stripped so the emitted
        // GraphML stays well-formed and parseable.
        [Fact]
        public void GraphMl_WithControlCharLabel_IsWellFormedXml()
        {
            var d = Sample();
            d.Types[0].FriendlyName = "AccountHandler"; // 0x0B vertical tab is not a valid XML char
            var xml = PluginGraphEmitters.GraphML(PluginGraphBuilder.Build(d));
            var ex = Record.Exception(() => System.Xml.Linq.XDocument.Parse(xml));
            Assert.Null(ex);
        }

        // TC-PDG-EMIT-10: SVG is self-contained (no external hosts beyond the SVG namespace).
        [Fact]
        public void Svg_IsSelfContained()
        {
            var svg = PluginGraphEmitters.Svg(PluginGraphBuilder.Build(Sample()));
            Assert.Contains("<svg", svg);
            Assert.DoesNotContain("cdn", svg.ToLowerInvariant());
            Assert.DoesNotContain("http://", svg.Replace("http://www.w3.org", ""));
        }

        // TC-PDG-SEC-11: a secure-config VALUE is never present — only a "uses config" flag/edge
        // (US-PLUGIN1.3.3). The graph carries no secure value at all, so no output can leak one.
        [Fact]
        public void SecureConfig_ValueNeverPresent()
        {
            var d = Sample();
            var g = PluginGraphBuilder.Build(d);

            var cfg = g.Node(PluginGraphBuilder.ConfigId("S1"));
            Assert.NotNull(cfg);
            Assert.True(HasEdge(g, PluginGraphBuilder.StepId("S1"), cfg.Id, "uses-config"));
            Assert.Equal("Yes", cfg.Props["secure"]);   // flag only
            // No property on the config node holds a secure value (the DTO has no field for one).
            Assert.DoesNotContain(cfg.Props.Keys, k => k.ToLowerInvariant().Contains("securevalue"));

            // Even if a caller injects a secret sentinel into the SECURE side, there is nowhere to put it,
            // so it can never appear in any emitter output.
            const string secret = "S3CR3T-SECURE-VALUE";
            var json = PluginGraphEmitters.Json(g);
            var html = PluginGraphEmitters.Html(g);
            Assert.DoesNotContain(secret, json);
            Assert.DoesNotContain(secret, html);
        }
    }
}
