using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace XrmToolSuite.FlowDependencyAnalyzer.Analysis
{
    /// <summary>
    /// Statically parses a Power Automate cloud flow's <c>clientdata</c> JSON into a <see cref="FlowDependencies"/>
    /// footprint: trigger, Dataverse tables/columns, connectors, connection references, environment variables,
    /// child flows, custom-API calls and HTTP actions. Pure and SDK-free (JToken only) so it stays unit-testable.
    /// <para>
    /// SECURITY: every HTTP endpoint URL, SAS/trigger URL, key/secret/authorization value is REDACTED — the
    /// returned model never stores or exposes a live URL or credential (stored as <c>[redacted]</c>).
    /// </para>
    /// <para>Malformed JSON never throws: it yields a <see cref="FlowDependencies"/> carrying a parse note.</para>
    /// </summary>
    public static class FlowClientDataParser
    {
        public const string Redacted = "[redacted]";

        // Property keys whose VALUE is a secret and must never be stored.
        private static readonly Regex SensitiveKey = new Regex(
            "authorization|password|secret|apikey|api_key|clientsecret|client_secret|sharedaccess|sas|(^|[^a-z])key$|token|credential|\\bpwd\\b|\\bsig\\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Values that look like a secret regardless of key (SAS query strings, bearer tokens…).
        private static readonly Regex SensitiveValue = new Regex(
            "sig=|SharedAccessSignature|sv=\\d{4}-\\d{2}-\\d{2}&|Bearer\\s+[A-Za-z0-9\\._-]{10,}|AccountKey=",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex HttpsUrl = new Regex(
            "https://[^\\s\"'<>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex GuidRx = new Regex(
            "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            RegexOptions.Compiled);

        // @parameters('name') — the env-variable / parameter reference shape.
        private static readonly Regex ParameterRef = new Regex(
            "@\\{?parameters\\('([^']+)'\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Dataverse SdkMessage codes used by the "Common Data Service (current environment)" connector trigger.
        private static readonly Dictionary<string, string> MessageMap = new Dictionary<string, string>
        {
            { "1", "Create" }, { "2", "Update" }, { "3", "Delete" },
            { "4", "CreateOrUpdate" }, { "5", "CreateOrDelete" },
            { "6", "UpdateOrDelete" }, { "7", "CreateOrUpdateOrDelete" },
        };

        public static FlowDependencies Parse(string flowName, string clientDataJson)
        {
            var dep = new FlowDependencies { FlowName = flowName };

            if (string.IsNullOrWhiteSpace(clientDataJson))
            {
                dep.ParseNote = "Flow has no clientdata (empty or unavailable); no dependencies could be parsed.";
                return dep;
            }

            JObject root;
            try
            {
                root = JObject.Parse(clientDataJson);
            }
            catch (Exception ex)
            {
                dep.ParseNote = "clientdata is not valid JSON, so this flow was not analyzed: " + ex.Message;
                return dep;
            }

            try
            {
                var definition = FindDefinition(root);
                var connMap = BuildConnectionReferenceMap(root);

                // Environment-variable references can appear anywhere in the definition tree.
                CollectEnvironmentVariables(definition ?? (JToken)root, dep);

                if (definition == null)
                {
                    dep.ParseNote = "clientdata parsed but no flow definition (properties.definition) was found.";
                    Dedupe(dep);
                    return dep;
                }

                // Triggers
                if (definition["triggers"] is JObject triggers)
                    foreach (var trig in triggers.Properties())
                        if (trig.Value is JObject trigObj)
                            ProcessTrigger(trig.Name, trigObj, connMap, dep);

                // Actions (recursively, through Scope/Condition/Switch/Foreach/Until nesting)
                if (definition["actions"] is JObject actions)
                    ProcessActionMap(actions, connMap, dep);
            }
            catch (Exception ex)
            {
                // Tolerate any unexpected shape — never throw out of the parser.
                dep.ParseNote = "clientdata was partially parsed; an unexpected shape was skipped: " + ex.Message;
            }

            Dedupe(dep);
            return dep;
        }

        // ------------------------------------------------------------------ definition & conn-ref map

        /// <summary>Finds the flow definition defensively across the shapes clientdata comes in.</summary>
        private static JObject FindDefinition(JObject root)
        {
            return root.SelectToken("properties.definition") as JObject
                ?? root["definition"] as JObject
                ?? (root["triggers"] != null || root["actions"] != null ? root : null)
                ?? (root.SelectToken("properties.definition.definition") as JObject);
        }

        /// <summary>
        /// Maps a connector key (the action's <c>host.connectionName</c>) to its connection-reference logical
        /// name from <c>properties.connectionReferences</c>. A key NOT present here is a direct connection.
        /// </summary>
        private static Dictionary<string, string> BuildConnectionReferenceMap(JObject root)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var crs = root.SelectToken("properties.connectionReferences") as JObject
                      ?? root["connectionReferences"] as JObject;
            if (crs == null) return map;

            foreach (var prop in crs.Properties())
            {
                var logical =
                    (string)prop.Value.SelectToken("connection.connectionReferenceLogicalName") ??
                    (string)prop.Value.SelectToken("connectionReferenceLogicalName");
                if (!string.IsNullOrEmpty(logical))
                    map[prop.Name] = logical;
            }
            return map;
        }

        // ------------------------------------------------------------------ triggers

        private static void ProcessTrigger(string name, JObject trig, Dictionary<string, string> connMap, FlowDependencies dep)
        {
            var type = (string)trig["type"] ?? "";
            var host = trig.SelectToken("inputs.host") as JObject;
            var parameters = trig.SelectToken("inputs.parameters") as JObject;

            var connectorId = ConnectorIdFrom(host);
            RegisterConnection(host, connectorId, connMap, dep);

            if (IsDataverseConnector(connectorId, host))
            {
                dep.TriggerType = "Dataverse";
                dep.TriggerEntity = DataverseEntity(parameters);
                dep.TriggerMessage = DataverseTriggerMessage(parameters);
                if (!string.IsNullOrEmpty(dep.TriggerEntity)) AddTable(dep, dep.TriggerEntity);
                CollectDataverseColumns(parameters, dep.TriggerEntity, dep);
            }
            else if (type.Equals("Recurrence", StringComparison.OrdinalIgnoreCase))
                dep.TriggerType = "Scheduled (recurrence)";
            else if (type.Equals("Request", StringComparison.OrdinalIgnoreCase))
                dep.TriggerType = "Manual/HTTP request";
            else
                dep.TriggerType = connectorId ?? type;

            ScanForLiterals(name, trig["inputs"], dep);
        }

        // ------------------------------------------------------------------ actions (recursive)

        private static void ProcessActionMap(JObject actions, Dictionary<string, string> connMap, FlowDependencies dep)
        {
            foreach (var prop in actions.Properties())
            {
                if (!(prop.Value is JObject op)) continue;
                ProcessOperation(prop.Name, op, connMap, dep);
                RecurseNested(op, connMap, dep);
            }
        }

        /// <summary>Descends through control-flow containers (Scope/Condition/Switch/Foreach/Until) to nested actions.</summary>
        private static void RecurseNested(JObject op, Dictionary<string, string> connMap, FlowDependencies dep)
        {
            foreach (var prop in op.Properties())
            {
                if (prop.Name.Equals("actions", StringComparison.OrdinalIgnoreCase) && prop.Value is JObject nested)
                    ProcessActionMap(nested, connMap, dep);
                else if (prop.Value is JObject child &&
                         !prop.Name.Equals("inputs", StringComparison.OrdinalIgnoreCase))
                    RecurseNested(child, connMap, dep); // else{actions}, cases.<k>{actions}, default{actions}
            }
        }

        private static void ProcessOperation(string name, JObject op, Dictionary<string, string> connMap, FlowDependencies dep)
        {
            var type = (string)op["type"] ?? "";
            var host = op.SelectToken("inputs.host") as JObject;
            var parameters = op.SelectToken("inputs.parameters") as JObject;

            if (type.Equals("Http", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("HttpWebhook", StringComparison.OrdinalIgnoreCase))
            {
                // External coupling — record the action but NEVER the endpoint URL (redacted).
                if (!dep.HttpActions.Contains(name)) dep.HttpActions.Add(name);
                dep.HardcodedLiterals.Add($"HTTP endpoint in '{name}': {Redacted}");
                return;
            }

            if (type.Equals("Workflow", StringComparison.OrdinalIgnoreCase))
            {
                // Child flow (RunFlow). host.workflowReferenceName / workflowId identifies the child.
                var childId = (string)op.SelectToken("inputs.host.workflowReferenceName")
                              ?? (string)op.SelectToken("inputs.host.workflow.name")
                              ?? (string)op.SelectToken("inputs.host.workflowId")
                              ?? (string)op.SelectToken("inputs.workflowReferenceName");
                if (!string.IsNullOrEmpty(childId)) dep.ChildFlows.Add(childId);
                return; // the child-flow id is a tracked dependency, not a hardcoded-literal concern
            }

            if (type.Equals("OpenApiConnection", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("OpenApiConnectionWebhook", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("OpenApiConnectionNotification", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("ApiConnection", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("ApiConnectionWebhook", StringComparison.OrdinalIgnoreCase))
            {
                var connectorId = ConnectorIdFrom(host);
                RegisterConnection(host, connectorId, connMap, dep);

                var operationId = (string)host?["operationId"] ?? "";
                if (IsDataverseConnector(connectorId, host))
                {
                    if (IsCustomApiOperation(operationId, parameters, out var apiName))
                        dep.CustomApis.Add(apiName);

                    var entity = DataverseEntity(parameters);
                    if (!string.IsNullOrEmpty(entity)) AddTable(dep, entity);
                    CollectDataverseColumns(parameters, entity, dep);
                }
            }

            // Any operation can carry hardcoded literals in its inputs (Compose, Set variable, …).
            ScanForLiterals(name, op["inputs"], dep);
        }

        // ------------------------------------------------------------------ connectors / connection refs

        private static string ConnectorIdFrom(JObject host)
        {
            if (host == null) return null;
            var apiId = (string)host["apiId"] ?? (string)host.SelectToken("api.name") ?? (string)host["connectionName"];
            if (string.IsNullOrEmpty(apiId)) return null;
            // apiId is often "/providers/Microsoft.PowerApps/apis/shared_x" — take the last segment.
            var slash = apiId.LastIndexOf('/');
            return slash >= 0 && slash < apiId.Length - 1 ? apiId.Substring(slash + 1) : apiId;
        }

        /// <summary>
        /// Records the connector, then resolves the connection reference. A <c>connectionName</c> that maps to a
        /// connection-reference logical name (via <c>properties.connectionReferences</c> or an inline host) is a
        /// connection reference; a raw connection id (GUID) that maps to nothing is a direct connection → not portable.
        /// </summary>
        private static void RegisterConnection(JObject host, string connectorId, Dictionary<string, string> connMap, FlowDependencies dep)
        {
            if (!string.IsNullOrEmpty(connectorId) && !dep.Connectors.Contains(connectorId, StringComparer.OrdinalIgnoreCase))
                dep.Connectors.Add(connectorId);

            if (host == null) return;
            var connectionName = (string)host["connectionName"];
            if (string.IsNullOrEmpty(connectionName)) return;

            // The referenced connection-reference logical name — from the top-level connectionReferences map
            // (solution-aware flows key it by connectionName) or an inline host connection block.
            string logical = null;
            if (connMap != null) connMap.TryGetValue(connectionName, out logical);
            logical = logical
                      ?? (string)host.SelectToken("connection.connectionReferenceLogicalName")
                      ?? (string)host.SelectToken("connectionReferences." + connectionName + ".connection.connectionReferenceLogicalName");

            if (!string.IsNullOrEmpty(logical))
            {
                if (!dep.ConnectionReferences.Contains(logical, StringComparer.OrdinalIgnoreCase))
                    dep.ConnectionReferences.Add(logical);
                return;
            }

            // No mapping: connectionName is a connection-reference logical name in solution-aware flows, OR a
            // direct connection id (GUID) when the flow was authored against a live connection. A GUID = direct.
            if (GuidRx.IsMatch(connectionName))
                dep.UsesDirectConnection = true;
            else if (!dep.ConnectionReferences.Contains(connectionName, StringComparer.OrdinalIgnoreCase))
                dep.ConnectionReferences.Add(connectionName);
        }

        private static bool IsDataverseConnector(string connectorId, JObject host)
        {
            var probe = (connectorId ?? "") + " " +
                        ((string)host?["apiId"] ?? "") + " " +
                        ((string)host?["connectionName"] ?? "");
            probe = probe.ToLowerInvariant();
            return probe.Contains("commondataservice") || probe.Contains("dataverse") || probe.Contains("dynamicscrm");
        }

        // ------------------------------------------------------------------ Dataverse table/column extraction

        private static string DataverseEntity(JObject parameters)
        {
            if (parameters == null) return null;
            foreach (var p in parameters.Properties())
            {
                var key = p.Name.ToLowerInvariant();
                if (key.EndsWith("entityname") || key == "entityname" || key.EndsWith("/entityname"))
                {
                    var val = (p.Value as JValue)?.Value as string;
                    if (!string.IsNullOrEmpty(val) && !val.StartsWith("@")) return val;
                }
            }
            return null;
        }

        private static string DataverseTriggerMessage(JObject parameters)
        {
            if (parameters == null) return null;
            foreach (var p in parameters.Properties())
            {
                var key = p.Name.ToLowerInvariant();
                if (key.EndsWith("/message") || key == "message")
                {
                    var val = ((p.Value as JValue)?.Value)?.ToString();
                    if (!string.IsNullOrEmpty(val))
                        return MessageMap.TryGetValue(val, out var m) ? m : val;
                }
            }
            return null;
        }

        private static void CollectDataverseColumns(JObject parameters, string entity, FlowDependencies dep)
        {
            if (parameters == null) return;
            foreach (var p in parameters.Properties())
            {
                var key = p.Name.ToLowerInvariant();
                var val = (p.Value as JValue)?.Value as string;
                if (string.IsNullOrEmpty(val) || val.StartsWith("@")) continue;

                if (key.EndsWith("$select") || key.EndsWith("/select"))
                    foreach (var col in val.Split(','))
                        AddColumn(dep, entity, col.Trim());
            }
        }

        private static bool IsCustomApiOperation(string operationId, JObject parameters, out string apiName)
        {
            apiName = null;
            var op = (operationId ?? "").ToLowerInvariant();
            bool isActionOp = op.Contains("unboundaction") || op.Contains("boundaction") || op.Contains("performaction")
                              || op.Contains("executeaction");
            if (!isActionOp) return false;

            if (parameters != null)
            {
                foreach (var p in parameters.Properties())
                {
                    var key = p.Name.ToLowerInvariant();
                    if (key.EndsWith("actionname") || key == "actionname" || key.EndsWith("/name"))
                    {
                        var val = (p.Value as JValue)?.Value as string;
                        if (!string.IsNullOrEmpty(val) && !val.StartsWith("@")) { apiName = val; return true; }
                    }
                }
            }
            apiName = operationId; // fall back to the operation id so the call is still visible
            return true;
        }

        // ------------------------------------------------------------------ env vars & hardcoded literals

        private static void CollectEnvironmentVariables(JToken node, FlowDependencies dep)
        {
            if (!(node is JContainer container)) return;
            foreach (var token in container.DescendantsAndSelf())
            {
                if (token is JValue v && v.Value is string s)
                {
                    foreach (Match m in ParameterRef.Matches(s))
                    {
                        var refName = m.Groups[1].Value;
                        // Skip Power Automate built-in parameters ($connections, $authentication): they are
                        // runtime/auth references, NOT environment variables, and flagging them produces a
                        // false "missing environment variable" finding since they never resolve to an EV.
                        if (string.IsNullOrEmpty(refName) || refName[0] == '$') continue;
                        // Heuristic: environment variables surface as parameters named like schema names;
                        // keep the reference, and the collector reconciles it against real EVs.
                        if (!dep.EnvironmentVariables.Contains(refName, StringComparer.OrdinalIgnoreCase))
                            dep.EnvironmentVariables.Add(refName);
                    }
                }
                if (token is JProperty prop &&
                    prop.Name.Equals("environmentVariables", StringComparison.OrdinalIgnoreCase) &&
                    prop.Value is JObject evObj)
                {
                    foreach (var ev in evObj.Properties())
                        if (!dep.EnvironmentVariables.Contains(ev.Name, StringComparer.OrdinalIgnoreCase))
                            dep.EnvironmentVariables.Add(ev.Name);
                }
            }
        }

        /// <summary>Scans an inputs subtree (or a bare string input) for hardcoded https URLs and GUIDs, redacting anything secret.</summary>
        private static void ScanForLiterals(string actionName, JToken inputs, FlowDependencies dep)
        {
            if (inputs == null) return;

            var values = new List<JValue>();
            if (inputs is JValue single) values.Add(single);
            else if (inputs is JContainer container) values.AddRange(container.DescendantsAndSelf().OfType<JValue>());

            foreach (var jv in values)
            {
                if (!(jv.Value is string val) || string.IsNullOrEmpty(val)) continue;
                var keyName = (jv.Parent as JProperty)?.Name ?? "";

                if (SensitiveKey.IsMatch(keyName) || SensitiveValue.IsMatch(val))
                {
                    Add(dep.HardcodedLiterals, $"secret in '{actionName}' ({keyName}): {Redacted}");
                    continue; // never store or further inspect a secret value
                }

                if (HttpsUrl.IsMatch(val))
                    // Never store the endpoint — redact. Only note that a hardcoded absolute URL exists.
                    Add(dep.HardcodedLiterals, $"hardcoded https URL in '{actionName}': {Redacted}");

                var guid = GuidRx.Match(val);
                if (guid.Success)
                    Add(dep.HardcodedLiterals, $"hardcoded GUID in '{actionName}': {guid.Value}");
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void AddTable(FlowDependencies dep, string table)
        {
            if (!string.IsNullOrEmpty(table) && !dep.Tables.Contains(table, StringComparer.OrdinalIgnoreCase))
                dep.Tables.Add(table);
        }

        private static void AddColumn(FlowDependencies dep, string entity, string column)
        {
            if (string.IsNullOrWhiteSpace(column)) return;
            var key = string.IsNullOrEmpty(entity) ? column : entity + "." + column;
            if (!dep.Columns.Contains(key, StringComparer.OrdinalIgnoreCase))
                dep.Columns.Add(key);
        }

        private static void Add(List<string> list, string value)
        {
            if (!list.Contains(value, StringComparer.OrdinalIgnoreCase))
                list.Add(value);
        }

        private static void Dedupe(FlowDependencies dep)
        {
            dep.Connectors = Distinct(dep.Connectors);
            dep.ConnectionReferences = Distinct(dep.ConnectionReferences);
            dep.EnvironmentVariables = Distinct(dep.EnvironmentVariables);
            dep.Tables = Distinct(dep.Tables);
            dep.Columns = Distinct(dep.Columns);
            dep.ChildFlows = Distinct(dep.ChildFlows);
            dep.CustomApis = Distinct(dep.CustomApis);
            dep.HttpActions = Distinct(dep.HttpActions);
            dep.HardcodedLiterals = Distinct(dep.HardcodedLiterals);
        }

        private static List<string> Distinct(List<string> list) =>
            list.Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
