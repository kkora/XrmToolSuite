using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace XrmToolSuite.CustomApiExplorer.Analysis
{
    /// <summary>Outcome of parsing one typed string into a CLR value for a Custom API parameter.</summary>
    public struct ParseOutcome
    {
        public bool Ok { get; }
        public object Value { get; }
        public string Error { get; }
        private ParseOutcome(bool ok, object value, string error) { Ok = ok; Value = value; Error = error; }
        public static ParseOutcome Success(object value) => new ParseOutcome(true, value, null);
        public static ParseOutcome Fail(string error) => new ParseOutcome(false, null, error);
    }

    /// <summary>
    /// SDK-free conversion of a textbox string into the CLR value a Custom API scalar parameter expects.
    /// Money/Picklist return the underlying decimal/int; the SDK invoker wraps them in Money/OptionSetValue.
    /// Complex types (Entity/EntityReference/collections) are not scalar and are handled by the invoker.
    /// </summary>
    public static class ValueParsing
    {
        public static bool IsScalar(CustomApiFieldType type)
        {
            switch (type)
            {
                case CustomApiFieldType.Entity:
                case CustomApiFieldType.EntityCollection:
                case CustomApiFieldType.EntityReference:
                    return false;
                default:
                    return true;
            }
        }

        public static ParseOutcome Parse(CustomApiFieldType type, string text)
        {
            text = text ?? string.Empty;
            var ci = CultureInfo.InvariantCulture;
            switch (type)
            {
                case CustomApiFieldType.String:
                    return ParseOutcome.Success(text);
                case CustomApiFieldType.StringArray:
                    return ParseOutcome.Success(
                        text.Length == 0 ? new string[0]
                            : text.Split(',').Select(s => s.Trim()).ToArray());
                case CustomApiFieldType.Boolean:
                    return bool.TryParse(text, out var b) ? ParseOutcome.Success(b)
                        : ParseOutcome.Fail($"'{text}' is not a boolean (true/false).");
                case CustomApiFieldType.Integer:
                case CustomApiFieldType.Picklist:
                    return int.TryParse(text, NumberStyles.Integer, ci, out var i) ? ParseOutcome.Success(i)
                        : ParseOutcome.Fail($"'{text}' is not an integer.");
                case CustomApiFieldType.Decimal:
                case CustomApiFieldType.Money:
                    return decimal.TryParse(text, NumberStyles.Number, ci, out var d) ? ParseOutcome.Success(d)
                        : ParseOutcome.Fail($"'{text}' is not a decimal.");
                case CustomApiFieldType.Float:
                    return double.TryParse(text, NumberStyles.Float, ci, out var f) ? ParseOutcome.Success(f)
                        : ParseOutcome.Fail($"'{text}' is not a number.");
                case CustomApiFieldType.DateTime:
                    return DateTime.TryParse(text, ci, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
                        ? ParseOutcome.Success(dt) : ParseOutcome.Fail($"'{text}' is not a date/time.");
                case CustomApiFieldType.Guid:
                    return Guid.TryParse(text, out var g) ? ParseOutcome.Success(g)
                        : ParseOutcome.Fail($"'{text}' is not a GUID.");
                default:
                    return ParseOutcome.Fail($"{type} must be provided as structured input.");
            }
        }
    }

    /// <summary>Validated result of binding user inputs to a Custom API's parameters.</summary>
    public sealed class ParameterBinding
    {
        /// <summary>Parsed scalar values keyed by parameter logical name (ready for the request).</summary>
        public Dictionary<string, object> Values { get; } = new Dictionary<string, object>();
        /// <summary>Complex (non-scalar) parameters the user supplied that the invoker must interpret.</summary>
        public Dictionary<string, string> ComplexInputs { get; } = new Dictionary<string, string>();
        public List<string> Errors { get; } = new List<string>();
        public List<string> MissingRequired { get; } = new List<string>();

        public bool CanInvoke => Errors.Count == 0 && MissingRequired.Count == 0;
    }

    /// <summary>
    /// SDK-free: validates and converts a set of typed string inputs against a Custom API's parameter
    /// metadata, and generates an illustrative sample-call snippet. The actual OrganizationRequest is built
    /// from the resulting <see cref="ParameterBinding"/> by the (SDK-dependent, manual-tested) invoker.
    /// </summary>
    public static class RequestBuilder
    {
        public static ParameterBinding Bind(CustomApiInfo api, IDictionary<string, string> inputs)
        {
            if (api == null) throw new ArgumentNullException(nameof(api));
            inputs = inputs ?? new Dictionary<string, string>();
            var binding = new ParameterBinding();

            foreach (var p in api.Parameters)
            {
                inputs.TryGetValue(p.LogicalName, out var raw);
                var hasValue = !string.IsNullOrWhiteSpace(raw);

                if (!hasValue)
                {
                    if (!p.IsOptional) binding.MissingRequired.Add(p.LogicalName);
                    continue;
                }

                if (!p.IsScalar)
                {
                    binding.ComplexInputs[p.LogicalName] = raw;
                    continue;
                }

                var outcome = ValueParsing.Parse(p.Type, raw);
                if (outcome.Ok) binding.Values[p.LogicalName] = outcome.Value;
                else binding.Errors.Add($"{p.LogicalName}: {outcome.Error}");
            }

            return binding;
        }

        /// <summary>
        /// An illustrative Web API snippet for the call — marked as a sample, never containing secrets.
        /// Uses the supplied scalar inputs where present, otherwise a type placeholder.
        /// </summary>
        public static string GenerateSnippet(CustomApiInfo api, IDictionary<string, string> inputs)
        {
            if (api == null) throw new ArgumentNullException(nameof(api));
            inputs = inputs ?? new Dictionary<string, string>();
            var verb = api.IsFunction ? "GET" : "POST";
            var lines = new List<string>
            {
                "// Illustrative sample — no secrets. Verify against your environment before use.",
                $"{verb} [Organization URI]/api/data/v9.2/{api.UniqueName}"
            };

            if (!api.IsFunction && api.Parameters.Count > 0)
            {
                lines.Add("Content-Type: application/json");
                lines.Add("{");
                var body = api.Parameters.Select(p =>
                {
                    inputs.TryGetValue(p.LogicalName, out var v);
                    var shown = string.IsNullOrWhiteSpace(v) ? $"<{p.Type}>" : v;
                    var quote = NeedsQuote(p.Type) ? "\"" : "";
                    return $"  \"{p.LogicalName}\": {quote}{shown}{quote}";
                });
                lines.Add(string.Join(",\n", body));
                lines.Add("}");
            }
            else if (api.IsFunction && api.Parameters.Count > 0)
            {
                var qs = string.Join("&", api.Parameters.Select(p =>
                {
                    inputs.TryGetValue(p.LogicalName, out var v);
                    return $"{p.LogicalName}={(string.IsNullOrWhiteSpace(v) ? $"<{p.Type}>" : v)}";
                }));
                lines[1] += "?" + qs;
            }

            return string.Join("\n", lines);
        }

        private static bool NeedsQuote(CustomApiFieldType type)
        {
            switch (type)
            {
                case CustomApiFieldType.String:
                case CustomApiFieldType.DateTime:
                case CustomApiFieldType.Guid:
                    return true;
                default:
                    return false;
            }
        }
    }
}
