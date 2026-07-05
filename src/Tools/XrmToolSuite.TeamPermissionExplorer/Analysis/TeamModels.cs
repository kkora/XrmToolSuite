using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Privileges;

namespace XrmToolSuite.TeamPermissionExplorer.Analysis
{
    /// <summary>
    /// A single team's security profile: identity, membership, assigned roles, the effective
    /// privileges those roles confer, owned-record counts, and the risk findings the rules produce.
    /// SDK-free (no Microsoft.Xrm.Sdk) so the risk logic stays unit-testable without a live org.
    /// Reuses the shared <see cref="GrantedPrivilege"/> / <see cref="PrivilegeEngine"/> instead of
    /// re-deriving effective privileges.
    /// </summary>
    public class TeamProfile
    {
        /// <summary>Team id (string form so the model carries no SDK Guid dependency).</summary>
        public string TeamId { get; set; }

        public string Name { get; set; }

        /// <summary>Owner / Access / AadSecurityGroup / AadOfficeGroup / Default.</summary>
        public string TeamType { get; set; }

        public string BusinessUnit { get; set; }

        public int MemberCount { get; set; }

        /// <summary>Names of the security roles assigned directly to the team.</summary>
        public List<string> RoleNames { get; set; } = new List<string>();

        /// <summary>The privileges the team's roles grant (SourceTeam = team name, ViaTeam = true).</summary>
        public List<GrantedPrivilege> Grants { get; set; } = new List<GrantedPrivilege>();

        /// <summary>Table logical name -&gt; count of records this team owns (common tables only).</summary>
        public Dictionary<string, int> OwnedRecordCounts { get; set; } = new Dictionary<string, int>();

        /// <summary>Default business-unit team (auto-created, cannot be deleted).</summary>
        public bool IsDefault { get; set; }

        /// <summary>Risk findings produced by <see cref="TeamRiskRules.Evaluate"/>.</summary>
        public List<Finding> Findings { get; set; } = new List<Finding>();

        /// <summary>
        /// Resolves the team's effective privileges (deepest scope per privilege) via the shared
        /// engine. Builds a <see cref="PrincipalPrivilegeSet"/> from <see cref="Grants"/> and hands it
        /// to <see cref="PrivilegeEngine.ResolveEffective"/>.
        /// </summary>
        public Dictionary<string, GrantedPrivilege> Effective()
        {
            var set = new PrincipalPrivilegeSet
            {
                PrincipalName = Name,
                PrincipalType = "Team",
                Grants = Grants ?? new List<GrantedPrivilege>()
            };
            return PrivilegeEngine.ResolveEffective(set);
        }

        /// <summary>True for AAD security/office group teams, whose membership syncs from Azure AD.</summary>
        public bool IsAadGroupTeam =>
            string.Equals(TeamType, "AadSecurityGroup", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(TeamType, "AadOfficeGroup", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Total records this team owns across the counted tables.</summary>
        public int TotalOwnedRecords
        {
            get
            {
                var total = 0;
                if (OwnedRecordCounts != null)
                    foreach (var v in OwnedRecordCounts.Values) total += v;
                return total;
            }
        }
    }

    /// <summary>Tunable thresholds for <see cref="TeamRiskRules"/>. Defaults are conservative.</summary>
    public class TeamRiskOptions
    {
        /// <summary>Over-privilege trips when a team holds Deep/Global scope on at least this many privileges.</summary>
        public int OverPrivilegeTableThreshold { get; set; } = 10;
    }
}
