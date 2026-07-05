using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.Core.Privileges
{
    /// <summary>
    /// Pure, deterministic effective-privilege engine. Given a principal's granted privileges and a
    /// table's privilege metadata, it computes the effective access and diagnoses exactly why an
    /// operation is allowed or denied. SDK-free and side-effect-free so it lifts cleanly into a
    /// console/CI check and is fully unit-testable without a live org. It NEVER mutates any input.
    /// </summary>
    public static class PrivilegeEngine
    {
        /// <summary>The deeper of two scopes.</summary>
        public static AccessScope Max(AccessScope x, AccessScope y) => (int)x >= (int)y ? x : y;

        /// <summary>
        /// Unions every grant in <paramref name="set"/> and keeps the DEEPEST scope per privilege name.
        /// The winning entry preserves the role/team that gave the deepest access; its
        /// <see cref="GrantedPrivilege.ViaTeam"/> is <c>true</c> only when EVERY contributing grant for
        /// that privilege is team-inherited (i.e. the principal has it solely because of a team).
        /// </summary>
        public static Dictionary<string, GrantedPrivilege> ResolveEffective(PrincipalPrivilegeSet set)
        {
            var effective = new Dictionary<string, GrantedPrivilege>(StringComparer.OrdinalIgnoreCase);
            if (set?.Grants == null) return effective;

            foreach (var group in set.Grants
                         .Where(g => g != null && !string.IsNullOrEmpty(g.PrivilegeName))
                         .GroupBy(g => g.PrivilegeName, StringComparer.OrdinalIgnoreCase))
            {
                var winner = group
                    .OrderByDescending(g => (int)g.Scope)
                    .ThenBy(g => g.ViaTeam) // prefer a direct grant on a scope tie
                    .First();

                effective[group.Key] = new GrantedPrivilege
                {
                    PrivilegeName = winner.PrivilegeName,
                    Scope = winner.Scope,
                    SourceRole = winner.SourceRole,
                    SourceTeam = winner.SourceTeam,
                    // team-only when there is no direct (non-team) grant for this privilege at all
                    ViaTeam = group.All(g => g.ViaTeam)
                };
            }

            return effective;
        }

        /// <summary>
        /// Evaluates whether <paramref name="set"/> can perform <paramref name="op"/> on
        /// <paramref name="target"/> at (at least) <paramref name="requiredScope"/>. For
        /// <see cref="CrmOperation.Append"/> pass <paramref name="appendToTarget"/> to also check the
        /// related table's AppendTo privilege (append A→B needs Append on A AND AppendTo on B).
        /// Produces a paste-able explanation and a read-only recommendation. Never mutates anything.
        /// </summary>
        public static GapVerdict Evaluate(
            PrincipalPrivilegeSet set,
            EntityPrivilege target,
            CrmOperation op,
            AccessScope requiredScope = AccessScope.Basic,
            EntityPrivilege appendToTarget = null)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var effective = ResolveEffective(set);
            var principal = string.IsNullOrEmpty(set.PrincipalName) ? "The principal" : set.PrincipalName;

            // ---- Append pair: needs Append on A AND AppendTo on B ----
            if (op == CrmOperation.Append && appendToTarget != null)
                return EvaluateAppendPair(effective, principal, target, appendToTarget, requiredScope);

            string requiredName = GetRequired(target, op);

            if (string.IsNullOrEmpty(requiredName))
            {
                return new GapVerdict
                {
                    Type = GapVerdictType.MissingPrivilege,
                    Allowed = false,
                    RequiredPrivilege = null,
                    RequiredScope = requiredScope,
                    HeldScope = AccessScope.None,
                    Explanation = $"Table '{target.EntityLogicalName}' does not expose a {op} privilege, " +
                                  $"so {principal} cannot {op} its records.",
                    Recommendation = $"Confirm that {op} is a supported operation on '{target.EntityLogicalName}'."
                };
            }

            effective.TryGetValue(requiredName, out var held);
            var heldScope = held?.Scope ?? AccessScope.None;

            // ---- Missing entirely ----
            if (held == null || heldScope == AccessScope.None)
            {
                return new GapVerdict
                {
                    Type = GapVerdictType.MissingPrivilege,
                    Allowed = false,
                    RequiredPrivilege = requiredName,
                    RequiredScope = requiredScope,
                    HeldScope = AccessScope.None,
                    Explanation = $"{principal} is denied {op} on '{target.EntityLogicalName}': the required " +
                                  $"privilege '{requiredName}' is not granted by any of the principal's roles.",
                    Recommendation = $"Grant a security role that includes '{requiredName}' at " +
                                     $"{ScopeWord(requiredScope)} access (or deeper)."
                };
            }

            // ---- Present but too shallow ----
            if ((int)heldScope < (int)requiredScope)
            {
                return new GapVerdict
                {
                    Type = GapVerdictType.InsufficientScope,
                    Allowed = false,
                    RequiredPrivilege = requiredName,
                    RequiredScope = requiredScope,
                    HeldScope = heldScope,
                    Explanation = $"{principal} holds '{requiredName}' but only at {ScopeWord(heldScope)} scope; " +
                                  $"{op} on '{target.EntityLogicalName}' requires {ScopeWord(requiredScope)} scope " +
                                  $"(held {ScopeWord(heldScope)} < required {ScopeWord(requiredScope)}).",
                    Recommendation = $"Raise '{requiredName}' to at least {ScopeWord(requiredScope)} on " +
                                     $"'{HeldSourceLabel(held)}', or assign a role that grants it at that depth.",
                    ContributingGrants = { held }
                };
            }

            // ---- Allowed — but flag if it's only there because of a team ----
            if (held.ViaTeam)
            {
                return new GapVerdict
                {
                    Type = GapVerdictType.TeamInheritanceOnly,
                    Allowed = true,
                    RequiredPrivilege = requiredName,
                    RequiredScope = requiredScope,
                    HeldScope = heldScope,
                    Explanation = $"{principal} can {op} '{target.EntityLogicalName}' — but ONLY through team " +
                                  $"'{held.SourceTeam}' (role '{held.SourceRole}'), not a directly-assigned role. " +
                                  $"Removing the team membership would revoke this access.",
                    Recommendation = $"If this access should be permanent, assign a role containing '{requiredName}' " +
                                     $"directly to {principal} instead of relying on team '{held.SourceTeam}'.",
                    ContributingGrants = { held }
                };
            }

            return new GapVerdict
            {
                Type = GapVerdictType.AccessAllowed,
                Allowed = true,
                RequiredPrivilege = requiredName,
                RequiredScope = requiredScope,
                HeldScope = heldScope,
                Explanation = $"{principal} can {op} '{target.EntityLogicalName}': holds '{requiredName}' at " +
                              $"{ScopeWord(heldScope)} scope via role '{held.SourceRole}'.",
                Recommendation = "No change required.",
                ContributingGrants = { held }
            };
        }

        private static GapVerdict EvaluateAppendPair(
            Dictionary<string, GrantedPrivilege> effective,
            string principal,
            EntityPrivilege target,
            EntityPrivilege appendToTarget,
            AccessScope requiredScope)
        {
            string appendName = GetRequired(target, CrmOperation.Append);
            string appendToName = GetRequired(appendToTarget, CrmOperation.AppendTo);

            effective.TryGetValue(appendName ?? string.Empty, out var appendHeld);
            effective.TryGetValue(appendToName ?? string.Empty, out var appendToHeld);

            var appendScope = appendHeld?.Scope ?? AccessScope.None;
            var appendToScope = appendToHeld?.Scope ?? AccessScope.None;

            bool appendOk = !string.IsNullOrEmpty(appendName) && (int)appendScope >= (int)requiredScope;
            bool appendToOk = !string.IsNullOrEmpty(appendToName) && (int)appendToScope >= (int)requiredScope;

            var contributing = new List<GrantedPrivilege>();
            if (appendHeld != null) contributing.Add(appendHeld);
            if (appendToHeld != null) contributing.Add(appendToHeld);

            if (appendOk && appendToOk)
            {
                return new GapVerdict
                {
                    Type = GapVerdictType.AccessAllowed,
                    Allowed = true,
                    RequiredPrivilege = $"{appendName} + {appendToName}",
                    RequiredScope = requiredScope,
                    HeldScope = Max(appendScope, appendToScope),
                    Explanation = $"{principal} can append '{target.EntityLogicalName}' records to " +
                                  $"'{appendToTarget.EntityLogicalName}': holds both '{appendName}' " +
                                  $"({ScopeWord(appendScope)}) and '{appendToName}' ({ScopeWord(appendToScope)}).",
                    Recommendation = "No change required.",
                    ContributingGrants = contributing
                };
            }

            // exactly one side present/sufficient — the classic silent half-gap
            if (appendOk ^ appendToOk)
            {
                string presentSide = appendOk
                    ? $"Append on '{target.EntityLogicalName}' ('{appendName}')"
                    : $"AppendTo on '{appendToTarget.EntityLogicalName}' ('{appendToName}')";
                string missingName = appendOk ? appendToName : appendName;
                string missingSide = appendOk
                    ? $"AppendTo on '{appendToTarget.EntityLogicalName}' ('{appendToName ?? "n/a"}')"
                    : $"Append on '{target.EntityLogicalName}' ('{appendName ?? "n/a"}')";

                return new GapVerdict
                {
                    Type = GapVerdictType.AppendMismatch,
                    Allowed = false,
                    RequiredPrivilege = missingName,
                    RequiredScope = requiredScope,
                    HeldScope = appendOk ? appendScope : appendToScope,
                    Explanation = $"{principal} has {presentSide} but is MISSING the matching {missingSide}. " +
                                  $"Relating '{target.EntityLogicalName}' to '{appendToTarget.EntityLogicalName}' " +
                                  $"needs BOTH sides, so the relate is denied.",
                    Recommendation = $"Grant '{missingName}' at {ScopeWord(requiredScope)} (or deeper) to complete " +
                                     $"the Append/AppendTo pair.",
                    ContributingGrants = contributing
                };
            }

            // neither side sufficient
            return new GapVerdict
            {
                Type = GapVerdictType.MissingPrivilege,
                Allowed = false,
                RequiredPrivilege = $"{appendName} + {appendToName}",
                RequiredScope = requiredScope,
                HeldScope = AccessScope.None,
                Explanation = $"{principal} cannot append '{target.EntityLogicalName}' records to " +
                              $"'{appendToTarget.EntityLogicalName}': neither '{appendName}' (Append) nor " +
                              $"'{appendToName}' (AppendTo) is granted at the required scope.",
                Recommendation = $"Grant both '{appendName}' and '{appendToName}' at " +
                                 $"{ScopeWord(requiredScope)} (or deeper).",
                ContributingGrants = contributing
            };
        }

        /// <summary>
        /// Privileges whose effective scope differs between two principals, including those present in
        /// exactly one of them (the other reported as <see cref="AccessScope.None"/>). Sorted by name.
        /// </summary>
        public static List<(string privilege, AccessScope a, AccessScope b)> Diff(
            PrincipalPrivilegeSet a, PrincipalPrivilegeSet b)
        {
            var ea = ResolveEffective(a);
            var eb = ResolveEffective(b);

            var names = new HashSet<string>(ea.Keys, StringComparer.OrdinalIgnoreCase);
            names.UnionWith(eb.Keys);

            var result = new List<(string, AccessScope, AccessScope)>();
            foreach (var name in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                var sa = ea.TryGetValue(name, out var ga) ? ga.Scope : AccessScope.None;
                var sb = eb.TryGetValue(name, out var gb) ? gb.Scope : AccessScope.None;
                if (sa != sb) result.Add((name, sa, sb));
            }
            return result;
        }

        private static string GetRequired(EntityPrivilege target, CrmOperation op) =>
            target?.RequiredPrivilegeNames != null &&
            target.RequiredPrivilegeNames.TryGetValue(op, out var name)
                ? name
                : null;

        private static string HeldSourceLabel(GrantedPrivilege g) =>
            g == null ? "(unknown role)"
            : g.ViaTeam ? $"team '{g.SourceTeam}' role '{g.SourceRole}'"
            : $"role '{g.SourceRole}'";

        private static string ScopeWord(AccessScope s)
        {
            switch (s)
            {
                case AccessScope.Basic: return "Basic (User)";
                case AccessScope.Local: return "Local (Business Unit)";
                case AccessScope.Deep: return "Deep (Parent: Child BU)";
                case AccessScope.Global: return "Global (Organization)";
                default: return "None";
            }
        }
    }
}
