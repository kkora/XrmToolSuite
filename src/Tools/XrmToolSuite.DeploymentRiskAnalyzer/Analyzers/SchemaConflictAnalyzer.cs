using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Compares source solution schema against the TARGET environment:
    /// attribute type mismatches, string-length shrinks, option set value conflicts,
    /// relationship schema-name collisions, and solution version downgrades.
    /// Requires a target connection; degrades to an informational note without one.
    /// </summary>
    public class SchemaConflictAnalyzer : IAnalyzer
    {
        public string Name => "Data Model Conflicts";
        public AnalyzerCategory Category => AnalyzerCategory.SchemaConflicts;
        public bool BenefitsFromTarget => true;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();

            if (!ctx.HasTarget)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Schema conflict check skipped",
                    "No target environment connected — cross-environment schema comparison is not possible.",
                    ctx.SolutionUniqueName, "Connect a target environment (toolbar button) and re-run the analysis."));
                return findings;
            }

            CheckSolutionVersion(ctx, findings, progress);

            var entityNames = ctx.SolutionEntityLogicalNames();
            int i = 0;
            foreach (var logical in entityNames)
            {
                progress($"Schema: comparing '{logical}' ({++i}/{entityNames.Count})…");

                var src = ctx.GetEntityDetail(logical);
                var tgt = ctx.GetEntityDetail(logical, fromTarget: true);
                if (src == null) continue;
                if (tgt == null) continue; // brand-new table — nothing to conflict with

                CompareAttributes(findings, logical, src, tgt);
                CompareRelationships(findings, logical, src, tgt);
            }

            return findings;
        }

        private void CheckSolutionVersion(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Schema: checking solution version in target…");
            var qe = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("version", "ismanaged"),
                TopCount = 1,
                Criteria = { Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, ctx.SolutionUniqueName) } }
            };
            var tgtSol = AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities.FirstOrDefault();
            if (tgtSol == null) return;

            var tgtVersionStr = tgtSol.GetAttributeValue<string>("version");
            if (Version.TryParse(ctx.SolutionVersion, out var srcV) &&
                Version.TryParse(tgtVersionStr, out var tgtV))
            {
                if (srcV <= tgtV)
                {
                    findings.Add(new RiskFinding(Category, Severity.High, "Solution version not incremented",
                        $"Source version {srcV} is not higher than the target's installed version {tgtV}. Import may be blocked or treated as no-op, and 'stage for upgrade' will fail.",
                        ctx.SolutionUniqueName,
                        $"Bump the solution version above {tgtV} before export (semantic scheme recommended: major.minor.build.revision)."));
                }
                else
                {
                    findings.Add(new RiskFinding(Category, Severity.Info, "Managed upgrade will run",
                        $"Target has v{tgtV}; importing v{srcV} performs an upgrade. Components removed from the solution since v{tgtV} will be DELETED from the target when using 'Upgrade' (data on deleted tables/columns is lost).",
                        ctx.SolutionUniqueName,
                        "Review removed components before import; use 'Stage for upgrade' to inspect deletions, and back up data on any tables slated for removal."));
                }
            }

            bool tgtManaged = tgtSol.GetAttributeValue<bool?>("ismanaged") ?? false;
            if (tgtManaged != ctx.SolutionIsManaged)
            {
                findings.Add(new RiskFinding(Category, Severity.Critical, "Managed/unmanaged mismatch",
                    $"Target has this solution as {(tgtManaged ? "MANAGED" : "UNMANAGED")} but you are deploying {(ctx.SolutionIsManaged ? "MANAGED" : "UNMANAGED")}. Import will fail.",
                    ctx.SolutionUniqueName,
                    "Keep the managed state consistent per environment; never flip managed/unmanaged for an installed solution."));
            }
        }

        private void CompareAttributes(List<RiskFinding> findings, string logical, EntityMetadata src, EntityMetadata tgt)
        {
            var tgtAttrs = (tgt.Attributes ?? Array.Empty<AttributeMetadata>())
                .ToDictionary(a => a.LogicalName, a => a, StringComparer.OrdinalIgnoreCase);

            foreach (var sa in src.Attributes ?? Array.Empty<AttributeMetadata>())
            {
                if (!tgtAttrs.TryGetValue(sa.LogicalName, out var ta)) continue; // new column, fine

                // 1) Type mismatch — hard import failure
                if (sa.AttributeType != ta.AttributeType)
                {
                    findings.Add(new RiskFinding(Category, Severity.Critical, "Attribute type mismatch",
                        $"'{logical}.{sa.LogicalName}' is {sa.AttributeType} in source but {ta.AttributeType} in target. Solution import WILL fail.",
                        $"{logical}.{sa.LogicalName}",
                        "Rename the source column (new schema name) or fix the target column to match. Types cannot be changed in place."));
                    continue;
                }

                // 2) String length shrink — import failure / truncation risk
                if (sa is StringAttributeMetadata ss && ta is StringAttributeMetadata ts &&
                    ss.MaxLength.HasValue && ts.MaxLength.HasValue && ss.MaxLength < ts.MaxLength)
                {
                    findings.Add(new RiskFinding(Category, Severity.High, "Column max length reduced",
                        $"'{logical}.{sa.LogicalName}' shrinks from {ts.MaxLength} to {ss.MaxLength} characters. Dataverse blocks reducing length via import.",
                        $"{logical}.{sa.LogicalName}",
                        $"Set the source max length back to ≥ {ts.MaxLength}, or plan a manual migration."));
                }

                // 3) Option set value conflicts
                if (sa is EnumAttributeMetadata se && ta is EnumAttributeMetadata te &&
                    se.OptionSet?.Options != null && te.OptionSet?.Options != null)
                {
                    var tgtOptions = te.OptionSet.Options
                        .Where(o => o.Value.HasValue)
                        .ToDictionary(o => o.Value.Value, o => o.Label?.UserLocalizedLabel?.Label ?? "");

                    foreach (var so in se.OptionSet.Options.Where(o => o.Value.HasValue))
                    {
                        if (tgtOptions.TryGetValue(so.Value.Value, out var tgtLabel))
                        {
                            var srcLabel = so.Label?.UserLocalizedLabel?.Label ?? "";
                            if (!string.Equals(srcLabel, tgtLabel, StringComparison.Ordinal) &&
                                srcLabel.Length > 0 && tgtLabel.Length > 0)
                            {
                                findings.Add(new RiskFinding(Category, Severity.Medium, "Choice value label conflict",
                                    $"'{logical}.{sa.LogicalName}' value {so.Value.Value}: source label '{srcLabel}' vs target label '{tgtLabel}'. Import overwrites the target label — existing reports/automation matching on label may break.",
                                    $"{logical}.{sa.LogicalName}",
                                    "Confirm the label change is intended; prefer matching on value, not label, in automation."));
                            }
                        }
                    }

                    // Values present in target but removed in source → deletion on upgrade
                    var srcValues = new HashSet<int>(se.OptionSet.Options.Where(o => o.Value.HasValue).Select(o => o.Value.Value));
                    foreach (var kvp in tgtOptions.Where(o => !srcValues.Contains(o.Key)))
                    {
                        findings.Add(new RiskFinding(Category, Severity.High, "Choice value removed",
                            $"'{logical}.{sa.LogicalName}' target has value {kvp.Key} ('{kvp.Value}') which no longer exists in source. Managed upgrade deletes it — rows using it end up with an invalid value.",
                            $"{logical}.{sa.LogicalName}",
                            $"Migrate rows off value {kvp.Key} before upgrading, or restore the option in source."));
                    }
                }
            }
        }

        private void CompareRelationships(List<RiskFinding> findings, string logical, EntityMetadata src, EntityMetadata tgt)
        {
            var tgtRels = (tgt.OneToManyRelationships ?? Array.Empty<OneToManyRelationshipMetadata>())
                .Concat(tgt.ManyToOneRelationships ?? Array.Empty<OneToManyRelationshipMetadata>())
                .GroupBy(r => r.SchemaName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var srcRels = (src.OneToManyRelationships ?? Array.Empty<OneToManyRelationshipMetadata>())
                .Concat(src.ManyToOneRelationships ?? Array.Empty<OneToManyRelationshipMetadata>());

            foreach (var sr in srcRels)
            {
                if (sr.SchemaName == null) continue;
                if (!tgtRels.TryGetValue(sr.SchemaName, out var tr)) continue;

                bool sameShape =
                    string.Equals(sr.ReferencedEntity, tr.ReferencedEntity, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(sr.ReferencingEntity, tr.ReferencingEntity, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(sr.ReferencingAttribute, tr.ReferencingAttribute, StringComparison.OrdinalIgnoreCase);

                if (!sameShape)
                {
                    findings.Add(new RiskFinding(Category, Severity.High, "Relationship schema name collision",
                        $"Relationship '{sr.SchemaName}': source is {sr.ReferencingEntity}→{sr.ReferencedEntity} (lookup {sr.ReferencingAttribute}) but target is {tr.ReferencingEntity}→{tr.ReferencedEntity} (lookup {tr.ReferencingAttribute}). Import will fail or corrupt the model.",
                        sr.SchemaName,
                        "Rename the source relationship (new schema name) — relationship shape cannot be changed in place."));
                }
            }

            // N:N collisions
            var tgtNn = (tgt.ManyToManyRelationships ?? Array.Empty<ManyToManyRelationshipMetadata>())
                .Where(r => r.SchemaName != null)
                .ToDictionary(r => r.SchemaName, r => r, StringComparer.OrdinalIgnoreCase);

            foreach (var sr in src.ManyToManyRelationships ?? Array.Empty<ManyToManyRelationshipMetadata>())
            {
                if (sr.SchemaName == null || !tgtNn.TryGetValue(sr.SchemaName, out var tr)) continue;
                bool sameShape =
                    string.Equals(sr.Entity1LogicalName, tr.Entity1LogicalName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(sr.Entity2LogicalName, tr.Entity2LogicalName, StringComparison.OrdinalIgnoreCase);
                if (!sameShape)
                {
                    findings.Add(new RiskFinding(Category, Severity.High, "N:N relationship collision",
                        $"N:N '{sr.SchemaName}': source links {sr.Entity1LogicalName}↔{sr.Entity2LogicalName}, target links {tr.Entity1LogicalName}↔{tr.Entity2LogicalName}.",
                        sr.SchemaName, "Rename the source N:N relationship."));
                }
            }
        }
    }
}
