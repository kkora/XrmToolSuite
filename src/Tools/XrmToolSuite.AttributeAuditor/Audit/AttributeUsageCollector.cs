using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace XrmToolSuite.AttributeAuditor.Audit
{
    /// <summary>
    /// Audits custom columns across the environment: builds a <see cref="ColumnAudit"/> per custom
    /// attribute, then marks usage evidence from forms, views, processes (workflows / business rules /
    /// cloud flows), and field security. A custom, unmanaged column with no evidence is a retirement
    /// candidate. UI-free and fail-soft; the reference-detection itself lives in the SDK-free
    /// <see cref="UsageScanners"/> so it is unit-tested independently.
    /// </summary>
    public static class AttributeUsageCollector
    {
        public static AuditResult Collect(AttributeAuditContext ctx, bool customEntitiesOnly, Action<string> progress)
        {
            var result = new AuditResult { EnvironmentName = ctx.EnvironmentName };

            progress?.Invoke("Loading tables and columns…");
            var entities = ctx.Entities()
                .Where(e => e.IsIntersect != true)
                .Where(e => !customEntitiesOnly || e.IsCustomEntity == true)
                .ToList();

            // Retrieve the reference sources once and index by their owning entity's logical name.
            progress?.Invoke("Reading forms, views and processes…");
            var formsByEntity = GroupBy(ctx.SafeRetrieveAll("systemform", "name", "objecttypecode", "formxml"), "objecttypecode");
            var viewsByEntity = GroupBy(ctx.SafeRetrieveAll("savedquery", "name", "returnedtypecode", "fetchxml", "layoutxml"), "returnedtypecode");
            var procsByEntity = GroupBy(ctx.SafeRetrieveAll("workflow", "name", "primaryentity", "xaml", "clientdata"), "primaryentity");

            foreach (var e in entities)
            {
                var custom = (e.Attributes ?? Array.Empty<AttributeMetadata>())
                    .Where(a => a.IsCustomAttribute == true && !string.IsNullOrEmpty(a.LogicalName))
                    .ToList();
                if (custom.Count == 0) continue;

                progress?.Invoke($"Auditing {e.LogicalName} ({custom.Count} custom column(s))…");

                var byName = new Dictionary<string, ColumnAudit>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in custom)
                {
                    var col = new ColumnAudit
                    {
                        Table = e.LogicalName,
                        TableDisplay = e.DisplayName?.UserLocalizedLabel?.Label ?? e.LogicalName,
                        LogicalName = a.LogicalName,
                        DisplayName = a.DisplayName?.UserLocalizedLabel?.Label ?? a.LogicalName,
                        AttributeType = a.AttributeType?.ToString() ?? "",
                        IsCustom = true,
                        IsManaged = a.IsManaged == true,
                    };
                    if (a.IsSecured == true)
                        col.Add(UsageSignal.FieldSecurity, "Protected by a field security profile");
                    byName[a.LogicalName] = col;
                    result.Columns.Add(col);
                }

                MarkForms(byName, Group(formsByEntity, e.LogicalName));
                MarkViews(byName, Group(viewsByEntity, e.LogicalName));
                MarkProcesses(byName, Group(procsByEntity, e.LogicalName));
            }

            return result;
        }

        private static void MarkForms(Dictionary<string, ColumnAudit> byName, IEnumerable<Entity> forms)
        {
            foreach (var f in forms)
            {
                var name = f.GetAttributeValue<string>("name");
                foreach (var col in UsageScanners.FormColumns(f.GetAttributeValue<string>("formxml")))
                    if (byName.TryGetValue(col, out var ca)) ca.Add(UsageSignal.Form, $"Form: {name}");
            }
        }

        private static void MarkViews(Dictionary<string, ColumnAudit> byName, IEnumerable<Entity> views)
        {
            foreach (var v in views)
            {
                var name = v.GetAttributeValue<string>("name");
                var cols = UsageScanners.FetchColumns(v.GetAttributeValue<string>("fetchxml"));
                cols.UnionWith(UsageScanners.LayoutColumns(v.GetAttributeValue<string>("layoutxml")));
                foreach (var col in cols)
                    if (byName.TryGetValue(col, out var ca)) ca.Add(UsageSignal.View, $"View: {name}");
            }
        }

        private static void MarkProcesses(Dictionary<string, ColumnAudit> byName, IEnumerable<Entity> procs)
        {
            foreach (var p in procs)
            {
                var name = p.GetAttributeValue<string>("name");
                var body = (p.GetAttributeValue<string>("xaml") ?? "") + "\n" + (p.GetAttributeValue<string>("clientdata") ?? "");
                foreach (var ca in byName.Values)
                    if (UsageScanners.ReferencesToken(body, ca.LogicalName)) ca.Add(UsageSignal.Process, $"Process: {name}");
            }
        }

        private static Dictionary<string, List<Entity>> GroupBy(List<Entity> rows, string keyAttr)
        {
            var map = new Dictionary<string, List<Entity>>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.GetAttributeValue<string>(keyAttr);
                if (string.IsNullOrEmpty(key)) continue;
                if (!map.TryGetValue(key, out var list)) map[key] = list = new List<Entity>();
                list.Add(r);
            }
            return map;
        }

        private static IEnumerable<Entity> Group(Dictionary<string, List<Entity>> map, string key) =>
            map.TryGetValue(key ?? "", out var list) ? list : Enumerable.Empty<Entity>();
    }
}
