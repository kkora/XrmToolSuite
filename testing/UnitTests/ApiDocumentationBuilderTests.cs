using System.Text.RegularExpressions;
using Xunit;
using XrmToolSuite.ApiDocumentationBuilder.Api;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the API Documentation Builder's SDK-free core: the <see cref="Redactor"/> safety
    /// engine, the <see cref="ApiDocEmitters"/> (Markdown/HTML/JSON + example payloads), and the
    /// <see cref="OpenApiEmitter"/>. Traces to US-DOC06.2.x / 4.x / 5.x. The SDK collector (ApiCollector) and
    /// the WinForms host are manual-tested (need a live connection).
    /// </summary>
    public class ApiDocumentationBuilderTests
    {
        private static ApiCatalog Sample()
        {
            var api = new ApiDoc
            {
                UniqueName = "contoso_Recalculate", DisplayName = "Recalculate",
                Description = "Recalculates totals. See https://api.contoso.com/hook?sig=SECRETSIG for the webhook.",
                BindingType = ApiBindingType.Entity, BoundEntityLogicalName = "account",
                IsFunction = false, IsPrivate = false, PluginTypeName = "Contoso.Plugins.Recalculate"
            };
            api.Parameters.Add(new ApiParameter { UniqueName = "Target", Type = ApiFieldType.EntityReference, IsOptional = false });
            api.Parameters.Add(new ApiParameter { UniqueName = "AsOfDate", Type = ApiFieldType.DateTime, IsOptional = true });
            api.Parameters.Add(new ApiParameter { UniqueName = "ApiKey", Type = ApiFieldType.String, IsOptional = false });
            api.ResponseProperties.Add(new ApiResponseProperty { UniqueName = "Total", Type = ApiFieldType.Money });

            var cat = new ApiCatalog { EnvironmentName = "Contoso DEV" };
            cat.Apis.Add(api);
            return cat;
        }

        // TC-DOC06-RED-01 (US-DOC06.4.2): a secret-named parameter gets a masked sample, not a real value.
        [Fact]
        public void Redactor_MasksSecretNamedParameterSample()
        {
            var r = new Redactor();
            Assert.True(r.IsSensitiveName("ApiKey"));
            Assert.True(r.IsSensitiveName("clientSecret"));
            Assert.False(r.IsSensitiveName("Target"));
            var secret = new ApiParameter { UniqueName = "ApiKey", Type = ApiFieldType.String };
            Assert.Equal("\"" + Redactor.Mask + "\"", r.SampleFor(secret));
            var normal = new ApiParameter { UniqueName = "AsOfDate", Type = ApiFieldType.DateTime };
            Assert.DoesNotContain(Redactor.Mask, r.SampleFor(normal));
        }

        // TC-DOC06-RED-02 (US-DOC06.4.2): free text has bearer tokens and URL query strings (SAS/trigger secrets) stripped.
        [Fact]
        public void Redactor_StripsBearerAndUrlSecrets()
        {
            var r = new Redactor();
            Assert.Equal("Bearer " + Redactor.Mask, r.RedactText("Bearer abc.DEF-123_xyz="));
            var masked = r.RedactText("Call https://host/api?sig=SECRET now");
            Assert.Contains("https://host/api?" + Redactor.Mask, masked);
            Assert.DoesNotContain("SECRET", masked);
        }

        // TC-DOC06-RED-03 (US-DOC06.4.2): operator-supplied extra redaction terms are honoured.
        [Fact]
        public void Redactor_HonoursUserSuppliedTerms()
        {
            var r = new Redactor(new[] { "ssn" });
            Assert.True(r.IsSensitiveName("CustomerSSN"));
        }

        // TC-DOC06-EX-04 (US-DOC06.4.1): example request is a template with secret params masked.
        [Fact]
        public void ExampleRequest_IsTemplateWithSecretsMasked()
        {
            var api = Sample().Apis[0];
            var json = ApiDocEmitters.ExampleRequestJson(api, new Redactor());
            Assert.Contains("\"Target\":", json);
            Assert.Contains("\"AsOfDate\":\"2020-01-01T00:00:00Z\"", json);
            Assert.Contains("\"ApiKey\":\"" + Redactor.Mask + "\"", json); // secret param masked
        }

        // TC-DOC06-MD-05 (US-DOC06.2.2 / 5.1): Markdown documents params/responses + a labelled example, secrets redacted.
        [Fact]
        public void Markdown_DocumentsApiWithRedactedExample()
        {
            var md = ApiDocEmitters.Markdown(Sample());
            Assert.Contains("# Custom API reference", md);
            Assert.Contains("## Recalculate", md);
            Assert.Contains("| Name | Type | Required | Description |", md);
            Assert.Contains("`Target`", md);
            Assert.Contains("Bound to account", md);
            Assert.Contains("template", md);                         // examples labelled as templates
            Assert.DoesNotContain("SECRETSIG", md);                  // URL secret in description stripped
        }

        // TC-DOC06-HTML-06 (US-DOC06.5.1): HTML is self-contained + theme-aware and escapes content.
        [Fact]
        public void Html_IsSelfContainedThemeAware()
        {
            var cat = Sample();
            cat.Apis[0].DisplayName = "<script>x</script>";
            var html = ApiDocEmitters.Html(cat);
            Assert.StartsWith("<!DOCTYPE html>", html);
            Assert.Contains("prefers-color-scheme:dark", html);
            Assert.Contains("&lt;script&gt;", html);
            Assert.DoesNotContain("<script>x</script>", html);
        }

        // TC-DOC06-OAS-07 (US-DOC06.4.1 / 5.1): OpenAPI-style spec has a path/operation with request+response schemas.
        [Fact]
        public void OpenApi_HasPathWithRequestAndResponseSchemas()
        {
            var spec = OpenApiEmitter.Generate(Sample());
            Assert.Contains("\"openapi\":\"3.0.3\"", spec);
            Assert.Contains("\"/contoso_Recalculate\"", spec);
            Assert.Contains("\"operationId\":\"contoso_Recalculate\"", spec);
            Assert.Contains("\"requestBody\"", spec);
            Assert.Contains("\"responses\"", spec);
            // Required non-optional params are listed; the DateTime response type maps to number (Money).
            Assert.Contains("\"required\":[", spec);
        }

        // TC-DOC06-OAS-08 (US-DOC06.4.2): OpenAPI marks secret-named params with password format + x-redacted.
        [Fact]
        public void OpenApi_AnnotatesSecretParameters()
        {
            var spec = OpenApiEmitter.Generate(Sample());
            Assert.Contains("\"format\":\"password\"", spec);
            Assert.Contains("\"x-redacted\":true", spec);
        }

        // TC-DOC06-TYPE-09 (US-DOC06.2.2): field-type → OpenAPI schema mapping is correct for representative types.
        [Fact]
        public void FieldTypes_MapToOpenApiSchemas()
        {
            Assert.Equal("{\"type\":\"boolean\"}", FieldTypes.OpenApiSchema(ApiFieldType.Boolean));
            Assert.Equal("{\"type\":\"string\",\"format\":\"date-time\"}", FieldTypes.OpenApiSchema(ApiFieldType.DateTime));
            Assert.Equal("{\"type\":\"string\",\"format\":\"uuid\"}", FieldTypes.OpenApiSchema(ApiFieldType.Guid));
            Assert.Equal("{\"type\":\"array\",\"items\":{\"type\":\"string\"}}", FieldTypes.OpenApiSchema(ApiFieldType.StringArray));
        }

        // TC-DOC06-JSON-10 (US-DOC06.5.1): raw JSON model carries the APIs, params, and redacts the description URL.
        [Fact]
        public void Json_CarriesModelAndRedactsDescription()
        {
            var json = ApiDocEmitters.Json(Sample());
            Assert.Contains("\"uniqueName\":\"contoso_Recalculate\"", json);
            Assert.Contains("\"pluginType\":\"Contoso.Plugins.Recalculate\"", json);
            Assert.Equal(4, Regex.Matches(json, "\"name\":").Count);   // 3 request params + 1 response property
            Assert.DoesNotContain("SECRETSIG", json);
        }
    }
}
