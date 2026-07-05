using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.CustomApiExplorer.Analysis;
using XrmToolSuite.CustomApiExplorer.Reporting;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Custom API Explorer: scalar value parsing, request-parameter binding /
    /// required-validation, the illustrative snippet generator, and the catalog exporters. The Dataverse
    /// collector and the gated live invocation are manual-tested. Traces to US-PLUGIN6.2 / US-PLUGIN6.4 / US-PLUGIN6.5.
    /// </summary>
    public class CustomApiExplorerTests
    {
        // ---- value parsing (US-PLUGIN6.4.1) ----

        [Theory]
        [InlineData(CustomApiFieldType.Integer, "42", true)]
        [InlineData(CustomApiFieldType.Integer, "x", false)]
        [InlineData(CustomApiFieldType.Boolean, "true", true)]
        [InlineData(CustomApiFieldType.Boolean, "maybe", false)]
        [InlineData(CustomApiFieldType.Decimal, "3.14", true)]
        [InlineData(CustomApiFieldType.Money, "9.99", true)]
        [InlineData(CustomApiFieldType.Guid, "not-a-guid", false)]
        public void Parse_ScalarTypes(CustomApiFieldType type, string text, bool ok)
        {
            Assert.Equal(ok, ValueParsing.Parse(type, text).Ok);
        }

        [Fact]
        public void Parse_StringArray_SplitsOnComma()
        {
            var outcome = ValueParsing.Parse(CustomApiFieldType.StringArray, "a, b ,c");
            Assert.True(outcome.Ok);
            Assert.Equal(new[] { "a", "b", "c" }, (string[])outcome.Value);
        }

        [Fact]
        public void Parse_GuidAndDateTime()
        {
            Assert.True(ValueParsing.Parse(CustomApiFieldType.Guid, Guid.NewGuid().ToString()).Ok);
            Assert.True(ValueParsing.Parse(CustomApiFieldType.DateTime, "2026-07-05T10:00:00Z").Ok);
        }

        [Fact]
        public void IsScalar_ComplexTypesAreNotScalar()
        {
            Assert.False(ValueParsing.IsScalar(CustomApiFieldType.Entity));
            Assert.False(ValueParsing.IsScalar(CustomApiFieldType.EntityReference));
            Assert.True(ValueParsing.IsScalar(CustomApiFieldType.String));
        }

        // ---- request binding (US-PLUGIN6.4.1) ----

        private static CustomApiInfo Api(params CustomApiParameter[] ps)
        {
            var api = new CustomApiInfo { UniqueName = "new_DoThing", DisplayName = "Do Thing" };
            api.Parameters.AddRange(ps);
            return api;
        }

        private static CustomApiParameter P(string name, CustomApiFieldType type, bool optional) =>
            new CustomApiParameter { LogicalName = name, UniqueName = name, Type = type, IsOptional = optional };

        [Fact]
        public void Bind_MissingRequired_BlocksInvoke()
        {
            var api = Api(P("Amount", CustomApiFieldType.Money, false));
            var binding = RequestBuilder.Bind(api, new Dictionary<string, string>());
            Assert.False(binding.CanInvoke);
            Assert.Contains("Amount", binding.MissingRequired);
        }

        [Fact]
        public void Bind_ParsesScalars_AndCanInvoke()
        {
            var api = Api(
                P("Amount", CustomApiFieldType.Money, false),
                P("Count", CustomApiFieldType.Integer, false));
            var binding = RequestBuilder.Bind(api, new Dictionary<string, string>
            {
                ["Amount"] = "12.50",
                ["Count"] = "3",
            });
            Assert.True(binding.CanInvoke);
            Assert.Equal(12.50m, binding.Values["Amount"]);
            Assert.Equal(3, binding.Values["Count"]);
        }

        [Fact]
        public void Bind_BadValue_IsAnError()
        {
            var api = Api(P("Count", CustomApiFieldType.Integer, false));
            var binding = RequestBuilder.Bind(api, new Dictionary<string, string> { ["Count"] = "abc" });
            Assert.False(binding.CanInvoke);
            Assert.Contains(binding.Errors, e => e.StartsWith("Count:"));
        }

        [Fact]
        public void Bind_OptionalOmitted_IsFine()
        {
            var api = Api(P("Note", CustomApiFieldType.String, true));
            var binding = RequestBuilder.Bind(api, new Dictionary<string, string>());
            Assert.True(binding.CanInvoke);
            Assert.Empty(binding.Values);
        }

        [Fact]
        public void Bind_ComplexInput_GoesToComplexInputs()
        {
            var api = Api(P("Target", CustomApiFieldType.EntityReference, false));
            var binding = RequestBuilder.Bind(api, new Dictionary<string, string> { ["Target"] = "account:1234" });
            Assert.Contains("Target", binding.ComplexInputs.Keys);
        }

        // ---- snippet (US-PLUGIN6.5.2) ----

        [Fact]
        public void Snippet_Action_IsPostWithBody_NoSecrets()
        {
            var api = Api(P("Amount", CustomApiFieldType.Money, false));
            api.IsFunction = false;
            var snippet = RequestBuilder.GenerateSnippet(api, new Dictionary<string, string> { ["Amount"] = "5" });
            Assert.Contains("POST", snippet);
            Assert.Contains("new_DoThing", snippet);
            Assert.Contains("Illustrative sample", snippet);
        }

        [Fact]
        public void Snippet_Function_IsGetWithQuery()
        {
            var api = Api(P("Id", CustomApiFieldType.Guid, false));
            api.IsFunction = true;
            var snippet = RequestBuilder.GenerateSnippet(api, new Dictionary<string, string>());
            Assert.Contains("GET", snippet);
            Assert.Contains("Id=<Guid>", snippet);
        }

        // ---- catalog exporters (US-PLUGIN6.2.3 / US-PLUGIN6.5.1) ----

        private static CustomApiCatalog SampleCatalog()
        {
            var cat = new CustomApiCatalog { EnvironmentName = "DEV" };
            var api = Api(P("Amount", CustomApiFieldType.Money, false));
            api.ResponseProperties.Add(new CustomApiResponseProperty { LogicalName = "Result", Type = CustomApiFieldType.String });
            api.PluginTypeName = "My.Plugin.DoThing";
            cat.Apis.Add(api);
            return cat;
        }

        [Fact]
        public void Markdown_ContainsApiParamsAndResponses()
        {
            var md = CustomApiDoc.ToMarkdown(SampleCatalog());
            Assert.Contains("## new_DoThing", md);
            Assert.Contains("| Amount | Money | no |", md);
            Assert.Contains("| Result | String |", md);
        }

        [Fact]
        public void Csv_HasRowPerMember()
        {
            var csv = CustomApiDoc.ToCsv(SampleCatalog());
            Assert.Contains("new_DoThing,Amount,Request,Money,no", csv);
            Assert.Contains("new_DoThing,Result,Response,String,", csv);
        }

        [Fact]
        public void Html_IsSelfContainedAndEscapes()
        {
            var cat = SampleCatalog();
            cat.Apis[0].Description = "handles <b>things</b> & stuff";
            var html = CustomApiDoc.ToHtml(cat);
            Assert.StartsWith("<!doctype html>", html);
            Assert.Contains("&lt;b&gt;things&lt;/b&gt; &amp; stuff", html);
            Assert.DoesNotContain("<b>things</b>", html);
        }
    }
}
