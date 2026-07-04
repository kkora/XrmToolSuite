using System;
using System.Collections.Generic;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Flows &amp; Plugins analyzer's flow paths (draft state + broken
    /// connection references parsed from clientdata) and the plugin-step conflict checks
    /// (duplicate registrations, execution-rank collisions), which read only the step's own columns.
    /// The plugin-step <em>health</em> checks (missing type/assembly, missing target table) rely on
    /// aliased LEFT-OUTER joins and stay in the manual suite. Traces to US-DG-5 (flows &amp; plugins).
    /// </summary>
    public class FlowPluginAnalyzerTests
    {
        // workflow.category: 0 = classic workflow, 4 = BPF, 5 = modern (cloud) flow. statecode 0 = Draft, 1 = Activated.
        private const int CatClassic = 0;
        private const int CatCloud = 5;
        private const int StateDraft = 0;
        private const int StateActivated = 1;

        // sdkmessageprocessingstep: stage 40 = Post-operation; mode 0 = synchronous, 1 = asynchronous; state 1 = Disabled.
        private const int StagePost = 40;
        private const int ModeSync = 0;
        private const int StepDisabled = 1;

        private static List<RiskFinding> Run(FakeOrganizationService source)
        {
            // Flow analysis reads workflow + connectionreference; plugin scan reads an (empty) step table.
            source.SeedIfAbsent("connectionreference").SeedIfAbsent("sdkmessageprocessingstep");
            var ctx = TestData.Context(source);
            return new FlowPluginAnalyzer().Analyze(ctx, _ => { });
        }

        // TC-DG-FP-01: an OFF (draft) cloud flow is flagged Medium (managed import preserves OFF state).
        [Fact]
        public void DraftCloudFlow_FlagsMedium()
        {
            var source = new FakeOrganizationService()
                .Seed("workflow", TestData.Flow("Notify on create", CatCloud, StateDraft, clientData: "{}"));

            var f = Assert.Single(Run(source), x => x.Title == "Cloud flow is OFF (draft)");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // TC-DG-FP-02: a draft classic process is flagged Low.
        [Fact]
        public void DraftClassicProcess_FlagsLow()
        {
            var source = new FakeOrganizationService()
                .Seed("workflow", TestData.Flow("Legacy WF", CatClassic, StateDraft));

            var f = Assert.Single(Run(source), x => x.Title == "Process is in Draft state");
            Assert.Equal(Severity.Low, f.Severity);
        }

        // TC-DG-FP-03: an activated cloud flow with a resolvable connection reference produces no finding.
        [Fact]
        public void ActivatedFlow_KnownConnectionRef_NoFinding()
        {
            var clientData = @"{ ""properties"": { ""connectionReferences"": {
                ""shared_office365"": { ""connection"": { ""connectionReferenceLogicalName"": ""contoso_mailbox"" } } } } }";
            var source = new FakeOrganizationService()
                .Seed("workflow", TestData.Flow("Send email", CatCloud, StateActivated, clientData))
                .Seed("connectionreference", TestData.ConnRef("contoso_mailbox"));

            Assert.Empty(Run(source));
        }

        // TC-DG-FP-04: a cloud flow referencing a connection reference absent from the env is flagged High.
        [Fact]
        public void Flow_MissingConnectionRef_FlagsHigh()
        {
            var clientData = @"{ ""properties"": { ""connectionReferences"": {
                ""shared_office365"": { ""connection"": { ""connectionReferenceLogicalName"": ""contoso_ghost"" } } } } }";
            var source = new FakeOrganizationService()
                .Seed("workflow", TestData.Flow("Broken flow", CatCloud, StateActivated, clientData))
                .Seed("connectionreference"); // none known in the environment

            var f = Assert.Single(Run(source), x => x.Title == "Flow references missing connection reference");
            Assert.Equal(Severity.High, f.Severity);
            Assert.Contains("contoso_ghost", f.Description);
        }

        // TC-DG-FP-05: the same plugin type registered twice on one event (no filters) is a duplicate → High.
        [Fact]
        public void SameType_SameEvent_NoFilters_FlagsDuplicateHigh()
        {
            Guid type = Guid.NewGuid(), message = Guid.NewGuid(), filter = Guid.NewGuid();
            var source = new FakeOrganizationService().Seed("sdkmessageprocessingstep",
                TestData.Step("Account: post-create A", type, message, filter, StagePost, ModeSync),
                TestData.Step("Account: post-create B", type, message, filter, StagePost, ModeSync));

            var f = Assert.Single(Run(source), x => x.Title == "Duplicate SDK step registration");
            Assert.Equal(Severity.High, f.Severity);
        }

        // TC-DG-FP-06: same type on one event but disjoint filtering attributes do not overlap → no duplicate.
        [Fact]
        public void SameType_DisjointFilters_NoDuplicate()
        {
            Guid type = Guid.NewGuid(), message = Guid.NewGuid(), filter = Guid.NewGuid();
            var source = new FakeOrganizationService().Seed("sdkmessageprocessingstep",
                TestData.Step("On name change", type, message, filter, StagePost, ModeSync, filteringAttributes: "name"),
                TestData.Step("On phone change", type, message, filter, StagePost, ModeSync, filteringAttributes: "telephone1"));

            Assert.DoesNotContain(Run(source), x => x.Title == "Duplicate SDK step registration");
        }

        // TC-DG-FP-07: same type where one step filters a subset the other (all-attributes) covers → overlap → duplicate.
        [Fact]
        public void SameType_OverlappingFilters_FlagsDuplicate()
        {
            Guid type = Guid.NewGuid(), message = Guid.NewGuid(), filter = Guid.NewGuid();
            var source = new FakeOrganizationService().Seed("sdkmessageprocessingstep",
                TestData.Step("On any change", type, message, filter, StagePost, ModeSync), // no filter = all attributes
                TestData.Step("On name change", type, message, filter, StagePost, ModeSync, filteringAttributes: "name"));

            Assert.Single(Run(source), x => x.Title == "Duplicate SDK step registration");
        }

        // TC-DG-FP-08: a duplicate where one step is disabled is not a runtime double-execution → no finding.
        [Fact]
        public void SameType_OneDisabled_NoDuplicate()
        {
            Guid type = Guid.NewGuid(), message = Guid.NewGuid(), filter = Guid.NewGuid();
            var source = new FakeOrganizationService().Seed("sdkmessageprocessingstep",
                TestData.Step("Active", type, message, filter, StagePost, ModeSync),
                TestData.Step("Disabled", type, message, filter, StagePost, ModeSync, stateCode: StepDisabled));

            Assert.DoesNotContain(Run(source), x => x.Title == "Duplicate SDK step registration");
        }

        // TC-DG-FP-09: different plugin types sharing a rank on one event run non-deterministically → Medium.
        [Fact]
        public void DifferentTypes_SameRank_FlagsRankConflictMedium()
        {
            Guid message = Guid.NewGuid(), filter = Guid.NewGuid();
            var source = new FakeOrganizationService().Seed("sdkmessageprocessingstep",
                TestData.Step("Plugin A", Guid.NewGuid(), message, filter, StagePost, ModeSync, rank: 1),
                TestData.Step("Plugin B", Guid.NewGuid(), message, filter, StagePost, ModeSync, rank: 1));

            var f = Assert.Single(Run(source), x => x.Title == "Plugin steps share an execution rank");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // TC-DG-FP-10: different types at distinct ranks on one event are deterministic → no rank conflict.
        [Fact]
        public void DifferentTypes_DistinctRanks_NoRankConflict()
        {
            Guid message = Guid.NewGuid(), filter = Guid.NewGuid();
            var source = new FakeOrganizationService().Seed("sdkmessageprocessingstep",
                TestData.Step("Plugin A", Guid.NewGuid(), message, filter, StagePost, ModeSync, rank: 1),
                TestData.Step("Plugin B", Guid.NewGuid(), message, filter, StagePost, ModeSync, rank: 2));

            Assert.DoesNotContain(Run(source), x => x.Title == "Plugin steps share an execution rank");
        }
    }
}
