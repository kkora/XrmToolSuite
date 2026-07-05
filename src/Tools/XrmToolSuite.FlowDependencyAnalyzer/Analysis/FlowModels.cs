using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.FlowDependencyAnalyzer.Analysis
{
    /// <summary>
    /// The static dependency footprint of a single cloud flow, parsed from its <c>clientdata</c> JSON.
    /// Deliberately SDK-free (no <c>Microsoft.Xrm.Sdk</c>) so the parser and rules stay unit-testable and
    /// liftable into a console/CI wrapper. Every HTTP endpoint, SAS/trigger URL and secret is redacted at
    /// parse time — this type never holds a live URL or credential.
    /// </summary>
    public sealed class FlowDependencies
    {
        public string FlowName { get; set; }

        /// <summary>Friendly trigger classification (e.g. "Dataverse", "Scheduled", "Manual/HTTP request").</summary>
        public string TriggerType { get; set; }

        /// <summary>For Dataverse triggers, the logical name of the table that starts the flow.</summary>
        public string TriggerEntity { get; set; }

        /// <summary>For Dataverse triggers, the message (Create / Update / Delete / …).</summary>
        public string TriggerMessage { get; set; }

        /// <summary>Connector ids the flow uses (e.g. <c>shared_commondataserviceforapps</c>).</summary>
        public List<string> Connectors { get; set; } = new List<string>();

        /// <summary>Connection-reference logical names the flow binds to.</summary>
        public List<string> ConnectionReferences { get; set; } = new List<string>();

        /// <summary>Environment-variable schema names referenced by the flow.</summary>
        public List<string> EnvironmentVariables { get; set; } = new List<string>();

        /// <summary>Dataverse tables (logical / set names) the flow reads or writes.</summary>
        public List<string> Tables { get; set; } = new List<string>();

        /// <summary>Dataverse columns the flow reads or writes (from <c>$select</c> / parameters).</summary>
        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>Child-flow references (Workflow / RunFlow actions) by workflow id or name.</summary>
        public List<string> ChildFlows { get; set; } = new List<string>();

        /// <summary>Custom-API invocations (unbound/bound action names).</summary>
        public List<string> CustomApis { get; set; } = new List<string>();

        /// <summary>HTTP action names (external coupling). Endpoint URLs are never stored — redacted.</summary>
        public List<string> HttpActions { get; set; } = new List<string>();

        /// <summary>True when the flow uses a direct connection instead of a connection reference (not portable).</summary>
        public bool UsesDirectConnection { get; set; }

        /// <summary>Hardcoded literals (absolute https URLs, GUIDs, environment URLs) with anything secret redacted.</summary>
        public List<string> HardcodedLiterals { get; set; } = new List<string>();

        // ---- inventory context (populated by the collector, optional for the parser) ----
        public string WorkflowId { get; set; }
        public string Owner { get; set; }
        public string State { get; set; }
        public string Solution { get; set; }

        /// <summary>Set when the clientdata could not be parsed (malformed JSON). Never throws — degrades here.</summary>
        public string ParseNote { get; set; }

        /// <summary>Every distinct component this flow depends on, tagged with its kind, for the reverse impact map.</summary>
        public IEnumerable<FlowComponent> AllComponents()
        {
            foreach (var c in Connectors) yield return new FlowComponent(FlowComponentKind.Connector, c);
            foreach (var c in ConnectionReferences) yield return new FlowComponent(FlowComponentKind.ConnectionReference, c);
            foreach (var c in EnvironmentVariables) yield return new FlowComponent(FlowComponentKind.EnvironmentVariable, c);
            foreach (var c in Tables) yield return new FlowComponent(FlowComponentKind.Table, c);
            foreach (var c in Columns) yield return new FlowComponent(FlowComponentKind.Column, c);
            foreach (var c in ChildFlows) yield return new FlowComponent(FlowComponentKind.ChildFlow, c);
            foreach (var c in CustomApis) yield return new FlowComponent(FlowComponentKind.CustomApi, c);
        }
    }

    public enum FlowComponentKind
    {
        Table,
        Column,
        Connector,
        ConnectionReference,
        EnvironmentVariable,
        ChildFlow,
        CustomApi
    }

    /// <summary>A single dependency edge target: a kind + its name.</summary>
    public sealed class FlowComponent
    {
        public FlowComponentKind Kind { get; set; }
        public string Name { get; set; }

        public FlowComponent() { }
        public FlowComponent(FlowComponentKind kind, string name) { Kind = kind; Name = name; }

        public override string ToString() => $"{Kind}: {Name}";
    }

    /// <summary>The reverse ("impacted flows for this component") view for one component.</summary>
    public sealed class FlowImpact
    {
        public FlowComponentKind Kind { get; set; }
        public string Component { get; set; }
        public List<string> ImpactedFlows { get; set; } = new List<string>();

        public FlowImpact() { }
        public FlowImpact(FlowComponentKind kind, string component)
        {
            Kind = kind;
            Component = component;
        }
    }

    /// <summary>
    /// The full analysis result: every flow's parsed dependencies, the risk findings, and the reverse
    /// component→flows impact map. SDK-free so it is fully unit-testable on <c>clientdata</c> fixtures.
    /// </summary>
    public sealed class FlowAnalysis
    {
        public List<FlowDependencies> Flows { get; set; } = new List<FlowDependencies>();
        public List<Finding> Findings { get; set; } = new List<Finding>();

        /// <summary>Names of every flow that depends on <paramref name="component"/> (case-insensitive, any kind).</summary>
        public List<string> ImpactedFlows(string component)
        {
            if (string.IsNullOrWhiteSpace(component)) return new List<string>();
            return Flows
                .Where(f => f.AllComponents().Any(c =>
                    string.Equals(c.Name, component, StringComparison.OrdinalIgnoreCase)))
                .Select(f => f.FlowName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Names of flows depending on a specific component kind+name (precise reverse lookup).</summary>
        public List<string> ImpactedFlows(FlowComponentKind kind, string component)
        {
            if (string.IsNullOrWhiteSpace(component)) return new List<string>();
            return Flows
                .Where(f => f.AllComponents().Any(c => c.Kind == kind &&
                    string.Equals(c.Name, component, StringComparison.OrdinalIgnoreCase)))
                .Select(f => f.FlowName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Builds the complete reverse impact map: one <see cref="FlowImpact"/> per distinct component.</summary>
        public List<FlowImpact> BuildImpactMap()
        {
            var map = new Dictionary<string, FlowImpact>(StringComparer.OrdinalIgnoreCase);
            foreach (var flow in Flows)
            {
                foreach (var comp in flow.AllComponents())
                {
                    if (string.IsNullOrWhiteSpace(comp.Name)) continue;
                    var key = comp.Kind + "|" + comp.Name;
                    if (!map.TryGetValue(key, out var impact))
                    {
                        impact = new FlowImpact(comp.Kind, comp.Name);
                        map[key] = impact;
                    }
                    if (!impact.ImpactedFlows.Contains(flow.FlowName, StringComparer.OrdinalIgnoreCase))
                        impact.ImpactedFlows.Add(flow.FlowName);
                }
            }
            foreach (var impact in map.Values)
                impact.ImpactedFlows.Sort(StringComparer.OrdinalIgnoreCase);
            return map.Values
                .OrderBy(i => i.Kind.ToString(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Component, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
