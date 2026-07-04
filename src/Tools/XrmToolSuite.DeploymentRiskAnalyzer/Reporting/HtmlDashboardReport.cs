using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Reporting
{
    /// <summary>
    /// Self-contained, theme-aware HTML "Deployment Risk" dashboard (no external CSS/JS/fonts,
    /// so it opens offline and honours the reader's OS light/dark preference). Print → Save as PDF
    /// from any browser produces a pixel-identical PDF via the <c>@media print</c> rules.
    ///
    /// SDK-free by design: depends only on <see cref="AnalysisResult"/> and the BCL, so it is
    /// unit-tested directly by the net8 test project and never pulls in ClosedXML/MigraDoc.
    ///
    /// Widgets that would need data the tool does not persist (scan history, an effort/cost
    /// heuristic) are deliberately omitted — everything rendered is derived from the current run.
    /// </summary>
    public static class HtmlReportExporter
    {
        public static void Export(AnalysisResult r, string path) =>
            File.WriteAllText(path, Build(r), Encoding.UTF8);

        /// <summary>Renders the full HTML document as a string (the unit-test seam).</summary>
        public static string Build(AnalysisResult r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            string E(string s) => WebUtility.HtmlEncode(s ?? "");

            // Risk band → label + colour token (semantic, not the accent).
            string bandText, bandVar;
            switch (r.Risk)
            {
                case OverallRisk.High: bandText = "HIGH RISK"; bandVar = "var(--crit)"; break;
                case OverallRisk.Medium: bandText = "MEDIUM RISK"; bandVar = "var(--high)"; break;
                default: bandText = "LOW RISK"; bandVar = "var(--good)"; break;
            }
            int gaugePct = Math.Max(0, Math.Min(100, r.Score));

            string target = string.IsNullOrWhiteSpace(r.TargetEnvironment)
                ? "" : $" to {E(r.TargetEnvironment)}";
            string leadIn =
                $"This solution has a {E(r.Risk.ToString().ToLowerInvariant())} deployment risk. " +
                $"Resolve the critical issues and review the recommendations before promoting{target}.";

            var sb = new StringBuilder();
            sb.Append("<title>Deployment Risk Report — ").Append(E(r.SolutionFriendlyName)).Append("</title>\n");
            sb.Append("<style>\n").Append(Css).Append("\n</style>\n");
            sb.Append("<div class=\"page\">\n");

            // ---- header ------------------------------------------------------
            sb.Append($@"<div class=""topbar"">
  <div class=""brand"">
    <div class=""shield""><svg width=""20"" height=""20"" viewBox=""0 0 24 24"" fill=""none""><path d=""M12 2l7 3v6c0 4.4-3 8.3-7 9.5C8 19.3 5 15.4 5 11V5l7-3z"" fill=""#fff"" opacity="".95""/><path d=""M9 12l2 2 4-4"" stroke=""var(--accent)"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/></svg></div>
    <div><h1>Deployment Risk Report</h1><p class=""sub"">XrmToolSuite · Deployment Risk Analyzer</p></div>
  </div>
  <div class=""facts"">
    <div class=""fact""><div class=""k"">Solution</div><div class=""v link"">{E(r.SolutionFriendlyName)}</div></div>
    <div class=""fact""><div class=""k"">Version</div><div class=""v"">{E(r.SolutionVersion)} · {(r.SolutionIsManaged ? "Managed" : "Unmanaged")}</div></div>
    <div class=""fact""><div class=""k"">Target</div><div class=""v"">{E(string.IsNullOrWhiteSpace(r.TargetEnvironment) ? r.SourceEnvironment : r.TargetEnvironment)}</div></div>
    <div class=""fact""><div class=""k"">Analyzed</div><div class=""v"">{r.AnalyzedOnUtc:dd MMM yyyy HH:mm} UTC</div></div>
  </div>
</div>
");

            // ---- hero: gauge + KPI cards ------------------------------------
            sb.Append("<div class=\"row hero\">\n");
            sb.Append($@"  <div class=""card gauge-card"">
    <div class=""gauge"">
      <svg width=""200"" height=""116"" viewBox=""0 0 200 116"" aria-hidden=""true"">
        <path d=""M16 104 A84 84 0 0 1 184 104"" fill=""none"" stroke=""var(--line)"" stroke-width=""13"" stroke-linecap=""round""/>
        <path d=""M16 104 A84 84 0 0 1 184 104"" fill=""none"" stroke=""{bandVar}"" stroke-width=""13"" stroke-linecap=""round"" pathLength=""100"" stroke-dasharray=""{gaugePct} 100""/>
      </svg>
      <div class=""readout""><div class=""score"">{gaugePct}</div><div class=""of"">/ 100</div></div>
      <div class=""band"" style=""color:{bandVar}"">{bandText}</div>
    </div>
    <div class=""gauge-text""><p>{leadIn}</p></div>
  </div>
");
            sb.Append("  <div class=\"row kpis\" style=\"margin-top:0\">\n");
            foreach (var k in new[]
                     {
                         (Severity.Critical, "var(--crit)", "!", "#fff"),
                         (Severity.High,     "var(--high)", "↑", "#fff"),
                         (Severity.Medium,   "var(--med)",  "▲", "#3a2c00"),
                         (Severity.Low,      "var(--low)",  "•", "#fff"),
                         (Severity.Info,     "var(--info)", "i", "#fff"),
                     })
            {
                sb.Append($@"    <div class=""card kpi""><div class=""head""><span class=""dot"" style=""background:{k.Item2};color:{k.Item4}"">{k.Item3}</span><span class=""lbl"">{k.Item1}</span></div><div class=""num"">{r.CountBySeverity(k.Item1)}</div></div>
");
            }
            sb.Append("  </div>\n</div>\n");

            // ---- analysis row: categories + top criticals -------------------
            sb.Append("<div class=\"row analysis\">\n");

            // Risk categories (derived from findings — score = worst severity in the area)
            var cats = r.Findings
                .GroupBy(f => f.Category)
                .Select(g => new { Cat = g.Key, Count = g.Count(), Score = CategoryScore(g) })
                .OrderByDescending(x => x.Score).ThenByDescending(x => x.Count)
                .ToList();

            sb.Append("  <div class=\"card\"><h2>Risk Categories</h2>");
            if (cats.Count == 0)
            {
                sb.Append("<p class=\"empty\">No findings — nothing flagged in any area.</p></div>\n");
            }
            else
            {
                sb.Append("<table><thead><tr><th>Category</th><th class=\"r\">Score</th><th class=\"c\">Issues</th></tr></thead><tbody>");
                foreach (var c in cats)
                    sb.Append($@"<tr><td>{E(CategoryName(c.Cat))}</td><td class=""r score-cell"" style=""color:{ScoreColor(c.Score)}"">{c.Score}</td><td class=""c"">{c.Count}</td></tr>");
                sb.Append("</tbody></table></div>\n");
            }

            // Top critical issues
            var topIssues = r.Findings
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.Category)
                .Take(5).ToList();
            sb.Append("  <div class=\"card\"><h2>Top Issues</h2>");
            if (topIssues.Count == 0)
                sb.Append("<p class=\"empty\">Clear for deployment. ✔</p></div>\n");
            else
            {
                sb.Append("<div class=\"issues\">");
                foreach (var f in topIssues)
                {
                    string ic = f.Severity >= Severity.Critical ? "var(--crit)"
                              : f.Severity >= Severity.High ? "var(--high)"
                              : f.Severity >= Severity.Medium ? "var(--med)" : "var(--low)";
                    sb.Append($@"<div class=""issue""><span class=""ic""><svg width=""18"" height=""18"" viewBox=""0 0 24 24""><circle cx=""12"" cy=""12"" r=""10"" fill=""{ic}""/><path d=""M12 7v6M12 16.5v.5"" stroke=""#fff"" stroke-width=""2"" stroke-linecap=""round""/></svg></span><div><div class=""t"">{E(f.Title)}</div><div class=""d"">{FormatComponent(f, E)}</div></div></div>");
                }
                sb.Append("</div></div>\n");
            }

            // Next steps (generic deployment guidance, tuned by the run)
            int crit = r.CountBySeverity(Severity.Critical);
            sb.Append("  <div class=\"card\"><h2>Next Steps</h2><div class=\"steps\">");
            sb.Append(Step("M20 6L9 17l-5-5",
                "Resolve all critical issues",
                crit > 0 ? $"{crit} must clear before deployment" : "None outstanding"));
            sb.Append(Step("M4 7h16M4 12h16M4 17h10",
                "Validate in a sandbox first",
                "Test the import before PRODUCTION"));
            sb.Append(Step("M6 2h9l5 5v15H6z",
                "Take a backup / stage for upgrade",
                r.SolutionIsManaged ? "Managed: stage before Apply upgrade" : "Unmanaged: no clean rollback — back up first"));
            sb.Append(Step("M3 12h4l3 8 4-16 3 8h4",
                "Share this report with stakeholders",
                "Print → Save as PDF for distribution"));
            sb.Append("</div></div>\n</div>\n");

            // ---- executive summary ------------------------------------------
            if (!string.IsNullOrWhiteSpace(r.AiSummary))
                sb.Append($@"<div class=""row""><div class=""card""><h2>Executive Summary</h2><div class=""prose"">{E(r.AiSummary)}</div></div></div>
");

            // ---- recommendations --------------------------------------------
            var recs = r.Findings
                .Where(f => f.Severity >= Severity.Low && !string.IsNullOrWhiteSpace(f.Recommendation))
                .OrderByDescending(f => f.Severity).ThenBy(f => f.Category)
                .ToList();
            if (recs.Count > 0)
            {
                var shown = recs.Take(8).ToList();
                string count = recs.Count > shown.Count ? $" <span class=\"n\">(top {shown.Count} of {recs.Count})</span>" : "";
                sb.Append($"<div class=\"row\"><div class=\"card\"><h2>Recommendations{count}</h2>");
                sb.Append("<table><thead><tr><th>Priority</th><th>Recommendation</th><th>Component</th></tr></thead><tbody>");
                foreach (var f in shown)
                    sb.Append($@"<tr><td><span class=""pill {f.Severity}"">{f.Severity}</span></td><td>{E(f.Recommendation)}</td><td><code>{E(f.AffectedComponent)}</code></td></tr>");
                sb.Append("</tbody></table></div></div>\n");
            }

            // ---- full findings detail (grouped by category) -----------------
            foreach (var group in r.Findings.GroupBy(f => f.Category))
            {
                sb.Append($"<div class=\"row\"><div class=\"card\"><h2>{E(CategoryName(group.Key))} <span class=\"n\">({group.Count()})</span></h2>");
                sb.Append("<table class=\"findings\"><thead><tr><th style=\"width:86px\">Severity</th><th style=\"width:30%\">Finding</th><th>Detail &amp; recommendation</th></tr></thead><tbody>");
                foreach (var f in group.OrderByDescending(x => x.Severity))
                {
                    string docs = string.IsNullOrEmpty(f.HelpUrl) ? "" : $@" <a href=""{E(f.HelpUrl)}"">docs ↗</a>";
                    sb.Append($@"<tr><td><span class=""pill {f.Severity}"">{f.Severity}</span></td><td><b>{E(f.Title)}</b><div class=""cmp""><code>{E(f.AffectedComponent)}</code></div></td><td>{E(f.Description)}<div class=""rec"">→ {E(f.Recommendation)}{docs}</div></td></tr>");
                }
                sb.Append("</tbody></table></div></div>\n");
            }

            // ---- footer ------------------------------------------------------
            string ran = string.Join(", ", r.AnalyzersRun);
            string skipped = r.AnalyzersSkipped.Any() ? $" — skipped: {E(string.Join(", ", r.AnalyzersSkipped))}" : "";
            sb.Append($@"<p class=""note"">Analyzers run: {E(ran)}{skipped}. Generated by the Deployment Risk Analyzer — use your browser's <b>Print → Save as PDF</b> for a PDF copy.</p>
");
            sb.Append("</div>\n");
            return sb.ToString();
        }

        // Category "score" = weight of the worst finding in that area (transparent, history-free).
        // Shared with the PDF report so both surfaces derive the same category scores.
        internal static int CategoryScore(IEnumerable<RiskFinding> findings)
        {
            switch ((Severity)findings.Max(f => (int)f.Severity))
            {
                case Severity.Critical: return 92;
                case Severity.High: return 76;
                case Severity.Medium: return 54;
                case Severity.Low: return 30;
                default: return 12;
            }
        }

        private static string ScoreColor(int score) =>
            score >= 70 ? "var(--crit)" : score >= 45 ? "var(--high)" : "var(--good)";

        private static string FormatComponent(RiskFinding f, Func<string, string> E)
        {
            if (string.IsNullOrWhiteSpace(f.AffectedComponent)) return E(f.Description);
            return $"<code>{E(f.AffectedComponent)}</code>";
        }

        private static string Step(string iconPath, string title, string detail) =>
            $@"<div class=""step""><span class=""si""><svg width=""15"" height=""15"" viewBox=""0 0 24 24"" fill=""none""><path d=""{iconPath}"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""/></svg></span><div><div class=""st"">{title}</div><div class=""sd"">{detail}</div></div></div>";

        // Friendly category label; shared with the PDF report.
        internal static string CategoryName(AnalyzerCategory c)
        {
            switch (c)
            {
                case AnalyzerCategory.Dependencies: return "Missing Dependencies";
                case AnalyzerCategory.EnvironmentVariables: return "Environment Variables";
                case AnalyzerCategory.FlowsAndPlugins: return "Flows & Plugins";
                case AnalyzerCategory.Security: return "Security Changes";
                case AnalyzerCategory.SchemaConflicts: return "Schema Conflicts";
                case AnalyzerCategory.DeletedComponents: return "Deleted Components";
                case AnalyzerCategory.Forms: return "Form Changes";
                case AnalyzerCategory.Ribbon: return "Ribbon Changes";
                case AnalyzerCategory.PowerPages: return "Power Pages";
                default: return "General";
            }
        }

        private const string Css = @"
:root{
  --bg:#eef1f6;--panel:#fff;--panel-2:#f7f9fc;--ink:#1a1d29;--ink-2:#55607a;--ink-3:#8b95ad;
  --line:#e3e8f0;--line-2:#eef1f6;--accent:#2563eb;--accent-soft:#e8efff;
  --crit:#d13438;--high:#f7871f;--med:#f5b423;--low:#8a8fa3;--info:#2b7fff;--good:#12a150;
  --shadow:0 1px 2px rgba(20,30,60,.06),0 6px 20px rgba(20,30,60,.05);--radius:14px;
}
@media (prefers-color-scheme:dark){:root{
  --bg:#05070f;--panel:#0e1524;--panel-2:#121b2d;--ink:#eaf0fb;--ink-2:#9aa7c2;--ink-3:#667191;
  --line:#1e2942;--line-2:#141d31;--accent:#4d8bff;--accent-soft:#14284d;
  --shadow:0 1px 2px rgba(0,0,0,.4),0 10px 30px rgba(0,0,0,.35);
}}
:root[data-theme=""light""]{
  --bg:#eef1f6;--panel:#fff;--panel-2:#f7f9fc;--ink:#1a1d29;--ink-2:#55607a;--ink-3:#8b95ad;
  --line:#e3e8f0;--line-2:#eef1f6;--accent:#2563eb;--accent-soft:#e8efff;
  --shadow:0 1px 2px rgba(20,30,60,.06),0 6px 20px rgba(20,30,60,.05);
}
:root[data-theme=""dark""]{
  --bg:#05070f;--panel:#0e1524;--panel-2:#121b2d;--ink:#eaf0fb;--ink-2:#9aa7c2;--ink-3:#667191;
  --line:#1e2942;--line-2:#141d31;--accent:#4d8bff;--accent-soft:#14284d;
  --shadow:0 1px 2px rgba(0,0,0,.4),0 10px 30px rgba(0,0,0,.35);
}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);font-family:""Segoe UI"",system-ui,-apple-system,Roboto,sans-serif;-webkit-font-smoothing:antialiased;line-height:1.5;font-variant-numeric:tabular-nums}
.page{max-width:1180px;margin:0 auto;padding:22px 22px 60px}
.topbar{display:flex;align-items:center;justify-content:space-between;gap:20px;background:var(--panel);border:1px solid var(--line);border-radius:var(--radius);padding:18px 24px;box-shadow:var(--shadow);flex-wrap:wrap}
.brand{display:flex;align-items:center;gap:13px}
.shield{width:38px;height:38px;border-radius:10px;flex:none;background:linear-gradient(135deg,var(--accent),#7aa2ff);display:grid;place-items:center}
.brand h1{margin:0;font-size:19px;letter-spacing:-.2px}
.brand .sub{margin:1px 0 0;font-size:12.5px;color:var(--ink-2)}
.facts{display:flex;gap:26px;flex-wrap:wrap}
.fact .k{font-size:10.5px;letter-spacing:.07em;text-transform:uppercase;color:var(--ink-3)}
.fact .v{font-size:14px;font-weight:600;margin-top:2px}
.fact .v.link{color:var(--accent)}
.row{display:grid;gap:16px;margin-top:16px}
.row.hero{grid-template-columns:minmax(340px,1.05fr) 2fr}
.row.kpis{grid-template-columns:repeat(5,1fr)}
.row.analysis{grid-template-columns:1.15fr 1fr 1.1fr}
@media (max-width:940px){.row.hero,.row.kpis,.row.analysis{grid-template-columns:1fr}}
.card{background:var(--panel);border:1px solid var(--line);border-radius:var(--radius);padding:18px 20px;box-shadow:var(--shadow);min-width:0}
.card h2{margin:0 0 14px;font-size:15px;letter-spacing:-.1px}
.card h2 .n{color:var(--ink-3);font-weight:500}
.empty{color:var(--ink-3);font-size:13px;margin:0}
.gauge-card{display:flex;gap:18px;align-items:center}
.gauge{position:relative;width:200px;flex:none;text-align:center}
.gauge>svg{display:block}
.gauge .readout{position:absolute;top:36px;left:0;right:0}
.gauge .score{font-size:46px;font-weight:800;letter-spacing:-2px;line-height:1;color:var(--ink)}
.gauge .of{font-size:12px;color:var(--ink-3);margin-top:3px}
.gauge .band{margin-top:5px;font-size:12.5px;font-weight:800;letter-spacing:.09em}
.gauge-text p{margin:0;font-size:13.5px;color:var(--ink-2)}
.kpi{text-align:center}
.kpi .head{display:flex;align-items:center;justify-content:center;gap:8px}
.dot{width:24px;height:24px;border-radius:50%;display:grid;place-items:center;flex:none;font-size:13px;font-weight:700;line-height:1}
.kpi .lbl{font-size:13px;color:var(--ink-2);font-weight:600}
.kpi .num{font-size:34px;font-weight:800;letter-spacing:-1px;margin-top:8px;line-height:1}
table{width:100%;border-collapse:collapse}
th{text-align:left;font-size:10.5px;letter-spacing:.06em;text-transform:uppercase;color:var(--ink-3);font-weight:600;padding:0 0 9px;border-bottom:1px solid var(--line)}
td{padding:10px 0;border-bottom:1px solid var(--line-2);font-size:13px;vertical-align:top}
tr:last-child td{border-bottom:0}
th.r,td.r{text-align:right}
th.c,td.c{text-align:center}
.score-cell{font-weight:700;font-size:14px}
.issues{display:flex;flex-direction:column;gap:13px}
.issue{display:flex;gap:11px}
.issue .ic{flex:none;margin-top:1px}
.issue .t{font-size:13.5px;font-weight:600}
.issue .d{font-size:12px;color:var(--ink-2);margin-top:1px}
code{font-family:""Cascadia Code"",ui-monospace,Consolas,monospace;font-size:11.5px;background:var(--panel-2);padding:1px 5px;border-radius:5px;word-break:break-word}
.steps{display:flex;flex-direction:column;gap:15px}
.step{display:flex;gap:12px}
.step .si{width:30px;height:30px;border-radius:8px;background:var(--accent-soft);color:var(--accent);display:grid;place-items:center;flex:none}
.step .st{font-size:13.5px;font-weight:600}
.step .sd{font-size:12px;color:var(--ink-2)}
.prose{white-space:pre-wrap;font-size:13.5px;line-height:1.6;color:var(--ink-2)}
.pill{display:inline-block;padding:3px 10px;border-radius:20px;font-size:11px;font-weight:700;color:#fff}
.pill.Critical{background:var(--crit)}.pill.High{background:var(--high)}.pill.Medium{background:var(--med);color:#3a2c00}.pill.Low{background:var(--low)}.pill.Info{background:var(--info)}
.findings td .cmp{margin-top:3px}
.findings .rec{color:var(--ink-2);margin-top:4px}
.findings a{color:var(--accent);text-decoration:none}
.note{font-size:11.5px;color:var(--ink-3);margin:22px 2px 0}
@media print{body{background:#fff}.card,.topbar{box-shadow:none;break-inside:avoid}a{color:inherit}}
";
    }
}
