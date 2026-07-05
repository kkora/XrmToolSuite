using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.SharingAnalyzer.Analysis;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the SDK-free sharing logic: <see cref="AccessRights"/> mask decoding,
    /// the <see cref="SharingSummary"/> aggregations, and the <see cref="SharingRiskRules"/> risk rules
    /// (all pure functions of a populated summary, so exact verdicts are asserted with no live org).
    /// Traces to EPIC-SEC4 (US-SEC4.2.1 rights decoding, US-SEC4.3.1/US-SEC4.3.2 risk findings).
    /// </summary>
    public class SharingAnalyzerTests
    {
        // ---- fixtures -------------------------------------------------------------------------

        private static SharedRecordAccess Share(
            string table, Guid objectId, Guid principalId, string type = "User",
            bool active = true, int mask = 1, string name = null) => new SharedRecordAccess
            {
                Table = table,
                ObjectId = objectId,
                PrincipalId = principalId,
                PrincipalType = type,
                PrincipalActive = active,
                AccessMask = mask,
                PrincipalName = name ?? (type + " " + principalId.ToString().Substring(0, 4))
            };

        /// <summary>A record shared with <paramref name="principals"/> distinct active users at Read access.</summary>
        private static IEnumerable<SharedRecordAccess> RecordSharedWith(string table, Guid recordId, int principals)
        {
            for (int i = 0; i < principals; i++)
                yield return Share(table, recordId, Guid.NewGuid());
        }

        private static SharingSummary SummaryOf(params SharedRecordAccess[] shares) =>
            new SharingSummary { Shares = shares.ToList(), ScannedTables = { "account" } };

        // ---- AccessRights.Decode (US-SEC4.2.1) ------------------------------------------------

        [Fact]
        public void Decode_Read_YieldsReadOnly()
        {
            Assert.Equal(new[] { "Read" }, AccessRights.Decode((int)AccessRight.Read).ToArray());
            Assert.Equal("R", AccessRights.Summary((int)AccessRight.Read));
        }

        [Fact]
        public void Decode_ReadWrite_YieldsBoth()
        {
            int mask = (int)AccessRight.Read | (int)AccessRight.Write;
            Assert.Equal(new[] { "Read", "Write" }, AccessRights.Decode(mask).ToArray());
            Assert.Equal("R/W", AccessRights.Summary(mask));
        }

        [Fact]
        public void Decode_FullMask_YieldsEveryRight()
        {
            int full = (int)AccessRight.Read | (int)AccessRight.Write | (int)AccessRight.Append |
                       (int)AccessRight.AppendTo | (int)AccessRight.Create | (int)AccessRight.Delete |
                       (int)AccessRight.Share | (int)AccessRight.Assign;

            Assert.Equal(
                new[] { "Read", "Write", "Append", "AppendTo", "Create", "Delete", "Share", "Assign" },
                AccessRights.Decode(full).ToArray());
            Assert.Equal("R/W/A/AT/C/D/S/AS", AccessRights.Summary(full));
        }

        [Fact]
        public void Decode_None_YieldsEmptyAndNoneSummary()
        {
            Assert.Empty(AccessRights.Decode(0));
            Assert.Equal("None", AccessRights.Summary(0));
        }

        // ---- aggregations (US-SEC4.2.1) -------------------------------------------------------

        [Fact]
        public void RecordStats_CountsDistinctPrincipalsAndShares()
        {
            var record = Guid.NewGuid();
            var p1 = Guid.NewGuid();
            var summary = SummaryOf(
                Share("account", record, p1, mask: 1),
                Share("account", record, p1, mask: 2),        // same principal, second row
                Share("account", record, Guid.NewGuid(), mask: 4));

            var stat = summary.RecordStats().Single();
            Assert.Equal(2, stat.DistinctPrincipals);
            Assert.Equal(3, stat.ShareCount);
            Assert.Equal(1 | 2 | 4, stat.CombinedMask);
        }

        [Fact]
        public void PrincipalStats_CountsInboundRecordsAndShares()
        {
            var user = Guid.NewGuid();
            var summary = SummaryOf(
                Share("account", Guid.NewGuid(), user),
                Share("account", Guid.NewGuid(), user),
                Share("contact", Guid.NewGuid(), Guid.NewGuid()));

            var stat = summary.PrincipalStats().Single(p => p.PrincipalId == user);
            Assert.Equal(2, stat.InboundRecords);
            Assert.Equal(2, stat.InboundShares);
        }

        // ---- excessive sharing -> High (US-SEC4.3.1) ------------------------------------------

        [Fact]
        public void Excessive_RecordSharedWithManyPrincipals_YieldsHigh()
        {
            var record = Guid.NewGuid();
            var summary = new SharingSummary
            {
                Shares = RecordSharedWith("account", record, 30).ToList(),  // over default 25
                ScannedTables = { "account" }
            };

            var findings = SharingRiskRules.Evaluate(summary);

            var f = findings.Single(x => x.Title == "Excessive record sharing");
            Assert.Equal(Severity.High, f.Severity);
        }

        [Fact]
        public void Excessive_RespectsCustomThreshold()
        {
            var record = Guid.NewGuid();
            var summary = new SharingSummary
            {
                Shares = RecordSharedWith("account", record, 6).ToList(),
                ScannedTables = { "account" }
            };

            var findings = SharingRiskRules.Evaluate(summary, new SharingRiskOptions { MaxPrincipalsPerRecord = 5 });

            Assert.Contains(findings, x => x.Title == "Excessive record sharing" && x.Severity == Severity.High);
        }

        // ---- inactive user -> Medium (US-SEC4.3.1) --------------------------------------------

        [Fact]
        public void InactiveUser_Share_YieldsMedium()
        {
            var summary = SummaryOf(
                Share("account", Guid.NewGuid(), Guid.NewGuid(), type: "User", active: false, name: "Bob (disabled)"));

            var findings = SharingRiskRules.Evaluate(summary);

            var f = findings.Single(x => x.Title == "Shared with an inactive user");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // ---- disabled / empty team -> Medium (US-SEC4.3.1) ------------------------------------

        [Fact]
        public void DisabledTeam_Share_YieldsMedium()
        {
            var summary = SummaryOf(
                Share("account", Guid.NewGuid(), Guid.NewGuid(), type: "Team", active: false, name: "Ghost Team"));

            var findings = SharingRiskRules.Evaluate(summary);

            var f = findings.Single(x => x.Title == "Shared with a disabled or empty team");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // ---- high inbound -> Medium (US-SEC4.3.2) ---------------------------------------------

        [Fact]
        public void HighInbound_User_YieldsMedium()
        {
            var user = Guid.NewGuid();
            var shares = Enumerable.Range(0, 12)
                .Select(_ => Share("account", Guid.NewGuid(), user, name: "Busy User"))
                .ToList();
            var summary = new SharingSummary { Shares = shares, ScannedTables = { "account" } };

            var findings = SharingRiskRules.Evaluate(summary, new SharingRiskOptions { MaxInboundPerPrincipal = 10 });

            var f = findings.Single(x => x.Title == "User with high inbound shared access");
            Assert.Equal(Severity.Medium, f.Severity);
        }

        // ---- statistical outlier -> Medium (US-SEC4.3.1) --------------------------------------

        [Fact]
        public void OutlierRecord_HighPrincipalCount_YieldsMedium()
        {
            var shares = new List<SharedRecordAccess>();
            // 8 "normal" records shared with 5 principals each ...
            for (int r = 0; r < 8; r++)
                shares.AddRange(RecordSharedWith("account", Guid.NewGuid(), 5));
            // ... and one clear outlier at 24 (still under the excessive threshold of 25).
            shares.AddRange(RecordSharedWith("account", Guid.NewGuid(), 24));
            var summary = new SharingSummary { Shares = shares, ScannedTables = { "account" } };

            var findings = SharingRiskRules.Evaluate(summary);

            var f = findings.Single(x => x.Title == "Record with unusually high shared-principal count");
            Assert.Equal(Severity.Medium, f.Severity);
            Assert.DoesNotContain(findings, x => x.Title == "Excessive record sharing");
        }

        // ---- clean -> single Info (US-SEC4.3.1) -----------------------------------------------

        [Fact]
        public void CleanSharing_YieldsSingleInfoFinding()
        {
            var summary = SummaryOf(
                Share("account", Guid.NewGuid(), Guid.NewGuid(), type: "User", active: true, mask: 1));

            var findings = SharingRiskRules.Evaluate(summary);

            var f = Assert.Single(findings);
            Assert.Equal(Severity.Info, f.Severity);
            Assert.Equal("No sharing risks detected", f.Title);
        }

        // ---- score / band (US-SEC4.3.1) -------------------------------------------------------

        [Fact]
        public void Score_CleanFindings_IsZeroAndLowBand()
        {
            var findings = SharingRiskRules.Evaluate(SummaryOf(
                Share("account", Guid.NewGuid(), Guid.NewGuid())));

            Assert.Equal(0, SharingRiskRules.Score(findings));
            Assert.Equal(ScoreBand.Low, SharingRiskRules.Band(findings));
        }

        [Fact]
        public void Score_ManyHighFindings_BandsHigh()
        {
            var shares = new List<SharedRecordAccess>();
            // Four excessively-shared records => four High findings (4 * 12 = 48 >= high threshold 40).
            for (int r = 0; r < 4; r++)
                shares.AddRange(RecordSharedWith("account", Guid.NewGuid(), 30));
            var summary = new SharingSummary { Shares = shares, ScannedTables = { "account" } };

            var findings = SharingRiskRules.Evaluate(summary);
            var risk = findings.Where(f => f.Severity > Severity.Info).ToList();

            Assert.Equal(4, risk.Count(f => f.Title == "Excessive record sharing"));
            Assert.Equal(48, SharingRiskRules.Score(risk));
            Assert.Equal(ScoreBand.High, SharingRiskRules.Band(risk));
        }

        // Regression: the scalar rollups must ignore null share rows like every other aggregation on
        // SharingSummary does, so a null in the list doesn't crash DistinctRecords/DistinctPrincipals or
        // the clean "no risks" path of Evaluate.
        [Fact]
        public void ScalarRollups_IgnoreNullShareRows()
        {
            var s = new SharingSummary
            {
                Shares = new List<SharedRecordAccess> { null, Share("account", Guid.NewGuid(), Guid.NewGuid()) },
                ScannedTables = { "account" }
            };

            Assert.Equal(1, s.DistinctRecords);
            Assert.Equal(1, s.DistinctPrincipals);
            var f = Assert.Single(SharingRiskRules.Evaluate(s)); // clean path must not throw
            Assert.Equal("No sharing risks detected", f.Title);
        }
    }
}
