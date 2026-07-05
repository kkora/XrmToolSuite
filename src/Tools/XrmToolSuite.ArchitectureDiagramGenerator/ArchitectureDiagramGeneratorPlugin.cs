using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.ArchitectureDiagramGenerator
{
    // MEF registration: this is what makes the tool appear in XrmToolBox.
    // NOTE: SmallImageBase64 and BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata.
    // If either is missing, MEF silently excludes the tool from the Tools list (no error,
    // no "Tools not loaded" entry). PluginIcons supplies suite defaults; replace with your
    // own base64 PNG icons if you like, but never remove the keys.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Architecture Diagram Generator"),
     ExportMetadata("Description", "Generates architecture diagrams from a Dataverse solution's components and platform dependencies — components classified into layers — exported to Mermaid, PlantUML, DOT/Graphviz, Markdown, self-contained HTML, or JSON."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "#1B1B2F"),
     ExportMetadata("PrimaryFontColor", "#FFFFFF"),
     ExportMetadata("SecondaryFontColor", "#BBBBBB")]
    public class ArchitectureDiagramGeneratorPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new ArchitectureDiagramGeneratorControl();
        }
    }
}
