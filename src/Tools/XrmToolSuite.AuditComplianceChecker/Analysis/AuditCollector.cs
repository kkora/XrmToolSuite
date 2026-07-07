using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.AuditComplianceChecker.Analysis
{
    /// <summary>
    /// Bridges Dataverse to the SDK-free audit models. Reads org/table/column audit settings from
    /// metadata and tallies audit activity from paged <c>audit</c> queries. Read-only. Every read is
    /// paged via <see cref="QueryExtensions.RetrieveAll"/> where applicable, honours
    /// <see cref="BackgroundWorker"/> cancellation, and degrades a failed read to a note rather than
    /// throwing. NOT unit-tested (needs a live org) — keep the compliance logic in
    /// <see cref="AuditComplianceRules"/>.
    /// </summary>
    public class AuditCollector
    {
        /// <summary>Business-day start hour (local, inclusive). Anything earlier counts as after-hours.</summary>
        public int BusinessHourStart { get; set; } = 7;

        /// <summary>Business-day end hour (local, exclusive). Anything at/after counts as after-hours.</summary>
        public int BusinessHourEnd { get; set; } = 19;

        /// <summary>Object type codes treated as security-related for the security-change tally (heuristic).</summary>
        private static readonly HashSet<string> SecurityObjectTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "role", "roleprivileges", "systemuserroles", "teamroles", "teammembership",
                "team", "fieldsecurityprofile", "fieldpermission", "principalobjectaccess",
            };

        /// <summary>
        /// Reads org/table/column audit configuration. Table headers come from
        /// <see cref="RetrieveAllEntitiesRequest"/> (metadata-only); column detail is fetched per
        /// sensitive table via <see cref="RetrieveEntityRequest"/> to bound cost.
        /// </summary>
        public AuditCoverage CollectCoverage(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var coverage = new AuditCoverage();

            // ---- org flag ----
            progress?.Invoke("Reading organization audit setting...");
            try
            {
                var org = svc.RetrieveMultiple(new QueryExpression("organization")
                {
                    ColumnSet = new ColumnSet("isauditenabled"),
                    TopCount = 1
                }).Entities.FirstOrDefault();
                coverage.OrgAuditEnabled = org?.GetAttributeValue<bool>("isauditenabled") ?? false;
            }
            catch (Exception ex)
            {
                coverage.Notes.Add($"Could not read organization.isauditenabled: {ex.Message}");
            }

            // ---- table headers ----
            progress?.Invoke("Retrieving table metadata...");
            EntityMetadata[] all = Array.Empty<EntityMetadata>();
            try
            {
                var resp = (RetrieveAllEntitiesResponse)svc.Execute(new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                });
                all = resp.EntityMetadata ?? Array.Empty<EntityMetadata>();
            }
            catch (Exception ex)
            {
                coverage.Notes.Add($"Could not retrieve table metadata: {ex.Message}");
                return coverage;
            }

            foreach (var md in all)
            {
                if (worker?.CancellationPending == true) break;
                if (md == null || string.IsNullOrEmpty(md.LogicalName)) continue;
                // Skip intersect (N:N) and non-auditable helper tables — they add noise, not coverage signal.
                if (md.IsIntersect == true) continue;

                coverage.Tables.Add(new TableAudit
                {
                    LogicalName = md.LogicalName,
                    DisplayName = md.DisplayName?.UserLocalizedLabel?.Label ?? md.LogicalName,
                    IsManaged = md.IsManaged ?? false,
                    IsAuditEnabled = md.IsAuditEnabled?.Value ?? false,
                    IsSensitive = SensitivityHeuristics.IsSensitiveTable(md.LogicalName)
                });
            }

            // ---- column detail for sensitive tables only (bounds the metadata cost) ----
            var sensitive = coverage.Tables.Where(t => t.IsSensitive).ToList();
            int i = 0;
            foreach (var t in sensitive)
            {
                if (worker?.CancellationPending == true) break;
                i++;
                progress?.Invoke($"Reading columns for sensitive tables ({i}/{sensitive.Count})...");
                try
                {
                    var er = (RetrieveEntityResponse)svc.Execute(new RetrieveEntityRequest
                    {
                        LogicalName = t.LogicalName,
                        EntityFilters = EntityFilters.Attributes,
                        RetrieveAsIfPublished = true
                    });

                    foreach (var attr in er.EntityMetadata?.Attributes ?? Array.Empty<AttributeMetadata>())
                    {
                        if (attr == null || string.IsNullOrEmpty(attr.LogicalName)) continue;
                        if (attr.IsValidForRead == false && attr.IsValidForCreate == false) continue;
                        var type = attr.AttributeType?.ToString();
                        t.Columns.Add(new ColumnAudit
                        {
                            LogicalName = attr.LogicalName,
                            Type = type,
                            IsAuditEnabled = attr.IsAuditEnabled?.Value ?? false,
                            IsSensitive = SensitivityHeuristics.IsSensitiveColumn(attr.LogicalName, type)
                        });
                    }
                }
                catch (Exception ex)
                {
                    coverage.Notes.Add($"Could not read columns for '{t.LogicalName}': {ex.Message}");
                }
            }

            return coverage;
        }

        /// <summary>
        /// Date-scoped, paged tally of audit activity. Optionally filtered to a set of table logical
        /// names (objecttypecode). Tallies by table/user/date, plus deletes, security changes, and
        /// after-hours changes. Heavy — always scope by date; honours cancellation.
        /// </summary>
        public AuditActivitySummary CollectActivity(
            IOrganizationService svc, DateTime fromUtc, DateTime toUtc,
            IEnumerable<string> tables, BackgroundWorker worker, Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            var summary = new AuditActivitySummary { FromUtc = fromUtc, ToUtc = toUtc };

            var tableFilter = (tables ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            progress?.Invoke("Querying audit activity...");
            var query = new QueryExpression("audit")
            {
                ColumnSet = new ColumnSet("action", "operation", "objecttypecode", "createdon", "userid")
            };
            query.Criteria.AddCondition("createdon", ConditionOperator.OnOrAfter, fromUtc);
            query.Criteria.AddCondition("createdon", ConditionOperator.OnOrBefore, toUtc);
            if (tableFilter.Length > 0)
                query.Criteria.AddCondition("objecttypecode", ConditionOperator.In, tableFilter.Cast<object>().ToArray());
            query.AddOrder("createdon", OrderType.Ascending);

            List<Entity> rows;
            try
            {
                rows = svc.RetrieveAll(query, count => progress?.Invoke($"Read {count} audit record(s)..."), worker);
            }
            catch (Exception ex)
            {
                summary.Notes.Add($"Could not query audit activity: {ex.Message}");
                return summary;
            }

            foreach (var row in rows)
            {
                if (worker?.CancellationPending == true) break;
                summary.TotalRecords++;

                var table = row.GetAttributeValue<string>("objecttypecode") ?? "(unknown)";
                Bump(summary.ByTable, table);

                var user = row.GetAttributeValue<EntityReference>("userid");
                Bump(summary.ByUser, user?.Name ?? user?.Id.ToString() ?? "(unknown)");

                var created = row.GetAttributeValue<DateTime>("createdon");
                if (created != default)
                {
                    var local = created.ToLocalTime();
                    Bump(summary.ByDate, local.ToString("yyyy-MM-dd"));
                    if (IsAfterHours(local)) summary.AfterHoursCount++;
                }

                var operation = row.GetAttributeValue<OptionSetValue>("operation")?.Value;
                if (operation == 3) summary.DeleteCount++; // 1 Create, 2 Update, 3 Delete, 4 Access

                if (SecurityObjectTypes.Contains(table)) summary.SecurityChangeCount++;
            }

            return summary;
        }

        private bool IsAfterHours(DateTime local)
        {
            if (local.DayOfWeek == DayOfWeek.Saturday || local.DayOfWeek == DayOfWeek.Sunday) return true;
            return local.Hour < BusinessHourStart || local.Hour >= BusinessHourEnd;
        }

        private static void Bump(IDictionary<string, int> map, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            map[key] = map.TryGetValue(key, out var c) ? c + 1 : 1;
        }
    }
}
