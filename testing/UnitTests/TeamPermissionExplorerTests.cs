using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Privileges;
using XrmToolSuite.TeamPermissionExplorer.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the SDK-free team risk rules (<see cref="TeamRiskRules"/>) and the
    /// <see cref="TeamProfile.Effective"/> helper, which reuses the shared
    /// <see cref="PrivilegeEngine.ResolveEffective"/>. All rules are pure functions of a populated
    /// <see cref="TeamProfile"/>, so exact verdicts are asserted with no live org.
    /// Traces to EPIC-SEC2 (US-SEC2.4.1 risk findings, US-SEC2.2.2 effective privileges).
    /// </summary>
    public class TeamPermissionExplorerTests
    {
        // ---- fixtures -------------------------------------------------------------------------

        private static GrantedPrivilege Grant(string name, AccessScope scope, string role = "Role A",
            string team = "Team X") => new GrantedPrivilege
            {
                PrivilegeName = name,
                Scope = scope,
                SourceRole = role,
                SourceTeam = team,
                ViaTeam = true
            };

        /// <summary>A healthy team: members, a role, one Basic grant, and an owned record.</summary>
        private static TeamProfile HealthyTeam()
        {
            return new TeamProfile
            {
                TeamId = "t1",
                Name = "Sales Team",
                TeamType = "Owner",
                BusinessUnit = "Contoso",
                MemberCount = 5,
                RoleNames = new List<string> { "Salesperson" },
                Grants = new List<GrantedPrivilege> { Grant("prvReadaccount", AccessScope.Basic) },
                OwnedRecordCounts = new Dictionary<string, int> { { "account", 12 } }
            };
        }

        // ---- no members (US-SEC2.4.1) ---------------------------------------------------------

        [Fact]
        public void NoMembers_NonAad_YieldsMedium()
        {
            var t = HealthyTeam();
            t.MemberCount = 0;

            var findings = TeamRiskRules.Evaluate(t);

            var f = findings.Single(x => x.Title == "Team has no members");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        [Fact]
        public void NoMembers_AadGroupTeam_DoesNotFlagEmpty()
        {
            var t = HealthyTeam();
            t.MemberCount = 0;
            t.TeamType = "AadSecurityGroup"; // membership syncs from Azure AD, so 0 is expected

            var findings = TeamRiskRules.Evaluate(t);

            Assert.DoesNotContain(findings, x => x.Title == "Team has no members");
            Assert.DoesNotContain(findings, x => x.Title == "Team is inactive / orphaned");
        }

        // ---- no roles (US-SEC2.4.1) -----------------------------------------------------------

        [Fact]
        public void NoRoles_YieldsMedium()
        {
            var t = HealthyTeam();
            t.RoleNames = new List<string>();
            t.Grants = new List<GrantedPrivilege>();

            var findings = TeamRiskRules.Evaluate(t);

            var f = findings.Single(x => x.Title == "Team has no security roles");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // ---- over-privileged (US-SEC2.4.1) ----------------------------------------------------

        [Fact]
        public void OverPrivileged_ManyDeepOrGlobal_YieldsHigh()
        {
            var t = HealthyTeam();
            // 12 distinct privileges at Global scope — over the default threshold of 10.
            t.Grants = Enumerable.Range(0, 12)
                .Select(i => Grant("prvWrite" + i, AccessScope.Global))
                .ToList();

            var findings = TeamRiskRules.Evaluate(t);

            var f = findings.Single(x => x.Title == "Team is over-privileged");
            Assert.Equal(Severity.High, f.Severity);
        }

        [Fact]
        public void OverPrivileged_RespectsCustomThreshold()
        {
            var t = HealthyTeam();
            t.Grants = Enumerable.Range(0, 4)
                .Select(i => Grant("prvWrite" + i, AccessScope.Deep))
                .ToList();

            var findings = TeamRiskRules.Evaluate(t, new TeamRiskOptions { OverPrivilegeTableThreshold = 3 });

            Assert.Contains(findings, x => x.Title == "Team is over-privileged");
        }

        // ---- duplicate role (US-SEC2.4.1) -----------------------------------------------------

        [Fact]
        public void DuplicateRole_SameRoleViaMultipleTeams_YieldsLow()
        {
            var t = HealthyTeam();
            t.Grants = new List<GrantedPrivilege>
            {
                Grant("prvReadaccount", AccessScope.Basic, role: "Salesperson", team: "Team A"),
                Grant("prvReadaccount", AccessScope.Basic, role: "Salesperson", team: "Team B"),
            };

            var findings = TeamRiskRules.Evaluate(t);

            var f = findings.Single(x => x.Title == "Duplicate role assignment");
            Assert.Equal(Severity.Low, f.Severity);
        }

        [Fact]
        public void DuplicateRole_SameRoleNameListedTwice_YieldsLow()
        {
            var t = HealthyTeam();
            t.RoleNames = new List<string> { "Salesperson", "Salesperson" };

            var findings = TeamRiskRules.Evaluate(t);

            Assert.Contains(findings, x => x.Title == "Duplicate role assignment" && x.Severity == Severity.Low);
        }

        // ---- orphaned (US-SEC2.4.1) -----------------------------------------------------------

        [Fact]
        public void Orphaned_ZeroMembersAndZeroOwned_YieldsMedium()
        {
            var t = HealthyTeam();
            t.MemberCount = 0;
            t.OwnedRecordCounts = new Dictionary<string, int>();

            var findings = TeamRiskRules.Evaluate(t);

            var f = findings.Single(x => x.Title == "Team is inactive / orphaned");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // ---- clean (US-SEC2.4.1) --------------------------------------------------------------

        [Fact]
        public void CleanTeam_YieldsSingleInfoFinding()
        {
            var findings = TeamRiskRules.Evaluate(HealthyTeam());

            var f = Assert.Single(findings);
            Assert.Equal(Severity.Info, f.Severity);
            Assert.Equal("No team risks detected", f.Title);
        }

        // ---- effective privileges reuse the shared engine (US-SEC2.2.2) -----------------------

        [Fact]
        public void Effective_ResolvesDeepestScopePerPrivilege()
        {
            var t = HealthyTeam();
            t.Grants = new List<GrantedPrivilege>
            {
                Grant("prvReadaccount", AccessScope.Basic, role: "Role A"),
                Grant("prvReadaccount", AccessScope.Global, role: "Role B"),
                Grant("prvReadaccount", AccessScope.Local, role: "Role C"),
            };

            var effective = t.Effective();

            Assert.Single(effective);
            Assert.Equal(AccessScope.Global, effective["prvReadaccount"].Scope);
            Assert.Equal("Role B", effective["prvReadaccount"].SourceRole);
        }

        [Fact]
        public void Effective_EmptyGrants_ReturnsEmpty()
        {
            var t = HealthyTeam();
            t.Grants = new List<GrantedPrivilege>();

            Assert.Empty(t.Effective());
        }
    }
}
