using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.SolutionKnowledgeGraph.Graph
{
    /// <summary>A node in the dependency graph (a solution component).</summary>
    public sealed class GraphNode
    {
        public string Id { get; set; }       // stable id (component object id)
        public string Type { get; set; }     // friendly type, e.g. "Table", "Form", "Plugin Step"
        public string Label { get; set; }    // display name

        public GraphNode() { }
        public GraphNode(string id, string type, string label) { Id = id; Type = type; Label = label; }
    }

    /// <summary>A directed edge: <see cref="From"/> depends on / requires <see cref="To"/>.</summary>
    public sealed class GraphEdge
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Kind { get; set; }

        public GraphEdge() { }
        public GraphEdge(string from, string to, string kind = null) { From = from; To = to; Kind = kind; }
    }

    /// <summary>
    /// Directed dependency graph with the SDK-free algorithms the tool exposes: dependency tracing
    /// (forward reachability), impact analysis (reverse reachability — "what breaks if I delete this"),
    /// and circular-dependency detection (Tarjan strongly-connected components). Pure and fully
    /// unit-testable; the Dataverse builder populates it, the renderers/exporters consume it.
    /// </summary>
    public sealed class GraphModel
    {
        private readonly Dictionary<string, GraphNode> _nodes = new Dictionary<string, GraphNode>();
        private readonly List<GraphEdge> _edges = new List<GraphEdge>();
        private readonly HashSet<string> _edgeKeys = new HashSet<string>();
        private readonly Dictionary<string, List<string>> _out = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<string>> _in = new Dictionary<string, List<string>>();

        public IReadOnlyCollection<GraphNode> Nodes => _nodes.Values;
        public IReadOnlyList<GraphEdge> Edges => _edges;
        public int NodeCount => _nodes.Count;
        public int EdgeCount => _edges.Count;

        public GraphNode AddNode(string id, string type, string label)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!_nodes.TryGetValue(id, out var node))
            {
                node = new GraphNode(id, type, label);
                _nodes[id] = node;
                _out[id] = new List<string>();
                _in[id] = new List<string>();
            }
            return node;
        }

        /// <summary>Adds a directed edge (From depends on To). Endpoints are auto-created if missing.</summary>
        public void AddEdge(string from, string to, string kind = null)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to) || from == to) return;
            if (!_nodes.ContainsKey(from)) AddNode(from, "Unknown", from);
            if (!_nodes.ContainsKey(to)) AddNode(to, "Unknown", to);
            var key = from + "" + to;
            if (!_edgeKeys.Add(key)) return;
            _edges.Add(new GraphEdge(from, to, kind));
            _out[from].Add(to);
            _in[to].Add(from);
        }

        public GraphNode Node(string id) => _nodes.TryGetValue(id, out var n) ? n : null;
        public IReadOnlyList<string> DirectDependencies(string id) => _out.TryGetValue(id, out var l) ? l : new List<string>();
        public IReadOnlyList<string> DirectDependents(string id) => _in.TryGetValue(id, out var l) ? l : new List<string>();

        /// <summary>All components the node (transitively) depends on — a dependency trace.</summary>
        public HashSet<string> DependencyTrace(string id) => Reach(id, _out);

        /// <summary>All components that (transitively) depend on the node — the deletion impact set.</summary>
        public HashSet<string> Impact(string id) => Reach(id, _in);

        private HashSet<string> Reach(string start, Dictionary<string, List<string>> adj)
        {
            var seen = new HashSet<string>();
            if (start == null || !_nodes.ContainsKey(start)) return seen;
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!adj.TryGetValue(cur, out var next)) continue;
                foreach (var n in next)
                    if (seen.Add(n)) queue.Enqueue(n);
            }
            seen.Remove(start);
            return seen;
        }

        /// <summary>
        /// Circular dependencies as strongly-connected components of size &gt; 1 (Tarjan), plus any
        /// self-loops. Each returned list is one cycle's node ids.
        /// </summary>
        public List<List<string>> Cycles()
        {
            var index = new Dictionary<string, int>();
            var low = new Dictionary<string, int>();
            var onStack = new HashSet<string>();
            var stack = new Stack<string>();
            var result = new List<List<string>>();
            int counter = 0;

            // Iterative Tarjan to avoid stack overflow on large graphs.
            foreach (var start in _nodes.Keys)
            {
                if (index.ContainsKey(start)) continue;
                var work = new Stack<(string node, int childIdx)>();
                work.Push((start, 0));
                while (work.Count > 0)
                {
                    var (v, ci) = work.Pop();
                    if (ci == 0)
                    {
                        index[v] = low[v] = counter++;
                        stack.Push(v); onStack.Add(v);
                    }
                    var children = _out[v];
                    bool recursed = false;
                    for (int i = ci; i < children.Count; i++)
                    {
                        var w = children[i];
                        if (!index.ContainsKey(w))
                        {
                            work.Push((v, i + 1));
                            work.Push((w, 0));
                            recursed = true;
                            break;
                        }
                        else if (onStack.Contains(w))
                        {
                            low[v] = Math.Min(low[v], index[w]);
                        }
                    }
                    if (recursed) continue;

                    if (low[v] == index[v])
                    {
                        var scc = new List<string>();
                        string w;
                        do { w = stack.Pop(); onStack.Remove(w); scc.Add(w); } while (w != v);
                        if (scc.Count > 1) result.Add(scc);
                    }
                    // propagate low-link to parent, if any
                    if (work.Count > 0)
                    {
                        var parent = work.Peek().node;
                        low[parent] = Math.Min(low[parent], low[v]);
                    }
                }
            }

            return result;
        }
    }
}
