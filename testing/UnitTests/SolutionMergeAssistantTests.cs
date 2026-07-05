using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.SolutionMergeAssistant.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the SDK-free merge comparison engine (<see cref="MergeRules.Compare"/>).
    /// All rules are pure functions of hand-built <see cref="SolutionInfo"/> / <see cref="ConfigItem"/>
    /// lists, so exact conflicts, severities, verdicts and determinism are asserted with no live org.
    /// Traces to EPIC-ALM1: US-ALM1.2.1/2.2 (overlap), US-ALM1.3.1 (publisher/version),
    /// US-ALM1.3.2 (managed state), US-ALM1.4.1 (env-var/conn-ref), US-ALM1.5.1 (verdict).
    /// </summary>
    public class SolutionMergeAssistantTests
    {
        // ---- fixtures -------------------------------------------------------------------------

        private static SolutionComponentRef Comp(int type, Guid id, string name, bool managed = false) =>
            new SolutionComponentRef
            {
                ComponentType = type,
                ComponentTypeName = ComponentTypes.Name(type),
                ObjectId = id,
                Name = name,
                IsManaged = managed
            };

        private static SolutionInfo Sol(string unique, string prefix = "new", string version = "1.0.0.0",
            bool managed = false, params SolutionComponentRef[] comps) =>
            new SolutionInfo
            {
                UniqueName = unique,
                FriendlyName = unique,
                Version = version,
                IsManaged = managed,
                PublisherPrefix = prefix,
                Components = comps.ToList()
            };

        // ---- overlap detection (US-ALM1.2.1 / US-ALM1.2.2) ------------------------------------

        [Fact]
        public void DuplicateComponent_AcrossTwoSolutions_IsFlagged()
        {
            var shared = Guid.NewGuid();
            var a = Sol("solA", comps: Comp(ComponentTypes.WebResource, shared, "new_script.js"));
            var b = Sol("solB", comps: Comp(ComponentTypes.WebResource, shared, "new_script.js"));

            var report = MergeRules.Compare(new[] { a, b });

            var dup = report.Findings.Single(f => f.Category == "Web Resources");
            Assert.Contains("Duplicate", dup.Title);
            Assert.Contains("new_script.js", dup.Component);
            Assert.Equal(Severity.Medium, dup.Severity); // web resource overwrite = Medium
        }

        [Fact]
        public void PluginAssemblyOverlap_IsHigh_AndCalledOutAsOwnCategory()
        {
            var shared = Guid.NewGuid();
            var a = Sol("solA", comps: Comp(ComponentTypes.PluginAssembly, shared, "Contoso.Plugins"));
            var b = Sol("solB", comps: Comp(ComponentTypes.PluginAssembly, shared, "Contoso.Plugins"));

            var report = MergeRules.Compare(new[] { a, b });

            var dup = report.Findings.Single(f => f.Category == "Plugin Assemblies");
            Assert.Equal(Severity.High, dup.Severity);
        }

        [Fact]
        public void NonOverlappingComponents_ProduceNoDuplicateFinding()
        {
            var a = Sol("solA", comps: Comp(ComponentTypes.WebResource, Guid.NewGuid(), "a.js"));
            var b = Sol("solB", comps: Comp(ComponentTypes.WebResource, Guid.NewGuid(), "b.js"));

            var report = MergeRules.Compare(new[] { a, b });

            Assert.DoesNotContain(report.Findings, f => f.Title.StartsWith("Duplicate"));
        }

        // ---- managed / unmanaged conflict (US-ALM1.3.2) ---------------------------------------

        [Fact]
        public void ManagedInOne_UnmanagedInAnother_IsHigh()
        {
            var shared = Guid.NewGuid();
            var managed = Sol("solManaged", managed: true,
                comps: Comp(ComponentTypes.WebResource, shared, "new_script.js", managed: true));
            var unmanaged = Sol("solUnmanaged", managed: false,
                comps: Comp(ComponentTypes.WebResource, shared, "new_script.js", managed: false));

            var report = MergeRules.Compare(new[] { managed, unmanaged });

            var conflict = report.Findings.Single(f => f.Category == "Managed State");
            Assert.Equal(Severity.High, conflict.Severity);
            Assert.Equal(MergeVerdict.HighRisk, report.Verdict);
        }

        // ---- publisher prefix mismatch (US-ALM1.3.1) ------------------------------------------

        [Fact]
        public void PublisherPrefixMismatch_IsMedium_AndPicksStandard()
        {
            var shared = Guid.NewGuid();
            // 'contoso' owns more components, so it is the recommended standard prefix.
            var a = Sol("solA", prefix: "contoso",
                comps: new[]
                {
                    Comp(ComponentTypes.WebResource, shared, "s.js"),
                    Comp(ComponentTypes.SystemForm, Guid.NewGuid(), "Main")
                });
            var b = Sol("solB", prefix: "new",
                comps: Comp(ComponentTypes.WebResource, shared, "s.js"));

            var report = MergeRules.Compare(new[] { a, b });

            var pub = report.Findings.Single(f => f.Category == "Publisher");
            Assert.Equal(Severity.Medium, pub.Severity);
            Assert.Contains("contoso", report.RecommendedStrategy);
        }

        [Fact]
        public void SamePublisherPrefix_ProducesNoPublisherFinding()
        {
            var a = Sol("solA", prefix: "new", comps: Comp(ComponentTypes.WebResource, Guid.NewGuid(), "a.js"));
            var b = Sol("solB", prefix: "new", comps: Comp(ComponentTypes.WebResource, Guid.NewGuid(), "b.js"));

            var report = MergeRules.Compare(new[] { a, b });

            Assert.DoesNotContain(report.Findings, f => f.Category == "Publisher");
        }

        // ---- version divergence (US-ALM1.3.1) -------------------------------------------------

        [Fact]
        public void VersionDifference_WithOverlap_IsMedium()
        {
            var shared = Guid.NewGuid();
            var a = Sol("solA", version: "1.0.0.0", comps: Comp(ComponentTypes.WebResource, shared, "s.js"));
            var b = Sol("solB", version: "2.0.0.0", comps: Comp(ComponentTypes.WebResource, shared, "s.js"));

            var report = MergeRules.Compare(new[] { a, b });

            var ver = report.Findings.Single(f => f.Category == "Version");
            Assert.Equal(Severity.Medium, ver.Severity);
        }

        // ---- env-var / connection-reference conflicts (US-ALM1.4.1) ---------------------------

        [Fact]
        public void EnvVarConflict_DifferentValues_IsHigh()
        {
            var a = Sol("solA");
            var b = Sol("solB");
            var config = new[]
            {
                new ConfigItem { Kind = "EnvVar", SchemaName = "new_ApiUrl", DefinitionOrValue = "https://a", OwningSolution = "solA" },
                new ConfigItem { Kind = "EnvVar", SchemaName = "new_ApiUrl", DefinitionOrValue = "https://b", OwningSolution = "solB" },
            };

            var report = MergeRules.Compare(new[] { a, b }, config);

            var conflict = report.Findings.Single(f => f.Category == "Environment Variables");
            Assert.Equal(Severity.High, conflict.Severity);
            Assert.Contains("new_ApiUrl", conflict.Component);
        }

        [Fact]
        public void ConnRef_IdenticalInBoth_IsMediumDuplicate()
        {
            var a = Sol("solA");
            var b = Sol("solB");
            var config = new[]
            {
                new ConfigItem { Kind = "ConnRef", SchemaName = "new_sharedpoint", DefinitionOrValue = "shared_sharepointonline", OwningSolution = "solA" },
                new ConfigItem { Kind = "ConnRef", SchemaName = "new_sharedpoint", DefinitionOrValue = "shared_sharepointonline", OwningSolution = "solB" },
            };

            var report = MergeRules.Compare(new[] { a, b }, config);

            var conflict = report.Findings.Single(f => f.Category == "Connection References");
            Assert.Equal(Severity.Medium, conflict.Severity);
        }

        [Fact]
        public void ConfigItem_InOneSolutionOnly_IsNotAConflict()
        {
            var a = Sol("solA");
            var b = Sol("solB");
            var config = new[]
            {
                new ConfigItem { Kind = "EnvVar", SchemaName = "new_only", DefinitionOrValue = "x", OwningSolution = "solA" },
            };

            var report = MergeRules.Compare(new[] { a, b }, config);

            Assert.DoesNotContain(report.Findings, f => f.Category == "Environment Variables");
        }

        // ---- verdict selection (US-ALM1.5.1) --------------------------------------------------

        [Fact]
        public void CleanMerge_YieldsSafeToMerge()
        {
            var a = Sol("solA", prefix: "new", version: "1.0.0.0",
                comps: Comp(ComponentTypes.WebResource, Guid.NewGuid(), "a.js"));
            var b = Sol("solB", prefix: "new", version: "1.0.0.0",
                comps: Comp(ComponentTypes.WebResource, Guid.NewGuid(), "b.js"));

            var report = MergeRules.Compare(new[] { a, b });

            Assert.Equal(MergeVerdict.SafeToMerge, report.Verdict);
            Assert.Equal(0, report.Score);
            Assert.Equal(ScoreBand.Low, report.Band);
        }

        [Fact]
        public void ManyHighConflicts_YieldDoNotMerge()
        {
            // Three overlapping plugin assemblies (each High) trips the do-not-merge threshold.
            var solsA = new List<SolutionComponentRef>();
            var solsB = new List<SolutionComponentRef>();
            for (int i = 0; i < 3; i++)
            {
                var id = Guid.NewGuid();
                solsA.Add(Comp(ComponentTypes.PluginAssembly, id, "Asm" + i));
                solsB.Add(Comp(ComponentTypes.PluginAssembly, id, "Asm" + i));
            }
            var a = new SolutionInfo { UniqueName = "solA", Version = "1.0.0.0", PublisherPrefix = "new", Components = solsA };
            var b = new SolutionInfo { UniqueName = "solB", Version = "1.0.0.0", PublisherPrefix = "new", Components = solsB };

            var report = MergeRules.Compare(new[] { a, b });

            Assert.Equal(MergeVerdict.DoNotMerge, report.Verdict);
        }

        [Fact]
        public void SingleSolution_YieldsInfoAndSafe()
        {
            var a = Sol("solA", comps: Comp(ComponentTypes.WebResource, Guid.NewGuid(), "a.js"));

            var report = MergeRules.Compare(new[] { a });

            Assert.Equal(MergeVerdict.SafeToMerge, report.Verdict);
            Assert.Contains(report.Findings, f => f.Severity == Severity.Info);
        }

        // ---- determinism (Definition of Done) -------------------------------------------------

        [Fact]
        public void Compare_IsDeterministic()
        {
            var shared = Guid.NewGuid();
            SolutionInfo[] Build() => new[]
            {
                Sol("solB", prefix: "beta", version: "2.0.0.0",
                    comps: Comp(ComponentTypes.WebResource, shared, "s.js", managed: true)),
                Sol("solA", prefix: "alpha", version: "1.0.0.0",
                    comps: Comp(ComponentTypes.WebResource, shared, "s.js", managed: false)),
            };
            var config = new[]
            {
                new ConfigItem { Kind = "EnvVar", SchemaName = "new_x", DefinitionOrValue = "1", OwningSolution = "solA" },
                new ConfigItem { Kind = "EnvVar", SchemaName = "new_x", DefinitionOrValue = "2", OwningSolution = "solB" },
            };

            var r1 = MergeRules.Compare(Build(), config);
            var r2 = MergeRules.Compare(Build(), config);

            Assert.Equal(r1.Verdict, r2.Verdict);
            Assert.Equal(r1.Score, r2.Score);
            Assert.Equal(
                string.Join("|", r1.Findings.Select(f => f.Category + ":" + f.Severity + ":" + f.Title)),
                string.Join("|", r2.Findings.Select(f => f.Category + ":" + f.Severity + ":" + f.Title)));
            Assert.Equal(r1.RecommendedStrategy, r2.RecommendedStrategy);
        }
    }
}
