using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;

namespace XrmToolSuite.PluginDependencyGraph.Graph
{
    /// <summary>
    /// Reads plugin-registration + solution metadata from Dataverse and projects it into the SDK-free
    /// <see cref="PluginRegistrationData"/>. UI-free so the projection stays testable-by-inspection; it
    /// pages via <c>RetrieveAll</c>, reports progress, honours cancellation, and degrades any query failure
    /// to a note (never throws). Secure-configuration VALUES are never retrieved — only the presence flag.
    /// </summary>
    public sealed class PluginCollector
    {
        public PluginRegistrationData Collect(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            var data = new PluginRegistrationData();
            void Report(string m) { progress?.Invoke(m); worker?.ReportProgress(0, m); }
            bool Cancelled() => worker != null && worker.CancellationPending;

            // ---- Assemblies ----
            Report("Retrieving plugin assemblies…");
            var assembliesByGuid = new Dictionary<Guid, PluginAssemblyInfo>();
            Try(data, "plugin assemblies", () =>
            {
                var q = new QueryExpression("pluginassembly")
                {
                    ColumnSet = new ColumnSet("pluginassemblyid", "name", "version", "isolationmode", "ismanaged")
                };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    var info = new PluginAssemblyInfo
                    {
                        Id = e.Id.ToString(),
                        Name = e.GetAttributeValue<string>("name"),
                        Version = e.GetAttributeValue<string>("version"),
                        IsolationMode = IsolationMode(e.GetAttributeValue<OptionSetValue>("isolationmode")),
                        IsManaged = e.GetAttributeValue<bool>("ismanaged")
                    };
                    assembliesByGuid[e.Id] = info;
                    data.Assemblies.Add(info);
                }
            });
            if (Cancelled()) return data;

            // ---- Plugin types ----
            Report("Retrieving plugin types…");
            Try(data, "plugin types", () =>
            {
                var q = new QueryExpression("plugintype")
                {
                    ColumnSet = new ColumnSet("plugintypeid", "typename", "friendlyname", "pluginassemblyid", "ismanaged")
                };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    data.Types.Add(new PluginTypeInfo
                    {
                        Id = e.Id.ToString(),
                        AssemblyId = e.GetAttributeValue<EntityReference>("pluginassemblyid")?.Id.ToString(),
                        TypeName = e.GetAttributeValue<string>("typename"),
                        FriendlyName = e.GetAttributeValue<string>("friendlyname"),
                        IsManaged = e.GetAttributeValue<bool>("ismanaged")
                    });
                }
            });
            if (Cancelled()) return data;

            // ---- Messages (id → name) ----
            Report("Retrieving SDK messages…");
            var messageNames = new Dictionary<Guid, string>();
            Try(data, "SDK messages", () =>
            {
                var q = new QueryExpression("sdkmessage") { ColumnSet = new ColumnSet("sdkmessageid", "name") };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                    messageNames[e.Id] = e.GetAttributeValue<string>("name");
            });

