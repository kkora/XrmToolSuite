using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace XrmToolSuite.PluginDependencyGraph.Graph
{
    /// <summary>
    /// Projects the SDK-free <see cref="PluginRegistrationData"/> into a <see cref="PluginGraph"/>:
    /// assembly → type → step → image, step → table/message/config, customapi → type, and
    /// solution → member component. Deterministic and unit-testable. NEVER emits a secure-config value
    /// (only a "uses config" flag/edge). BCL-only.
    /// </summary>
    public static class PluginGraphBuilder
    {
        // Stable id prefixes so ids never collide across node types.
        public static string AssemblyId(string id) => "asm:" + id;
        public static string TypeId(string id) => "type:" + id;
        public static string StepId(string id) => "step:" + id;
        public static string ImageId(string id) => "img:" + id;
        public static string TableId(string logical) => "table:" + (logical ?? "").ToLowerInvariant();
        public static string MessageId(string message) => "msg:" + (message ?? "").ToLowerInvariant();
        public static string CustomApiId(string id) => "capi:" + id;
        public static string SolutionId(string uniqueName) => "sol:" + (uniqueName ?? "").ToLowerInvariant();
        public static string ConfigId(string stepId) => "cfg:" + stepId;

        public static PluginGraph Build(PluginRegistrationData data)
        {
            var g = new PluginGraph();
            if (data == null) return g;

            var nodes = new Dictionary<string, PluginNode>(StringComparer.OrdinalIgnoreCase);
            var edgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var edges = new List<PluginEdge>();

            PluginNode Add(string id, PluginNodeType type, string label, bool managed)
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (!nodes.TryGetValue(id, out var n))
                {
                    n = new PluginNode(id, type, label, managed);
                    nodes[id] = n;
                }
                return n;
            }

            void Link(string from, string to, string kind)
            {
                if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return;
                if (!nodes.ContainsKey(from) || !nodes.ContainsKey(to)) return;
                var key = from + "|" + to + "|" + kind;
                if (!edgeKeys.Add(key)) return;
                edges.Add(new PluginEdge(from, to, kind));
            }

            // Solutions (by friendly name → node id, for member linking).
            var solByFriendly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in data.Solutions ?? Enumerable.Empty<PluginSolutionInfo>())
            {
                var id = SolutionId(s.UniqueName);
                var node = Add(id, PluginNodeType.Solution, s.FriendlyName ?? s.UniqueName, s.IsManaged);
                if (node != null)
                {
                    node.Props["uniqueName"] = s.UniqueName ?? "";
                    node.Props["managed"] = s.IsManaged ? "Yes" : "No";
                    if (!string.IsNullOrEmpty(s.FriendlyName)) solByFriendly[s.FriendlyName] = id;
                }
            }

            string SolNodeFor(string owningSolution)
                => (!string.IsNullOrEmpty(owningSolution) && solByFriendly.TryGetValue(owningSolution, out var sid)) ? sid : null;

            // Assemblies.
            foreach (var a in data.Assemblies ?? Enumerable.Empty<PluginAssemblyInfo>())
            {
                var id = AssemblyId(a.Id);
                var node = Add(id, PluginNodeType.Assembly, a.Name, a.IsManaged);
                if (node == null) continue;
                node.Props["version"] = a.Version ?? "";
                node.Props["isolationMode"] = a.IsolationMode ?? "";
                node.Props["managed"] = a.IsManaged ? "Yes" : "No";
                node.Props["solution"] = a.OwningSolution ?? "";
                var sid = SolNodeFor(a.OwningSolution);
                if (sid != null) Link(sid, id, "member");
            }

            // Plugin types.
            foreach (var t in data.Types ?? Enumerable.Empty<PluginTypeInfo>())
            {
                var id = TypeId(t.Id);
                var node = Add(id, PluginNodeType.PluginType, t.FriendlyName ?? t.TypeName, t.IsManaged);
                if (node == null) continue;
                node.Props["typeName"] = t.TypeName ?? "";
                node.Props["managed"] = t.IsManaged ? "Yes" : "No";
                Link(AssemblyId(t.AssemblyId), id, "contains");
            }

            // Steps (+ table / message / config nodes and edges).
            foreach (var s in data.Steps ?? Enumerable.Empty<PluginStepInfo>())
            {
                var id = StepId(s.Id);
                var node = Add(id, PluginNodeType.Step, s.Name, s.IsManaged);
                if (node == null) continue;
                node.Props["message"] = s.MessageName ?? "";
                node.Props["table"] = s.PrimaryEntity ?? "";
                node.Props["stage"] = s.Stage ?? "";
                node.Props["mode"] = s.Mode ?? "";
                node.Props["rank"] = s.Rank.ToString(CultureInfo.InvariantCulture);
                node.Props["filteringAttributes"] = s.FilteringAttributes ?? "";
                node.Props["impersonatingUser"] = s.ImpersonatingUser ?? "";
                node.Props["state"] = s.State ?? "";
                node.Props["supportedDeployment"] = s.SupportedDeployment ?? "";
                node.Props["managed"] = s.IsManaged ? "Yes" : "No";
                node.Props["solution"] = s.OwningSolution ?? "";
                node.Props["usesSecureConfig"] = s.UsesSecureConfig ? "Yes" : "No";
                node.Props["usesUnsecureConfig"] = s.UsesUnsecureConfig ? "Yes" : "No";

                Link(TypeId(s.TypeId), id, "registers");

                if (!string.IsNullOrWhiteSpace(s.PrimaryEntity))
                {
                    var tid = TableId(s.PrimaryEntity);
                    Add(tid, PluginNodeType.Table, s.PrimaryEntity, false);
                    Link(id, tid, "on-table");
                }

                if (!string.IsNullOrWhiteSpace(s.MessageName))
                {
                    var mid = MessageId(s.MessageName);
                    Add(mid, PluginNodeType.Message, s.MessageName, false);
                    Link(id, mid, "on-message");
                }

                if (s.UsesSecureConfig || s.UsesUnsecureConfig)
                {
                    var cid = ConfigId(s.Id);
                    var cnode = Add(cid, PluginNodeType.Config, "config", s.IsManaged);
                    if (cnode != null)
                    {
                        // Flag/edge only. NEVER a secure value; unsecure preview is redacted upstream.
                        cnode.Props["secure"] = s.UsesSecureConfig ? "Yes" : "No";
                        cnode.Props["unsecure"] = s.UsesUnsecureConfig ? "Yes" : "No";
                        cnode.Props["unsecurePreview"] = s.UnsecureConfigRedacted ?? "";
                    }
                    Link(id, cid, "uses-config");
                }

                var sid = SolNodeFor(s.OwningSolution);
                if (sid != null) Link(sid, id, "member");
            }

            // Images.
            foreach (var img in data.Images ?? Enumerable.Empty<PluginImageInfo>())
            {
                var id = ImageId(img.Id);
                var node = Add(id, PluginNodeType.Image, img.Name ?? img.ImageType, false);
                if (node == null) continue;
                node.Props["imageType"] = img.ImageType ?? "";
                node.Props["attributes"] = img.Attributes ?? "";
                Link(StepId(img.StepId), id, "image");
            }

            // Custom APIs.
            foreach (var c in data.CustomApis ?? Enumerable.Empty<CustomApiInfo>())
            {
                var id = CustomApiId(c.Id);
                var node = Add(id, PluginNodeType.CustomApi, c.Name ?? c.UniqueName, c.IsManaged);
                if (node == null) continue;
                node.Props["uniqueName"] = c.UniqueName ?? "";
                node.Props["boundEntity"] = c.BoundEntity ?? "";
                node.Props["managed"] = c.IsManaged ? "Yes" : "No";
                if (!string.IsNullOrEmpty(c.PluginTypeId))
                    Link(id, TypeId(c.PluginTypeId), "implements");
                var sid = SolNodeFor(c.OwningSolution);
                if (sid != null) Link(sid, id, "member");
            }

            // Deterministic ordering.
            g.Nodes = nodes.Values
                .OrderBy(n => (int)n.Type)
                .ThenBy(n => n.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            g.Edges = edges
                .OrderBy(e => e.FromId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.ToId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Kind, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return g;
        }
    }
}
