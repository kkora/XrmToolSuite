using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Xunit;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.SolutionComplexityScore.Analysis;

namespace XrmToolSuite.CollectorTests
{
    /// <summary>
    /// Headless tests for the Solution Complexity Score data collector. The collector is UI-free and
    /// depends only on <see cref="IOrganizationService"/>, so it runs against the shared
    /// <see cref="FakeOrganizationService"/> with no live org. Downstream scoring
    /// (<c>ComplexityMetrics.Compute</c>) is covered SDK-free in testing/UnitTests; these cases verify
    /// the query-driven counting that turns solution rows into a <see cref="ComponentCounts"/> tally.
    /// Traces to US-SOLN08-2 (component inventory). TC-SOLN08-COL-01..08.
    /// </summary>
    public class ComplexityCollectorTests
    {
        private static readonly Guid SolId = new Guid("5C000000-0000-0000-0000-000000000001");

        private static Entity Solution() => new Entity("solution", SolId)
        {
            ["uniquename"] = "contoso_core",
            ["friendlyname"] = "Contoso Core",
            ["version"] = "1.0.0.0",
            ["ismanaged"] = false
        };

        private static Entity Component(int type) => new Entity("solutioncomponent", Guid.NewGuid())
        {
            ["componenttype"] = new OptionSetValue(type),
            ["objectid"] = Guid.NewGuid(),
            ["solutionid"] = SolId
        };

        private static Entity Web(int type) =>
            new Entity("webresource", Guid.NewGuid()) { ["webresourcetype"] = new OptionSetValue(type) };

        private static Entity Form(int type, string name, string formxml) =>
            new Entity("systemform", Guid.NewGuid()) { ["type"] = new OptionSetValue(type), ["name"] = name, ["formxml"] = formxml };

        private static Entity Workflow(int category) =>
            new Entity("workflow", Guid.NewGuid()) { ["category"] = new OptionSetValue(category) };

        private static Entity Row(string logical) => new Entity(logical, Guid.NewGuid());

        private static string FormXml(int controls) =>
            "<form>" + string.Concat(Enumerable.Repeat("<control id='c'/>", controls)) + "</form>";

        /// <summary>Build the context (loading components) and run the collector.</summary>
        private static ComponentCounts Run(FakeOrganizationService fake)
        {
            var ctx = new ComplexityContext(fake, Solution());
            ctx.LoadComponents();
            return ComplexityCollector.Collect(ctx, _ => { });
        }

        // TC-SOLN08-COL-01: solutioncomponent type codes tally into the metadata counts.
        [Fact]
        public void ComponentTypes_TallyByCode()
        {
            var fake = new FakeOrganizationService().Seed("solutioncomponent",
                Component(ComplexityContext.CT_Entity),
                Component(ComplexityContext.CT_Entity),
                Component(ComplexityContext.CT_Attribute),
                Component(ComplexityContext.CT_EntityRelationship),
                Component(ComplexityContext.CT_SdkMessageProcessingStep),
                Component(ComplexityContext.CT_CustomControl));

            var c = Run(fake);
            Assert.Equal(2, c.Tables);
            Assert.Equal(1, c.Columns);
            Assert.Equal(1, c.Relationships);
            Assert.Equal(1, c.PluginSteps);
            Assert.Equal(1, c.Pcfs);
        }

        // TC-SOLN08-COL-02: only JScript (type 3) web resources count as JavaScript.
        [Fact]
        public void WebResources_CountJScriptOnly()
        {
            var fake = new FakeOrganizationService().Seed("webresource", Web(3), Web(3), Web(1) /* CSS */);
            Assert.Equal(2, Run(fake).JavaScriptWebResources);
        }

        // TC-SOLN08-COL-03: dashboards (type 0) and forms are counted separately.
        [Fact]
        public void Forms_DashboardsSplitFromForms()
        {
            var fake = new FakeOrganizationService().Seed("systemform",
                Form(0, "Sales Dashboard", FormXml(1)),
                Form(2, "Account Main", FormXml(3)));

            var c = Run(fake);
            Assert.Equal(1, c.Dashboards);
            Assert.Equal(1, c.Forms);
        }

        // TC-SOLN08-COL-04: the widest form is the one with the most <control> elements.
        [Fact]
        public void Forms_WidestByControlCount()
        {
            var fake = new FakeOrganizationService().Seed("systemform",
                Form(2, "Narrow", FormXml(2)),
                Form(2, "Wide", FormXml(5)));

            var c = Run(fake);
            Assert.Equal(5, c.WidestForm);
            Assert.Equal("Wide", c.WidestFormName);
        }

        // TC-SOLN08-COL-05: workflow category splits business rules / flows / classic workflows.
        [Fact]
        public void Workflows_SplitByCategory()
        {
            var fake = new FakeOrganizationService().Seed("workflow",
                Workflow(2),  // business rule
                Workflow(5),  // modern flow
                Workflow(0),  // classic workflow
                Workflow(3)); // action -> classic bucket

            var c = Run(fake);
            Assert.Equal(1, c.BusinessRules);
            Assert.Equal(1, c.Flows);
            Assert.Equal(2, c.Workflows);
        }

        // TC-SOLN08-COL-06: Apps = model-driven (appmodule) + canvas (canvasapp).
        [Fact]
        public void Apps_SumModelDrivenAndCanvas()
        {
            var fake = new FakeOrganizationService()
                .Seed("appmodule", Row("appmodule"), Row("appmodule"))
                .Seed("canvasapp", Row("canvasapp"));
            Assert.Equal(3, Run(fake).Apps);
        }

        // TC-SOLN08-COL-07: views and charts come from savedquery / savedqueryvisualization.
        [Fact]
        public void ViewsAndCharts_Counted()
        {
            var fake = new FakeOrganizationService()
                .Seed("savedquery", Row("savedquery"), Row("savedquery"))
                .Seed("savedqueryvisualization", Row("savedqueryvisualization"));

            var c = Run(fake);
            Assert.Equal(2, c.Views);
            Assert.Equal(1, c.Charts);
        }

        // TC-SOLN08-COL-08: a form with no formxml contributes zero controls (no crash, widest stays 0).
        [Fact]
        public void Form_NullFormXml_ZeroControls()
        {
            var fake = new FakeOrganizationService().Seed("systemform",
                new Entity("systemform", Guid.NewGuid()) { ["type"] = new OptionSetValue(2), ["name"] = "Empty" });

            var c = Run(fake);
            Assert.Equal(1, c.Forms);
            Assert.Equal(0, c.WidestForm);
        }
    }
}
