using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Power Pages readiness: site settings, web roles, table permissions vs basic
    /// forms/lists, broken web files, empty content snippets, and cache guidance.
    /// Supports both the classic 'adx_' and the enhanced-data-model 'mspp_' schemas.
    /// </summary>
    public class PowerPagesAnalyzer : IAnalyzer
    {
        public string Name => "Power Pages Readiness";
        public AnalyzerCategory Category => AnalyzerCategory.PowerPages;
        public bool BenefitsFromTarget => false;

        private class Schema
        {
            public string Prefix;                       // "adx" or "mspp"
            public string Website => Prefix + "_website";
            public string SiteSetting => Prefix + "_sitesetting";
            public string WebRole => Prefix + "_webrole";
            public string EntityPermission => Prefix + "_entitypermission";
            public string EntityForm => Prefix + "_entityform";
            public string EntityList => Prefix + "_entitylist";
            public string WebFile => Prefix + "_webfile";
            public string ContentSnippet => Prefix + "_contentsnippet";
        }

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();

            progress("Power Pages: detecting portal schema…");
            var schema = DetectSchema(ctx);
            if (schema == null)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "No Power Pages site detected",
                    "Neither adx_website nor mspp_website returned data — skipping portal checks.",
                    "-", "No action required if this environment hosts no Power Pages site."));
                return findings;
            }

            progress("Power Pages: checking web roles…");
            CheckWebRoles(ctx, schema, findings);

            progress("Power Pages: checking table permissions vs forms/lists…");
            CheckTablePermissions(ctx, schema, findings);

            progress("Power Pages: checking web files…");
            CheckWebFiles(ctx, schema, findings);

            progress("Power Pages: checking content snippets…");
            CheckContentSnippets(ctx, schema, findings);

            CheckBaselineSiteSettings(ctx, schema, findings);

            // Always-on operational checklist
            findings.Add(new RiskFinding(Category, Severity.Info, "Post-deployment portal cache",
                "Portal metadata is cached server-side. Configuration imported via solution/deployment profiles may not appear until the cache is refreshed.",
                schema.Website,
                "After deployment: Power Pages admin center → site → 'Sync' (or restart the site); for config-only changes use /_services/about → Clear cache as an admin."));

            return findings;
        }

        private static Schema DetectSchema(AnalyzerContext ctx)
        {
            foreach (var prefix in new[] { "adx", "mspp" })
            {
                try
                {
                    var qe = new QueryExpression(prefix + "_website") { ColumnSet = new ColumnSet(false), TopCount = 1 };
                    var rows = ctx.Source.RetrieveMultiple(qe);
                    if (rows.Entities.Count > 0) return new Schema { Prefix = prefix };
                }
                catch { /* table not present */ }
            }
            return null;
        }

        private void CheckWebRoles(AnalyzerContext ctx, Schema s, List<RiskFinding> findings)
        {
            var qe = new QueryExpression(s.WebRole)
            {
                ColumnSet = new ColumnSet(s.Prefix + "_name",
                    s.Prefix + "_anonymoususersrole", s.Prefix + "_authenticatedusersrole")
            };
            var roles = AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities;

            if (roles.Count == 0)
            {
                findings.Add(new RiskFinding(Category, Severity.High, "No web roles found",
                    "The portal has no web roles. All table permissions hang off web roles, so no data access will work.",
                    s.WebRole, "Create at least Anonymous Users and Authenticated Users web roles and associate table permissions."));
                return;
            }

            bool hasAnon = roles.Any(r => r.GetAttributeValue<bool?>(s.Prefix + "_anonymoususersrole") == true);
            bool hasAuth = roles.Any(r => r.GetAttributeValue<bool?>(s.Prefix + "_authenticatedusersrole") == true);

            if (!hasAnon)
                findings.Add(new RiskFinding(Category, Severity.Medium, "No Anonymous Users web role",
                    "No web role is flagged as the anonymous default. Unauthenticated visitors will have no permission context.",
                    s.WebRole, "Mark one web role as 'Anonymous Users Role = Yes' (exactly one per website)."));

            if (!hasAuth)
                findings.Add(new RiskFinding(Category, Severity.Medium, "No Authenticated Users web role",
                    "No web role is flagged as the authenticated default; signed-in users only get explicitly assigned roles.",
                    s.WebRole, "Mark one web role as 'Authenticated Users Role = Yes' (exactly one per website)."));
        }

        private void CheckTablePermissions(AnalyzerContext ctx, Schema s, List<RiskFinding> findings)
        {
            // Tables surfaced by basic forms / lists
            var usedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var formQe = new QueryExpression(s.EntityForm)
            {
                ColumnSet = new ColumnSet(s.Prefix + "_entityname", s.Prefix + "_name",
                    s.Prefix + "_entitypermissionsenabled")
            };
            var forms = AnalyzerContext.SafeRetrieve(ctx.Source, formQe).Entities;
            foreach (var f in forms)
            {
                var table = f.GetAttributeValue<string>(s.Prefix + "_entityname");
                if (!string.IsNullOrEmpty(table)) usedTables.Add(table);

                if (f.GetAttributeValue<bool?>(s.Prefix + "_entitypermissionsenabled") == false)
                {
                    findings.Add(new RiskFinding(Category, Severity.High, "Basic form bypasses table permissions",
                        $"Form '{f.GetAttributeValue<string>(s.Prefix + "_name")}' has 'Enable Table Permissions' OFF — the form runs with elevated portal service access.",
                        f.GetAttributeValue<string>(s.Prefix + "_name"),
                        "Enable table permissions on the form and grant appropriate permissions via web roles."));
                }
            }

            var listQe = new QueryExpression(s.EntityList)
            {
                ColumnSet = new ColumnSet(s.Prefix + "_entityname", s.Prefix + "_name")
            };
            foreach (var l in AnalyzerContext.SafeRetrieve(ctx.Source, listQe).Entities)
            {
                var table = l.GetAttributeValue<string>(s.Prefix + "_entityname");
                if (!string.IsNullOrEmpty(table)) usedTables.Add(table);
            }

            if (usedTables.Count == 0) return;

            var permQe = new QueryExpression(s.EntityPermission)
            {
                ColumnSet = new ColumnSet(s.Prefix + "_entitylogicalname")
            };
            var permittedTables = new HashSet<string>(
                AnalyzerContext.SafeRetrieve(ctx.Source, permQe).Entities
                    .Select(p => p.GetAttributeValue<string>(s.Prefix + "_entitylogicalname"))
                    .Where(n => !string.IsNullOrEmpty(n)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var table in usedTables.Where(t => !permittedTables.Contains(t)))
            {
                findings.Add(new RiskFinding(Category, Severity.High, "Table used on portal without table permission",
                    $"Table '{table}' is surfaced by a basic form or list but has NO table permission record. Visitors will get 'You do not have permission to view these records.'",
                    table,
                    $"Create a table permission for '{table}' with the right scope (Global/Contact/Account/Parent) and associate it to the correct web role(s)."));
            }
        }

        private void CheckWebFiles(AnalyzerContext ctx, Schema s, List<RiskFinding> findings)
        {
            var qe = new QueryExpression(s.WebFile)
            {
                ColumnSet = new ColumnSet(s.Prefix + "_name")
            };
            var files = AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities;
            if (files.Count == 0) return;

            var ids = files.Select(f => f.Id).Cast<object>().ToArray();
            var noteQe = new QueryExpression("annotation")
            {
                ColumnSet = new ColumnSet("objectid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("objectid", ConditionOperator.In, ids),
                        new ConditionExpression("isdocument", ConditionOperator.Equal, true)
                    }
                }
            };
            var withContent = new HashSet<Guid>(
                AnalyzerContext.SafeRetrieve(ctx.Source, noteQe).Entities
                    .Select(a => a.GetAttributeValue<EntityReference>("objectid")?.Id ?? Guid.Empty));

            foreach (var f in files.Where(f => !withContent.Contains(f.Id)))
            {
                findings.Add(new RiskFinding(Category, Severity.Medium, "Web file has no content",
                    $"Web file '{f.GetAttributeValue<string>(s.Prefix + "_name")}' has no attached document (annotation). It will 404 on the portal.",
                    f.GetAttributeValue<string>(s.Prefix + "_name"),
                    "Re-upload the file content, or migrate web files with a tool that carries annotations (e.g., pac powerpages / Configuration Migration)."));
            }
        }

        private void CheckContentSnippets(AnalyzerContext ctx, Schema s, List<RiskFinding> findings)
        {
            var qe = new QueryExpression(s.ContentSnippet)
            {
                ColumnSet = new ColumnSet(s.Prefix + "_name", s.Prefix + "_value")
            };
            foreach (var snip in AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities)
            {
                if (string.IsNullOrWhiteSpace(snip.GetAttributeValue<string>(s.Prefix + "_value")))
                {
                    findings.Add(new RiskFinding(Category, Severity.Low, "Empty content snippet",
                        $"Content snippet '{snip.GetAttributeValue<string>(s.Prefix + "_name")}' has no value; pages referencing it render blank sections.",
                        snip.GetAttributeValue<string>(s.Prefix + "_name"),
                        "Provide a value for the snippet or remove references to it."));
                }
            }
        }

        private void CheckBaselineSiteSettings(AnalyzerContext ctx, Schema s, List<RiskFinding> findings)
        {
            // Settings commonly forgotten between environments.
            var expected = new[]
            {
                "Authentication/Registration/Enabled",
                "Authentication/Registration/LocalLoginEnabled"
            };

            var qe = new QueryExpression(s.SiteSetting)
            {
                ColumnSet = new ColumnSet(s.Prefix + "_name", s.Prefix + "_value")
            };
            var settings = AnalyzerContext.SafeRetrieve(ctx.Source, qe).Entities
                .Select(x => x.GetAttributeValue<string>(s.Prefix + "_name"))
                .Where(n => n != null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var name in expected.Where(n => !settings.Contains(n)))
            {
                findings.Add(new RiskFinding(Category, Severity.Low, "Baseline site setting missing",
                    $"Site setting '{name}' is not defined. If your target relies on it, sign-in behavior may differ across environments.",
                    name, $"Define '{name}' explicitly (site settings are environment config — include them in your portal deployment profile)."));
            }
        }
    }
}
