using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.FlowDependencyAnalyzer
{
    // SmallImageBase64/BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata — omit
    // either and MEF silently drops this tool from the Tools list. See PluginIcons.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Flow Dependency Analyzer"),
     ExportMetadata("Description", "Read-only static mapper of cloud-flow dependencies (Dataverse tables/columns, connectors, connection references, environment variables, child flows, custom APIs, HTTP actions) from clientdata, with reverse component-impact analysis and Excel/PDF/JSON/HTML export. Secrets and URLs are always redacted."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "White"),
     ExportMetadata("PrimaryFontColor", "#000000"),
     ExportMetadata("SecondaryFontColor", "DarkGray")]
    public class FlowDependencyAnalyzerPlugin : PluginBase
    {
        // The Excel-export (ClosedXML) and native-PDF (PdfSharp/MigraDoc) chains ship in the Plugins
        // ROOT alongside this DLL (NOT a subfolder): XrmToolBox's plugin analysis must be able to
        // resolve this tool's direct ClosedXML / PdfSharp / MigraDoc references at scan time, and our
        // AssemblyResolve handler isn't registered until the plugin is first instantiated. If those
        // references aren't resolvable during that scan, XrmToolBox silently drops the tool from the
        // Tools list. So the deps must sit next to us where normal probing finds them.
        //
        // SixLabors.Fonts 1.0.0 is mispackaged (compiled against System.Numerics.Vectors 4.1.3.0 /
        // System.Memory 4.0.1.1 but its NuGet deps demand higher, assembly 4.1.4.0 / 4.0.1.2).
        // Current XrmToolBox ships those facades in its app dir with binding redirects that cover
        // the range, so the mismatch resolves for free at runtime. This handler is a scoped safety
        // net for hosts whose redirects don't cover the range: for OUR shipped dependencies only,
        // it satisfies a failed bind with whatever compatible version sits next to us (ignoring the
        // requested version = a runtime binding redirect). It deliberately does nothing for any
        // other assembly so it can never interfere with the other tools in the same AppDomain.
        private static readonly string DependencyDirectory =
            Path.GetDirectoryName(typeof(FlowDependencyAnalyzerPlugin).Assembly.Location) ?? string.Empty;

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

        static FlowDependencyAnalyzerPlugin()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependency;
        }

        private static Assembly ResolveDependency(object sender, ResolveEventArgs args)
        {
            var simpleName = new AssemblyName(args.Name).Name;

            // Only ever resolve assemblies THIS tool ships — never anything else in the shared AppDomain.
            if (!OwnedDependencies.Contains(simpleName))
                return null;

            // An already-loaded assembly of the same simple name satisfies the bind even if the
            // requested version differs — this is what papers over the facade version mismatch.
            foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(loaded.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                    return loaded;
            }

            // Re-entrancy guard: LoadFrom below triggers nested resolves for the loaded assembly's
            // own dependencies. Without this, a dependency cycle recurses into a StackOverflow.
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
            return new FlowDependencyAnalyzerControl();
        }
    }
}
