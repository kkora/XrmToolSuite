using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.FlowDependencyAnalyzer.Analysis
{
    /// <summary>
    /// Tunable toggles for the flow risk rules. Defaults match the tool's shipped behaviour.
    /// </summary>
    public sealed class FlowRiskOptions
    {
        /// <summary>Flag flows that use a direct connection instead of a connection reference (High). Default on.</summary>
        public bool FlagDirectConnections { get; set; } = true;

        /// <summary>Flag hardcoded environment URLs / GUIDs / table names (Medium). Default on.</summary>
        public bool FlagHardcodedLiterals { get; set; } = true;

        /// <summary>Flag references to missing/deleted metadata (Critical/High). Default on.</summary>
        public bool FlagMissingMetadata { get; set; } = true;

        public static FlowRiskOptions Default => new FlowRiskOptions();
    }

    /// <summary>
    /// Known-good component sets used to detect references to missing/deleted metadata. A <c>null</c> set means
    /// "resolution unavailable" — the rules degrade to an informational note rather than raising false missing
    /// findings (the collector sets these to null when a metadata query fails). Never throws.
    /// </summary>
    public sealed class MissingLookup
    {
        /// <summary>Table logical names that exist in the environment; null = table resolution unavailable.</summary>
        public ISet<string> KnownTables { get; set; }

        /// <summary>Column keys ("table.column" and/or bare "column") that exist; null = column resolution unavailable.</summary>
        public ISet<string> KnownColumns { get; set; }

        public bool TablesResolvable => KnownTables != null;
        public bool ColumnsResolvable => KnownColumns != null;

        public bool TableMissing(string table) =>
            TablesResolvable && !string.IsNullOrEmpty(table) && !KnownTables.Contains(table);

        public bool ColumnMissing(string columnKey)
        {
            if (!ColumnsResolvable || string.IsNullOrEmpty(columnKey)) return false;
            if (KnownColumns.Contains(columnKey)) return false;
            // Accept a match on the bare column name too (parser stores "table.column").
            var dot = columnKey.LastIndexOf('.');
            var bare = dot >= 0 ? columnKey.Substring(dot + 1) : columnKey;
            return !KnownColumns.Contains(bare);
        }

        /// <summary>An empty lookup: nothing resolvable, so no missing-metadata findings are raised.</summary>
        public static MissingLookup Empty => new MissingLookup();
    }

    /// <summary>
    /// Deterministic, SDK-free risk rules over parsed flow dependencies. Produces the findings, and the caller
    /// gets the reverse impact map from <see cref="FlowAnalysis.BuildImpactMap"/>. Category is always "Flow".
    /// A failed lookup degrades to Info; the rules never throw.
    /// </summary>
    public static class FlowRiskRules
    {
        public const string Category = "Flow";

        public static FlowAnalysis Analyze(
            IEnumerable<FlowDependencies> flows,
            ISet<string> knownConnRefs,
            ISet<string> knownEnvVars,
            MissingLookup missing,
            FlowRiskOptions opts = null)
        {
            opts = opts ?? FlowRiskOptions.Default;
            missing = missing ?? MissingLookup.Empty;

            var analysis = new FlowAnalysis();
            analysis.Flows.AddRange(flows ?? Enumerable.Empty<FlowDependencies>());

            foreach (var flow in analysis.Flows)
            {
                try
                {
                    AnalyzeFlow(flow, knownConnRefs, knownEnvVars, missing, opts, analysis.Findings);
                }
                catch (Exception ex)
                {
                    // Never let one flow break the batch — surface as an informational note.
                    analysis.Findings.Add(new Finding(Category, Severity.Info,
                        "Flow could not be fully analyzed",
                        "An unexpected error occurred while applying risk rules to this flow: " + ex.Message,
                        component: flow.FlowName));
                }
            }

            return analysis;
        }

        private static void AnalyzeFlow(
            FlowDependencies flow,
            ISet<string> knownConnRefs,
            ISet<string> knownEnvVars,
            MissingLookup missing,
            FlowRiskOptions opts,
            List<Finding> findings)
        {
            if (!string.IsNullOrEmpty(flow.ParseNote))
            {
                findings.Add(new Finding(Category, Severity.Info,
                    "Flow definition could not be parsed",
                    flow.ParseNote,
                    component: flow.FlowName,
                    recommendation: "Open the flow and confirm its definition is intact; re-export if the clientdata is corrupt."));
            }

            // --- Direct connection usage (portability) → High
            if (opts.FlagDirectConnections && flow.UsesDirectConnection)
            {
                findings.Add(new Finding(Category, Severity.High,
                    "Flow uses a direct connection",
                    $"Flow '{flow.FlowName}' binds a connector to a direct connection instead of a connection reference. " +
                    "Direct connections are not portable and block a clean managed import into another environment.",
                    component: flow.FlowName,
                    recommendation: "Rework the flow to use a connection reference, then re-add it to the solution."));
            }

            // --- Missing connection references → High
            if (opts.FlagMissingMetadata && knownConnRefs != null)
            {
                foreach (var cr in flow.ConnectionReferences.Where(c => !knownConnRefs.Contains(c)))
                {
                    findings.Add(new Finding(Category, Severity.High,
                        "Flow references a missing connection reference",
                        $"Flow '{flow.FlowName}' references connection reference '{cr}', which does not exist in this environment. The flow cannot be turned on.",
                        component: cr,
                        recommendation: $"Create or fix connection reference '{cr}', or edit the flow to point at an existing one."));
                }
            }
            else if (opts.FlagMissingMetadata && knownConnRefs == null && flow.ConnectionReferences.Count > 0)
            {
                findings.Add(new Finding(Category, Severity.Info,
                    "Connection-reference resolution unavailable",
                    $"Connection references could not be listed for this environment, so missing-reference checks were skipped for '{flow.FlowName}'.",
                    component: flow.FlowName));
            }

            // --- Missing environment variables → High
            if (opts.FlagMissingMetadata && knownEnvVars != null)
            {
                foreach (var ev in flow.EnvironmentVariables.Where(e => !knownEnvVars.Contains(e)))
                {
                    findings.Add(new Finding(Category, Severity.High,
                        "Flow references a missing environment variable",
                        $"Flow '{flow.FlowName}' references environment variable '{ev}', which is not defined in this environment.",
                        component: ev,
                        recommendation: $"Add environment variable '{ev}' to the solution (with a value in the target), or remove the reference."));
                }
            }

            // --- Missing tables → Critical / missing columns → High
            if (opts.FlagMissingMetadata)
            {
                if (missing.TablesResolvable)
                {
                    foreach (var table in flow.Tables.Where(missing.TableMissing))
                    {
                        findings.Add(new Finding(Category, Severity.Critical,
                            "Flow references a missing table",
                            $"Flow '{flow.FlowName}' references table '{table}', which does not exist in this environment. The flow will fail at run time.",
                            component: table,
                            recommendation: $"Restore or include table '{table}' in the target, or update the flow to a valid table."));
                    }
                }

                if (missing.ColumnsResolvable)
                {
                    foreach (var col in flow.Columns.Where(missing.ColumnMissing))
                    {
                        findings.Add(new Finding(Category, Severity.High,
                            "Flow references a missing column",
                            $"Flow '{flow.FlowName}' references column '{col}', which does not exist in this environment.",
                            component: col,
                            recommendation: $"Add column '{col}' to the target, or update the flow to a valid column."));
                    }
                }
            }

            // --- Hardcoded literals → Medium (already redacted by the parser)
            if (opts.FlagHardcodedLiterals)
            {
                foreach (var literal in flow.HardcodedLiterals)
                {
                    findings.Add(new Finding(Category, Severity.Medium,
                        "Hardcoded literal in flow definition",
                        $"Flow '{flow.FlowName}' contains a hardcoded literal — {literal}. Hardcoded URLs, GUIDs and table names make a flow environment-specific.",
                        component: flow.FlowName,
                        recommendation: "Move the value into an environment variable so the flow is portable across environments."));
                }
            }
        }
    }
}
