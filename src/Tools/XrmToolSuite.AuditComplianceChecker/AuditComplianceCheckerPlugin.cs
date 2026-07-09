using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;

namespace XrmToolSuite.AuditComplianceChecker
{
    // SmallImageBase64/BigImageBase64 are REQUIRED by XrmToolBox's IPluginMetadata — omit
    // either and MEF silently drops this tool from the Tools list. See PluginIcons.
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "Audit Compliance Checker"),
     ExportMetadata("Description", "Checks org/table/column audit settings and audit activity, flags coverage gaps and risky changes, and scores audit compliance readiness, with an exportable report (Excel/PDF/JSON/HTML/CSV)."),
     ExportMetadata("SmallImageBase64", PluginIcons.Small),
     ExportMetadata("BigImageBase64", PluginIcons.Big),
     ExportMetadata("BackgroundColor", "White"),
     ExportMetadata("PrimaryFontColor", "#000000"),
     ExportMetadata("SecondaryFontColor", "DarkGray")]
    public class AuditComplianceCheckerPlugin : PluginBase
    {
        // This tool ships the Excel-export (ClosedXML) and native-PDF (PdfSharp/MigraDoc) chains in a
        // per-tool SUBFOLDER named after this assembly (Plugins\XrmToolSuite.AuditComplianceChecker\),
        // the XrmToolBox store convention, so our dep versions stay isolated from every other tool's copy
        // in the shared AppDomain. This is safe for the Tools-list scan: we keep ClosedXML/PdfSharp/
        // MigraDoc types out of all signatures (method-body locals only), so MEF composition never
        // loads them while reading our metadata — the tool lists fine whether or not they're on the
        // probe path. They're only needed at runtime (export), which this handler satisfies below.
        //
        // The handler below is a SCOPED runtime fallback: for OUR shipped dependencies only, it
        // satisfies a failed bind with whatever compatible version sits next to us (papering over the
        // SixLabors.Fonts facade version mismatch on hosts whose binding redirects don't cover it). It
        // deliberately does nothing for any other assembly so it can never interfere with the other
        // tools in the same AppDomain.
        private static readonly string PluginDirectory =
            Path.GetDirectoryName(typeof(AuditComplianceCheckerPlugin).Assembly.Location) ?? string.Empty;

        // Probe the per-tool subfolder first, then fall back to the plugin dir (root) so the same
        // handler resolves deps whether they ship in the subfolder (current) or the root (legacy).
        private static readonly string[] DependencyDirectories =
        {
            Path.Combine(PluginDirectory, typeof(AuditComplianceCheckerPlugin).Assembly.GetName().Name),
            PluginDirectory,
        };

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

        static AuditComplianceCheckerPlugin()
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
            return new AuditComplianceCheckerControl();
        }
    }
}
