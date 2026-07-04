using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Analyzers
{
    /// <summary>
    /// Environment variables (missing values, unset secrets, data-source vars)
    /// and connection references (unbound connections, missing counterparts in target).
    /// </summary>
    public class EnvironmentVariableAnalyzer : IAnalyzer
    {
        public string Name => "Environment Variables & Connection References";
        public AnalyzerCategory Category => AnalyzerCategory.EnvironmentVariables;
        public bool BenefitsFromTarget => true;

        // environmentvariabledefinition.type option values
        private const int TypeSecret = 100000005;
        private const int TypeDataSource = 100000004;

        public List<RiskFinding> Analyze(AnalyzerContext ctx, Action<string> progress)
        {
            var findings = new List<RiskFinding>();
            AnalyzeEnvironmentVariables(ctx, findings, progress);
            AnalyzeConnectionReferences(ctx, findings, progress);
            return findings;
        }

        private void AnalyzeEnvironmentVariables(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Environment variables: reading definitions…");

            EntityCollection defs;
            try
            {
                defs = ctx.QuerySolutionRows("environmentvariabledefinition",
                    "environmentvariabledefinitionid",
                    "schemaname", "displayname", "type", "defaultvalue", "isrequired");
            }
            catch (Exception ex)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Environment variable scan unavailable",
                    ex.Message, ctx.SolutionUniqueName, "Verify the environmentvariabledefinition table is accessible."));
                return;
            }

            if (defs.Entities.Count == 0) return;

            // Current values in SOURCE (used to detect values accidentally packed into the solution)
            var defIds = defs.Entities.Select(d => d.Id).Cast<object>().ToArray();
            var valQuery = new QueryExpression("environmentvariablevalue")
            {
                ColumnSet = new ColumnSet("environmentvariabledefinitionid", "value"),
                Criteria = { Conditions = { new ConditionExpression("environmentvariabledefinitionid", ConditionOperator.In, defIds) } }
            };
            var sourceValues = AnalyzerContext.SafeRetrieve(ctx.Source, valQuery).Entities
                .GroupBy(v => v.GetAttributeValue<EntityReference>("environmentvariabledefinitionid").Id)
                .ToDictionary(g => g.Key, g => g.First().GetAttributeValue<string>("value"));

            // Value rows that ship INSIDE this solution (bad practice: overwrites target values on import)
            var packagedValueDefIds = new HashSet<Guid>();
            try
            {
                var packaged = ctx.QuerySolutionRows("environmentvariablevalue",
                    "environmentvariablevalueid", "environmentvariabledefinitionid");
                foreach (var v in packaged.Entities)
                    packagedValueDefIds.Add(v.GetAttributeValue<EntityReference>("environmentvariabledefinitionid").Id);
            }
            catch { /* best effort */ }

            foreach (var def in defs.Entities)
            {
                var schema = def.GetAttributeValue<string>("schemaname");
                var display = def.GetAttributeValue<string>("displayname") ?? schema;
                var type = def.GetAttributeValue<OptionSetValue>("type")?.Value ?? 0;
                var defaultValue = def.GetAttributeValue<string>("defaultvalue");
                bool hasSourceValue = sourceValues.TryGetValue(def.Id, out var srcVal) && !string.IsNullOrWhiteSpace(srcVal);

                if (type == TypeSecret)
                {
                    // Secrets are never transported; each environment must be configured manually.
                    findings.Add(new RiskFinding(Category, Severity.High, "Secret environment variable requires manual setup",
                        $"'{display}' is a Secret-type variable. Its Azure Key Vault reference is environment-specific and is not carried by the solution.",
                        schema,
                        $"After import, set the Key Vault reference for '{schema}' in the target and grant the Dataverse service principal access to the vault.",
                        "https://learn.microsoft.com/power-apps/maker/data-platform/environmentvariables-azure-key-vault-secrets"));
                    continue;
                }

                if (packagedValueDefIds.Contains(def.Id))
                {
                    findings.Add(new RiskFinding(Category, Severity.Medium, "Environment variable VALUE packaged in solution",
                        $"'{display}' ships a current value inside the solution. On import it overwrites the target's value — a classic source of dev URLs leaking to production.",
                        schema,
                        $"Remove the current value of '{schema}' before export (Solution > Environment variable > Remove from this solution: value)."));
                }

                if (!ctx.HasTarget)
                {
                    if (string.IsNullOrWhiteSpace(defaultValue) && !hasSourceValue)
                    {
                        findings.Add(new RiskFinding(Category, Severity.Medium, "Environment variable has no default value",
                            $"'{display}' has neither a default nor a current value. Connect a target to verify it is configured there; flows depending on it will fail otherwise.",
                            schema, $"Provide a default value or set a current value for '{schema}' in the target after import."));
                    }
                    continue;
                }

                // Target-side verification
                var tgtDef = GetTargetDefinition(ctx, schema);
                if (tgtDef == null)
                {
                    var sev = string.IsNullOrWhiteSpace(defaultValue) ? Severity.High : Severity.Low;
                    findings.Add(new RiskFinding(Category, sev, "Environment variable new to target",
                        $"'{display}' does not exist in the target yet." +
                        (sev == Severity.High ? " It has no default value, so dependent flows/plugins will fail until a value is set." : " A default value exists, but confirm it is correct for the target."),
                        schema,
                        $"During/after import, set the current value of '{schema}' in the target environment."));
                }
                else
                {
                    bool tgtHasValue = TargetHasValue(ctx, tgtDef.Id);
                    var tgtDefault = tgtDef.GetAttributeValue<string>("defaultvalue");
                    if (!tgtHasValue && string.IsNullOrWhiteSpace(tgtDefault))
                    {
                        findings.Add(new RiskFinding(Category, Severity.High, "Environment variable unset in target",
                            $"'{display}' exists in the target but has no current value and no default.",
                            schema, $"Set the current value of '{schema}' in the target before deploying dependent components."));
                    }
                    if (type == TypeDataSource && !tgtHasValue)
                    {
                        findings.Add(new RiskFinding(Category, Severity.Medium, "Data source variable unbound in target",
                            $"Data-source variable '{display}' has no target binding (e.g., SharePoint site/list).",
                            schema, $"Bind '{schema}' to the correct data source in the target."));
                    }
                }
            }
        }

        private static Entity GetTargetDefinition(AnalyzerContext ctx, string schemaName)
        {
            var qe = new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("schemaname", "defaultvalue", "type"),
                TopCount = 1,
                Criteria = { Conditions = { new ConditionExpression("schemaname", ConditionOperator.Equal, schemaName) } }
            };
            return AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities.FirstOrDefault();
        }

        private static bool TargetHasValue(AnalyzerContext ctx, Guid defId)
        {
            var qe = new QueryExpression("environmentvariablevalue")
            {
                ColumnSet = new ColumnSet("value"),
                TopCount = 1,
                Criteria = { Conditions = { new ConditionExpression("environmentvariabledefinitionid", ConditionOperator.Equal, defId) } }
            };
            var row = AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities.FirstOrDefault();
            return !string.IsNullOrWhiteSpace(row?.GetAttributeValue<string>("value"));
        }

        private void AnalyzeConnectionReferences(AnalyzerContext ctx, List<RiskFinding> findings, Action<string> progress)
        {
            progress("Connection references: validating bindings…");

            EntityCollection refs;
            try
            {
                refs = ctx.QuerySolutionRows("connectionreference", "connectionreferenceid",
                    "connectionreferencelogicalname", "connectionreferencedisplayname", "connectorid", "connectionid");
            }
            catch (Exception ex)
            {
                findings.Add(new RiskFinding(Category, Severity.Info, "Connection reference scan unavailable",
                    ex.Message, ctx.SolutionUniqueName, "Verify the connectionreference table is accessible."));
                return;
            }

            foreach (var cr in refs.Entities)
            {
                var logical = cr.GetAttributeValue<string>("connectionreferencelogicalname");
                var display = cr.GetAttributeValue<string>("connectionreferencedisplayname") ?? logical;
                var connector = cr.GetAttributeValue<string>("connectorid") ?? "unknown connector";

                if (!ctx.HasTarget)
                {
                    findings.Add(new RiskFinding(Category, Severity.Info, "Connection reference must be bound at import",
                        $"'{display}' ({ShortConnector(connector)}) will require a connection during target import.",
                        logical,
                        $"Prepare a connection for {ShortConnector(connector)} in the target, owned by a service account, and map it during import (or via deployment settings file in pipelines)."));
                    continue;
                }

                var qe = new QueryExpression("connectionreference")
                {
                    ColumnSet = new ColumnSet("connectionid"),
                    TopCount = 1,
                    Criteria = { Conditions = { new ConditionExpression("connectionreferencelogicalname", ConditionOperator.Equal, logical) } }
                };
                var tgt = AnalyzerContext.SafeRetrieve(ctx.Target, qe).Entities.FirstOrDefault();

                if (tgt == null)
                {
                    findings.Add(new RiskFinding(Category, Severity.Medium, "Connection reference new to target",
                        $"'{display}' does not exist in the target yet. Import will prompt for (or pipelines must supply) a connection.",
                        logical,
                        $"Create/identify a {ShortConnector(connector)} connection in the target and include it in your deployment settings file."));
                }
                else if (string.IsNullOrWhiteSpace(tgt.GetAttributeValue<string>("connectionid")))
                {
                    findings.Add(new RiskFinding(Category, Severity.High, "Connection reference unbound in target",
                        $"'{display}' exists in the target but has NO connection bound. Flows using it will be off or failing.",
                        logical,
                        $"Bind a valid {ShortConnector(connector)} connection to '{logical}' in the target, then re-activate dependent flows."));
                }
            }
        }

        private static string ShortConnector(string connectorId)
        {
            if (string.IsNullOrEmpty(connectorId)) return "connector";
            var idx = connectorId.LastIndexOf('/');
            return idx >= 0 && idx < connectorId.Length - 1 ? connectorId.Substring(idx + 1) : connectorId;
        }
    }
}
