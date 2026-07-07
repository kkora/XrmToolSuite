using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.PluginDependencyGraph.Graph
{
    /// <summary>The kind of node in the plugin dependency graph. Drives colouring and layering.</summary>
    public enum PluginNodeType
    {
        Assembly,
        PluginType,
        Step,
        Image,
        Table,
        Message,
        CustomApi,
        Solution,
        Config
    }

    /// <summary>
    /// A single node in the plugin dependency graph. SDK-free (no Microsoft.Xrm.Sdk) so it stays
    /// unit-testable and liftable into a console/CI wrapper. <see cref="Props"/> carries display
    /// detail for the details panel; it MUST NEVER contain a secure-configuration value.
    /// </summary>
    public sealed class PluginNode
    {
        public string Id { get; set; }
        public PluginNodeType Type { get; set; }
        public string Label { get; set; }
        public Dictionary<string, string> Props { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public bool IsManaged { get; set; }

        public PluginNode() { }

        public PluginNode(string id, PluginNodeType type, string label, bool isManaged = false)
        {
            Id = id;
            Type = type;
            Label = label;
            IsManaged = isManaged;
        }
    }

    /// <summary>A directed edge: <see cref="FromId"/> → <see cref="ToId"/> with a semantic <see cref="Kind"/>.</summary>
    public sealed class PluginEdge
    {
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string Kind { get; set; }

        public PluginEdge() { }

        public PluginEdge(string fromId, string toId, string kind = null)
        {
            FromId = fromId;
            ToId = toId;
            Kind = kind;
        }
    }

    /// <summary>
    /// The whole plugin pipeline as a graph: assembly → type → step → image, and step → table/message/config,
    /// customapi → type, solution → member components. Pure data, deterministic, fully unit-testable. The
    /// SDK collector projects <see cref="PluginRegistrationData"/> into this via <see cref="PluginGraphBuilder"/>;
    /// the emitters/exporters consume it.
    /// </summary>
    public sealed class PluginGraph
    {
        public List<PluginNode> Nodes { get; set; } = new List<PluginNode>();
        public List<PluginEdge> Edges { get; set; } = new List<PluginEdge>();

        public PluginNode Node(string id) =>
            id == null ? null : Nodes.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>Direct forward neighbours (this → x).</summary>
        public IEnumerable<string> Successors(string id) =>
            Edges.Where(e => string.Equals(e.FromId, id, StringComparison.OrdinalIgnoreCase)).Select(e => e.ToId);

        /// <summary>Direct reverse neighbours (x → this).</summary>
        public IEnumerable<string> Predecessors(string id) =>
            Edges.Where(e => string.Equals(e.ToId, id, StringComparison.OrdinalIgnoreCase)).Select(e => e.FromId);

        /// <summary>
        /// Returns the isolated footprint of <paramref name="nodeId"/> WITHOUT re-querying: the node itself,
        /// everything forward-reachable from it (its types/steps/images/tables/messages/config) and everything
        /// that reaches it directly-and-transitively backward (its owning type/assembly/solution). Edges are
        /// kept only when both endpoints survive. Deterministic.
        /// </summary>
        public PluginGraph Subgraph(string nodeId)
        {
            if (Node(nodeId) == null) return new PluginGraph();

            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { nodeId };
            foreach (var d in Reach(nodeId, forward: true)) keep.Add(d);
            foreach (var a in Reach(nodeId, forward: false)) keep.Add(a);

            return Project(keep);
        }

        /// <summary>
        /// Returns a trimmed copy per the (all-optional) filter WITHOUT re-querying: keeps the STEP nodes whose
        /// table/message/stage/mode/solution match every supplied criterion, then keeps each surviving step's
        /// lineage (owning type/assembly/solution) and dependents (images/table/message/config). When every
        /// criterion is null the whole graph is returned. Deterministic.
        /// </summary>
        public PluginGraph Filter(string byTable = null, string message = null, string stage = null,
            string mode = null, string solution = null)
        {
            bool any = !IsBlank(byTable) || !IsBlank(message) || !IsBlank(stage) || !IsBlank(mode) || !IsBlank(solution);
            if (!any) return Project(new HashSet<string>(Nodes.Select(n => n.Id), StringComparer.OrdinalIgnoreCase));

            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var step in Nodes.Where(n => n.Type == PluginNodeType.Step))
            {
                if (!Matches(step, "table", byTable)) continue;
                if (!Matches(step, "message", message)) continue;
                if (!Matches(step, "stage", stage)) continue;
                if (!Matches(step, "mode", mode)) continue;
                if (!Matches(step, "solution", solution)) continue;

                keep.Add(step.Id);
                foreach (var d in Reach(step.Id, forward: true)) keep.Add(d);
                foreach (var a in Reach(step.Id, forward: false)) keep.Add(a);
            }

            return Project(keep);
        }

        private static bool IsBlank(string s) => string.IsNullOrWhiteSpace(s);

        private static bool Matches(PluginNode step, string propKey, string wanted)
        {
            if (IsBlank(wanted)) return true;
            step.Props.TryGetValue(propKey, out var actual);
            return string.Equals(actual ?? "", wanted, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<string> Reach(string start, bool forward)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var next in forward ? Successors(cur) : Predecessors(cur))
                    if (seen.Add(next)) queue.Enqueue(next);
            }
            seen.Remove(start);
            return seen;
        }

        private PluginGraph Project(HashSet<string> keep)
        {
            var g = new PluginGraph
            {
                Nodes = Nodes.Where(n => keep.Contains(n.Id))
                    .Select(n => new PluginNode(n.Id, n.Type, n.Label, n.IsManaged)
                    {
                        Props = new Dictionary<string, string>(n.Props, StringComparer.OrdinalIgnoreCase)
                    })
                    .ToList(),
                Edges = Edges.Where(e => keep.Contains(e.FromId) && keep.Contains(e.ToId))
                    .Select(e => new PluginEdge(e.FromId, e.ToId, e.Kind))
                    .ToList()
            };
            return g;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Plain retrieval DTOs. The SDK collector fills these (no SDK types leak here) so the builder,
    // rules and emitters stay SDK-free and unit-testable. A secure-configuration VALUE is never
    // carried on any DTO — only a boolean "uses secure config" flag.
    // ---------------------------------------------------------------------------------------------

    public sealed class PluginAssemblyInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string IsolationMode { get; set; }   // "Sandbox" / "None" / "External"
        public bool IsManaged { get; set; }
        public string OwningSolution { get; set; }   // friendly name, may be null
    }

    public sealed class PluginTypeInfo
    {
        public string Id { get; set; }
        public string AssemblyId { get; set; }
        public string TypeName { get; set; }
        public string FriendlyName { get; set; }
        public bool IsManaged { get; set; }
    }

    public sealed class PluginStepInfo
    {
        public string Id { get; set; }
        public string TypeId { get; set; }
        public string Name { get; set; }
        public string MessageName { get; set; }
        public string PrimaryEntity { get; set; }        // table logical name; "" for global/none
        public string Stage { get; set; }                // PreValidation / PreOperation / PostOperation
        public string Mode { get; set; }                 // Synchronous / Asynchronous
        public int Rank { get; set; }
        public string FilteringAttributes { get; set; }
        public string ImpersonatingUser { get; set; }
        public string State { get; set; }                // Enabled / Disabled
        public string SupportedDeployment { get; set; }  // ServerOnly / Offline / Both
        public bool IsManaged { get; set; }
        /// <summary>Flag only — the value is NEVER retrieved or carried.</summary>
        public bool UsesSecureConfig { get; set; }
        public bool UsesUnsecureConfig { get; set; }
        /// <summary>Redacted preview of the unsecure config (secrets masked). Never a secure value.</summary>
        public string UnsecureConfigRedacted { get; set; }
        public string OwningSolution { get; set; }
    }

    public sealed class PluginImageInfo
    {
        public string Id { get; set; }
        public string StepId { get; set; }
        public string Name { get; set; }
        public string ImageType { get; set; }   // PreImage / PostImage / Both
        public string Attributes { get; set; }
    }

    public sealed class CustomApiInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string PluginTypeId { get; set; }  // may be null when no plugin backs the API
        public bool IsManaged { get; set; }
        public string BoundEntity { get; set; }
        public string OwningSolution { get; set; }
    }

    public sealed class PluginSolutionInfo
    {
        public string Id { get; set; }
        public string UniqueName { get; set; }
        public string FriendlyName { get; set; }
        public bool IsManaged { get; set; }
    }

    /// <summary>
    /// Everything retrieved from Dataverse, projected into SDK-free lists. Populated by the collector,
    /// consumed by <see cref="PluginGraphBuilder"/> and <see cref="PluginRiskRules"/>.
    /// </summary>
    public sealed class PluginRegistrationData
    {
        public List<PluginAssemblyInfo> Assemblies { get; set; } = new List<PluginAssemblyInfo>();
        public List<PluginTypeInfo> Types { get; set; } = new List<PluginTypeInfo>();
        public List<PluginStepInfo> Steps { get; set; } = new List<PluginStepInfo>();
        public List<PluginImageInfo> Images { get; set; } = new List<PluginImageInfo>();
        public List<CustomApiInfo> CustomApis { get; set; } = new List<CustomApiInfo>();
        public List<PluginSolutionInfo> Solutions { get; set; } = new List<PluginSolutionInfo>();
        /// <summary>Diagnostic notes (query failures degraded rather than thrown).</summary>
        public List<string> Notes { get; set; } = new List<string>();
    }
}
