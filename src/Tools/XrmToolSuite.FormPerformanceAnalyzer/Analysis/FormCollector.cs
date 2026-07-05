using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.FormPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Retrieves model-driven main forms (<c>systemform</c>, <c>type = 2</c>) and their entities' active
    /// form-scoped business rules (<c>workflow</c>, <c>category = 2</c>) from a live connection, parses each
    /// form's FormXML via <see cref="FormXmlParser"/>, and scores it with <see cref="FormScorer"/>. This is
    /// the only Dataverse-touching piece, so it is deliberately kept out of the SDK-free unit-test set.
    /// Per-form failures degrade to a <see cref="FormModel.ParseFailed"/> row rather than throwing, so one
    /// bad form can't fail the batch.
    /// </summary>
    public sealed class FormCollector
    {
        private readonly FormScoreOptions _options;

        public FormCollector(FormScoreOptions options = null)
        {
            _options = options ?? FormScoreOptions.Default;
        }

        /// <summary>
        /// Friendly names for well-known control classids (upper-case, braces stripped). Anything not here
        /// with a datafield is a standard field control; an unmatched classid is reported as "unknown control".
        /// </summary>
        private static readonly Dictionary<string, string> KnownControls =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "E7A81278-8635-4D9E-8D4D-59480B391C5B", "Subgrid" },
                { "5C5600E0-1D6E-4205-A272-BE48DA2CA630", "Quick View Form" },
                { "06375649-C143-495E-A496-C962E5B4488E", "Notes" },
                { "9FDF5F91-88B1-47F4-AD53-C11EFC01A01D", "Web Resource" },
                { "F9A8A302-114E-466A-B582-6771B2AE0D92", "IFRAME" },
                { "4273EDBD-AC1D-40D3-9FB2-095C621B552D", "Standard field" },
                { "5D68B988-0661-4DB2-BC3E-17598AD3BE6C", "Standard lookup" },
                { "270BD3DB-D9AF-4782-9025-509E298DEC0A", "Standard option set" },
                { "67FAC785-CD58-4F9F-ABB3-4B7DDC6ED5ED", "Standard radio/checkbox" },
                { "533B9E00-756B-4312-95A0-DC888637AC78", "Standard date" },
            };

        /// <summary>Resolves a control classid GUID to a friendly name, or "unknown control".</summary>
        public static string ResolveControl(string classId)
        {
            if (string.IsNullOrWhiteSpace(classId)) return "unknown control";
            var key = classId.Trim().Trim('{', '}').ToUpperInvariant();
            return KnownControls.TryGetValue(key, out var name) ? name : "unknown control";
        }

        public List<FormScore> Collect(
            IOrganizationService svc,
            IEnumerable<string> entityFilterOrNull,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));

            var filter = entityFilterOrNull?
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .ToList();

            progress?.Invoke("Retrieving active business rules...");
            var ruleCounts = RetrieveBusinessRuleCounts(svc, worker);

            progress?.Invoke("Retrieving main forms...");
            var forms = svc.RetrieveAll(BuildFormQuery(filter), worker: worker);

            var results = new List<FormScore>();
            int i = 0;
            foreach (var row in forms)
            {
                if (worker?.CancellationPending == true) break;
                i++;
                progress?.Invoke($"Scoring form {i} of {forms.Count}...");
                results.Add(ScoreRow(row, ruleCounts));
            }

            progress?.Invoke($"Scored {results.Count} form(s).");
            return FormScorer.Rank(results);
        }

        private static QueryExpression BuildFormQuery(List<string> entityFilter)
        {
            var query = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("name", "objecttypecode", "formxml", "formactivationstate", "type")
            };
            // type = 2 => Main form (the only type this tool scores).
            query.Criteria.AddCondition("type", ConditionOperator.Equal, 2);
            if (entityFilter != null && entityFilter.Count > 0)
                query.Criteria.AddCondition("objecttypecode", ConditionOperator.In, entityFilter.Cast<object>().ToArray());
            return query;
        }

        /// <summary>Active (statecode = 1) form-scoped business rules (category = 2) counted per primary entity.</summary>
        private static Dictionary<string, int> RetrieveBusinessRuleCounts(IOrganizationService svc, BackgroundWorker worker)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var query = new QueryExpression("workflow")
                {
                    ColumnSet = new ColumnSet("primaryentity")
                };
                query.Criteria.AddCondition("category", ConditionOperator.Equal, 2); // business rule
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1); // activated
                // Business rules exist as a definition (type 1) plus per-activation rows; count definitions.
                query.Criteria.AddCondition("type", ConditionOperator.Equal, 1);

                foreach (var w in svc.RetrieveAll(query, worker: worker))
                {
                    var entity = w.GetAttributeValue<string>("primaryentity");
                    if (string.IsNullOrWhiteSpace(entity)) continue;
                    counts.TryGetValue(entity, out var c);
                    counts[entity] = c + 1;
                }
            }
            catch (Exception)
            {
                // Degrade to "no business-rule signal" rather than failing the whole analysis.
            }
            return counts;
        }

        private FormScore ScoreRow(Entity row, Dictionary<string, int> ruleCounts)
        {
            var name = row.GetAttributeValue<string>("name");
            var entity = row.GetAttributeValue<string>("objecttypecode");
            var formXml = row.GetAttributeValue<string>("formxml");
            int state = row.GetAttributeValue<OptionSetValue>("formactivationstate")?.Value ?? 1;

            FormModel model;
            try
            {
                model = FormXmlParser.Parse(formXml);
            }
            catch (Exception)
            {
                model = new FormModel { ParseFailed = true };
            }
            model.FormName = name;
            model.Entity = entity;

            int ruleCount = 0;
            if (!string.IsNullOrWhiteSpace(entity))
                ruleCounts.TryGetValue(entity, out ruleCount);

            var score = FormScorer.Score(model, ruleCount, _options);
            score.State = state == 1 ? "Active" : "Inactive";
            return score;
        }
    }
}
