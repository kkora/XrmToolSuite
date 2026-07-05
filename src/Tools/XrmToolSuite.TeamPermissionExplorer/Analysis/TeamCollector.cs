using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Privileges;

namespace XrmToolSuite.TeamPermissionExplorer.Analysis
{
    /// <summary>
    /// Bridges Dataverse to the SDK-free <see cref="TeamProfile"/> / <see cref="TeamRiskRules"/>:
    /// reads teams, memberships, roles, role privileges, and owned-record counts, builds
    /// <see cref="GrantedPrivilege"/>s (depth-mask -&gt; <see cref="AccessScope"/>, ViaTeam = true), then
    /// runs the risk rules per team. Read-only. Every read is via <see cref="QueryExtensions.RetrieveAll"/>
    /// and every per-source failure degrades to an Info finding rather than throwing.
    /// </summary>
    public class TeamCollector
    {
        /// <summary>Small set of common team-ownable tables for the owned-record summary.</summary>
        private static readonly string[] CommonOwnedTables =
            { "account", "contact", "opportunity", "lead", "incident", "task", "phonecall", "email" };

        /// <summary>
        /// Loads every team (optionally filtered to one team type) with members, roles, effective
        /// privileges, owned-record counts, and risk findings.
        /// </summary>
        public List<TeamProfile> Collect(
            IOrganizationService svc,
            string teamTypeFilterOrNull,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var profiles = new List<TeamProfile>();

            progress?.Invoke("Loading teams...");
            var teams = RetrieveTeams(svc, worker, progress);

            // Member counts in one pass over teammembership.
            progress?.Invoke("Counting members...");
            var memberCounts = CountMembers(svc, worker, progress);

            // Team -> role ids, plus a role-id -> name map.
            progress?.Invoke("Loading team roles...");
            var teamRoleIds = LoadTeamRoles(svc, worker, progress);
            var roleIds = teamRoleIds.SelectMany(kv => kv.Value).Distinct().ToList();
            var roleNames = RetrieveNames(svc, "role", roleIds);

            // Role -> granted privileges (name + scope).
            progress?.Invoke("Loading role privileges...");
            var rolePrivileges = LoadRolePrivileges(svc, roleIds, roleNames, worker, progress);

            // Owned-record counts per team across the common tables.
            progress?.Invoke("Counting owned records...");
            var ownedCounts = CountOwnedRecords(svc, worker, progress);

            foreach (var team in teams)
            {
                if (worker?.CancellationPending == true) break;

                var profile = new TeamProfile
                {
                    TeamId = team.Id.ToString(),
                    Name = team.GetAttributeValue<string>("name"),
                    TeamType = MapTeamType(team),
                    BusinessUnit = GetLookupName(team, "businessunitid"),
                    IsDefault = team.GetAttributeValue<bool>("isdefault"),
                    MemberCount = memberCounts.TryGetValue(team.Id, out var mc) ? mc : 0
                };

                if (teamRoleIds.TryGetValue(team.Id, out var rids))
                {
                    foreach (var rid in rids)
                    {
                        if (roleNames.TryGetValue(rid, out var rn) && !string.IsNullOrEmpty(rn))
                            profile.RoleNames.Add(rn);
                        if (rolePrivileges.TryGetValue(rid, out var grants))
                        {
                            // Re-attribute each grant to this team (SourceTeam = team name, ViaTeam = true).
                            foreach (var g in grants)
                            {
                                profile.Grants.Add(new GrantedPrivilege
                                {
                                    PrivilegeName = g.PrivilegeName,
                                    Scope = g.Scope,
                                    SourceRole = g.SourceRole,
                                    SourceTeam = profile.Name,
                                    ViaTeam = true
                                });
                            }
                        }
                    }
                }

                if (ownedCounts.TryGetValue(team.Id, out var owned))
                    profile.OwnedRecordCounts = owned;

                try
                {
                    profile.Findings = TeamRiskRules.Evaluate(profile);
                }
                catch (Exception ex)
                {
                    profile.Findings = new List<Finding>
                    {
                        new Finding(TeamRiskRules.Category, Severity.Info, "Risk evaluation skipped",
                            $"Could not evaluate risk rules for this team: {ex.Message}", profile.Name)
                    };
                }

                profiles.Add(profile);
            }

            if (!string.IsNullOrEmpty(teamTypeFilterOrNull))
                profiles = profiles
                    .Where(p => string.Equals(p.TeamType, teamTypeFilterOrNull, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return profiles
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The users who inherit a team's privileges = its members. Returns (fullname, userId) pairs.
        /// Degrades a read failure to an empty list.
        /// </summary>
        public List<(string User, Guid UserId)> InheritingUsers(
            IOrganizationService svc, Guid teamId, BackgroundWorker worker)
        {
            var result = new List<(string, Guid)>();
            if (svc == null) return result;
            try
            {
                var query = new QueryExpression("teammembership")
                {
                    ColumnSet = new ColumnSet("systemuserid")
                };
                query.Criteria.AddCondition("teamid", ConditionOperator.Equal, teamId);
                var link = query.AddLink("systemuser", "systemuserid", "systemuserid");
                link.EntityAlias = "u";
                link.Columns = new ColumnSet("fullname", "isdisabled");

                foreach (var row in svc.RetrieveAll(query, null, worker))
                {
                    var uid = GetGuid(row, "systemuserid");
                    var name = GetAlias(row, "u.fullname") ?? uid.ToString();
                    result.Add((name, uid));
                }
            }
            catch (Exception)
            {
                // membership is degradable — return whatever we have
            }
            return result
                .OrderBy(r => r.Item1, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ---- reads ------------------------------------------------------------------------------

        private static List<Entity> RetrieveTeams(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            try
            {
                var query = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("name", "teamtype", "businessunitid", "isdefault", "membershiptype")
                };
                query.AddOrder("name", OrderType.Ascending);
                return svc.RetrieveAll(query, null, worker).ToList();
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not read teams: {ex.Message}");
                return new List<Entity>();
            }
        }

        private static Dictionary<Guid, int> CountMembers(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            var counts = new Dictionary<Guid, int>();
            try
            {
                var query = new QueryExpression("teammembership")
                {
                    ColumnSet = new ColumnSet("teamid")
                };
                foreach (var row in svc.RetrieveAll(query, null, worker))
                {
                    var teamId = GetGuid(row, "teamid");
                    if (teamId == Guid.Empty) continue;
                    counts[teamId] = counts.TryGetValue(teamId, out var c) ? c + 1 : 1;
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not count team members: {ex.Message}");
            }
            return counts;
        }

        private static Dictionary<Guid, List<Guid>> LoadTeamRoles(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            var map = new Dictionary<Guid, List<Guid>>();
            try
            {
                var query = new QueryExpression("teamroles")
                {
                    ColumnSet = new ColumnSet("teamid", "roleid")
                };
                foreach (var row in svc.RetrieveAll(query, null, worker))
                {
                    var teamId = GetGuid(row, "teamid");
                    var roleId = GetGuid(row, "roleid");
                    if (teamId == Guid.Empty || roleId == Guid.Empty) continue;
                    if (!map.TryGetValue(teamId, out var list)) map[teamId] = list = new List<Guid>();
                    if (!list.Contains(roleId)) list.Add(roleId);
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not read team roles: {ex.Message}");
            }
            return map;
        }

        private static Dictionary<Guid, List<GrantedPrivilege>> LoadRolePrivileges(
            IOrganizationService svc, List<Guid> roleIds, Dictionary<Guid, string> roleNames,
            BackgroundWorker worker, Action<string> progress)
        {
            var map = new Dictionary<Guid, List<GrantedPrivilege>>();
            if (roleIds == null || roleIds.Count == 0) return map;

            try
            {
                var query = new QueryExpression("roleprivileges")
                {
                    ColumnSet = new ColumnSet("roleid", "privilegedepthmask")
                };
                query.Criteria.AddCondition("roleid", ConditionOperator.In, roleIds.Cast<object>().ToArray());
                var link = query.AddLink("privilege", "privilegeid", "privilegeid");
                link.EntityAlias = "pv";
                link.Columns = new ColumnSet("name");

                foreach (var row in svc.RetrieveAll(query, null, worker))
                {
                    if (worker?.CancellationPending == true) break;
                    var roleId = GetGuid(row, "roleid");
                    var name = GetAlias(row, "pv.name");
                    if (roleId == Guid.Empty || string.IsNullOrEmpty(name)) continue;
                    var mask = GetInt(row, "privilegedepthmask");
                    roleNames.TryGetValue(roleId, out var roleName);

                    if (!map.TryGetValue(roleId, out var list)) map[roleId] = list = new List<GrantedPrivilege>();
                    list.Add(new GrantedPrivilege
                    {
                        PrivilegeName = name,
                        Scope = ScopeFromMask(mask),
                        SourceRole = roleName ?? roleId.ToString(),
                        ViaTeam = true
                    });
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not read role privileges: {ex.Message}");
            }
            return map;
        }

        /// <summary>
        /// Owned-record counts per team via one aggregate/count FetchXML per common table (grouped by
        /// owningteam). A failure on any single table degrades that table to 0 rather than aborting.
        /// </summary>
        private static Dictionary<Guid, Dictionary<string, int>> CountOwnedRecords(
            IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            var result = new Dictionary<Guid, Dictionary<string, int>>();

            foreach (var table in CommonOwnedTables)
            {
                if (worker?.CancellationPending == true) break;
                try
                {
                    var fetch =
                        $"<fetch aggregate='true'>" +
                        $"<entity name='{table}'>" +
                        $"<attribute name='{table}id' alias='cnt' aggregate='count'/>" +
                        $"<attribute name='owningteam' alias='team' groupby='true'/>" +
                        $"</entity></fetch>";

                    var rows = svc.RetrieveMultiple(new FetchExpression(fetch)).Entities;
                    foreach (var row in rows)
                    {
                        var teamId = GetAliasGuid(row, "team");
                        if (teamId == Guid.Empty) continue;
                        var cnt = GetAliasInt(row, "cnt");
                        if (!result.TryGetValue(teamId, out var byTable))
                            result[teamId] = byTable = new Dictionary<string, int>();
                        byTable[table] = cnt;
                    }
                }
                catch (Exception ex)
                {
                    progress?.Invoke($"Owned-record count for '{table}' unavailable (treated as 0): {ex.Message}");
                }
            }

            return result;
        }

        private static Dictionary<Guid, string> RetrieveNames(IOrganizationService svc, string entity, List<Guid> ids)
        {
            var map = new Dictionary<Guid, string>();
            if (ids == null || ids.Count == 0) return map;
            try
            {
                var query = new QueryExpression(entity) { ColumnSet = new ColumnSet("name") };
                query.Criteria.AddCondition(entity + "id", ConditionOperator.In, ids.Distinct().Cast<object>().ToArray());
                foreach (var row in svc.RetrieveAll(query))
                    map[row.Id] = row.GetAttributeValue<string>("name");
            }
            catch (Exception)
            {
                // names are cosmetic
            }
            return map;
        }

        // ---- mapping helpers --------------------------------------------------------------------

        private static string MapTeamType(Entity team)
        {
            // teamtype option set: 0 Owner, 1 Access, 2 AAD security group, 3 AAD office group.
            var os = team.GetAttributeValue<OptionSetValue>("teamtype");
            switch (os?.Value)
            {
                case 0: return team.GetAttributeValue<bool>("isdefault") ? "Default" : "Owner";
                case 1: return "Access";
                case 2: return "AadSecurityGroup";
                case 3: return "AadOfficeGroup";
                default: return os == null ? "Owner" : "Type" + os.Value;
            }
        }

        private static string GetLookupName(Entity e, string attr)
        {
            var r = e.GetAttributeValue<EntityReference>(attr);
            return r?.Name ?? (r != null ? r.Id.ToString() : null);
        }

        private static AccessScope ScopeFromMask(int mask)
        {
            if ((mask & 8) != 0) return AccessScope.Global;
            if ((mask & 4) != 0) return AccessScope.Deep;
            if ((mask & 2) != 0) return AccessScope.Local;
            if ((mask & 1) != 0) return AccessScope.Basic;
            return AccessScope.None;
        }

        private static Guid GetGuid(Entity e, string attr)
        {
            if (!e.Contains(attr) || e[attr] == null) return Guid.Empty;
            var v = e[attr];
            if (v is Guid g) return g;
            if (v is EntityReference r) return r.Id;
            if (v is AliasedValue a && a.Value is Guid ag) return ag;
            return Guid.TryParse(v.ToString(), out var parsed) ? parsed : Guid.Empty;
        }

        private static int GetInt(Entity e, string attr)
        {
            if (!e.Contains(attr) || e[attr] == null) return 0;
            var v = e[attr];
            if (v is int i) return i;
            if (v is AliasedValue a && a.Value is int ai) return ai;
            return int.TryParse(v.ToString(), out var parsed) ? parsed : 0;
        }

        private static string GetAlias(Entity e, string alias)
        {
            if (e.Contains(alias) && e[alias] is AliasedValue a) return a.Value?.ToString();
            return e.GetAttributeValue<string>(alias);
        }

        private static Guid GetAliasGuid(Entity e, string alias)
        {
            if (!e.Contains(alias)) return Guid.Empty;
            if (e[alias] is AliasedValue a)
            {
                if (a.Value is Guid g) return g;
                if (a.Value is EntityReference r) return r.Id;
                return Guid.TryParse(a.Value?.ToString(), out var parsed) ? parsed : Guid.Empty;
            }
            return Guid.Empty;
        }

        private static int GetAliasInt(Entity e, string alias)
        {
            if (e.Contains(alias) && e[alias] is AliasedValue a && a.Value is int i) return i;
            return 0;
        }
    }
}
