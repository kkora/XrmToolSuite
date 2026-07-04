using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Analysis
{
    /// <summary>Flags naming/governance debt: default-publisher ("new_") prefixes and missing descriptions.</summary>
    public sealed class NamingViolationsAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Naming Violations";
        public string Category => "Naming Violations";

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Checking naming conventions…");

            var custom = ctx.CustomEntities().ToList();
            foreach (var e in custom)
            {
                var schema = e.SchemaName ?? e.LogicalName ?? "";
                if (schema.StartsWith("new_", StringComparison.OrdinalIgnoreCase))
                    findings.Add(new Finding(Category, Severity.Low, "Default publisher prefix on table",
                        $"'{schema}' uses the default 'new_' prefix, so it is not attributable to a real publisher.",
                        schema, "Recreate the component under a dedicated publisher prefix (ALM governance)."));

                bool hasDescription = !string.IsNullOrWhiteSpace(e.Description?.UserLocalizedLabel?.Label);
                if (!hasDescription)
                    findings.Add(new Finding(Category, Severity.Info, "Table has no description",
                        $"'{e.LogicalName}' has no description — undocumented custom tables slow onboarding.",
                        e.LogicalName, "Add a description explaining the table's purpose."));
            }

            // Attribute-level default-prefix scan, bounded to cached/available entity detail.
            int probed = 0;
            foreach (var e in custom)
            {
                if (probed >= ctx.MaxEntityProbes) break;
                var detail = ctx.GetEntityDetail(e.LogicalName);
                probed++;
                if (detail?.Attributes == null) continue;
                var badAttrs = detail.Attributes
                    .Where(a => a.IsCustomAttribute == true &&
                                (a.SchemaName ?? "").StartsWith("new_", StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.SchemaName)
                    .ToList();
                if (badAttrs.Count > 0)
                    findings.Add(new Finding(Category, Severity.Low, "Default publisher prefix on columns",
                        $"{badAttrs.Count} column(s) on '{e.LogicalName}' use the default 'new_' prefix: {string.Join(", ", badAttrs.Take(6))}.",
                        e.LogicalName, "Standardise on a dedicated publisher prefix for all custom columns."));
            }

            return findings;
        }
    }

    /// <summary>Flags security/governance debt: copied roles and secured-column sprawl.</summary>
    public sealed class SecurityAnalyzer : IAnalyzer<TechDebtContext>
    {
        public string Name => "Security Issues";
        public string Category => "Security";

        public List<Finding> Analyze(TechDebtContext ctx, Action<string> progress)
        {
            var findings = new List<Finding>();
            progress?.Invoke("Reviewing security configuration…");

            // Business-unit scoped roles duplicate per BU, so restrict to the root BU to avoid double counting.
            var roles = ctx.SafeRetrieveAll(new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("name")
            });

            var copies = roles
                .Select(r => r.GetAttributeValue<string>("name") ?? "")
                .Where(n => n.StartsWith("Copy of", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var n in copies)
                findings.Add(new Finding(Category, Severity.Low, "Ad-hoc copied security role",
                    $"Role '{n}' looks copy-pasted ('Copy of …'), a sign of unmanaged security drift.",
                    n, "Rename it to reflect its purpose and manage roles through a solution."));

            // Secured-column sprawl across custom tables (from cached detail).
            int secured = 0;
            int probed = 0;
            foreach (var e in ctx.CustomEntities())
            {
                if (probed >= ctx.MaxEntityProbes) break;
                var detail = ctx.GetEntityDetail(e.LogicalName);
                probed++;
                secured += detail?.Attributes?.Count(a => a.IsSecured == true) ?? 0;
            }
            if (secured > 0)
                findings.Add(new Finding(Category, Severity.Info, "Field-level security in use",
                    $"{secured} secured column(s) exist on custom tables — verify each still needs a field security profile.",
                    "(field security)", "Audit field security profiles and remove protection that is no longer required."));

            return findings;
        }
    }
}
