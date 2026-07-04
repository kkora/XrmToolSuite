namespace XrmToolSuite.Core.Analysis
{
    /// <summary>
    /// A single finding produced by an analyzer: one issue, note, or observation with a severity and
    /// (optionally) a concrete remediation. Deliberately UI-free and SDK-free so analyzers stay
    /// liftable into a console/CI wrapper and findings can be exported by any reporter.
    /// <para>
    /// <see cref="Category"/> is a free-form display string (e.g. "Environment Variables") rather than
    /// an enum, so each tool defines its own set of categories without a shared enum having to know them.
    /// </para>
    /// </summary>
    public class Finding
    {
        /// <summary>Display category the finding belongs to (groups findings in reports).</summary>
        public string Category { get; set; }

        public Severity Severity { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        /// <summary>Logical/schema name or display name of the affected component.</summary>
        public string Component { get; set; }

        /// <summary>Concrete remediation step for the fix checklist. Null/empty = nothing to action.</summary>
        public string Recommendation { get; set; }

        /// <summary>Optional deep link / docs URL.</summary>
        public string HelpUrl { get; set; }

        public Finding() { }

        public Finding(string category, Severity severity, string title, string description,
            string component = null, string recommendation = null, string helpUrl = null)
        {
            Category = category;
            Severity = severity;
            Title = title;
            Description = description;
            Component = component;
            Recommendation = recommendation;
            HelpUrl = helpUrl;
        }
    }
}
