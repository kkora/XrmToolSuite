using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.FormPerformanceAnalyzer
{
    // MEF registration: this is what makes the tool appear in XrmToolBox.
    // NOTE: SmallImageBase64 and BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata.
    // If either is missing, MEF silently excludes the tool from the Tools list (no error,
    // no "Tools not loaded" entry). PluginIcons supplies the suite defaults.
    //
    // This is a SINGLE-DLL tool: it scores forms fully offline from FormXML and exports CSV/HTML by
    // hand, so it ships no ClosedXML/PdfSharp dependency chain and needs no AssemblyResolve handler.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Form Performance Analyzer"),
     ExportMetadata("Description", "Statically scores model-driven main forms by load 'heaviness' (tabs, sections, fields, PCF/custom controls, subgrids, quick views, scripts, handlers, business rules), bands them Light/Moderate/Heavy/Critical, gives targeted optimization recommendations, and exports CSV/HTML."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "White"),
     ExportMetadata("PrimaryFontColor", "#000000"),
     ExportMetadata("SecondaryFontColor", "DarkGray")]
    public class FormPerformanceAnalyzerPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new FormPerformanceAnalyzerControl();
        }
    }
}
