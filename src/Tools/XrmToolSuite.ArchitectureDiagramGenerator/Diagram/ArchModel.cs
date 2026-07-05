using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.ArchitectureDiagramGenerator.Diagram
{
    // =====================================================================================
    // SDK-free architecture-diagram model. NOTHING in this file (or DiagramEmitters) references
    // Microsoft.Xrm.Sdk, so the whole model + emitter pipeline is unit-testable with the plain .NET
    // SDK — no Dataverse, no WinForms. The SDK collector (ArchCollector) fills an ArchDiagram from a
    // live solution; the emitters turn it into Mermaid / PlantUML / DOT / Markdown / HTML / JSON.
    // =====================================================================================

    /// <summary>Architectural layer a component sits in (columns / swimlanes in the diagram).</summary>
    public static class ArchLayers
    {
        public const string Apps = "Apps";
        public const string Ui = "UI";
        public const string Automation = "Automation";
        public const string Code = "Code";
        public const string Data = "Data";
        public const string Security = "Security";
        public const string Config = "Configuration";
        public const string Other = "Other";

        /// <summary>Canonical left-to-right ordering used by every layered emitter.</summary>
        public static readonly string[] Order =
        {
            Apps, Ui, Automation, Code, Data, Security, Config, Other
        };

        /// <summary>Sort key for a layer (unknown layers sort last, before nothing).</summary>
        public static int Rank(string layer)
        {
            var i = Array.IndexOf(Order, layer ?? Other);
            return i < 0 ? Order.Length : i;
        }
    }

    /// <summary>
    /// Maps a Dataverse <c>solutioncomponent.componenttype</c> option value to a friendly type label and
    /// the architectural layer it belongs to. Mirrors the Solution Knowledge Graph's type table so the
    /// two tools stay consistent. Unknown types degrade to a generic label in the <see cref="ArchLayers.Other"/> layer.
    /// </summary>
    public static class ComponentCatalog
    {
        private static readonly Dictionary<int, (string label, string layer)> Map =
            new Dictionary<int, (string, string)>
        {
            {1,  ("Table", ArchLayers.Data)},
            {2,  ("Column", ArchLayers.Data)},
            {9,  ("Option Set", ArchLayers.Data)},
            {10, ("Relationship", ArchLayers.Data)},
            {20, ("Security Role", ArchLayers.Security)},
            {26, ("View", ArchLayers.Ui)},
            {29, ("Workflow / Flow", ArchLayers.Automation)},
            {59, ("Chart", ArchLayers.Ui)},
            {60, ("Form", ArchLayers.Ui)},
            {61, ("Web Resource", ArchLayers.Code)},
            {80, ("Model-driven App", ArchLayers.Apps)},
            {90, ("Plugin Type", ArchLayers.Code)},
            {91, ("Plugin Assembly", ArchLayers.Code)},
            {92, ("Plugin Step", ArchLayers.Automation)},
            {300,("Canvas App", ArchLayers.Apps)},
            {380,("Environment Variable", ArchLayers.Config)},
            {381,("Environment Variable Value", ArchLayers.Config)},
        };

        public static string Label(int componentType) =>
            Map.TryGetValue(componentType, out var v) ? v.label : $"Component ({componentType})";

        public static string Layer(int componentType) =>
            Map.TryGetValue(componentType, out var v) ? v.layer : ArchLayers.Other;
    }

    /// <summary>Which layout an emitter should produce.</summary>
    public enum DiagramLayout
    {
        /// <summary>Group nodes into layer subgraphs / swimlanes (the architecture view).</summary>
        Layered,

        /// <summary>Flat directed dependency graph, no grouping.</summary>
        DependencyGraph
    }

    /// <summary>Flow direction for the emitted diagram.</summary>
    public enum DiagramDirection
    {
        LeftToRight,
        TopToBottom
    }

    /// <summary>Emitter options: layout style, direction, and whether to keep unconnected nodes.</summary>
    public sealed class DiagramOptions
    {
        public DiagramLayout Layout { get; set; } = DiagramLayout.Layered;
        public DiagramDirection Direction { get; set; } = DiagramDirection.LeftToRight;

        /// <summary>Drop nodes that have no edge (declutters large dependency graphs). Default keeps them.</summary>
        public bool HideOrphans { get; set; }

        public static DiagramOptions Default() => new DiagramOptions();
    }

    /// <summary>One node — a solution component.</summary>
    public sealed class ArchNode
    {
        /// <summary>Stable key (the component's objectid string); edges reference this.</summary>
        public string Key { get; set; }
        public string Name { get; set; }
        public string TypeLabel { get; set; }
        public string Layer { get; set; }

        public ArchNode() { }
        public ArchNode(string key, string name, string typeLabel, string layer)
        {
            Key = key; Name = name; TypeLabel = typeLabel; Layer = layer;
        }
    }

    /// <summary>One directed edge — <c>from</c> depends on / requires <c>to</c>.</summary>
    public sealed class ArchEdge
    {
        public string FromKey { get; set; }
        public string ToKey { get; set; }
        public string Label { get; set; }

        public ArchEdge() { }
        public ArchEdge(string fromKey, string toKey, string label = "requires")
        {
            FromKey = fromKey; ToKey = toKey; Label = label;
        }
    }

    /// <summary>
    /// The finished, render-agnostic architecture diagram. Every emitter (Mermaid, PlantUML, DOT, Markdown,
    /// HTML, JSON) consumes exactly this shape. SDK-free.
    /// </summary>
    public sealed class ArchDiagram
    {
        public string Title { get; set; }
        public string SolutionName { get; set; }
        public string UniqueName { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public bool IsManaged { get; set; }
        public string BrandingHeader { get; set; }

        public List<ArchNode> Nodes { get; set; } = new List<ArchNode>();
        public List<ArchEdge> Edges { get; set; } = new List<ArchEdge>();

        /// <summary>Advisory lines (permission/degradation notes) — never a hard error.</summary>
        public List<string> Notes { get; set; } = new List<string>();

        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

        public string DisplayTitle =>
            !string.IsNullOrWhiteSpace(Title) ? Title
            : !string.IsNullOrWhiteSpace(SolutionName) ? SolutionName + " — architecture"
            : "Solution architecture";

        /// <summary>Nodes filtered per <paramref name="options"/> (orphan hiding), in stable order.</summary>
        public IReadOnlyList<ArchNode> VisibleNodes(DiagramOptions options)
        {
            var nodes = Nodes ?? new List<ArchNode>();
            if (options == null || !options.HideOrphans) return nodes;

            var connected = new HashSet<string>();
            foreach (var e in Edges ?? new List<ArchEdge>())
            {
                if (e.FromKey != null) connected.Add(e.FromKey);
                if (e.ToKey != null) connected.Add(e.ToKey);
            }
            return nodes.Where(n => connected.Contains(n.Key)).ToList();
        }

        /// <summary>Edges whose endpoints are both visible, in stable order.</summary>
        public IReadOnlyList<ArchEdge> VisibleEdges(DiagramOptions options)
        {
            var keys = new HashSet<string>(VisibleNodes(options).Select(n => n.Key));
            return (Edges ?? new List<ArchEdge>())
                .Where(e => keys.Contains(e.FromKey) && keys.Contains(e.ToKey))
                .ToList();
        }

        /// <summary>Visible layers, canonical order, each with its nodes (name-sorted).</summary>
        public IEnumerable<KeyValuePair<string, List<ArchNode>>> NodesByLayer(DiagramOptions options)
        {
            return VisibleNodes(options)
                .GroupBy(n => n.Layer ?? ArchLayers.Other)
                .OrderBy(g => ArchLayers.Rank(g.Key))
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new KeyValuePair<string, List<ArchNode>>(
                    g.Key,
                    g.OrderBy(n => n.Name ?? "", StringComparer.OrdinalIgnoreCase).ToList()));
        }

        /// <summary>Per-layer counts across the (unfiltered) node set — used for the legend.</summary>
        public IEnumerable<KeyValuePair<string, int>> LayerCounts()
        {
            return (Nodes ?? new List<ArchNode>())
                .GroupBy(n => n.Layer ?? ArchLayers.Other)
                .OrderBy(g => ArchLayers.Rank(g.Key))
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()));
        }
    }
}
