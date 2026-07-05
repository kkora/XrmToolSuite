using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Trends
{
    /// <summary>
    /// The JSON + disk half of the trend store: loads and saves the per-machine snapshot history next to the
    /// tool's XrmToolBox settings. Deliberately separate from <see cref="TrendStore"/> (which is pure and
    /// unit-tested) — this uses Newtonsoft + file I/O, so it is manual-tested. Fail-soft: a missing or corrupt
    /// file reads as empty history rather than throwing. Read-only against Dataverse (nothing is written to the org).
    /// </summary>
    public static class TrendHistoryFile
    {
        private const string FileName = "XrmToolSuite.TechnicalDebtAnalyzer.trends.json";

        /// <summary>History file path under the XrmToolBox Settings folder (per machine/user).</summary>
        public static string Path()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return System.IO.Path.Combine(appData, "MscrmTools", "XrmToolBox", "Settings", FileName);
        }

        public static List<DebtSnapshot> Load()
        {
            try
            {
                var path = Path();
                if (!File.Exists(path)) return new List<DebtSnapshot>();
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<List<DebtSnapshot>>(json) ?? new List<DebtSnapshot>();
            }
            catch
            {
                return new List<DebtSnapshot>(); // corrupt/unreadable history must never break the tool
            }
        }

        public static void Save(List<DebtSnapshot> history)
        {
            if (history == null) return;
            var path = Path();
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonConvert.SerializeObject(history, Formatting.Indented));
        }
    }
}
