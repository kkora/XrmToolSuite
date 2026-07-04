using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.TemplateTool
{
    // MEF registration: this is what makes the tool appear in XrmToolBox.
    // NOTE: SmallImageBase64 and BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata.
    // If either is missing, MEF silently excludes the tool from the Tools list (no error,
    // no "Tools not loaded" entry). PluginIcons supplies suite defaults; replace with your
    // own base64 PNG icons if you like, but never remove the keys.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Template Tool"),
     ExportMetadata("Description", "Starter tool — replace with your description."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "White"),
     ExportMetadata("PrimaryFontColor", "#000000"),
     ExportMetadata("SecondaryFontColor", "DarkGray")]
    public class TemplateToolPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new TemplateToolControl();
        }
    }
}
