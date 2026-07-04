using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Models
{
    /// <summary>Severity of an individual finding.</summary>
    public enum Severity
    {
        Info = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>Overall deployment risk classification.</summary>
    public enum OverallRisk
    {
        Low,
        Medium,
        High
    }

    /// <summary>Functional area an analyzer/finding belongs to.</summary>
    public enum AnalyzerCategory
    {
        Dependencies,
        EnvironmentVariables,
        FlowsAndPlugins,
        Security,
        SchemaConflicts,
        DeletedComponents,
        Forms,
        Ribbon,
        PowerPages,
        General
    }

    /// <summary>A single risk finding produced by an analyzer.</summary>
    public class RiskFinding
    {
        public AnalyzerCategory Category { get; set; }
        public Severity Severity { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        /// <summary>Logical/schema name or display name of the affected component.</summary>
        public string AffectedComponent { get; set; }
        /// <summary>Concrete remediation step for the fix checklist.</summary>
        public string Recommendation { get; set; }
        /// <summary>Optional deep link / docs URL.</summary>
        public string HelpUrl { get; set; }

        public RiskFinding() { }

        public RiskFinding(AnalyzerCategory category, Severity severity, string title,
            string description, string affectedComponent, string recommendation, string helpUrl = null)
        {
            Category = category;
            Severity = severity;
            Title = title;
            Description = description;
            AffectedComponent = affectedComponent;
            Recommendation = recommendation;
            HelpUrl = helpUrl;
        }
    }

    /// <summary>Full result of an analysis run, consumed by the UI and the exporters.</summary>
    public class AnalysisResult
    {
        public string SolutionUniqueName { get; set; }
        public string SolutionFriendlyName { get; set; }
        public string SolutionVersion { get; set; }
        public bool SolutionIsManaged { get; set; }
        public string SourceEnvironment { get; set; }
        public string TargetEnvironment { get; set; }
        public DateTime AnalyzedOnUtc { get; set; } = DateTime.UtcNow;
        public List<RiskFinding> Findings { get; } = new List<RiskFinding>();
        public List<string> AnalyzersRun { get; } = new List<string>();
        public List<string> AnalyzersSkipped { get; } = new List<string>();

        public int Score { get; set; }
        public OverallRisk Risk { get; set; }

        /// <summary>Executive summary (AI-generated or offline template); null until generated. Included in exports.</summary>
        public string AiSummary { get; set; }

        public int CountBySeverity(Severity s) => Findings.Count(f => f.Severity == s);

        public Dictionary<string, int> SeveritySummary() =>
            Enum.GetValues(typeof(Severity)).Cast<Severity>()
                .ToDictionary(s => s.ToString(), CountBySeverity);
    }
}
