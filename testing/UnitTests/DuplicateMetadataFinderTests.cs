using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.DuplicateMetadataFinder.Analysis;
using XrmToolSuite.DuplicateMetadataFinder.Reporting;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Duplicate Metadata Finder engine: text-similarity primitives, per-pair
    /// scoring, blocking by kind, threshold-driven grouping/clustering, and the recommended-primary
    /// rule. The Dataverse collector that produces the MetadataComponent list is manual-tested.
    /// Traces to US-ADMIN3.2 / US-ADMIN3.3 / US-ADMIN3.4.
    /// </summary>
    public class DuplicateMetadataFinderTests
    {
        // ---- text-similarity primitives (US-ADMIN3.2.1) ----

        [Fact]
        public void Normalize_StripsCaseAndSeparators()
        {
            Assert.Equal("phonenumber", TextSimilarity.Normalize("Phone_Number"));
            Assert.Equal("phonenumber", TextSimilarity.Normalize("  phone number "));
            Assert.Equal("", TextSimilarity.Normalize(null));
        }

        [Fact]
        public void NameRatio_IdenticalAfterNormalize_IsOne()
        {
            Assert.Equal(1d, TextSimilarity.NameRatio("Phone Number", "phone_number"), 3);
        }

        [Fact]
        public void NameRatio_TwoBlanks_IsZero_NotOne()
        {
            // Two empty names must not read as a perfect duplicate.
            Assert.Equal(0d, TextSimilarity.NameRatio("", null), 3);
        }

        [Fact]
        public void Levenshtein_KnownDistance()
        {
            Assert.Equal(0, TextSimilarity.Levenshtein("abc", "abc"));
            Assert.Equal(1, TextSimilarity.Levenshtein("abc", "abd"));
            Assert.Equal(3, TextSimilarity.Levenshtein("abc", ""));
        }

        [Fact]
        public void Tokenize_SplitsCamelCaseAndSeparators()
        {
            Assert.Equal(new[] { "new", "phone", "number" }, TextSimilarity.Tokenize("new_PhoneNumber"));
        }

        [Fact]
        public void Jaccard_OverlapRatio()
        {
            // {a,b} ∩ {b,c} = {b} (1) over union {a,b,c} (3) = 1/3.
            Assert.Equal(1d / 3d, TextSimilarity.Jaccard(new[] { "a", "b" }, new[] { "b", "c" }), 3);
            Assert.Equal(1d, TextSimilarity.Jaccard(new[] { "a", "b" }, new[] { "b", "a" }), 3);
            Assert.Equal(0d, TextSimilarity.Jaccard(new string[0], new string[0]), 3);
        }

        [Fact]
        public void ContentHash_StableAndContentSensitive()
        {
            Assert.Equal(TextSimilarity.ContentHash("x"), TextSimilarity.ContentHash("x"));
            Assert.NotEqual(TextSimilarity.ContentHash("x"), TextSimilarity.ContentHash("y"));
        }

        // ---- pair scoring (US-ADMIN3.2 / US-ADMIN3.3.2) ----

        private static MetadataComponent Col(string key, string display, string schema,
            string type = "String", int usage = 0, bool managed = false) =>
            new MetadataComponent
            {
                Kind = ComponentKind.Column,
                Key = key,
                DisplayName = display,
                SchemaName = schema,
                DataType = type,
                UsageCount = usage,
                IsManaged = managed,
                IsCustom = true,
            };

        [Fact]
        public void Score_IdenticalColumns_Is100()
        {
            var pair = SimilarityEngine.Score(
                Col("account.new_phone", "Phone Number", "new_phone"),
                Col("contact.new_phone2", "Phone Number", "new_phone"));
            Assert.Equal(100, pair.Score);
        }

        [Fact]
        public void Score_DifferentType_LowersScore()
        {
            var same = SimilarityEngine.Score(
                Col("a", "Amount", "new_amount", "Money"),
                Col("b", "Amount", "new_amount2", "Money")).Score;
            var diff = SimilarityEngine.Score(
                Col("a", "Amount", "new_amount", "Money"),
                Col("b", "Amount", "new_amount2", "String")).Score;
            Assert.True(diff < same, $"expected type mismatch to lower score ({diff} < {same})");
        }

        [Fact]
        public void Score_CrossKind_IsZeroAndNeverGroups()
        {
            var col = Col("a", "Status", "new_status");
            var form = new MetadataComponent { Kind = ComponentKind.Form, Key = "f", DisplayName = "Status" };
            Assert.Equal(0, SimilarityEngine.Score(col, form).Score);
        }

        [Fact]
        public void Score_IdenticalContentHash_ShortCircuitsTo100_Exact()
        {
            var a = new MetadataComponent { Kind = ComponentKind.WebResource, Key = "wr1", DisplayName = "a.js", ContentHash = "H" };
            var b = new MetadataComponent { Kind = ComponentKind.WebResource, Key = "wr2", DisplayName = "totally_different.js", ContentHash = "H" };
            var pair = SimilarityEngine.Score(a, b);
            Assert.Equal(100, pair.Score);
            Assert.True(pair.IsExactContentMatch);
        }

        [Fact]
        public void Score_OptionSetOverlap_Contributes()
        {
            MetadataComponent Opt(string key, IReadOnlyList<string> vals) => new MetadataComponent
            {
                Kind = ComponentKind.OptionSet, Key = key, DisplayName = "Status Reason",
                SchemaName = "new_statusreason", DataType = "Picklist", OptionValues = vals,
            };
            var high = SimilarityEngine.Score(
                Opt("a", new[] { "Open", "Closed", "Pending" }),
                Opt("b", new[] { "Open", "Closed", "Pending" }));
            var low = SimilarityEngine.Score(
                Opt("a", new[] { "Open", "Closed", "Pending" }),
                Opt("b", new[] { "Red", "Green", "Blue" }));
            Assert.True(high.Score > low.Score);
            Assert.Contains(high.Factors, f => f.Name == "option-overlap");
        }

        // ---- grouping / clustering (US-ADMIN3.4.1) ----

        [Fact]
        public void Group_ClustersTransitiveDuplicates_AcrossContainers()
        {
            var comps = new[]
            {
                Col("account.new_phone", "Phone Number", "new_phone", usage: 5),
                Col("contact.new_phone", "Phone Number", "new_phonenum", usage: 2),
                Col("lead.new_phone", "Phone Number", "new_phone_num", usage: 1),
                Col("account.new_email", "Email Address", "new_email"), // unrelated
            };
            var result = SimilarityEngine.Group(comps, threshold: 80);
            var group = Assert.Single(result.Groups);
            Assert.Equal(3, group.Members.Count);
            Assert.Equal(ComponentKind.Column, group.Kind);
        }

        [Fact]
        public void Group_ThresholdFiltersWeakPairs()
        {
            var comps = new[]
            {
                Col("a", "Customer Phone", "new_custphone"),
                Col("b", "Vendor Address", "new_vendaddr"),
            };
            Assert.Empty(SimilarityEngine.Group(comps, threshold: 80).Groups);
        }

        [Fact]
        public void Group_RecommendsMostReferencedPrimary()
        {
            var comps = new[]
            {
                Col("account.new_phone", "Phone Number", "new_phone", usage: 1),
                Col("contact.new_phone", "Phone Number", "new_phone2", usage: 9),
            };
            var group = Assert.Single(SimilarityEngine.Group(comps, threshold: 80).Groups);
            Assert.Equal("contact.new_phone", group.RecommendedPrimary.Key);
            Assert.Contains("most referenced", group.RecommendationReason());
        }

        [Fact]
        public void Group_TieBreaksTowardManagedDeterministically()
        {
            var comps = new[]
            {
                Col("a.new_x", "Region", "new_region", usage: 3, managed: false),
                Col("b.new_x", "Region", "new_region2", usage: 3, managed: true),
            };
            var group = Assert.Single(SimilarityEngine.Group(comps, threshold: 80).Groups);
            Assert.True(group.RecommendedPrimary.IsManaged);
        }

        [Fact]
        public void Group_RanksWorstFirst()
        {
            var comps = new[]
            {
                Col("a1", "Phone Number", "new_phone"),
                Col("a2", "Phone Number", "new_phone"),           // identical -> 100
                Col("b1", "Customer Region", "new_custregion"),
                Col("b2", "Customer Regions", "new_custregions"),  // near -> < 100
            };
            var ranked = SimilarityEngine.Group(comps, threshold: 70).Ranked().ToList();
            Assert.Equal(2, ranked.Count);
            Assert.True(ranked[0].TopScore >= ranked[1].TopScore);
            Assert.Equal(100, ranked[0].TopScore);
        }

        [Fact]
        public void Group_EmptyOrSingle_NoGroups()
        {
            Assert.Empty(SimilarityEngine.Group(null, 80).Groups);
            Assert.Empty(SimilarityEngine.Group(new[] { Col("a", "Phone", "new_phone") }, 80).Groups);
        }

        // ---- ReportModel projection (US-ADMIN3.5.1) ----

        [Fact]
        public void Report_Projects_MetricsAndOneFindingPerGroup()
        {
            var scan = SimilarityEngine.Group(new[]
            {
                Col("account.new_phone", "Phone Number", "new_phone", usage: 5),
                Col("contact.new_phone", "Phone Number", "new_phone2", usage: 1),
                Col("a.new_region", "Region", "new_region"),
                Col("b.new_region", "Region", "new_region2"),
            }, threshold: 80);
            scan.EnvironmentName = "DEV";

            var model = DuplicateReport.ToReportModel(scan);
            Assert.Equal("Duplicate Metadata Finder", model.ToolName);
            Assert.Equal(scan.GroupCount, model.Findings.Count);
            Assert.Contains(model.Metrics, m => m.Label == "Duplicate groups" && m.Value == "2");
            // 2 groups of 2 -> 2 redundant -> score 10 -> Low band.
            Assert.Equal(10, model.Score);
            Assert.Equal(Core.Analysis.ScoreBand.Low, model.Band);
        }

        [Fact]
        public void Report_EmptyScan_ZeroScoreNoFindings()
        {
            var scan = SimilarityEngine.Group(new[]
            {
                Col("a", "Alpha", "new_alpha"),
                Col("b", "Omega", "new_omega"),
            }, threshold: 80);
            var model = DuplicateReport.ToReportModel(scan);
            Assert.Equal(0, model.Score);
            Assert.Empty(model.Findings);
            Assert.Contains("No duplicate groups", model.LeadIn);
        }

        [Fact]
        public void Report_ExactContentMatch_IsHighSeverity()
        {
            var a = new MetadataComponent { Kind = ComponentKind.WebResource, Key = "wr1", DisplayName = "a.js", ContentHash = "H" };
            var b = new MetadataComponent { Kind = ComponentKind.WebResource, Key = "wr2", DisplayName = "b.js", ContentHash = "H" };
            var scan = SimilarityEngine.Group(new[] { a, b }, threshold: 80);
            var group = Assert.Single(scan.Groups);
            Assert.Equal(Core.Analysis.Severity.High, DuplicateReport.SeverityFor(group));
        }

        [Fact]
        public void Report_ScoreCapsAt100()
        {
            var comps = Enumerable.Range(0, 40)
                .Select(i => Col("t" + i + ".new_phone", "Phone Number", "new_phone" + i))
                .ToArray();
            var scan = SimilarityEngine.Group(comps, threshold: 80);
            Assert.Equal(100, DuplicateReport.Score(scan));
        }
    }
}
