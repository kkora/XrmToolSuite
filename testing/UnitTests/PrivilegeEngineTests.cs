using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Privileges;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the SDK-free effective-privilege engine
    /// (<see cref="PrivilegeEngine"/> over <see cref="PrivilegeModels"/>). The engine is a pure
    /// function of the granted privileges + table metadata, so exact verdicts are asserted with no
    /// live org. Traces to EPIC-SEC1 (US-SEC1.2.x resolve/deepest scope, US-SEC1.3.x gap
    /// classification, US-SEC1.5.x diff).
    /// </summary>
    public class PrivilegeEngineTests
    {
        // ---- fixtures -------------------------------------------------------------------------

        private static EntityPrivilege Account()
        {
            return new EntityPrivilege
            {
                EntityLogicalName = "account",
                CanBeAppended = true,
                RequiredPrivilegeNames = new Dictionary<CrmOperation, string>
                {
                    { CrmOperation.Create, "prvCreateaccount" },
                    { CrmOperation.Read, "prvReadaccount" },
                    { CrmOperation.Write, "prvWriteaccount" },
                    { CrmOperation.Append, "prvAppendaccount" },
                    { CrmOperation.AppendTo, "prvAppendToaccount" },
                }
            };
        }

        private static EntityPrivilege Contact()
        {
            return new EntityPrivilege
            {
                EntityLogicalName = "contact",
                CanBeAppended = true,
                RequiredPrivilegeNames = new Dictionary<CrmOperation, string>
                {
                    { CrmOperation.Append, "prvAppendcontact" },
                    { CrmOperation.AppendTo, "prvAppendTocontact" },
                }
            };
        }

        private static PrincipalPrivilegeSet Set(string name, params GrantedPrivilege[] grants) =>
            new PrincipalPrivilegeSet { PrincipalName = name, PrincipalType = "User", Grants = grants.ToList() };

        private static GrantedPrivilege Grant(string name, AccessScope scope, string role = "Role A",
            bool viaTeam = false, string team = null) =>
            new GrantedPrivilege { PrivilegeName = name, Scope = scope, SourceRole = role, ViaTeam = viaTeam, SourceTeam = team };

        // ---- ResolveEffective -----------------------------------------------------------------

        // TC-SEC1-RESOLVE-01 (US-SEC1.2.1): duplicate grants collapse to the DEEPEST scope, keeping the winning source.
        [Fact]
        public void ResolveEffective_KeepsDeepestScope_AcrossDuplicateGrants()
        {
            var set = Set("Ann",
                Grant("prvReadaccount", AccessScope.Basic, role: "Salesperson"),
                Grant("prvReadaccount", AccessScope.Global, role: "VP"),
                Grant("prvReadaccount", AccessScope.Local, role: "Manager"));

            var effective = PrivilegeEngine.ResolveEffective(set);

            Assert.Single(effective);
            Assert.Equal(AccessScope.Global, effective["prvReadaccount"].Scope);
            Assert.Equal("VP", effective["prvReadaccount"].SourceRole);
            Assert.False(effective["prvReadaccount"].ViaTeam); // has direct grants
        }

        // TC-SEC1-RESOLVE-02 (US-SEC1.2.1): a privilege granted ONLY through teams is flagged ViaTeam on the resolved entry.
        [Fact]
        public void ResolveEffective_FlagsTeamOnly_WhenNoDirectGrant()
        {
            var set = Set("Ann",
                Grant("prvWriteaccount", AccessScope.Local, role: "Editors", viaTeam: true, team: "Sales Team"),
                Grant("prvWriteaccount", AccessScope.Basic, role: "Editors", viaTeam: true, team: "Ops Team"));

            var effective = PrivilegeEngine.ResolveEffective(set);

            Assert.True(effective["prvWriteaccount"].ViaTeam);
            Assert.Equal(AccessScope.Local, effective["prvWriteaccount"].Scope);
        }

        // Regression: a DEEPER team grant coexisting with a SHALLOWER direct grant — the deepest scope is
        // team-only, so the resolved entry is ViaTeam and records the direct-only scope (US-SEC1.2.1).
        [Fact]
        public void ResolveEffective_DeepestScopeFromTeam_TracksDirectScopeAndViaTeam()
        {
            var set = Set("Ann",
                Grant("prvReadaccount", AccessScope.Basic, role: "Salesperson"),                       // direct, shallow
                Grant("prvReadaccount", AccessScope.Global, role: "Admins", viaTeam: true, team: "IT")); // team, deepest

            var e = PrivilegeEngine.ResolveEffective(set)["prvReadaccount"];
            Assert.Equal(AccessScope.Global, e.Scope);
            Assert.Equal(AccessScope.Basic, e.DirectScope);
            Assert.True(e.ViaTeam); // top scope reachable only via the team
        }

        // Regression: with direct Basic + team Global, a Deep requirement is met only via the team =>
        // TeamInheritanceOnly; but a Basic requirement is met by the direct grant => AccessAllowed (the direct
        // grant must NOT be over-flagged as team-dependent). This is the mixed case the old ViaTeam missed.
        [Fact]
        public void Evaluate_MixedDirectAndTeam_FlagsTeamDependenceOnlyWhenRequiredScopeNeedsTheTeam()
        {
            var set = Set("Ann",
                Grant("prvReadaccount", AccessScope.Basic),
                Grant("prvReadaccount", AccessScope.Global, role: "Admins", viaTeam: true, team: "IT"));

            var deep = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Read, AccessScope.Deep);
            Assert.True(deep.Allowed);
            Assert.Equal(GapVerdictType.TeamInheritanceOnly, deep.Type);

            var basic = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Read, AccessScope.Basic);
            Assert.True(basic.Allowed);
            Assert.Equal(GapVerdictType.AccessAllowed, basic.Type); // direct Basic suffices; not team-dependent
        }

        // Regression: an Append pair where BOTH privileges are held but below the required scope must be
        // InsufficientScope with the real held scope — not MissingPrivilege with a false HeldScope=None.
        [Fact]
        public void Evaluate_AppendPair_BothHeldButShallow_IsInsufficientScopeNotMissing()
        {
            var set = Set("Ann",
                Grant("prvAppendaccount", AccessScope.Basic),
                Grant("prvAppendTocontact", AccessScope.Basic));

            var v = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Append, AccessScope.Deep, Contact());

            Assert.False(v.Allowed);
            Assert.Equal(GapVerdictType.InsufficientScope, v.Type);
            Assert.Equal(AccessScope.Basic, v.HeldScope); // NOT None
        }

        // ---- Evaluate: allow / missing / scope ------------------------------------------------

        // TC-SEC1-EVAL-01 (US-SEC1.3.1): sufficient scope from a direct role => AccessAllowed.
        [Fact]
        public void Evaluate_AccessAllowed_WhenScopeSufficient()
        {
            var set = Set("Ann", Grant("prvReadaccount", AccessScope.Global, role: "VP"));

            var v = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Read, AccessScope.Local);

            Assert.True(v.Allowed);
            Assert.Equal(GapVerdictType.AccessAllowed, v.Type);
            Assert.Equal("prvReadaccount", v.RequiredPrivilege);
        }

        // TC-SEC1-EVAL-02 (US-SEC1.3.1): privilege absent entirely => MissingPrivilege, denied.
        [Fact]
        public void Evaluate_MissingPrivilege_WhenAbsent()
        {
            var set = Set("Ann", Grant("prvReadaccount", AccessScope.Global));

            var v = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Create, AccessScope.Basic);

            Assert.False(v.Allowed);
            Assert.Equal(GapVerdictType.MissingPrivilege, v.Type);
            Assert.Equal("prvCreateaccount", v.RequiredPrivilege);
            Assert.Equal(AccessScope.None, v.HeldScope);
        }

        // TC-SEC1-EVAL-03 (US-SEC1.3.1): held shallower than required => InsufficientScope, held vs required reported.
        [Fact]
        public void Evaluate_InsufficientScope_WhenHeldTooShallow()
        {
            var set = Set("Ann", Grant("prvWriteaccount", AccessScope.Basic, role: "Salesperson"));

            var v = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Write, AccessScope.Deep);

            Assert.False(v.Allowed);
            Assert.Equal(GapVerdictType.InsufficientScope, v.Type);
            Assert.Equal(AccessScope.Basic, v.HeldScope);
            Assert.Equal(AccessScope.Deep, v.RequiredScope);
        }

        // TC-SEC1-EVAL-04 (US-SEC1.3.1): sole grant via team but sufficient => TeamInheritanceOnly, still allowed.
        [Fact]
        public void Evaluate_TeamInheritanceOnly_WhenSoleGrantViaTeam()
        {
            var set = Set("Ann",
                Grant("prvReadaccount", AccessScope.Global, role: "Readers", viaTeam: true, team: "Support Team"));

            var v = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Read, AccessScope.Basic);

            Assert.True(v.Allowed);
            Assert.Equal(GapVerdictType.TeamInheritanceOnly, v.Type);
            Assert.Contains("Support Team", v.Explanation);
        }

        // ---- Evaluate: Append pair ------------------------------------------------------------

        // TC-SEC1-APPEND-01 (US-SEC1.1.2): Append present on A but AppendTo missing on B => AppendMismatch.
        [Fact]
        public void Evaluate_AppendMismatch_WhenAppendPresentButAppendToMissing()
        {
            var set = Set("Ann",
                Grant("prvAppendaccount", AccessScope.Global, role: "Integrator"));
            // no prvAppendTocontact granted

            var v = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Append, AccessScope.Basic, Contact());

            Assert.False(v.Allowed);
            Assert.Equal(GapVerdictType.AppendMismatch, v.Type);
            Assert.Equal("prvAppendTocontact", v.RequiredPrivilege);
        }

        // TC-SEC1-APPEND-02 (US-SEC1.1.2): both sides present => allowed.
        [Fact]
        public void Evaluate_AppendPair_Allowed_WhenBothSidesPresent()
        {
            var set = Set("Ann",
                Grant("prvAppendaccount", AccessScope.Global, role: "Integrator"),
                Grant("prvAppendTocontact", AccessScope.Global, role: "Integrator"));

            var v = PrivilegeEngine.Evaluate(set, Account(), CrmOperation.Append, AccessScope.Basic, Contact());

            Assert.True(v.Allowed);
            Assert.Equal(GapVerdictType.AccessAllowed, v.Type);
        }

        // ---- Diff -----------------------------------------------------------------------------

        // TC-SEC1-DIFF-01 (US-SEC1.5.1): diff highlights differing scopes and present-in-one-only privileges.
        [Fact]
        public void Diff_HighlightsScopeDifferences_AndPresentInOneOnly()
        {
            var a = Set("Ann",
                Grant("prvReadaccount", AccessScope.Global),
                Grant("prvWriteaccount", AccessScope.Local));
            var b = Set("Bob",
                Grant("prvReadaccount", AccessScope.Basic),        // differing scope
                Grant("prvDeleteaccount", AccessScope.Local));     // present only in B

            var diff = PrivilegeEngine.Diff(a, b);

            // prvReadaccount differs; prvWriteaccount only in A; prvDeleteaccount only in B; (writeaccount matched? no)
            Assert.Contains(diff, d => d.privilege == "prvReadaccount" && d.a == AccessScope.Global && d.b == AccessScope.Basic);
            Assert.Contains(diff, d => d.privilege == "prvWriteaccount" && d.a == AccessScope.Local && d.b == AccessScope.None);
            Assert.Contains(diff, d => d.privilege == "prvDeleteaccount" && d.a == AccessScope.None && d.b == AccessScope.Local);
            Assert.Equal(3, diff.Count);
        }

        // TC-SEC1-MAX-01: Max returns the deeper scope.
        [Fact]
        public void Max_ReturnsDeeperScope()
        {
            Assert.Equal(AccessScope.Deep, PrivilegeEngine.Max(AccessScope.Basic, AccessScope.Deep));
            Assert.Equal(AccessScope.Global, PrivilegeEngine.Max(AccessScope.Global, AccessScope.Local));
        }
    }
}
