using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// New tables without role coverage, secured columns without field security
    /// profiles, and (with a target) roles that grant nobody access.
    /// </summary>
    public class SecurityAnalyzer : IAnalyzer
    {
        public string Name => "Security Impact";
        public AnalyzerCategory Category => AnalyzerCategory.Security;
        public bool BenefitsFromTarget => true;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();

            var entityNames = ctx.SolutionEntityLogicalNames();
            var solutionRoleIds = new HashSet<Guid>(ctx.ComponentIds(AnalyzerContext.CT_Role));

            progress("Security: checking role coverage for solution tables…");
            CheckRoleCoverage(ctx, findings, entityNames, solutionRoleIds);

            progress("Security: checking field security…");
            CheckFieldSecurity(ctx, findings, entityNames);

            if (ctx.HasTarget)
            {
                progress("Security: checking role assignments in target…");
                CheckTargetRoleAssignments(ctx, findings, solutionRoleIds);
            }

            return findings;
        }

        private void CheckRoleCoverage(AnalyzerContext ctx, List<RiskFinding> findings,
            List<string> entityNames, HashSet<Guid> solutionRoleIds)
        {
            foreach (var logical in entityNames)
            {
                var meta = ctx.SourceEntities().FirstOrDefault(e => e.LogicalName == logical);
                if (meta == null || meta.IsCustomEntity != true) continue;
                if (meta.OwnershipType == OwnershipTypes.None) continue; // virtual etc.

                // Privilege names follow prv{Action}{SchemaNameNoUnderscore-ish}; safest generic match:
                // find privileges whose name ends with the entity schema name.
                var schema = meta.SchemaName ?? logical;

                var qe = new QueryExpression("privilege")
                {
                    ColumnSet = new ColumnSet("name"),
                    Criteria = { Conditions = { new ConditionExpression("name", ConditionOperator.EndsWith, schema) } }
                };
                var privIds = AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities.Select(p => p.Id).ToList();
                if (privIds.Count == 0) continue;

                // Which roles grant any of these privileges?
                var rpQe = new QueryExpression("roleprivileges")
                {
                    ColumnSet = new ColumnSet("roleid", "privilegeid"),
                    Criteria = { Conditions = { new ConditionExpression("privilegeid", ConditionOperator.In, privIds.Cast<object>().ToArray()) } }
                };
                var grantingRoles = AnalyzerContext.SafeRetrieve(ctx.Source, rpQe).Entities
                    .Select(rp => rp.GetAttributeValue<Guid>("roleid"))
                    .Distinct()
                    .ToList();

                bool coveredBySolutionRole = grantingRoles.Any(solutionRoleIds.Contains);
                bool coveredByAnyCustomRole = false;

                if (!coveredBySolutionRole && grantingRoles.Count > 0)
                {
                    // Exclude System Administrator/Customizer noise: check for non-system role names.
                    var roleQe = new QueryExpression("role")
                    {
                        ColumnSet = new ColumnSet("name"),
                        Criteria = { Conditions = { new ConditionExpression("roleid", ConditionOperator.In, grantingRoles.Take(200).Cast<object>().ToArray()) } }
                    };
                    coveredByAnyCustomRole = AnalyzerContext.SafeRetrieve(ctx.Source, roleQe).Entities
                        .Select(r => r.GetAttributeValue<string>("name"))
                        .Any(n => n != "System Administrator" && n != "System Customizer");
                }

                if (!coveredBySolutionRole)
                {
                    findings.Add(new RiskFinding(Category,
                        coveredByAnyCustomRole ? Severity.Medium : Severity.High,
                        "Table has no role coverage in this solution",
                        coveredByAnyCustomRole
                            ? $"Table '{logical}' is granted only by roles OUTSIDE this solution. If those roles aren't deployed separately, target users will get 'access denied'."
                            : $"Table '{logical}' is not granted by any custom security role. After import, only admins will see it.",
                        logical,
                        $"Add privileges for '{logical}' to a security role and include that role in the solution (or a companion security solution).",
                        "https://learn.microsoft.com/power-platform/admin/security-roles-privileges"));
                }
            }
        }

        private void CheckFieldSecurity(AnalyzerContext ctx, List<RiskFinding> findings, List<string> entityNames)
        {
            foreach (var logical in entityNames)
            {
                var detail = ctx.GetEntityDetail(logical);
                if (detail?.Attributes == null) continue;

                var securedAttrs = detail.Attributes
                    .Where(a => a.IsSecured == true)
                    .Select(a => a.LogicalName)
                    .ToList();
                if (securedAttrs.Count == 0) continue;

                foreach (var attr in securedAttrs)
                {
                    var qe = new QueryExpression("fieldpermission")
                    {
                        ColumnSet = new ColumnSet("fieldpermissionid"),
                        TopCount = 1,
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("entityname", ConditionOperator.Equal, logical),
                                new ConditionExpression("attributelogicalname", ConditionOperator.Equal, attr)
                            }
                        }
                    };
                    var any = AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities.Any();
                    if (!any)
                    {
                        findings.Add(new RiskFinding(Category, Severity.Medium, "Secured column has no field security profile",
                            $"Column '{logical}.{attr}' is secured (IsSecured=true) but no field permission grants access. All non-admin users will see it masked/blocked.",
                            $"{logical}.{attr}",
                            $"Create/extend a Field Security Profile granting read (and update if needed) on '{attr}', add the profile to the solution, and assign it to teams/users in the target."));
                    }
                }
            }
        }

        private void CheckTargetRoleAssignments(AnalyzerContext ctx, List<RiskFinding> findings, HashSet<Guid> solutionRoleIds)
        {
            if (solutionRoleIds.Count == 0) return;

            // Resolve role names from source, then look up assignment counts by NAME in target
            var roleQe = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("name"),
                Criteria = { Conditions = { new ConditionExpression("roleid", ConditionOperator.In, solutionRoleIds.Take(100).Cast<object>().ToArray()) } }
            };
            var roleNames = AnalyzerContext.SafeRetrieve(ctx.Source, roleQe).Entities
                .Select(r => r.GetAttributeValue<string>("name"))
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct()
                .ToList();

            foreach (var roleName in roleNames)
            {
                // Users holding this role in target
                var userQe = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet("systemuserid"),
                    TopCount = 1
                };
                var roleLink = userQe.AddLink("systemuserroles", "systemuserid", "systemuserid");
                var rLink = roleLink.AddLink("role", "roleid", "roleid");
                rLink.LinkCriteria.AddCondition("name", ConditionOperator.Equal, roleName);

                bool anyUser = AnalyzerContext.SafeRetrieve(ctx.Target, userQe).Entities.Any();

                bool anyTeam = false;
                if (!anyUser)
                {
                    var teamQe = new QueryExpression("team") { ColumnSet = new ColumnSet("teamid"), TopCount = 1 };
                    var tRoles = teamQe.AddLink("teamroles", "teamid", "teamid");
                    var tRole = tRoles.AddLink("role", "roleid", "roleid");
                    tRole.LinkCriteria.AddCondition("name", ConditionOperator.Equal, roleName);
                    anyTeam = AnalyzerContext.SafeRetrieve(ctx.Target, teamQe).Entities.Any();
                }

                if (!anyUser && !anyTeam)
                {
                    findings.Add(new RiskFinding(Category, Severity.Medium, "Role assigned to nobody in target",
                        $"Security role '{roleName}' (in this solution) is not assigned to any user or team in the target. Functionality gated by it will be unusable.",
                        roleName,
                        $"After import, assign '{roleName}' to the appropriate users, teams, or (best practice) an AAD-group team in the target."));
                }
            }
        }
    }
}
