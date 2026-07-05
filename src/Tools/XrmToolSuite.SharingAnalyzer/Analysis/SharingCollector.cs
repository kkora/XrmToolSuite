using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.SharingAnalyzer.Analysis
{
    /// <summary>
    /// Bridges Dataverse to the SDK-free <see cref="SharingSummary"/> / <see cref="SharingRiskRules"/>:
    /// scans <c>principalobjectaccess</c> (POA) scoped by table, resolves each principal to a
    /// systemuser (disabled state) or team (membership), decodes the access mask, and builds the summary.
    /// Read-only. POA is large and un-indexed for ad-hoc queries, so the default is a per-table scoped
    /// scan; a full-environment scan requires an explicit opt-in. Every per-table failure degrades to an
    /// Info note rather than throwing, and cancellation is honored between pages and tables.
    /// </summary>
    public class SharingCollector
    {
        /// <summary>
        /// Scans POA for the requested tables and returns a fully-aggregated summary. When
        /// <paramref name="fullScanOptIn"/> is false and no tables are supplied, nothing is scanned
        /// (a note explains why). Set <paramref name="fullScanOptIn"/> to scan every table's POA rows.
        /// </summary>
        public SharingSummary Collect(
            IOrganizationService svc,
            IEnumerable<string> tables,
            bool fullScanOptIn,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));

            var summary = new SharingSummary();
            var requested = (tables ?? Enumerable.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            // Object type code <-> logical name maps (drives per-table scoping and full-scan resolution).
            progress?.Invoke("Reading entity metadata...");
            var (otcToName, nameToOtc) = LoadObjectTypeCodes(svc);

            if (requested.Count == 0 && !fullScanOptIn)
            {
                summary.CollectionNotes.Add(new Finding(
                    SharingRiskRules.Category, Severity.Info, "No tables selected",
                    "Pick one or more tables to scan, or opt in to a full-environment scan. " +
                    "A per-table scoped scan is the default to stay within service-protection limits."));
                return summary;
            }

            // Gather POA rows (raw), then resolve principals in bulk.
            var rows = new List<PoaRow>();

            if (requested.Count > 0)
            {
                summary.ScannedTables.AddRange(requested);
                int i = 0;
                foreach (var table in requested)
                {
                    if (worker?.CancellationPending == true) break;
                    i++;
                    progress?.Invoke($"Scanning shares for '{table}' ({i}/{requested.Count})...");

                    if (!nameToOtc.TryGetValue(table, out var otc))
                    {
                        summary.CollectionNotes.Add(new Finding(
                            SharingRiskRules.Category, Severity.Info, "Table not found",
                            $"Table '{table}' has no object type code in this environment and was skipped.",
                            table));
                        continue;
                    }

                    try
                    {
                        rows.AddRange(ScanTable(svc, table, otc, worker, progress));
                    }
                    catch (Exception ex)
                    {
                        summary.CollectionNotes.Add(new Finding(
                            SharingRiskRules.Category, Severity.Info, "Sharing scan degraded",
                            $"Could not read shares for '{table}': {ex.Message}. Results exclude this table.",
                            table));
                    }
                }
            }
            else // fullScanOptIn with no explicit tables
            {
                summary.ScannedTables.Add("(full environment)");
                try
                {
                    rows.AddRange(ScanAll(svc, otcToName, worker, progress));
                }
                catch (Exception ex)
                {
                    summary.CollectionNotes.Add(new Finding(
                        SharingRiskRules.Category, Severity.Info, "Full sharing scan degraded",
                        $"The full-environment POA scan failed: {ex.Message}.",
                        "(full environment)"));
                }
            }

            progress?.Invoke("Resolving principals...");
            ResolvePrincipals(svc, rows, worker, summary);

            summary.Shares = rows
                .Where(r => r.PrincipalId != Guid.Empty)
                .Select(r => new SharedRecordAccess
                {
                    Table = r.Table,
                    ObjectId = r.ObjectId,
                    PrincipalId = r.PrincipalId,
                    PrincipalName = r.PrincipalName,
                    PrincipalType = r.PrincipalType,
                    PrincipalActive = r.PrincipalActive,
                    AccessMask = r.AccessMask
                })
                .ToList();

            progress?.Invoke($"Collected {summary.TotalShares} share(s) on {summary.DistinctRecords} record(s).");
            return summary;
        }

        // ---- POA scanning ------------------------------------------------------------------------

        private static IEnumerable<PoaRow> ScanTable(
            IOrganizationService svc, string table, int otc, BackgroundWorker worker, Action<string> progress)
        {
            var query = new QueryExpression("principalobjectaccess")
            {
                ColumnSet = new ColumnSet("objectid", "principalid", "accessrightsmask", "objecttypecode")
            };
            query.Criteria.AddCondition("objecttypecode", ConditionOperator.Equal, otc);

            return svc.RetrieveAll(query,
                count => progress?.Invoke($"'{table}': {count} share row(s) so far..."),
                worker).Select(e => MapRow(e, table)).Where(r => r != null);
        }

        private static IEnumerable<PoaRow> ScanAll(
            IOrganizationService svc, Dictionary<int, string> otcToName, BackgroundWorker worker, Action<string> progress)
        {
            var query = new QueryExpression("principalobjectaccess")
            {
                ColumnSet = new ColumnSet("objectid", "principalid", "accessrightsmask", "objecttypecode")
            };

            var result = new List<PoaRow>();
            foreach (var e in svc.RetrieveAll(query,
                         count => progress?.Invoke($"Full scan: {count} share row(s) so far..."), worker))
            {
                var otc = GetInt(e, "objecttypecode");
                otcToName.TryGetValue(otc, out var table);
                var row = MapRow(e, table ?? $"otc:{otc}");
                if (row != null) result.Add(row);
            }
            return result;
        }

        private static PoaRow MapRow(Entity e, string table)
        {
            var principal = e.GetAttributeValue<EntityReference>("principalid");
            if (principal == null || principal.Id == Guid.Empty) return null;

            return new PoaRow
            {
                Table = table,
                ObjectId = GetGuid(e, "objectid"),
                PrincipalId = principal.Id,
                PrincipalLogicalName = principal.LogicalName,
                PrincipalName = principal.Name,
                AccessMask = GetInt(e, "accessrightsmask")
            };
        }

        // ---- principal resolution ----------------------------------------------------------------

        private static void ResolvePrincipals(
            IOrganizationService svc, List<PoaRow> rows, BackgroundWorker worker, SharingSummary summary)
        {
            var userIds = rows
                .Where(r => string.Equals(r.PrincipalLogicalName, "systemuser", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.PrincipalId).Distinct().ToList();
            var teamIds = rows
                .Where(r => string.Equals(r.PrincipalLogicalName, "team", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.PrincipalId).Distinct().ToList();

            var users = ResolveUsers(svc, userIds, worker, summary);
            var teams = ResolveTeams(svc, teamIds, worker, summary);

            foreach (var r in rows)
            {
                if (users.TryGetValue(r.PrincipalId, out var u))
                {
                    r.PrincipalType = "User";
                    r.PrincipalName = u.Name ?? r.PrincipalName ?? r.PrincipalId.ToString();
                    r.PrincipalActive = u.Active;
                }
                else if (teams.TryGetValue(r.PrincipalId, out var t))
                {
                    r.PrincipalType = "Team";
                    r.PrincipalName = t.Name ?? r.PrincipalName ?? r.PrincipalId.ToString();
                    r.PrincipalActive = t.Active;
                }
                else
                {
                    // Unknown principal type — keep it but mark type from the lookup logical name.
                    r.PrincipalType = string.Equals(r.PrincipalLogicalName, "team", StringComparison.OrdinalIgnoreCase)
                        ? "Team" : "User";
                    r.PrincipalName = r.PrincipalName ?? r.PrincipalId.ToString();
                    r.PrincipalActive = true;
                }
            }
        }

        private static Dictionary<Guid, (string Name, bool Active)> ResolveUsers(
            IOrganizationService svc, List<Guid> ids, BackgroundWorker worker, SharingSummary summary)
        {
            var map = new Dictionary<Guid, (string, bool)>();
            if (ids.Count == 0) return map;
            try
            {
                var query = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet("fullname", "isdisabled")
                };
                query.Criteria.AddCondition("systemuserid", ConditionOperator.In, ids.Cast<object>().ToArray());
                foreach (var u in svc.RetrieveAll(query, null, worker))
                {
                    var disabled = u.GetAttributeValue<bool>("isdisabled");
                    map[u.Id] = (u.GetAttributeValue<string>("fullname"), !disabled);
                }
            }
            catch (Exception ex)
            {
                summary.CollectionNotes.Add(new Finding(
                    SharingRiskRules.Category, Severity.Info, "User resolution degraded",
                    $"Could not resolve shared-with users (active state assumed): {ex.Message}."));
            }
            return map;
        }

        private static Dictionary<Guid, (string Name, bool Active)> ResolveTeams(
            IOrganizationService svc, List<Guid> ids, BackgroundWorker worker, SharingSummary summary)
        {
            var map = new Dictionary<Guid, (string, bool)>();
            if (ids.Count == 0) return map;
            try
            {
                var names = new Dictionary<Guid, string>();
                var query = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("name", "teamtype")
                };
                query.Criteria.AddCondition("teamid", ConditionOperator.In, ids.Cast<object>().ToArray());
                var teams = svc.RetrieveAll(query, null, worker);
                foreach (var t in teams)
                    names[t.Id] = t.GetAttributeValue<string>("name");

                var memberCounts = CountTeamMembers(svc, ids, worker);
                foreach (var t in teams)
                {
                    var count = memberCounts.TryGetValue(t.Id, out var c) ? c : 0;
                    // A team is "active" for sharing purposes when it has at least one member.
                    map[t.Id] = (names.TryGetValue(t.Id, out var n) ? n : t.Id.ToString(), count > 0);
                }
            }
            catch (Exception ex)
            {
                summary.CollectionNotes.Add(new Finding(
                    SharingRiskRules.Category, Severity.Info, "Team resolution degraded",
                    $"Could not resolve shared-with teams (active state assumed): {ex.Message}."));
            }
            return map;
        }

        private static Dictionary<Guid, int> CountTeamMembers(
            IOrganizationService svc, List<Guid> teamIds, BackgroundWorker worker)
        {
            var counts = new Dictionary<Guid, int>();
            if (teamIds.Count == 0) return counts;
            var query = new QueryExpression("teammembership")
            {
                ColumnSet = new ColumnSet("teamid")
            };
            query.Criteria.AddCondition("teamid", ConditionOperator.In, teamIds.Cast<object>().ToArray());
            foreach (var m in svc.RetrieveAll(query, null, worker))
            {
                var teamId = GetGuid(m, "teamid");
                if (teamId == Guid.Empty) continue;
                counts[teamId] = counts.TryGetValue(teamId, out var c) ? c + 1 : 1;
            }
            return counts;
        }

        // ---- metadata ----------------------------------------------------------------------------

        /// <summary>Builds OTC-&gt;logical-name and logical-name-&gt;OTC maps for scoping and full-scan resolution.</summary>
        private static (Dictionary<int, string> otcToName, Dictionary<string, int> nameToOtc) LoadObjectTypeCodes(
            IOrganizationService svc)
        {
            var otcToName = new Dictionary<int, string>();
            var nameToOtc = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var response = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = false
            });

            foreach (var m in response.EntityMetadata)
            {
                if (string.IsNullOrEmpty(m.LogicalName) || m.ObjectTypeCode == null) continue;
                otcToName[m.ObjectTypeCode.Value] = m.LogicalName;
                nameToOtc[m.LogicalName] = m.ObjectTypeCode.Value;
            }
            return (otcToName, nameToOtc);
        }

        /// <summary>Tables that can hold record-level shares (owner-based), for the table picker.</summary>
        public static List<(string LogicalName, string Display)> ShareableTables(IOrganizationService svc)
        {
            var response = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = false
            });

            return response.EntityMetadata
                .Where(m => m.ObjectTypeCode != null &&
                            m.OwnershipType == OwnershipTypes.UserOwned &&
                            m.IsValidForAdvancedFind == true)
                .Select(m => (m.LogicalName, m.DisplayName?.UserLocalizedLabel?.Label))
                .OrderBy(x => x.Item1, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ---- value helpers -----------------------------------------------------------------------

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
            if (v is OptionSetValue os) return os.Value;
            if (v is AliasedValue a && a.Value is int ai) return ai;
            return int.TryParse(v.ToString(), out var parsed) ? parsed : 0;
        }

        private sealed class PoaRow
        {
            public string Table;
            public Guid ObjectId;
            public Guid PrincipalId;
            public string PrincipalLogicalName;
            public string PrincipalName;
            public string PrincipalType;
            public bool PrincipalActive = true;
            public int AccessMask;
        }
    }
}
