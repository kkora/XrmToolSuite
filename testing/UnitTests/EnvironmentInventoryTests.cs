using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.EnvironmentInventory.Inventory;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the Environment Inventory normalization model, exporters and report projection.
    /// Snapshots are built by hand (no Dataverse SDK); the live <c>InventoryCollector</c> is manual-tested.
    /// Traces to US-ADMIN07.4.1 (search/filter), US-ADMIN07.4.2 (detail), US-ADMIN07.3.2 (no secrets) and
    /// US-ADMIN07.5.1 (export engine).
    /// </summary>
    public class EnvironmentInventoryTests
    {
        private static InventorySnapshot Sample()
        {
            var snap = new InventorySnapshot
            {
                EnvironmentName = "DEV",
                CollectedOnUtc = new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc),
                UnavailableSources = { "PCF controls" }
            };
            snap.Items.Add(new InventoryItem
            {
                Category = "Solutions", ComponentType = "Solution", Name = "Core Sales",
                SchemaName = "coresales", Owner = "Contoso", IsManaged = true,
                ModifiedOn = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                Details = new Dictionary<string, string> { ["Version"] = "1.0.0.0" }
            });
            snap.Items.Add(new InventoryItem
            {
                Category = "Tables", ComponentType = "Custom Table", Name = "Widget",
                SchemaName = "new_widget", IsManaged = false,
                Details = new Dictionary<string, string> { ["Columns"] = "12" }
            });
            snap.Items.Add(new InventoryItem
            {
                Category = "Tables", ComponentType = "Table", Name = "Account",
                SchemaName = "account", IsManaged = true
            });
            snap.Items.Add(new InventoryItem
            {
                Category = "Configuration", ComponentType = "Environment Variable",
                Name = "API Base Url", SchemaName = "new_apibaseurl", IsManaged = false,
                Details = new Dictionary<string, string> { ["Type"] = "String" }
            });
            return snap;
        }

        // ---- Filter (US-ADMIN07.4.1) ----

        [Fact]
        public void Filter_ByText_MatchesNameSchemaAndType_CaseInsensitive()
        {
            var snap = Sample();
            Assert.Single(snap.Filter("widget", null, null));       // matches name/schema
            Assert.Single(snap.Filter("CUSTOM TABLE", null, null)); // matches component type, case-insensitive
            Assert.Single(snap.Filter("account", null, null));      // matches a single row by name/schema
        }

        [Fact]
        public void Filter_ByCategory_PinsCategory()
        {
            var snap = Sample();
            var tables = snap.Filter(null, "Tables", null).ToList();
            Assert.Equal(2, tables.Count);
            Assert.All(tables, i => Assert.Equal("Tables", i.Category));
        }

        [Fact]
        public void Filter_ByManaged_PinsManagedState()
        {
            var snap = Sample();
            Assert.Equal(2, snap.Filter(null, null, true).Count());   // Core Sales + Account
            Assert.Equal(2, snap.Filter(null, null, false).Count());  // Widget + Env var
        }

        [Fact]
        public void Filter_CombinesTextCategoryManaged()
        {
            var snap = Sample();
            var result = snap.Filter("a", "Tables", true).ToList();
            Assert.Single(result);
            Assert.Equal("Account", result[0].Name);
        }

        // ---- Counts (US-ADMIN07.4.1) ----

        [Fact]
        public void CountByCategory_CountsPerCategory()
        {
            var counts = Sample().CountByCategory();
            Assert.Equal(1, counts["Solutions"]);
            Assert.Equal(2, counts["Tables"]);
            Assert.Equal(1, counts["Configuration"]);
        }

        [Fact]
        public void Categories_AreDistinctAndTotalIsRowCount()
        {
            var snap = Sample();
            Assert.Equal(3, snap.Categories().Count());
            Assert.Equal(4, snap.Total);
        }

        // Regression: a null/blank Category must bucket identically in Categories() and CountByCategory()
        // (both as "(uncategorized)"), so the export summary counts and per-category detail sections reconcile.
        [Fact]
        public void NullOrBlankCategory_BucketsConsistentlyInBothViews()
        {
            var snap = new InventorySnapshot();
            snap.Items.Add(new InventoryItem { Category = null, ComponentType = "X", Name = "a" });
            snap.Items.Add(new InventoryItem { Category = "   ", ComponentType = "X", Name = "b" });
            snap.Items.Add(new InventoryItem { Category = "Tables", ComponentType = "Table", Name = "c" });

            var cats = snap.Categories().OrderBy(x => x).ToList();
            var counts = snap.CountByCategory();

            Assert.Equal(cats, counts.Keys.OrderBy(x => x).ToList()); // identical key sets
            Assert.Contains("(uncategorized)", cats);
            Assert.Equal(2, counts["(uncategorized)"]);               // null + blank collapse together
        }

        // ---- CSV export (US-ADMIN07.5.1) ----

        [Fact]
        public void Csv_HasHeaderAndRows()
        {
            var csv = InventoryExporter.ToCsv(Sample());
            Assert.StartsWith("Category,ComponentType,Name,SchemaName,Owner,Managed,ModifiedOn,Details", csv);
            Assert.Contains("Solutions,Solution,Core Sales,coresales,Contoso,Managed", csv);
        }

        [Fact]
        public void Csv_QuotesAndEscapesSpecialCharacters()
        {
            var snap = new InventorySnapshot();
            snap.Items.Add(new InventoryItem
            {
                Category = "Tables", ComponentType = "Table",
                Name = "Weird, \"quoted\" name", SchemaName = "line\nbreak"
            });
            var csv = InventoryExporter.ToCsv(snap);
            Assert.Contains("\"Weird, \"\"quoted\"\" name\"", csv); // comma + doubled quotes
            Assert.Contains("\"line\nbreak\"", csv);                 // newline quoted
        }

        // Regression: a value that Excel/Sheets would read as a formula (leading =, +, -, @ or tab) is
        // neutralized with a leading apostrophe, so a CSV export can't smuggle a spreadsheet formula.
        [Fact]
        public void Csv_NeutralizesFormulaInjection()
        {
            var snap = new InventorySnapshot();
            snap.Items.Add(new InventoryItem { Category = "Tables", ComponentType = "Table", Name = "=1+2" });
            var csv = InventoryExporter.ToCsv(snap);
            Assert.Contains("'=1+2", csv);        // apostrophe-prefixed => read as text
            Assert.DoesNotContain(",=1+2", csv);  // the raw formula never appears at a field boundary
        }

        // ---- JSON export (US-ADMIN07.5.1) ----

        [Fact]
        public void Json_IsRoundTrippableAndCarriesItems()
        {
            var json = InventoryExporter.ToJson(Sample());
            using (var doc = System.Text.Json.JsonDocument.Parse(json)) // throws if malformed
            {
                var root = doc.RootElement;
                Assert.Equal(4, root.GetProperty("total").GetInt32());
                Assert.Equal("DEV", root.GetProperty("environmentName").GetString());
                Assert.Equal(4, root.GetProperty("items").GetArrayLength());
                Assert.Equal(2, root.GetProperty("countByCategory").GetProperty("Tables").GetInt32());
            }
        }

        [Fact]
        public void Json_EscapesQuotesAndControlChars()
        {
            var snap = new InventorySnapshot();
            snap.Items.Add(new InventoryItem { Category = "X", ComponentType = "Y", Name = "a\"b\tc" });
            var json = InventoryExporter.ToJson(snap);
            using (var doc = System.Text.Json.JsonDocument.Parse(json))
            {
                Assert.Equal("a\"b\tc", doc.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
            }
        }

        // ---- Markdown / HTML export (US-ADMIN07.5.1) ----

        [Fact]
        public void Markdown_ContainsSummaryAndCategoryHeaders()
        {
            var md = InventoryExporter.ToMarkdown(Sample());
            Assert.Contains("## Summary", md);
            Assert.Contains("## Solutions", md);
            Assert.Contains("## Tables", md);
            Assert.Contains("## Configuration", md);
            Assert.Contains("Unavailable sources", md);
        }

        [Fact]
        public void Html_IsSelfContainedWithCategoryHeaders()
        {
            var html = InventoryExporter.ToHtml(Sample());
            Assert.Contains("<!DOCTYPE html>", html);
            Assert.Contains("<style>", html);                 // self-contained, no external CSS
            Assert.Contains("<h2>Solutions</h2>", html);
            Assert.Contains("<h2>Tables</h2>", html);
            Assert.DoesNotContain("http://", html);           // no external references
        }

        [Fact]
        public void Html_EscapesMarkup()
        {
            var snap = new InventorySnapshot();
            snap.Items.Add(new InventoryItem { Category = "Web/Dev", ComponentType = "Web Resource", Name = "<script>x</script>" });
            var html = InventoryExporter.ToHtml(snap);
            Assert.Contains("&lt;script&gt;", html);
            Assert.DoesNotContain("<script>x", html);
        }

        // ---- No secrets (US-ADMIN07.3.2) ----

        [Fact]
        public void Exports_NeverEmitSecretOrValueColumn()
        {
            var snap = Sample();
            var csv = InventoryExporter.ToCsv(snap);
            var header = csv.Split('\n')[0];
            Assert.DoesNotContain("Secret", header, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Value", header, StringComparison.OrdinalIgnoreCase);

            // The whole payload must not carry any "secret"/"value" keyed detail for env vars.
            foreach (var text in new[] { csv, InventoryExporter.ToJson(snap), InventoryExporter.ToMarkdown(snap), InventoryExporter.ToHtml(snap) })
            {
                Assert.DoesNotContain("secretvalue", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("environmentvariablevalue", text, StringComparison.OrdinalIgnoreCase);
            }
        }

        // ---- Report projection (US-ADMIN07.5.1) ----

        [Fact]
        public void ToReportModel_ProjectsMetricsWithZeroScore()
        {
            var model = InventorySummary.ToReportModel(Sample());
            Assert.Equal("Environment Inventory", model.ToolName);
            Assert.Equal("components", model.ScoreWord);
            Assert.Equal(0, model.Score);
            Assert.Equal(ScoreBand.Low, model.Band);
            Assert.Empty(model.Findings);
            Assert.Contains(model.Metrics, m => m.Label == "Total components" && m.Value == "4");
            Assert.Contains(model.Metrics, m => m.Label == "Tables" && m.Value == "2");
        }

        [Fact]
        public void ToMetrics_IncludesTotalAndUnavailableSources()
        {
            var metrics = InventorySummary.ToMetrics(Sample());
            Assert.Equal("4", metrics.First(m => m.Label == "Total components").Value);
            Assert.Contains(metrics, m => m.Label == "Unavailable sources" && m.Value.Contains("PCF"));
        }
    }
}
