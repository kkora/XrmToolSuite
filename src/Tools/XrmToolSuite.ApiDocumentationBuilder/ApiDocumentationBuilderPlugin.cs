using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.ApiDocumentationBuilder
{
    // MEF registration: this is what makes the tool appear in XrmToolBox.
    // NOTE: SmallImageBase64 and BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata.
    // If either is missing, MEF silently excludes the tool from the Tools list (no error,
    // no "Tools not loaded" entry). PluginIcons supplies suite defaults; replace with your
    // own base64 PNG icons if you like, but never remove the keys.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "API Documentation Builder"),
     ExportMetadata("Description", "Documents Dataverse Custom APIs (parameters, responses, binding, backing plugin) as a redaction-safe reference and a best-effort OpenAPI-style JSON spec; exports Markdown, self-contained HTML, raw JSON, and OpenAPI JSON. Read-only — never invokes an API; masks secret-named values."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "#1B1B2F"),
     ExportMetadata("PrimaryFontColor", "#FFFFFF"),
     ExportMetadata("SecondaryFontColor", "#BBBBBB")]
    public class ApiDocumentationBuilderPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new ApiDocumentationBuilderControl();
        }
    }
}
