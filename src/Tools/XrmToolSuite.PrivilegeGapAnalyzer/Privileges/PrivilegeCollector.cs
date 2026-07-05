using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Privileges; // effective-privilege model/engine (promoted to shared core)

namespace XrmToolSuite.PrivilegeGapAnalyzer.Privileges
{
    /// <summary>User / Team / Role — the three principal kinds the collector can resolve.</summary>
    public enum PrincipalKind { User, Team, Role }

    /// <summary>
    /// Bridges Dataverse to the SDK-free <see cref="PrivilegeEngine"/>: reads a principal's roles
    /// (direct and team-inherited), the role privileges, and the target table's privilege metadata,
    /// then hands plain <see cref="PrincipalPrivilegeSet"/> / <see cref="EntityPrivilege"/> objects to
    /// the engine. Every read is via <see cref="QueryExtensions.RetrieveAll"/> and query failures
    /// degrade softly (partial data + a progress note) rather than throwing. Read-only — no writes.
    /// </summary>
    public class PrivilegeCollector
    {
        private readonly IOrganizationService _service;

        public PrivilegeCollector(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>Builds the effective privilege set for a user (direct roles + team-role inheritance).</summary>
        public PrincipalPrivilegeSet BuildForUser(Guid userId, BackgroundWorker worker, Action<string> progress)
        {
            var set = new PrincipalPrivilegeSet
            {
                PrincipalType = "User",
                PrincipalName = RetrieveName("systemuser", userId, "fullname") ?? userId.ToString()
            };

            // Direct roles: systemuserroles (systemuserid -> roleid)
            var directRoleIds = LookupIds("systemuserroles", "systemuserid", userId, "roleid", worker, progress,
                "user's directly-assigned roles");

            // Team-inherited roles: teammembership (systemuserid -> teamid) then teamroles (teamid -> roleid)
            var teamIds = LookupIds("teammembership", "systemuserid", userId, "teamid", worker, progress,
                "user's team memberships");
            var teamNameById = RetrieveNames("team", teamIds);
            var teamRolePairs = LookupTeamRoles(teamIds, worker, progress);

            var roleNameById = RetrieveNames("role",
                directRoleIds.Concat(teamRolePairs.Select(p => p.roleId)).Distinct().ToList());

            var grants = new List<GrantedPrivilege>();
            grants.AddRange(CollectRolePrivileges(directRoleIds, roleNameById, null, null, false, worker, progress));

            foreach (var pair in teamRolePairs)
            {
                teamNameById.TryGetValue(pair.teamId, out var teamName);
                grants.AddRange(CollectRolePrivileges(new List<Guid> { pair.roleId }, roleNameById,
                    teamName, pair.teamId, true, worker, progress));
            }

            set.Grants = grants;
            return set;
        }

        /// <summary>Builds the effective privilege set for a team (its roles).</summary>
        public PrincipalPrivilegeSet BuildForTeam(Guid teamId, BackgroundWorker worker, Action<string> progress)
        {
            var set = new PrincipalPrivilegeSet
            {
                PrincipalType = "Team",
                PrincipalName = RetrieveName("team", teamId, "name") ?? teamId.ToString()
            };

            var roleIds = LookupIds("teamroles", "teamid", teamId, "roleid", worker, progress, "team's roles");
            var roleNameById = RetrieveNames("role", roleIds);
            set.Grants = CollectRolePrivileges(roleIds, roleNameById, null, null, false, worker, progress);
            return set;
        }

        /// <summary>Builds the privilege set for a single security role.</summary>
        public PrincipalPrivilegeSet BuildForRole(Guid roleId, BackgroundWorker worker, Action<string> progress)
        {
            var set = new PrincipalPrivilegeSet
            {
                PrincipalType = "Role",
                PrincipalName = RetrieveName("role", roleId, "name") ?? roleId.ToString()
            };

            var roleIds = new List<Guid> { roleId };
            var roleNameById = RetrieveNames("role", roleIds);
            set.Grants = CollectRolePrivileges(roleIds, roleNameById, null, null, false, worker, progress);
            return set;
        }

        /// <summary>Dispatches to the type-specific builder.</summary>
        public PrincipalPrivilegeSet Build(PrincipalKind kind, Guid id, BackgroundWorker worker, Action<string> progress)
        {
            switch (kind)
            {
                case PrincipalKind.Team: return BuildForTeam(id, worker, progress);
                case PrincipalKind.Role: return BuildForRole(id, worker, progress);
                default: return BuildForUser(id, worker, progress);
            }
        }

        /// <summary>
        /// Reads a table's privilege metadata (RetrieveEntity + EntityFilters.Privileges) and maps each
        /// <see cref="SecurityPrivilegeMetadata"/> to the operation it gates and its concrete name.
        /// </summary>
        public EntityPrivilege BuildEntityPrivilege(string entityLogicalName)
        {
            var result = new EntityPrivilege { EntityLogicalName = entityLogicalName };
            try
            {
                var resp = (RetrieveEntityResponse)_service.Execute(new RetrieveEntityRequest
                {
                    LogicalName = entityLogicalName,
                    EntityFilters = EntityFilters.Privileges
                });

                var privileges = resp.EntityMetadata?.Privileges ?? Array.Empty<SecurityPrivilegeMetadata>();
                foreach (var p in privileges)
                {
                    if (!TryMapOperation(p.PrivilegeType, out var op)) continue;
                    result.RequiredPrivilegeNames[op] = p.Name;
                    if (op == CrmOperation.Append || op == CrmOperation.AppendTo)
                        result.CanBeAppended = true;
                }
            }
            catch (Exception)
            {
                // Degrade softly: an empty privilege map makes the engine report MissingPrivilege
                // rather than crashing the analysis.
            }
            return result;
        }

        // ---- helpers ---------------------------------------------------------------------------

        private List<GrantedPrivilege> CollectRolePrivileges(
            List<Guid> roleIds,
            Dictionary<Guid, string> roleNameById,
            string sourceTeam,
            Guid? teamId,
            bool viaTeam,
            BackgroundWorker worker,
            Action<string> progress)
        {
            var grants = new List<GrantedPrivilege>();
            if (roleIds == null || roleIds.Count == 0) return grants;

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

                var rows = _service.RetrieveAll(query, null, worker);
                foreach (var row in rows)
                {
                    if (worker?.CancellationPending == true) break;

                    var roleId = GetGuid(row, "roleid");
                    var mask = GetInt(row, "privilegedepthmask");
                    var name = GetAlias(row, "pv.name");
                    if (string.IsNullOrEmpty(name)) continue;

                    roleNameById.TryGetValue(roleId, out var roleName);
                    grants.Add(new GrantedPrivilege
                    {
                        PrivilegeName = name,
                        Scope = ScopeFromMask(mask),
                        SourceRole = roleName ?? roleId.ToString(),
                        SourceTeam = viaTeam ? sourceTeam : null,
                        ViaTeam = viaTeam
                    });
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not read role privileges: {ex.Message}");
            }

            return grants;
        }

        private List<(Guid teamId, Guid roleId)> LookupTeamRoles(
            List<Guid> teamIds, BackgroundWorker worker, Action<string> progress)
        {
            var pairs = new List<(Guid, Guid)>();
            if (teamIds == null || teamIds.Count == 0) return pairs;

            try
            {
                var query = new QueryExpression("teamroles")
                {
                    ColumnSet = new ColumnSet("teamid", "roleid")
                };
                query.Criteria.AddCondition("teamid", ConditionOperator.In, teamIds.Cast<object>().ToArray());

                foreach (var row in _service.RetrieveAll(query, null, worker))
                {
                    var teamId = GetGuid(row, "teamid");
                    var roleId = GetGuid(row, "roleid");
                    if (roleId != Guid.Empty) pairs.Add((teamId, roleId));
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not read team roles: {ex.Message}");
            }
            return pairs;
        }

        private List<Guid> LookupIds(
            string entity, string filterAttr, Guid filterValue, string idAttr,
            BackgroundWorker worker, Action<string> progress, string what)
        {
            var ids = new List<Guid>();
            try
            {
                var query = new QueryExpression(entity) { ColumnSet = new ColumnSet(idAttr) };
                query.Criteria.AddCondition(filterAttr, ConditionOperator.Equal, filterValue);
                foreach (var row in _service.RetrieveAll(query, null, worker))
                {
                    var id = GetGuid(row, idAttr);
                    if (id != Guid.Empty) ids.Add(id);
                }
            }
            catch (Exception ex)
            {
                progress?.Invoke($"Could not read {what}: {ex.Message}");
            }
            return ids.Distinct().ToList();
        }

        private Dictionary<Guid, string> RetrieveNames(string entity, List<Guid> ids)
        {
            var map = new Dictionary<Guid, string>();
            if (ids == null || ids.Count == 0) return map;

            string nameAttr = entity == "role" ? "name" : entity == "team" ? "name" : "name";
            string idAttr = entity + "id";
            try
            {
                var query = new QueryExpression(entity) { ColumnSet = new ColumnSet(nameAttr) };
                query.Criteria.AddCondition(idAttr, ConditionOperator.In, ids.Distinct().Cast<object>().ToArray());
                foreach (var row in _service.RetrieveAll(query))
                    map[row.Id] = row.GetAttributeValue<string>(nameAttr);
            }
            catch (Exception)
            {
                // names are cosmetic; leave map partial
            }
            return map;
        }

        private string RetrieveName(string entity, Guid id, string nameAttr)
        {
            try
            {
                var e = _service.Retrieve(entity, id, new ColumnSet(nameAttr));
                return e.GetAttributeValue<string>(nameAttr);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static AccessScope ScopeFromMask(int mask)
        {
            if ((mask & 8) != 0) return AccessScope.Global;
            if ((mask & 4) != 0) return AccessScope.Deep;
            if ((mask & 2) != 0) return AccessScope.Local;
            if ((mask & 1) != 0) return AccessScope.Basic;
            return AccessScope.None;
        }

        private static bool TryMapOperation(PrivilegeType type, out CrmOperation op)
        {
            switch (type)
            {
                case PrivilegeType.Create: op = CrmOperation.Create; return true;
                case PrivilegeType.Read: op = CrmOperation.Read; return true;
                case PrivilegeType.Write: op = CrmOperation.Write; return true;
                case PrivilegeType.Delete: op = CrmOperation.Delete; return true;
                case PrivilegeType.Append: op = CrmOperation.Append; return true;
                case PrivilegeType.AppendTo: op = CrmOperation.AppendTo; return true;
                case PrivilegeType.Assign: op = CrmOperation.Assign; return true;
                case PrivilegeType.Share: op = CrmOperation.Share; return true;
                default: op = CrmOperation.Read; return false;
            }
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
    }
}
