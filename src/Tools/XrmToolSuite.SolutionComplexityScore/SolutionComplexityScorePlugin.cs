using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.SolutionComplexityScore
{
    // SmallImageBase64/BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata — omit either and
    // MEF silently drops this tool from the Tools list. See PluginIcons.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Solution Complexity Score"),
     ExportMetadata("Description", "Measures a solution's complexity, maintainability, and upgrade/migration/testing effort with an executive dashboard and Excel/PDF/HTML/JSON reports."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "#1B1B2F"),
     ExportMetadata("PrimaryFontColor", "#FFFFFF"),
     ExportMetadata("SecondaryFontColor", "#BBBBBB")]
    public class SolutionComplexityScorePlugin : PluginBase
    {
        // Ships the Excel (ClosedXML) and native-PDF (PdfSharp/MigraDoc GDI) chains in a per-tool
        // SUBFOLDER named after this assembly (Plugins\XrmToolSuite.SolutionComplexityScore\), the
        // XrmToolBox store convention, so our dep versions stay isolated from every other tool's copy
        // in the shared AppDomain. This is safe for the Tools-list scan: we keep ClosedXML/PdfSharp/
        // MigraDoc types out of all signatures (method-body locals only), so MEF composition never
        // loads them while reading our metadata. This scoped AssemblyResolve handler is a safety net
        // for the mispackaged SixLabors.Fonts facades and touches ONLY our shipped assemblies. Keep
        // this set in sync with the csproj/nuspec lists.
        private static readonly string PluginDirectory =
            Path.GetDirectoryName(typeof(SolutionComplexityScorePlugin).Assembly.Location) ?? string.Empty;

        // Probe the per-tool subfolder first, then fall back to the plugin dir (root) so the same
        // handler resolves deps whether they ship in the subfolder (current) or the root (legacy).
        private static readonly string[] DependencyDirectories =
        {
            Path.Combine(PluginDirectory, typeof(SolutionComplexityScorePlugin).Assembly.GetName().Name),
            PluginDirectory,
        };

        private static readonly HashSet<string> OwnedDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ClosedXML", "DocumentFormat.OpenXml", "ExcelNumberFormat", "XLParser", "Irony",
            "SixLabors.Fonts", "System.IO.Packaging", "System.Numerics.Vectors",
            "System.Runtime.CompilerServices.Unsafe", "System.Buffers", "System.Memory",
            "System.Threading.Tasks.Extensions",
            "PdfSharp-gdi", "PdfSharp.Charting-gdi", "MigraDoc.DocumentObjectModel-gdi",
            "MigraDoc.Rendering-gdi", "MigraDoc.RtfRendering-gdi",
        };

        [ThreadStatic] private static HashSet<string> _resolving;

        static SolutionComplexityScorePlugin()
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
                foreach (var dir in DependencyDirectories)
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    var candidate = Path.Combine(dir, simpleName + ".dll");
                    if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
                }
                return null;
            }
            finally
            {
                resolving.Remove(simpleName);
            }
        }

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new SolutionComplexityScoreControl();
        }
    }
}
