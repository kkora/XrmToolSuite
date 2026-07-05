using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.SolutionDocumentationGenerator
{
    // MEF registration: this is what makes the tool appear in XrmToolBox.
    // SmallImageBase64 / BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata — omit either and MEF
    // silently drops the tool from the Tools list (no error). PluginIcons supplies suite defaults.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Solution Documentation Generator"),
     ExportMetadata("Description", "Scans a Dataverse solution and generates multi-section technical/business documentation (schema, UI, automation, plug-ins, web resources, custom APIs, config, roles, diagrams, summaries) exported to Word/PDF/Markdown/HTML/Excel/JSON."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "#1B1B2F"),
     ExportMetadata("PrimaryFontColor", "#FFFFFF"),
     ExportMetadata("SecondaryFontColor", "#BBBBBB")]
    public class SolutionDocumentationGeneratorPlugin : PluginBase
    {
        // The Excel-export (ClosedXML) and native-PDF (PdfSharp/MigraDoc) chains ship in the Plugins ROOT
        // alongside this DLL (NOT a subfolder): XrmToolBox's plugin analysis must resolve this tool's direct
        // ClosedXML / PdfSharp / MigraDoc references at scan time, before this AssemblyResolve handler is
        // registered. If those references aren't resolvable during that scan, XrmToolBox silently drops the
        // tool from the Tools list. The handler below is a scoped runtime fallback for hosts whose binding
        // redirects don't cover SixLabors.Fonts' facade version mismatch. It is limited to the OwnedDependencies
        // whitelist so it can never interfere with the other tools in the shared AppDomain, and keeps a
        // re-entrancy guard so a dependency cycle cannot StackOverflow.
        private static readonly string DependencyDirectory =
            Path.GetDirectoryName(typeof(SolutionDocumentationGeneratorPlugin).Assembly.Location) ?? string.Empty;

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

        static SolutionDocumentationGeneratorPlugin()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependency;
        }

        private static Assembly ResolveDependency(object sender, ResolveEventArgs args)
        {
            var simpleName = new AssemblyName(args.Name).Name;

            // Only ever resolve assemblies THIS tool ships — never anything else in the shared AppDomain.
            if (!OwnedDependencies.Contains(simpleName))
                return null;

            // An already-loaded assembly of the same simple name satisfies the bind even if the requested
            // version differs — this is what papers over the facade version mismatch.
            foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(loaded.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                    return loaded;
            }

            // Re-entrancy guard: LoadFrom triggers nested resolves for the loaded assembly's own dependencies.
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
            return new SolutionDocumentationGeneratorControl();
        }
    }
}
