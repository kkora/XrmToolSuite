using System.Collections.Generic;

namespace XrmToolSuite.Core.Privileges
{
    /// <summary>
    /// Dataverse access depth for a single privilege. Order is meaningful: a deeper scope is
    /// strictly MORE access than a shallower one, so <see cref="AccessScope"/> values are directly
    /// comparable (<c>Deep &gt; Local &gt; Basic</c>). <see cref="None"/> means the privilege is absent.
    /// Maps to the classic depth wording: Basic = User, Local = Business Unit, Deep = Parent:Child BUs,
    /// Global = Organization.
    /// </summary>
    public enum AccessScope
    {
        None = 0,
        Basic = 1,   // User
        Local = 2,   // Business Unit
        Deep = 3,    // Parent: Child Business Units
        Global = 4   // Organization
    }

    /// <summary>The eight Dataverse operations a privilege can gate.</summary>
    public enum CrmOperation
    {
        Create,
        Read,
        Write,
        Delete,
        Append,
        AppendTo,
        Assign,
        Share
    }

    /// <summary>
    /// One privilege granted to a principal by a specific role (optionally inherited through a team).
    /// Pure data — no Dataverse types — so the engine stays unit-testable without a live org.
    /// </summary>
    public class GrantedPrivilege
    {
        /// <summary>Concrete privilege name, e.g. <c>prvCreateaccount</c>.</summary>
        public string PrivilegeName { get; set; }

        /// <summary>Deepest access level this grant confers.</summary>
        public AccessScope Scope { get; set; }

        /// <summary>Security role that carries the privilege.</summary>
        public string SourceRole { get; set; }

        /// <summary>Team whose membership pulled the role in (null for a directly-assigned role).</summary>
        public string SourceTeam { get; set; }

        /// <summary><c>true</c> when the grant reaches the principal only through a team's role.</summary>
        public bool ViaTeam { get; set; }

        /// <summary>
        /// On a RESOLVED (effective) entry only: the deepest scope this privilege reaches through
        /// DIRECTLY-assigned (non-team) roles — <see cref="AccessScope.None"/> if it is reachable only via
        /// teams. Lets the evaluator decide, for a specific required scope, whether removing team membership
        /// would actually drop the principal below it (true team dependence) vs. leaving them still sufficient.
        /// </summary>
        public AccessScope DirectScope { get; set; }
    }

    /// <summary>The full set of privileges a principal (user, team, or role) effectively holds.</summary>
    public class PrincipalPrivilegeSet
    {
        public string PrincipalName { get; set; }

        /// <summary>"User", "Team", or "Role".</summary>
        public string PrincipalType { get; set; }

        public List<GrantedPrivilege> Grants { get; set; } = new List<GrantedPrivilege>();
    }

    /// <summary>
    /// The privilege metadata for a single table: which concrete privilege name gates each operation.
    /// <para>
    /// Append semantics: to append record A to record B you need <see cref="CrmOperation.Append"/> on A
    /// AND <see cref="CrmOperation.AppendTo"/> on B. The two sides are modelled as two
    /// <see cref="EntityPrivilege"/> instances so the engine can flag the common silent half-gap.
    /// </para>
    /// </summary>
    public class EntityPrivilege
    {
        public string EntityLogicalName { get; set; }

        /// <summary>Operation → concrete privilege name (e.g. Create → prvCreateaccount).</summary>
        public Dictionary<CrmOperation, string> RequiredPrivilegeNames { get; set; }
            = new Dictionary<CrmOperation, string>();

        /// <summary><c>true</c> when the table exposes an Append/AppendTo privilege at all.</summary>
        public bool CanBeAppended { get; set; }
    }

    /// <summary>Classification of a gap verdict — the unambiguous cause of an allow/deny.</summary>
    public enum GapVerdictType
    {
        AccessAllowed,
        MissingPrivilege,
        InsufficientScope,
        TeamInheritanceOnly,
        AppendMismatch,
        BusinessUnitBoundary
    }

    /// <summary>
    /// The result of evaluating one principal against one table+operation: a verdict with the exact
    /// missing privilege / scope shortfall, a paste-able explanation, and a read-only recommendation.
    /// The engine never mutates anything — recommendations are suggestions only.
    /// </summary>
    public class GapVerdict
    {
        public GapVerdictType Type { get; set; }

        public bool Allowed { get; set; }

        public string RequiredPrivilege { get; set; }

        public AccessScope RequiredScope { get; set; }

        public AccessScope HeldScope { get; set; }

        /// <summary>Plain-language cause, safe to paste into a support ticket.</summary>
        public string Explanation { get; set; }

        /// <summary>Suggested fix (which role to grant / which scope to raise). Read-only advice.</summary>
        public string Recommendation { get; set; }

        /// <summary>The grants that produced the held access (for the effective-privilege view).</summary>
        public List<GrantedPrivilege> ContributingGrants { get; set; } = new List<GrantedPrivilege>();
    }
}
