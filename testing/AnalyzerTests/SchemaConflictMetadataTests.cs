using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>
    /// Executable tests for the Data Model Conflicts analyzer's METADATA-level branches — attribute type
    /// mismatch, string-length shrink, choice (option set) conflicts, and relationship schema-name
    /// collisions. These were previously manual-only because they need constructed source + target
    /// <see cref="EntityMetadata"/>; they are now seeded headlessly via <see cref="MetaBuilder"/> (reflection).
    /// Traces to US-ALM07-6 (schema conflicts). TC-ALM07-SC-06..10.
    /// </summary>
    public class SchemaConflictMetadataTests
    {
        private const string Table = "new_account";

        /// <summary>
        /// Wire a source solution that owns one custom table (<see cref="Table"/>), with the given source and
        /// target entity detail, and run the analyzer. No target solution row is seeded, so the version/managed
        /// checks stay silent and only the metadata comparisons fire.
        /// </summary>
        private static List<RiskFinding> Run(EntityMetadata sourceDetail, EntityMetadata targetDetail)
        {
            var solution = TestData.Solution(uniqueName: "contoso_core", version: "1.0.0.0", managed: true);
            var entityId = System.Guid.NewGuid();

            var source = new FakeOrganizationService()
                .Seed("solutioncomponent", TestData.SolutionComponent(AnalyzerContext.CT_Entity, entityId, solution.Id))
                .SeedAllEntities(MetaBuilder.Entity(Table, metadataId: entityId))
                .SeedEntityDetail(Table, sourceDetail);

            var target = new FakeOrganizationService()
                .SeedEntityDetail(Table, targetDetail);

            var ctx = TestData.Context(source, target, solution);
            return new SchemaConflictAnalyzer().Analyze(ctx, _ => { });
        }

        private static EntityMetadata Detail(params AttributeMetadata[] attributes) =>
            MetaBuilder.Entity(Table, attributes: attributes);

        // TC-ALM07-SC-06: an attribute whose type differs between source and target is a Critical import blocker.
        [Fact]
        public void AttributeTypeMismatch_FlagsCritical()
        {
            var findings = Run(
                Detail(MetaBuilder.StringAttr("new_field")),
                Detail(MetaBuilder.IntAttr("new_field")));

            var f = Assert.Single(findings, x => x.Title == "Attribute type mismatch");
            Assert.Equal(Severity.Critical, f.Severity);
            Assert.Equal("new_account.new_field", f.AffectedComponent);
        }

        // TC-ALM07-SC-07: shrinking a string column's max length below the target's is a High conflict.
        [Fact]
        public void StringLengthShrink_FlagsHigh()
        {
            var findings = Run(
                Detail(MetaBuilder.StringAttr("new_field", maxLength: 100)),
                Detail(MetaBuilder.StringAttr("new_field", maxLength: 200)));

            Assert.Contains(findings, x => x.Title == "Column max length reduced" && x.Severity == Severity.High);
        }

        // TC-ALM07-SC-08: the same choice value carrying different labels is a Medium conflict.
        [Fact]
        public void ChoiceLabelConflict_FlagsMedium()
        {
            var findings = Run(
                Detail(MetaBuilder.PicklistAttr("new_choice", (1, "Alpha"))),
                Detail(MetaBuilder.PicklistAttr("new_choice", (1, "Beta"))));

            Assert.Contains(findings, x => x.Title == "Choice value label conflict" && x.Severity == Severity.Medium);
        }

        // TC-ALM07-SC-09: a choice value that exists in target but not source is deleted on upgrade -> High.
        [Fact]
        public void ChoiceValueRemoved_FlagsHigh()
        {
            var findings = Run(
                Detail(MetaBuilder.PicklistAttr("new_choice", (1, "Alpha"))),
                Detail(MetaBuilder.PicklistAttr("new_choice", (1, "Alpha"), (2, "Gamma"))));

            Assert.Contains(findings, x => x.Title == "Choice value removed" && x.Severity == Severity.High);
        }

        // TC-ALM07-SC-10: a relationship whose shape differs under the same schema name is a High collision.
        [Fact]
        public void RelationshipShapeCollision_FlagsHigh()
        {
            var src = MetaBuilder.Entity(Table, oneToMany: new[]
            {
                MetaBuilder.OneToMany("new_rel", referencedEntity: "account", referencingEntity: "contact", referencingAttribute: "new_accountid")
            });
            var tgt = MetaBuilder.Entity(Table, oneToMany: new[]
            {
                MetaBuilder.OneToMany("new_rel", referencedEntity: "account", referencingEntity: "lead", referencingAttribute: "new_accountid")
            });

            Assert.Contains(Run(src, tgt), x => x.Title == "Relationship schema name collision" && x.Severity == Severity.High);
        }
    }
}
