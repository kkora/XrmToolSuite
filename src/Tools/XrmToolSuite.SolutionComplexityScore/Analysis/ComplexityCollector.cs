using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace XrmToolSuite.SolutionComplexityScore.Analysis
{
    /// <summary>
    /// Turns a live solution into a <see cref="ComponentCounts"/> tally. UI-free and fail-soft: every
    /// query is scoped to the solution and degrades to zero on error, so a permission gap lowers a
    /// dimension rather than aborting the scan. Splitting webresource/systemform/workflow rows by their
    /// sub-type is what lets the tool separate JS from CSS, dashboards from forms, and flows from workflows.
    /// </summary>
    public static class ComplexityCollector
    {
        // webresourcetype: 3 = JScript. systemform type: 0 = Dashboard. workflow category: 0=Workflow,
        // 2=BusinessRule, 3=Action, 5=Modern (cloud) flow.
        public static ComponentCounts Collect(ComplexityContext ctx, Action<string> progress)
        {
            var c = new ComponentCounts();

            progress?.Invoke("Counting tables, columns and relationships…");
            c.Tables = ctx.CountOfType(ComplexityContext.CT_Entity);
            c.Columns = ctx.CountOfType(ComplexityContext.CT_Attribute);
            c.Relationships = ctx.CountOfType(ComplexityContext.CT_EntityRelationship);
            c.PluginSteps = ctx.CountOfType(ComplexityContext.CT_SdkMessageProcessingStep);
            c.Pcfs = ctx.CountOfType(ComplexityContext.CT_CustomControl);

            progress?.Invoke("Counting views and charts…");
            c.Views = ctx.QuerySolutionRows("savedquery", "savedqueryid", "savedqueryid").Entities.Count;
            c.Charts = ctx.QuerySolutionRows("savedqueryvisualization", "savedqueryvisualizationid", "savedqueryvisualizationid").Entities.Count;

            progress?.Invoke("Inspecting web resources…");
            var web = ctx.QuerySolutionRows("webresource", "webresourceid", "webresourcetype");
            c.JavaScriptWebResources = web.Entities.Count(w => w.GetAttributeValue<OptionSetValue>("webresourcetype")?.Value == 3);

            progress?.Invoke("Inspecting forms and dashboards…");
            var forms = ctx.QuerySolutionRows("systemform", "formid", "type", "name", "formxml");
            foreach (var f in forms.Entities)
            {
                int type = f.GetAttributeValue<OptionSetValue>("type")?.Value ?? -1;
                if (type == 0) { c.Dashboards++; continue; }
                c.Forms++;
                int controls = CountControls(f.GetAttributeValue<string>("formxml"));
                if (controls > c.WidestForm)
                {
                    c.WidestForm = controls;
                    c.WidestFormName = f.GetAttributeValue<string>("name");
                }
            }

            progress?.Invoke("Classifying processes (workflows, flows, business rules)…");
            var procs = ctx.QuerySolutionRows("workflow", "workflowid", "category");
            foreach (var p in procs.Entities)
            {
                switch (p.GetAttributeValue<OptionSetValue>("category")?.Value)
                {
                    case 2: c.BusinessRules++; break;
                    case 5: c.Flows++; break;
                    default: c.Workflows++; break; // 0 Workflow, 3 Action, others
                }
            }

            progress?.Invoke("Counting custom APIs and apps…");
            c.CustomApis = ctx.QuerySolutionRows("customapi", "customapiid", "customapiid").Entities.Count;
            int mdApps = ctx.QuerySolutionRows("appmodule", "appmoduleid", "appmoduleid").Entities.Count;
            int canvas = ctx.QuerySolutionRows("canvasapp", "canvasappid", "canvasappid").Entities.Count;
            c.Apps = mdApps + canvas;

            return c;
        }

        private static readonly Regex ControlRegex = new Regex("<control\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static int CountControls(string formXml) =>
            string.IsNullOrEmpty(formXml) ? 0 : ControlRegex.Matches(formXml).Count;
    }
}
