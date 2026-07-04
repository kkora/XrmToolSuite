namespace XrmToolSuite.Core.Analysis
{
    /// <summary>
    /// Severity of an individual <see cref="Finding"/>. Shared by every analysis tool in the suite
    /// (deployment risk, technical debt, complexity, AI review). Integer order is meaningful and is
    /// used for sorting and score weighting, so do not renumber.
    /// </summary>
    public enum Severity
    {
        Info = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>Overall classification a score is banded into (Low / Medium / High).</summary>
    public enum ScoreBand
    {
        Low,
        Medium,
        High
    }
}
