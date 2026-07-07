using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.PluginDependencyGraph.Graph
{
    /// <summary>Tunable thresholds for <see cref="PluginRiskRules"/>. Defaults are deterministic.</summary>
    public sealed class PluginRiskOptions
    {
        /// <summary>An assembly whose distinct table+message fan-out exceeds this is high-impact.</summary>
        public int HighImpactThreshold { get; set; } = 5;
    }

    /// <summary>
    /// Deterministic, SDK-free risk rules over the plugin registration data + graph (category "Plugin"):
    /// high-impact assemblies (large table/message fan-out, ranked), duplicate/overlapping steps
    /// (same message+entity+stage+mode), and unmanaged registrations. Pure — no UI, no Dataverse.
    /// </summary>
    public static class PluginRiskRules
    {
        public const string Category = "Plugin";

        public static List<Finding> Evaluate(PluginGraph g, PluginRegistrationData data, PluginRiskOptions opts = null)
        {
            opts = opts ?? new PluginRiskOptions();
            var findings = new List<Finding>();
            if (data == null) return findings;

            HighImpactAssemblies(data, opts, findings);
            DuplicateSteps(data, findings);
            UnmanagedRegistrations(data, findings);

            return findings
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.Component, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // US-PLUGIN1.4.1 — high-impact assemblies (table/message fan-out over threshold), ranked.
        private static void HighImpactAssemblies(PluginRegistrationData data, PluginRiskOptions opts, List<Finding> findings)
        {
            var typeToAssembly = (data.Types ?? new List<PluginTypeInfo>())
                .Where(t => t.Id != null)
                .ToDictionary(t => t.Id, t => t.AssemblyId, StringComparer.OrdinalIgnoreCase);
            var assemblyById = (data.Assemblies ?? new List<PluginAssemblyInfo>())
                .Where(a => a.Id != null)
                .ToDictionary(a => a.Id, a => a, StringComparer.OrdinalIgnoreCase);

            var byAssembly = new Dictionary<string, (HashSet<string> tables, HashSet<string> messages)>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in data.Steps ?? new List<PluginStepInfo>())
            {
                if (s.TypeId == null || !typeToAssembly.TryGetValue(s.TypeId, out var asmId) || asmId == null) continue;
                if (!byAssembly.TryGetValue(asmId, out var sets))
                {
                    sets = (new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    byAssembly[asmId] = sets;
                }
                if (!string.IsNullOrWhiteSpace(s.PrimaryEntity)) sets.tables.Add(s.PrimaryEntity);
                if (!string.IsNullOrWhiteSpace(s.MessageName)) sets.messages.Add(s.MessageName);
            }

            var ranked = byAssembly
                .Select(kv => new
                {
                    AsmId = kv.Key,
                    Tables = kv.Value.tables.Count,
                    Messages = kv.Value.messages.Count,
                    Fanout = kv.Value.tables.Count + kv.Value.messages.Count
                })
                .Where(x => x.Fanout > opts.HighImpactThreshold)
                .OrderByDescending(x => x.Fanout)
                .ToList();

            int rank = 0;
            foreach (var x in ranked)
            {
                rank++;
                assemblyById.TryGetValue(x.AsmId, out var asm);
                var name = asm?.Name ?? x.AsmId;
                var severity = x.Fanout >= opts.HighImpactThreshold * 2 ? Severity.High : Severity.Medium;
                findings.Add(new Finding(
                    Category, severity,
                    $"High-impact plugin assembly (rank {rank})",
                    $"'{name}' has broad blast radius: {x.Tables} table(s) and {x.Messages} message(s) " +
                    $"(fan-out {x.Fanout}, threshold {opts.HighImpactThreshold}). Changes here affect many operations.",
                    component: name,
                    recommendation: "Review this assembly's registrations before refactoring, merging, or removing it; consider splitting responsibilities."));
            }
        }

        // US-PLUGIN1.4.2 — duplicate/overlapping steps on the same message+entity+stage+mode.
        private static void DuplicateSteps(PluginRegistrationData data, List<Finding> findings)
        {
            var groups = (data.Steps ?? new List<PluginStepInfo>())
                .GroupBy(s => string.Join("|", new[]
                {
                    (s.MessageName ?? "").ToLowerInvariant(),
                    (s.PrimaryEntity ?? "").ToLowerInvariant(),
                    (s.Stage ?? "").ToLowerInvariant(),
                    (s.Mode ?? "").ToLowerInvariant()
                }))
                .Where(grp => grp.Count() > 1);

            foreach (var grp in groups.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var steps = grp.ToList();
                var first = steps[0];
                // High if 3+ overlap or any two share the same execution rank (ordering ambiguity); else Medium.
                bool rankCollision = steps.GroupBy(s => s.Rank).Any(r => r.Count() > 1);
                var severity = (steps.Count >= 3 || rankCollision) ? Severity.High : Severity.Medium;
                var entity = string.IsNullOrWhiteSpace(first.PrimaryEntity) ? "(global)" : first.PrimaryEntity;
                findings.Add(new Finding(
                    Category, severity,
                    "Duplicate / overlapping plugin steps",
                    $"{steps.Count} steps share message '{first.MessageName}' on '{entity}' at {first.Stage}/{first.Mode}" +
                    (rankCollision ? " with a colliding execution rank" : "") +
                    $": {string.Join(", ", steps.Select(s => s.Name))}.",
                    component: $"{first.MessageName} / {entity}",
                    recommendation: "Confirm each step is intentional; remove redundant registrations or set distinct ranks to make ordering explicit."));
            }
        }

        // US-PLUGIN1.4.3 — unmanaged registrations (assemblies + steps) flagged High, naming the component
        // and its owning solution where known.
        private static void UnmanagedRegistrations(PluginRegistrationData data, List<Finding> findings)
        {
            var assemblyById = (data.Assemblies ?? new List<PluginAssemblyInfo>())
                .Where(a => a.Id != null)
                .ToDictionary(a => a.Id, a => a, StringComparer.OrdinalIgnoreCase);
            var typeToAssembly = (data.Types ?? new List<PluginTypeInfo>())
                .Where(t => t.Id != null)
                .ToDictionary(t => t.Id, t => t.AssemblyId, StringComparer.OrdinalIgnoreCase);

            foreach (var a in (data.Assemblies ?? new List<PluginAssemblyInfo>())
                .Where(a => !a.IsManaged)
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                findings.Add(new Finding(
                    Category, Severity.High,
                    "Unmanaged plugin assembly",
                    $"Assembly '{a.Name}' is unmanaged{Owning(a.OwningSolution)}. Unmanaged registrations can be an out-of-process change to a controlled environment.",
                    component: a.Name,
                    recommendation: "Confirm this assembly should be unmanaged in this environment; move it into a managed solution for controlled ALM if appropriate."));
            }

            // Unmanaged steps whose owning assembly is managed (or unknown) — the interesting out-of-band case;
            // steps inside an already-flagged unmanaged assembly are covered by the assembly finding above.
            foreach (var s in (data.Steps ?? new List<PluginStepInfo>())
                .Where(s => !s.IsManaged)
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            {
                bool asmUnmanaged = s.TypeId != null
                    && typeToAssembly.TryGetValue(s.TypeId, out var asmId) && asmId != null
                    && assemblyById.TryGetValue(asmId, out var asm) && !asm.IsManaged;
                if (asmUnmanaged) continue;

                var entity = string.IsNullOrWhiteSpace(s.PrimaryEntity) ? "(global)" : s.PrimaryEntity;
                findings.Add(new Finding(
                    Category, Severity.High,
                    "Unmanaged plugin step",
                    $"Step '{s.Name}' ({s.MessageName} on {entity}) is unmanaged{Owning(s.OwningSolution)}.",
                    component: s.Name,
                    recommendation: "Verify this step belongs here; register it through a managed solution to keep the pipeline under ALM control."));
            }
        }

        private static string Owning(string solution)
            => string.IsNullOrWhiteSpace(solution) ? "" : $" (owning solution: {solution})";
    }
}