            // ---- Message filters (id → primary entity) ----
            Report("Retrieving message filters…");
            var filterEntity = new Dictionary<Guid, string>();
            Try(data, "SDK message filters", () =>
            {
                var q = new QueryExpression("sdkmessagefilter") { ColumnSet = new ColumnSet("sdkmessagefilterid", "primaryobjecttypecode") };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    var tc = e.GetAttributeValue<string>("primaryobjecttypecode");
                    if (!string.IsNullOrEmpty(tc) && !string.Equals(tc, "none", StringComparison.OrdinalIgnoreCase))
                        filterEntity[e.Id] = tc;
                }
            });
            if (Cancelled()) return data;

            // ---- Steps ----
            Report("Retrieving processing steps…");
            Try(data, "processing steps", () =>
            {
                var q = new QueryExpression("sdkmessageprocessingstep")
                {
                    ColumnSet = new ColumnSet(
                        "sdkmessageprocessingstepid", "name", "plugintypeid", "sdkmessageid", "sdkmessagefilterid",
                        "stage", "mode", "rank", "filteringattributes", "impersonatinguserid", "statecode",
                        "supporteddeployment", "configuration", "sdkmessageprocessingstepsecureconfigid", "ismanaged")
                };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    var msgRef = e.GetAttributeValue<EntityReference>("sdkmessageid");
                    var filterRef = e.GetAttributeValue<EntityReference>("sdkmessagefilterid");
                    var impersonate = e.GetAttributeValue<EntityReference>("impersonatinguserid");
                    var unsecure = e.GetAttributeValue<string>("configuration");
                    bool hasSecure = e.GetAttributeValue<EntityReference>("sdkmessageprocessingstepsecureconfigid") != null;

                    string entity = "";
                    if (filterRef != null && filterEntity.TryGetValue(filterRef.Id, out var ent)) entity = ent;

                    data.Steps.Add(new PluginStepInfo
                    {
                        Id = e.Id.ToString(),
                        TypeId = e.GetAttributeValue<EntityReference>("plugintypeid")?.Id.ToString(),
                        Name = e.GetAttributeValue<string>("name"),
                        MessageName = msgRef != null && messageNames.TryGetValue(msgRef.Id, out var mn) ? mn : msgRef?.Name,
                        PrimaryEntity = entity,
                        Stage = Stage(e.GetAttributeValue<OptionSetValue>("stage")),
                        Mode = Mode(e.GetAttributeValue<OptionSetValue>("mode")),
                        Rank = e.GetAttributeValue<int>("rank"),
                        FilteringAttributes = e.GetAttributeValue<string>("filteringattributes"),
                        ImpersonatingUser = impersonate?.Name ?? (impersonate != null ? impersonate.Id.ToString() : null),
                        State = State(e.GetAttributeValue<OptionSetValue>("statecode")),
                        SupportedDeployment = Deployment(e.GetAttributeValue<OptionSetValue>("supporteddeployment")),
                        IsManaged = e.GetAttributeValue<bool>("ismanaged"),
                        UsesSecureConfig = hasSecure,                 // flag only — value never read
                        UsesUnsecureConfig = !string.IsNullOrWhiteSpace(unsecure),
                        UnsecureConfigRedacted = Redact(unsecure)
                    });
                }
            });
            if (Cancelled()) return data;

            // ---- Images ----
            Report("Retrieving step images…");
            Try(data, "step images", () =>
            {
                var q = new QueryExpression("sdkmessageprocessingstepimage")
                {
                    ColumnSet = new ColumnSet("sdkmessageprocessingstepimageid", "sdkmessageprocessingstepid", "name", "imagetype", "attributes1")
                };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    data.Images.Add(new PluginImageInfo
                    {
                        Id = e.Id.ToString(),
                        StepId = e.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid")?.Id.ToString(),
                        Name = e.GetAttributeValue<string>("name"),
                        ImageType = ImageType(e.GetAttributeValue<OptionSetValue>("imagetype")),
                        Attributes = e.GetAttributeValue<string>("attributes1")
                    });
                }
            });
            if (Cancelled()) return data;

            // ---- Custom APIs (optional; degrade if the table is absent on older orgs) ----
            Report("Retrieving custom APIs…");
            Try(data, "custom APIs", () =>
            {
                var q = new QueryExpression("customapi")
                {
                    ColumnSet = new ColumnSet("customapiid", "name", "uniquename", "plugintypeid", "ismanaged", "boundentitylogicalname")
                };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    data.CustomApis.Add(new CustomApiInfo
                    {
                        Id = e.Id.ToString(),
                        Name = e.GetAttributeValue<string>("name"),
                        UniqueName = e.GetAttributeValue<string>("uniquename"),
                        PluginTypeId = e.GetAttributeValue<EntityReference>("plugintypeid")?.Id.ToString(),
                        IsManaged = e.GetAttributeValue<bool>("ismanaged"),
                        BoundEntity = e.GetAttributeValue<string>("boundentitylogicalname")
                    });
                }
            });
            if (Cancelled()) return data;

            // ---- Solution membership (owning solution for assemblies + steps) ----
            Report("Resolving owning solutions…");
            ResolveSolutions(svc, worker, data);

            Report($"Loaded {data.Assemblies.Count} assemblies, {data.Types.Count} types, {data.Steps.Count} steps, " +
                   $"{data.Images.Count} images, {data.CustomApis.Count} custom APIs.");
            return data;
        }

        private void ResolveSolutions(IOrganizationService svc, BackgroundWorker worker, PluginRegistrationData data)
        {
            // solutioncomponent componenttype: 91 = Plugin Assembly, 92 = SDK Message Processing Step.
            const int CtAssembly = 91, CtStep = 92;

            var solutionsByGuid = new Dictionary<Guid, PluginSolutionInfo>();
            Try(data, "solutions", () =>
            {
                var q = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid", "uniquename", "friendlyname", "ismanaged"),
                    Criteria = { Conditions = { new ConditionExpression("isvisible", ConditionOperator.Equal, true) } }
                };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    solutionsByGuid[e.Id] = new PluginSolutionInfo
                    {
                        Id = e.Id.ToString(),
                        UniqueName = e.GetAttributeValue<string>("uniquename"),
                        FriendlyName = e.GetAttributeValue<string>("friendlyname"),
                        IsManaged = e.GetAttributeValue<bool>("ismanaged")
                    };
                }
            });

            var componentSolution = new Dictionary<Guid, Guid>(); // objectid → solutionid (first wins)
            Try(data, "solution components", () =>
            {
                var q = new QueryExpression("solutioncomponent")
                {
                    ColumnSet = new ColumnSet("objectid", "solutionid", "componenttype"),
                    Criteria = { Conditions = { new ConditionExpression("componenttype", ConditionOperator.In, CtAssembly, CtStep) } }
                };
                foreach (var e in svc.RetrieveAll(q, null, worker))
                {
                    var objId = e.GetAttributeValue<Guid>("objectid");
                    var solId = e.GetAttributeValue<EntityReference>("solutionid")?.Id ?? Guid.Empty;
                    if (objId != Guid.Empty && solId != Guid.Empty && !componentSolution.ContainsKey(objId))
                        componentSolution[objId] = solId;
                }
            });

            // Only surface non-default solutions as nodes (Active/Default add noise).
            foreach (var s in solutionsByGuid.Values)
            {
                if (string.Equals(s.UniqueName, "Active", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.UniqueName, "Default", StringComparison.OrdinalIgnoreCase)) continue;
                if (data.Solutions.All(x => x.UniqueName != s.UniqueName)) data.Solutions.Add(s);
            }

            string Friendly(Guid objId)
            {
                if (!componentSolution.TryGetValue(objId, out var solId)) return null;
                return solutionsByGuid.TryGetValue(solId, out var s) ? s.FriendlyName : null;
            }

            foreach (var a in data.Assemblies)
                if (Guid.TryParse(a.Id, out var g)) a.OwningSolution = Friendly(g);
            foreach (var st in data.Steps)
                if (Guid.TryParse(st.Id, out var g)) st.OwningSolution = Friendly(g);
        }

        private static void Try(PluginRegistrationData data, string what, Action action)
        {
            try { action(); }
            catch (Exception ex) { data.Notes.Add($"Could not retrieve {what}: {ex.Message}"); }
        }

        // ---- option-set + redaction helpers ----

        private static string IsolationMode(OptionSetValue v)
        {
            switch (v?.Value)
            {
                case 1: return "None";
                case 2: return "Sandbox";
                case 3: return "External";
                default: return "";
            }
        }

        private static string Stage(OptionSetValue v)
        {
            switch (v?.Value)
            {
                case 10: return "PreValidation";
                case 20: return "PreOperation";
                case 40: return "PostOperation";
                default: return v?.Value.ToString() ?? "";
            }
        }

        private static string Mode(OptionSetValue v)
        {
            switch (v?.Value)
            {
                case 0: return "Synchronous";
                case 1: return "Asynchronous";
                default: return "";
            }
        }

        private static string State(OptionSetValue v)
        {
            switch (v?.Value)
            {
                case 0: return "Enabled";
                case 1: return "Disabled";
                default: return "";
            }
        }

        private static string Deployment(OptionSetValue v)
        {
            switch (v?.Value)
            {
                case 0: return "ServerOnly";
                case 1: return "OutlookClientOnly";
                case 2: return "Both";
                default: return "";
            }
        }

        private static string ImageType(OptionSetValue v)
        {
            switch (v?.Value)
            {
                case 0: return "PreImage";
                case 1: return "PostImage";
                case 2: return "Both";
                default: return "";
            }
        }

        private static readonly Regex SecretKey =
            new Regex(@"(?i)(password|pwd|secret|apikey|api[-_ ]?key|token|clientsecret|connectionstring|accountkey|sharedaccesskey)", RegexOptions.Compiled);
        private static readonly Regex Guidish =
            new Regex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.Compiled);
        private static readonly Regex LongToken =
            new Regex(@"[A-Za-z0-9+/=_\-]{24,}", RegexOptions.Compiled);

        /// <summary>
        /// Produces a redacted, length-capped preview of the UNSECURE config for display. Never a secure
        /// value. Masks anything resembling a secret key/value, GUIDs and long tokens so nothing sensitive
        /// leaks into the graph/exports.
        /// </summary>
        internal static string Redact(string config)
        {
            if (string.IsNullOrWhiteSpace(config)) return "";
            var s = config.Replace("\r", " ").Replace("\n", " ").Trim();

            // key=value / "key":"value" pairs whose key looks sensitive → mask the value.
            s = Regex.Replace(s,
                @"(?i)\b(password|pwd|secret|apikey|api[-_ ]?key|token|clientsecret|connectionstring|accountkey|sharedaccesskey)\b(\s*[:=]\s*)(""?)([^""\s,;]+)(\3)",
                m => m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value + "***" + m.Groups[5].Value);

            s = Guidish.Replace(s, "***");
            s = LongToken.Replace(s, "***");

            if (SecretKey.IsMatch(config) && !s.Contains("***"))
                s = "[redacted — contains sensitive keys]";

            const int cap = 200;
            return s.Length > cap ? s.Substring(0, cap) + "…" : s;
        }
    }
}
