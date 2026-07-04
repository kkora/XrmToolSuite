namespace XrmToolSuite.SolutionComplexityScore.Analysis
{
    /// <summary>
    /// Plain, SDK-free tally of the components in a solution. Populated by the Dataverse collector and
    /// consumed by <see cref="ComplexityMetrics"/>. Kept as a POCO so the whole scoring/effort model is
    /// unit-testable without a connection.
    /// </summary>
    public sealed class ComponentCounts
    {
        public int Tables { get; set; }
        public int Columns { get; set; }
        public int Relationships { get; set; }
        public int Forms { get; set; }
        public int Views { get; set; }
        public int Charts { get; set; }
        public int PluginSteps { get; set; }
        public int Workflows { get; set; }      // classic workflows
        public int Flows { get; set; }          // modern (cloud) flows
        public int BusinessRules { get; set; }
        public int JavaScriptWebResources { get; set; }
        public int Pcfs { get; set; }           // custom controls / PCF
        public int CustomApis { get; set; }
        public int Dashboards { get; set; }
        public int Apps { get; set; }           // model-driven + canvas apps

        /// <summary>The widest form seen (max column/control count) — an outlier signal for the dashboard.</summary>
        public int WidestForm { get; set; }
        public string WidestFormName { get; set; }
    }
}
