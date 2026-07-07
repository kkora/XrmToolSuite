using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.CustomApiExplorer
{
    // MEF registration: this is what makes the tool appear in XrmToolBox.
    // NOTE: SmallImageBase64 and BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata.
    // If either is missing, MEF silently excludes the tool from the Tools list (no error,
    // no "Tools not loaded" entry). PluginIcons supplies suite defaults; replace with your
    // own base64 PNG icons if you like, but never remove the keys.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Custom API Explorer"),
     ExportMetadata("Description", "Browse and document Custom APIs (parameters, responses, binding, backing plugin) and safely test them from a confirmation-gated invoke console. Inventory/documentation is read-only; the console is the only write path and is explicitly confirmed. Never stores or displays secrets. HTML/Markdown/CSV export."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "White"),
     ExportMetadata("PrimaryFontColor", "#000000"),
     ExportMetadata("SecondaryFontColor", "DarkGray")]
    public class CustomApiExplorerPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new CustomApiExplorerControl();
        }
    }
}
