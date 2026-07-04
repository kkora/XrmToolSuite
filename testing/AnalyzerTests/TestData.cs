using System;
using Microsoft.Xrm.Sdk;
using XrmToolSuite.AnalyzerTests.Fakes;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;

namespace XrmToolSuite.AnalyzerTests
{
    /// <summary>Builders for the fixtures the analyzer tests share.</summary>
    internal static class TestData
    {
        public static Entity Solution(string uniqueName = "contoso_core", string version = "1.0.0.0", bool managed = false)
        {
            var e = new Entity("solution", Guid.NewGuid());
            e["uniquename"] = uniqueName;
            e["version"] = version;
            e["ismanaged"] = managed;
            e["publisherid"] = new EntityReference("publisher", Guid.NewGuid());
            return e;
        }

        /// <summary>
        /// Builds an AnalyzerContext over the given fakes. The source is pre-seeded with an empty
        /// solutioncomponent table and empty entity metadata so LoadComponents()/SolutionEntityLogicalNames()
        /// resolve to "no components" - tests that need components seed them explicitly on <paramref name="source"/>.
        /// </summary>
        public static AnalyzerContext Context(FakeOrganizationService source, FakeOrganizationService target = null, Entity solution = null)
        {
            source.SeedIfAbsent("solutioncomponent");
            var ctx = new AnalyzerContext(source, target, solution ?? Solution());
            ctx.LoadComponents();
            return ctx;
        }

        /// <summary>An environmentvariabledefinition row.</summary>
        public static Entity EnvVarDef(string schema, string display = null, int? type = null,
            string defaultValue = null, bool required = false)
        {
            var e = new Entity("environmentvariabledefinition", Guid.NewGuid());
            e["schemaname"] = schema;
            e["displayname"] = display ?? schema;
            if (type.HasValue) e["type"] = new OptionSetValue(type.Value);
            e["defaultvalue"] = defaultValue;
            e["isrequired"] = required;
            return e;
        }

        /// <summary>An environmentvariablevalue row for a definition.</summary>
        public static Entity EnvVarValue(Guid definitionId, string value)
        {
            var e = new Entity("environmentvariablevalue", Guid.NewGuid());
            e["environmentvariabledefinitionid"] = new EntityReference("environmentvariabledefinition", definitionId);
            e["value"] = value;
            return e;
        }

        /// <summary>A workflow row (flows/processes live in the workflow table).</summary>
        public static Entity Flow(string name, int category, int stateCode, string clientData = null, int type = 1)
        {
            var e = new Entity("workflow", Guid.NewGuid());
            e["name"] = name;
            e["category"] = new OptionSetValue(category);
            e["statecode"] = new OptionSetValue(stateCode);
            e["type"] = new OptionSetValue(type);
            if (clientData != null) e["clientdata"] = clientData;
            return e;
        }

        /// <summary>
        /// An sdkmessageprocessingstep row. Steps sharing (message, filter, stage) fire on the same event;
        /// duplicate-registration and rank-conflict checks compare within such a group.
        /// </summary>
        public static Entity Step(string name, Guid pluginType, Guid message, Guid filter,
            int stage = 40, int mode = 0, int rank = 1, string filteringAttributes = null, int stateCode = 0)
        {
            var e = new Entity("sdkmessageprocessingstep", Guid.NewGuid());
            e["name"] = name;
            e["plugintypeid"] = new EntityReference("plugintype", pluginType);
            e["sdkmessageid"] = new EntityReference("sdkmessage", message);
            e["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", filter);
            e["stage"] = new OptionSetValue(stage);
            e["mode"] = new OptionSetValue(mode);
            e["rank"] = rank;
            if (filteringAttributes != null) e["filteringattributes"] = filteringAttributes;
            e["statecode"] = new OptionSetValue(stateCode);
            return e;
        }

        /// <summary>
        /// A solutioncomponent row. <paramref name="solutionId"/> must match the owning solution's Id so
        /// the fake's solutionid-Equal filter (used by LoadComponents and the deleted-component diff) matches.
        /// </summary>
        public static Entity SolutionComponent(int componentType, Guid objectId, Guid solutionId)
        {
            var e = new Entity("solutioncomponent", Guid.NewGuid());
            e["componenttype"] = new OptionSetValue(componentType);
            e["objectid"] = objectId;
            e["solutionid"] = solutionId;
            return e;
        }

        /// <summary>A solution row for the TARGET side, keyed by unique name with an explicit managed flag.</summary>
        public static Entity TargetSolution(string uniqueName, bool managed)
        {
            var e = new Entity("solution", Guid.NewGuid());
            e["uniquename"] = uniqueName;
            e["ismanaged"] = managed;
            return e;
        }

        /// <summary>A systemform row (forms live in the systemform table; formxml holds the layout).</summary>
        public static Entity SystemForm(string name, string entity, string formXml)
        {
            var e = new Entity("systemform", Guid.NewGuid());
            e["name"] = name;
            e["objecttypecode"] = entity;
            e["formxml"] = formXml;
            e["type"] = new OptionSetValue(2); // main form
            return e;
        }

        /// <summary>A ribboncustomization row (ribbondiffxml holds command-bar customizations for an entity).</summary>
        public static Entity RibbonCustomization(string entity, string ribbonDiffXml)
        {
            var e = new Entity("ribboncustomization", Guid.NewGuid());
            e["entity"] = entity;
            e["ribbondiffxml"] = ribbonDiffXml;
            return e;
        }

        /// <summary>A webresource row (existence check for form/ribbon references).</summary>
        public static Entity WebResource(string name)
        {
            var e = new Entity("webresource", Guid.NewGuid());
            e["name"] = name;
            return e;
        }

        /// <summary>A missingdependency row as returned by RetrieveMissingDependenciesRequest.</summary>
        public static Entity MissingDependency(int requiredComponentType, Guid? objectId = null, Guid? baseSolutionId = null)
        {
            var e = new Entity("dependency");
            e["requiredcomponenttype"] = new OptionSetValue(requiredComponentType);
            e["requiredcomponentobjectid"] = objectId ?? Guid.NewGuid();
            if (baseSolutionId.HasValue) e["requiredcomponentbasesolutionid"] = baseSolutionId.Value;
            return e;
        }

        /// <summary>A connectionreference row.</summary>
        public static Entity ConnRef(string logicalName, string display = null, string connectorId = null, string connectionId = null)
        {
            var e = new Entity("connectionreference", Guid.NewGuid());
            e["connectionreferencelogicalname"] = logicalName;
            e["connectionreferencedisplayname"] = display ?? logicalName;
            e["connectorid"] = connectorId;
            e["connectionid"] = connectionId;
            return e;
        }
    }
}
