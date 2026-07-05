using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmToolSuite.CustomApiExplorer.Analysis
{
    /// <summary>Outcome of a live Custom API invocation — the typed results, or the raw fault on failure.</summary>
    public sealed class InvokeResult
    {
        public bool Success { get; set; }
        public string Fault { get; set; }
        public Dictionary<string, object> Results { get; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// The tool's ONLY write/execute path: builds an <see cref="OrganizationRequest"/> from a validated
    /// <see cref="ParameterBinding"/> and runs it against the connected org. SDK-dependent → manual-tested.
    /// The caller (the control) must have shown an explicit confirmation dialog first; this method assumes
    /// consent. It never reads, stores or emits secrets — only the parameter values the user typed.
    /// </summary>
    public static class CustomApiInvoker
    {
        public static InvokeResult Invoke(
            IOrganizationService service, CustomApiInfo api, ParameterBinding binding, EntityReference target)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (api == null) throw new ArgumentNullException(nameof(api));
            if (binding == null) throw new ArgumentNullException(nameof(binding));

            var result = new InvokeResult();
            try
            {
                var request = new OrganizationRequest(api.UniqueName);
                if (api.RequiresTarget && target != null)
                    request["Target"] = target;

                var typeByName = api.Parameters.ToDictionary(p => p.LogicalName, p => p.Type);

                foreach (var kv in binding.Values)
                    request[kv.Key] = WrapScalar(typeByName.TryGetValue(kv.Key, out var t) ? t : CustomApiFieldType.String, kv.Value);

                foreach (var kv in binding.ComplexInputs)
                {
                    var t = typeByName.TryGetValue(kv.Key, out var tt) ? tt : CustomApiFieldType.EntityReference;
                    var complex = WrapComplex(t, kv.Value);
                    if (complex != null) request[kv.Key] = complex;
                }

                var response = service.Execute(request);
                result.Success = true;
                if (response?.Results != null)
                    foreach (var r in response.Results)
                        result.Results[r.Key] = r.Value;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Fault = ex.ToString();
            }
            return result;
        }

        private static object WrapScalar(CustomApiFieldType type, object value)
        {
            switch (type)
            {
                case CustomApiFieldType.Money:
                    return value is decimal m ? new Money(m) : value;
                case CustomApiFieldType.Picklist:
                    return value is int i ? (object)new OptionSetValue(i) : value;
                default:
                    return value; // bool, DateTime, decimal, double, int, string, string[], Guid
            }
        }

        /// <summary>Best-effort parse of a complex parameter typed as "logicalname:guid".</summary>
        private static object WrapComplex(CustomApiFieldType type, string raw)
        {
            if (type != CustomApiFieldType.EntityReference || string.IsNullOrWhiteSpace(raw)) return null;
            var parts = raw.Split(':');
            if (parts.Length == 2 && Guid.TryParse(parts[1].Trim(), out var id))
                return new EntityReference(parts[0].Trim(), id);
            return null;
        }
    }
}
