using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.FlowDependencyAnalyzer.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Flow Dependency Analyzer's pure logic — the clientdata parser, the risk rules,
    /// and the reverse component-impact map (US-PA1.2.x / 1.3.x / 1.4.x / 1.5.x / 1.6.x). No Dataverse, no
    /// WinForms. The SDK collector (FlowCollector) is manual-tested against a live org.
    /// </summary>
    public class FlowDependencyAnalyzerTests
    {
        // A representative solution-aware cloud flow: Dataverse trigger, a Dataverse read with $select, a
        // connection-reference-bound connector, a direct-connection connector, an HTTP action with a secret
        // URL + bearer token, a child flow, a custom-API (unbound action) call, an env-var reference, and a
        // hardcoded environment URL/GUID.
        private const string ClientData = @"{
  ""properties"": {
    ""connectionReferences"": {
      ""shared_commondataserviceforapps"": { ""connection"": { ""connectionReferenceLogicalName"": ""new_dataverseconn"" } },
      ""shared_office365"": { ""connection"": { ""connectionReferenceLogicalName"": ""new_office365conn"" } }
    },
    ""definition"": {
      ""triggers"": {
        ""When_a_row_is_updated"": {
          ""type"": ""OpenApiConnectionWebhook"",
          ""inputs"": {
            ""host"": { ""connectionName"": ""shared_commondataserviceforapps"", ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps"", ""operationId"": ""SubscribeWebhookTrigger"" },
            ""parameters"": { ""subscriptionRequest/entityname"": ""account"", ""subscriptionRequest/message"": ""2"" }
          }
        }
      },
      ""actions"": {
        ""Get_contact"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": { ""connectionName"": ""shared_commondataserviceforapps"", ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps"", ""operationId"": ""GetItem"" },
            ""parameters"": { ""entityName"": ""contacts"", ""$select"": ""firstname,lastname,emailaddress1"" }
          }
        },
        ""Send_an_email"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": { ""connectionName"": ""shared_office365"", ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_office365"", ""operationId"": ""SendEmailV2"" },
            ""parameters"": { ""emailMessage/To"": ""@parameters('new_recipientvar')"" }
          }
        },
        ""Call_unbound_action"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": { ""connectionName"": ""shared_commondataserviceforapps"", ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps"", ""operationId"": ""PerformUnboundAction"" },
            ""parameters"": { ""actionName"": ""new_RecalculatePremium"" }
          }
        },
        ""Run_a_Child_Flow"": { ""type"": ""Workflow"", ""inputs"": { ""host"": { ""workflowReferenceName"": ""12345678-1234-1234-1234-1234567890ab"" } } },
        ""HTTP_Post"": {
          ""type"": ""Http"",
          ""inputs"": { ""method"": ""POST"", ""uri"": ""https://secret.example.com/webhook?sig=abc123secret"", ""headers"": { ""Authorization"": ""Bearer supersecrettoken12345"" } }
        },
        ""Direct_SharePoint"": {
          ""type"": ""OpenApiConnection"",
          ""inputs"": {
            ""host"": { ""connectionName"": ""aef1e2c3-d4b5-4600-9876-5432100fedcb"", ""apiId"": ""/providers/Microsoft.PowerApps/apis/shared_sharepointonline"", ""operationId"": ""GetItems"" },
            ""parameters"": { ""dataset"": ""https://contoso.sharepoint.com/sites/x"" }
          }
        },
        ""Compose_hardcoded"": { ""type"": ""Compose"", ""inputs"": ""https://contoso.crm.dynamics.com/api/data/v9.2/accounts(00000000-0000-0000-0000-000000000099)"" }
      }
    }
  }
}";

        private static FlowDependencies Parse() => FlowClientDataParser.Parse("Order flow", ClientData);

        // ---------------------------------------------------------------- Trigger (US-PA1.2.1)

        [Fact]
        public void Parse_DataverseTrigger_ResolvesTypeEntityAndMessage()
        {
            var dep = Parse();
            Assert.Equal("Dataverse", dep.TriggerType);
            Assert.Equal("account", dep.TriggerEntity);
            Assert.Equal("Update", dep.TriggerMessage); // SdkMessage code 2 → Update
        }

        // Regression: Dataverse trigger message codes must map to the correct CRUD type.
        // Code 2=Update and 3=Delete were previously swapped, silently mislabeling the two most
        // common triggers (US-PA1.2.1).
        [Theory]
        [InlineData("1", "Create")]
        [InlineData("2", "Update")]
        [InlineData("3", "Delete")]
        [InlineData("4", "CreateOrUpdate")]
        public void Parse_TriggerMessageCode_MapsToCorrectCrudType(string code, string expected)
        {
            var json = @"{ ""properties"": { ""definition"": { ""triggers"": { ""t"": {
                ""type"": ""OpenApiConnectionWebhook"",
                ""inputs"": { ""host"": { ""connectionName"": ""shared_commondataserviceforapps"" },
                    ""parameters"": { ""subscriptionRequest/entityname"": ""account"", ""subscriptionRequest/message"": """ + code + @""" } } } } } } }";
            var dep = FlowClientDataParser.Parse("f", json);
            Assert.Equal(expected, dep.TriggerMessage);
        }

        // Regression: Power Automate built-in parameters ($connections, $authentication) are runtime/auth
        // references, not environment variables — they must not be collected as EVs (which would raise a
        // false "missing environment variable" finding).
        [Fact]
        public void Parse_DoesNotCollectBuiltinParametersAsEnvironmentVariables()
        {
            var json = @"{ ""properties"": { ""definition"": { ""actions"": { ""a"": {
                ""type"": ""OpenApiConnection"",
                ""inputs"": { ""x"": ""@parameters('$connections')['shared_x']['connectionId']"",
                              ""y"": ""@parameters('$authentication')"",
                              ""z"": ""@parameters('new_realvar')"" } } } } } }";
            var dep = FlowClientDataParser.Parse("f", json);
            Assert.DoesNotContain("$connections", dep.EnvironmentVariables);
            Assert.DoesNotContain("$authentication", dep.EnvironmentVariables);
            Assert.Contains("new_realvar", dep.EnvironmentVariables);
        }

        // ---------------------------------------------------------------- Tables & columns (US-PA1.2.2)

        [Fact]
        public void Parse_ExtractsTablesAndColumns()
        {
            var dep = Parse();
            Assert.Contains("account", dep.Tables);   // from the trigger
            Assert.Contains("contacts", dep.Tables);  // from the Get-a-row action
            Assert.Contains("contacts.firstname", dep.Columns);
            Assert.Contains("contacts.lastname", dep.Columns);
            Assert.Contains("contacts.emailaddress1", dep.Columns);
        }

        // ---------------------------------------------------------------- Connectors & conn refs (US-PA1.3.1)

        [Fact]
        public void Parse_ExtractsConnectorsAndConnectionReferences()
        {
            var dep = Parse();
            Assert.Contains("shared_commondataserviceforapps", dep.Connectors);
            Assert.Contains("shared_office365", dep.Connectors);
            Assert.Contains("shared_sharepointonline", dep.Connectors);
            Assert.Contains("new_dataverseconn", dep.ConnectionReferences);
            Assert.Contains("new_office365conn", dep.ConnectionReferences);
        }

        // ---------------------------------------------------------------- Direct connection (US-PA1.3.2)

        [Fact]
        public void Parse_DetectsDirectConnection()
        {
            var dep = Parse();
            Assert.True(dep.UsesDirectConnection); // Direct_SharePoint binds a raw connection GUID
        }

        // ---------------------------------------------------------------- Child flows & custom APIs (US-PA1.4.1)

        [Fact]
        public void Parse_ExtractsChildFlowsAndCustomApis()
        {
            var dep = Parse();
            Assert.Contains("12345678-1234-1234-1234-1234567890ab", dep.ChildFlows);
            Assert.Contains("new_RecalculatePremium", dep.CustomApis);
        }

        // ---------------------------------------------------------------- Environment variables (US-PA1.3.1)

        [Fact]
        public void Parse_ExtractsEnvironmentVariableReferences()
        {
            var dep = Parse();
            Assert.Contains("new_recipientvar", dep.EnvironmentVariables);
        }

        // ---------------------------------------------------------------- HTTP + secret redaction (US-PA1.4.2)

        [Fact]
        public void Parse_HttpActionRecorded_ButUrlsAndSecretsRedacted()
        {
            var dep = Parse();
            Assert.Contains("HTTP_Post", dep.HttpActions);

            // Nothing the parser stores may contain the endpoint URL, SAS signature, or bearer token.
            var everything = dep.HttpActions
                .Concat(dep.HardcodedLiterals)
                .Concat(dep.Connectors)
                .Concat(dep.Columns)
                .Concat(dep.Tables)
                .Concat(new[] { dep.TriggerEntity ?? "", dep.TriggerType ?? "" });

            foreach (var leak in new[] { "secret.example.com", "supersecrettoken12345", "sig=abc123secret", "Bearer " })
                Assert.DoesNotContain(everything, s => s.Contains(leak));

            // The HTTP endpoint is surfaced only as a redacted marker.
            Assert.Contains(dep.HardcodedLiterals, s => s.Contains("HTTP_Post") && s.Contains(FlowClientDataParser.Redacted));
        }

        [Fact]
        public void Parse_HardcodedHttpsUrl_IsRedactedNotStored()
        {
            var dep = Parse();
            // The absolute environment URL in the Compose action is flagged but the URL itself is redacted.
            Assert.Contains(dep.HardcodedLiterals, s => s.Contains("Compose_hardcoded") && s.Contains(FlowClientDataParser.Redacted));
            Assert.DoesNotContain(dep.HardcodedLiterals, s => s.Contains("contoso.crm.dynamics.com"));
        }

        // ---------------------------------------------------------------- Malformed input (never throws)

        [Theory]
        [InlineData("{ not valid json")]
        [InlineData("")]
        [InlineData(null)]
        public void Parse_Malformed_DegradesToParseNote_NoThrow(string bad)
        {
            var dep = FlowClientDataParser.Parse("Broken", bad);
            Assert.NotNull(dep);
            Assert.False(string.IsNullOrEmpty(dep.ParseNote));
            Assert.Empty(dep.Connectors);
        }

        // ---------------------------------------------------------------- Risk rules (US-PA1.3.2 / 1.5.x)

        [Fact]
        public void Rules_DirectConnection_IsHigh()
        {
            var analysis = Analyze(knownConnRefs: All("new_dataverseconn", "new_office365conn"), knownEnvVars: All("new_recipientvar"), knownTables: All("account", "contacts"));
            Assert.Contains(analysis.Findings, f => f.Severity == Severity.High && f.Title.Contains("direct connection"));
        }

        [Fact]
        public void Rules_HardcodedLiteral_IsMedium()
        {
            var analysis = Analyze(knownConnRefs: All("new_dataverseconn", "new_office365conn"), knownEnvVars: All("new_recipientvar"), knownTables: All("account", "contacts"));
            Assert.Contains(analysis.Findings, f => f.Severity == Severity.Medium && f.Title.StartsWith("Hardcoded"));
        }

        [Fact]
        public void Rules_MissingTable_IsCritical()
        {
            // Known tables exclude 'contacts' → Critical missing-table finding.
            var analysis = Analyze(knownConnRefs: All("new_dataverseconn", "new_office365conn"), knownEnvVars: All("new_recipientvar"), knownTables: All("account"));
            Assert.Contains(analysis.Findings, f => f.Severity == Severity.Critical && f.Component == "contacts" && f.Title.Contains("missing table"));
        }

        [Fact]
        public void Rules_MissingConnectionReference_And_EnvVar_AreHigh()
        {
            // Known conn refs exclude office365; known env vars empty → both High.
            var analysis = Analyze(knownConnRefs: All("new_dataverseconn"), knownEnvVars: All(), knownTables: All("account", "contacts"));
            Assert.Contains(analysis.Findings, f => f.Severity == Severity.High && f.Component == "new_office365conn" && f.Title.Contains("missing connection reference"));
            Assert.Contains(analysis.Findings, f => f.Severity == Severity.High && f.Component == "new_recipientvar" && f.Title.Contains("missing environment variable"));
        }

        [Fact]
        public void Rules_ResolutionUnavailable_RaisesNoFalseMissingFindings()
        {
            // null sets = resolution unavailable → no missing-metadata findings, and it never throws.
            var dep = Parse();
            var analysis = FlowRiskRules.Analyze(new[] { dep }, knownConnRefs: null, knownEnvVars: null, missing: new MissingLookup());
            Assert.DoesNotContain(analysis.Findings, f => f.Title.Contains("missing table"));
            Assert.DoesNotContain(analysis.Findings, f => f.Title.Contains("missing connection reference"));
            Assert.DoesNotContain(analysis.Findings, f => f.Title.Contains("missing environment variable"));
        }

        // ---------------------------------------------------------------- Reverse impact (US-PA1.6.1)

        [Fact]
        public void Impact_ReverseMap_ListsEveryFlowDependingOnAComponent()
        {
            var a = new FlowAnalysis();
            a.Flows.Add(FlowClientDataParser.Parse("Flow A", ClientData));
            a.Flows.Add(FlowClientDataParser.Parse("Flow B", ClientData));

            var impacted = a.ImpactedFlows("account");
            Assert.Equal(new[] { "Flow A", "Flow B" }, impacted);

            var byKind = a.ImpactedFlows(FlowComponentKind.Table, "contacts");
            Assert.Contains("Flow A", byKind);

            var map = a.BuildImpactMap();
            Assert.Contains(map, m => m.Kind == FlowComponentKind.ConnectionReference && m.Component == "new_office365conn"
                && m.ImpactedFlows.Contains("Flow A") && m.ImpactedFlows.Contains("Flow B"));
        }

        // ---------------------------------------------------------------- helpers

        private static FlowAnalysis Analyze(ISet<string> knownConnRefs, ISet<string> knownEnvVars, ISet<string> knownTables)
        {
            var dep = Parse();
            var missing = new MissingLookup { KnownTables = knownTables };
            return FlowRiskRules.Analyze(new[] { dep }, knownConnRefs, knownEnvVars, missing);
        }

        private static ISet<string> All(params string[] values) =>
            new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
    }
}
