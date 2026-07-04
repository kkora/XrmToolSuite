using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.AttributeAuditor
{
    // MEF registration: this is what makes the tool appear in XrmToolBox.
    // SmallImageBase64/BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata — omit
    // either and MEF silently drops this tool from the Tools list. See PluginIcons.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Attribute Auditor"),
     ExportMetadata("Description", "Audits unused attributes across entities"),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "White"),
     ExportMetadata("PrimaryFontColor", "#000000"),
     ExportMetadata("SecondaryFontColor", "DarkGray")]
    public class AttributeAuditorPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new AttributeAuditorControl();
        }
    }
}
