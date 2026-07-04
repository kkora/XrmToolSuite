using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.TechnicalDebtAnalyzer
{
    // SmallImageBase64/BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata — omit
    // either and MEF silently drops this tool from the Tools list. See PluginIcons.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Technical Debt Analyzer"),
     ExportMetadata("Description", "Scans a Dataverse environment to calculate a 0-100 Technical Debt Score and produce prioritized cleanup recommendations with Excel/PDF/HTML/JSON reports."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "#1B1B2F"),
     ExportMetadata("PrimaryFontColor", "#FFFFFF"),
     ExportMetadata("SecondaryFontColor", "#BBBBBB")]
    public class TechnicalDebtAnalyzerPlugin : PluginBase
    {
        // This tool ships the Excel (ClosedXML) and native-PDF (PdfSharp/MigraDoc GDI) chains in the
        // Plugins ROOT alongside this DLL (NOT a subfolder) so XrmToolBox can resolve its direct
        // ClosedXML/PdfSharp/MigraDoc references at scan time. This scoped AssemblyResolve handler is a
        // safety net for hosts whose binding redirects don't cover the mispackaged SixLabors.Fonts 1.0.0
        // facades: for OUR shipped dependencies ONLY it satisfies a failed bind with a compatible
        // version next to us. It does nothing for any other assembly, so it can never interfere with the
        // other tools sharing the AppDomain. Keep this set in sync with the csproj/nuspec dependency lists.
        private static readonly string DependencyDirectory =
            Path.GetDirectoryName(typeof(TechnicalDebtAnalyzerPlugin).Assembly.Location) ?? string.Empty;

        private static readonly HashSet<string> OwnedDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ClosedXML", "DocumentFormat.OpenXml", "ExcelNumberFormat", "XLParser", "Irony",
            "SixLabors.Fonts", "System.IO.Packaging", "System.Numerics.Vectors",
            "System.Runtime.CompilerServices.Unsafe", "System.Buffers", "System.Memory",
            "System.Threading.Tasks.Extensions",
            // Native-PDF chain (PdfSharp/MigraDoc, GDI build — assemblies carry a -gdi suffix)
            "PdfSharp-gdi", "PdfSharp.Charting-gdi", "MigraDoc.DocumentObjectModel-gdi",
            "MigraDoc.Rendering-gdi", "MigraDoc.RtfRendering-gdi",
        };

        [ThreadStatic] private static HashSet<string> _resolving;

        static TechnicalDebtAnalyzerPlugin()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependency;
        }

        private static Assembly ResolveDependency(object sender, ResolveEventArgs args)
        {
            var simpleName = new AssemblyName(args.Name).Name;
            if (!OwnedDependencies.Contains(simpleName))
                return null;

            foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
                if (string.Equals(loaded.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                    return loaded;

            var resolving = _resolving ?? (_resolving = new HashSet<string>());
            if (!resolving.Add(simpleName))
                return null;
            try
            {
                var candidate = Path.Combine(DependencyDirectory, simpleName + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            }
            finally
            {
                resolving.Remove(simpleName);
            }
        }

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new TechnicalDebtAnalyzerControl();
        }
    }
}
