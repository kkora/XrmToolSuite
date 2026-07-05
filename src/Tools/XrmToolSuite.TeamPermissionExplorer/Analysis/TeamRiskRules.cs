using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Privileges;

namespace XrmToolSuite.TeamPermissionExplorer.Analysis
{
    /// <summary>
    /// Pure, deterministic, SDK-free risk rules over a <see cref="TeamProfile"/>. Every finding states
    /// the concrete evidence behind it. Category is always "Team". Never touches Dataverse — the
    /// collector does the reads and hands a fully-populated profile in.
    /// </summary>
    public static class TeamRiskRules
    {
        public const string Category = "Team";

        /// <summary>
        /// Evaluates all team-hygiene rules and returns the findings (highest severity first). When no
        /// rule trips, returns a single Info "No team risks detected" finding.
        /// </summary>
        public static List<Finding> Evaluate(TeamProfile t, TeamRiskOptions opts = null)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            opts = opts ?? new TeamRiskOptions();

            var findings = new List<Finding>();
            var component = string.IsNullOrEmpty(t.Name) ? (t.TeamId ?? "(team)") : t.Name;

            bool noMembers = t.MemberCount <= 0;
            bool noRoles = (t.RoleNames == null || t.RoleNames.Count == 0) &&
                           (t.Grants == null || t.Grants.Count == 0);
            bool noOwnedRecords = t.TotalOwnedRecords <= 0;

            // ---- No members (AAD-group teams sync membership from Azure AD, so 0 is expected there) ----
            if (noMembers && !t.IsAadGroupTeam)
            {
                findings.Add(new Finding(
                    Category, Severity.Medium,
                    "Team has no members",
                    $"Team '{component}' has 0 members, so no user currently inherits its {CountEffective(t)} " +
                    "effective privilege(s). An empty non-AAD team grants nothing and is usually stale.",
                    component,
                    "Add members if the team is in use, or remove the team if it is obsolete."));
            }

            // ---- No roles ----
            if (noRoles)
            {
                findings.Add(new Finding(
                    Category, Severity.Medium,
                    "Team has no security roles",
                    $"Team '{component}' has no security roles assigned, so membership confers no privileges. " +
                    "The team cannot own records that require access beyond the default.",
                    component,
                    "Assign a security role to the team, or remove the team if it serves no purpose."));
            }

            // ---- Over-privileged: Deep or Global scope on many privileges ----
            var effective = t.Effective();
            int broad = effective.Values.Count(g => g.Scope == AccessScope.Deep || g.Scope == AccessScope.Global);
            if (broad >= opts.OverPrivilegeTableThreshold)
            {
                findings.Add(new Finding(
                    Category, Severity.High,
                    "Team is over-privileged",
                    $"Team '{component}' holds Deep or Global scope on {broad} privilege(s) " +
                    $"(threshold {opts.OverPrivilegeTableThreshold}). Every member inherits organization-wide or " +
                    "cross-business-unit access, a large blast radius.",
                    component,
                    "Review whether Deep/Global scope is required; prefer Local (Business Unit) or Basic (User) scope."));
            }

            // ---- Duplicate role: same role reaches the team more than once ----
            foreach (var dup in DuplicateRoles(t))
            {
                findings.Add(new Finding(
                    Category, Severity.Low,
                    "Duplicate role assignment",
                    $"Role '{dup.role}' reaches team '{component}' via {dup.count} sources " +
                    $"({string.Join(", ", dup.sources)}). Redundant assignments make privilege review harder.",
                    component,
                    "Consolidate the redundant role assignment so the team gets each role once."));
            }

            // ---- Inactive / orphaned: no members AND no owned records ----
            if (noMembers && noOwnedRecords && !t.IsAadGroupTeam)
            {
                findings.Add(new Finding(
                    Category, Severity.Medium,
                    "Team is inactive / orphaned",
                    $"Team '{component}' has 0 members and owns 0 records across the counted tables. " +
                    "It is a strong candidate for cleanup.",
                    component,
                    "Confirm the team is unused and remove it to reduce the security surface."));
            }

            if (findings.Count == 0)
            {
                findings.Add(new Finding(
                    Category, Severity.Info,
                    "No team risks detected",
                    $"Team '{component}' has members, roles, and no over-privilege, duplicate-role, or " +
                    "orphaned-team indicators.",
                    component));
            }

            return findings.OrderByDescending(f => f.Severity).ToList();
        }

        private static int CountEffective(TeamProfile t) => t.Effective().Count;

        /// <summary>
        /// Roles that reach the team via more than one source. Detected from (a) a role name appearing
        /// more than once in <see cref="TeamProfile.RoleNames"/>, or (b) grants where the same
        /// <see cref="GrantedPrivilege.SourceRole"/> is attributed to more than one distinct SourceTeam.
        /// </summary>
        private static IEnumerable<(string role, int count, List<string> sources)> DuplicateRoles(TeamProfile t)
        {
            var results = new List<(string, int, List<string>)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // (a) duplicate role name in the assigned-roles list
            if (t.RoleNames != null)
            {
                foreach (var g in t.RoleNames
                             .Where(r => !string.IsNullOrEmpty(r))
                             .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
                             .Where(g => g.Count() > 1))
                {
                    if (seen.Add(g.Key))
                        results.Add((g.Key, g.Count(), new List<string> { $"{g.Count()}x in role list" }));
                }
            }

            // (b) same role attributed to multiple teams
            if (t.Grants != null)
            {
                foreach (var g in t.Grants
                             .Where(x => x != null && !string.IsNullOrEmpty(x.SourceRole))
                             .GroupBy(x => x.SourceRole, StringComparer.OrdinalIgnoreCase))
                {
                    var teams = g.Select(x => x.SourceTeam)
                                 .Where(s => !string.IsNullOrEmpty(s))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToList();
                    if (teams.Count > 1 && seen.Add(g.Key))
                        results.Add((g.Key, teams.Count, teams));
                }
            }

            return results;
        }
    }
}
