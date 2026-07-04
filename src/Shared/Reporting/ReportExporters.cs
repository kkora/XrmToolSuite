using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Reporting
{
    /// <summary>
    /// CI/CD-friendly JSON: summary + findings + a pass/fail block against a band threshold.
    /// Generic over <see cref="ReportModel"/>, so every scoring tool in the suite emits the same shape.
    /// Requires Newtonsoft.Json (provided by XrmToolBox at runtime; referenced, not shipped).
    /// </summary>
    public static class JsonReportExporter
    {
        public static void Export(ReportModel r, string path, ScoreBand failAt = ScoreBand.High)
        {
            var payload = new
            {
                tool = r.ToolName,
                version = r.ToolVersion,
                subject = new
                {
                    name = r.SubjectName,
                    key = r.SubjectKey,
                    version = r.SubjectVersion,
                    managed = r.IsManaged
                },
                environments = new { source = r.SourceEnvironment, target = r.TargetEnvironment },
                analyzedOnUtc = r.AnalyzedOnUtc,
                analyzersRun = r.AnalyzersRun,
                analyzersSkipped = r.AnalyzersSkipped,
                score = r.Score,
                band = r.Band.ToString(),
                scoreWord = r.ScoreWord,
                metrics = r.Metrics.Select(m => new { label = m.Label, value = m.Value, hint = m.Hint }),
                aiSummary = r.AiSummary,
                summary = r.SeveritySummary(),
                ci = new
                {
                    failAtBand = failAt.ToString(),
                    pass = r.Band < failAt,
                    suggestedExitCode = r.Band < failAt ? 0 : 1
                },
                findings = r.Findings
                    .OrderByDescending(f => f.Severity)
                    .Select(f => new
                    {
                        category = f.Category,
                        severity = f.Severity.ToString(),
                        title = f.Title,
                        component = f.Component,
                        description = f.Description,
                        recommendation = f.Recommendation,
                        helpUrl = f.HelpUrl
                    })
            };

            var json = JsonConvert.SerializeObject(payload, Formatting.Indented,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }

    /// <summary>Ordered, actionable fix checklist (Markdown). BCL-only, so unit-testable.</summary>
    public static class FixChecklistGenerator
    {
        public static string Generate(ReportModel r)
        {
            var sb = new StringBuilder();
            var subjectLine = string.IsNullOrEmpty(r.SubjectVersion)
                ? r.SubjectName : $"{r.SubjectName} v{r.SubjectVersion}";
            sb.AppendLine($"# Fix Checklist — {subjectLine}");
            sb.AppendLine();
            sb.AppendLine($"{Capitalize(r.ScoreWord)}: **{r.Band}** (score {r.Score}/100) — generated {r.AnalyzedOnUtc:u}");
            sb.AppendLine();

            var actionable = r.Findings
                .Where(f => f.Severity >= Severity.Low && !string.IsNullOrWhiteSpace(f.Recommendation))
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.Category)
                .ToList();

            if (actionable.Count == 0)
            {
                sb.AppendLine("No actionable items. ✔");
            }
            else
            {
                foreach (var group in actionable.GroupBy(f => f.Severity).OrderByDescending(g => g.Key))
                {
                    sb.AppendLine($"## {group.Key} ({group.Count()})");
                    foreach (var f in group)
                        sb.AppendLine($"- [ ] **[{f.Category}] {f.Title}** — `{f.Component}`: {f.Recommendation}");
                    sb.AppendLine();
                }
            }

            if (r.ChecklistGuidance.Count > 0)
            {
                sb.AppendLine("---");
                sb.AppendLine("## Guidance");
                foreach (var line in r.ChecklistGuidance)
                    sb.AppendLine($"- [ ] {line}");
            }

            return sb.ToString();
        }

        public static void Export(ReportModel r, string path) =>
            File.WriteAllText(path, Generate(r), Encoding.UTF8);

        private static string Capitalize(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
