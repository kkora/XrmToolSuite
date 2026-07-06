using System;
using System.IO;
using System.Linq;
using XrmToolSuite.UiSmokeTests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace XrmToolSuite.UiSmokeTests
{
    /// <summary>
    /// TIER-3b — CONNECTED walkthrough (opt-in, LOCAL only). Unlike the load smoke test, this one proves a
    /// tool comes up *connected*: it opens a plugin, waits for its "Connected to &lt;env&gt;" marker (written by
    /// the plugin's UpdateConnection), and captures a screenshot. That exercises the pre-seeded-connection
    /// path (Option 1) end-to-end — the connection selected in XrmToolBox is handed to the opened plugin.
    ///
    /// WHY IT IS OPT-IN AND NOT IN CI:
    ///   * It needs a LIVE Dataverse connection. The suite's XTS-CI-* connections are interactive
    ///     (OnlineFederation / AD, SavePassword=false), so they reconnect only while an MSAL token is warm in
    ///     THIS Windows profile — fine on a dev box you just used, impossible to drive unattended on a CI
    ///     runner (it would block on an auth prompt). Wiring this into CI would require a service-principal
    ///     (ClientSecret/Certificate) connection instead.
    ///   * So it only runs when you explicitly set XTB_CONNECTED_TEST=1. Without it, the test no-ops (passes)
    ///     with a logged "skipped" line, so `dotnet test` on the project stays green on machines without a
    ///     warm connection.
    ///
    /// PRE-FLIGHT: run scripts/Setup-TestConnection.ps1 first — it validates the target connection exists in
    /// ConnectionsV2.xml, warns if it can't run unattended, and prints the env vars to set.
    ///
    /// Env vars:
    ///   XTB_CONNECTED_TEST   "1" to actually run (otherwise the test is skipped/no-op)
    ///   XTB_EXE              full path to XrmToolBox.exe (same as the load smoke test)
    ///   XTB_TEST_CONNECTION  connection name to expect connected (default "XTS-CI-TEST") — NEVER a prod org
    ///   XTB_WALKTHROUGH_TOOL plugin display name to open (default "Deployment Risk Analyzer")
    ///   UISMOKE_SCREENSHOT_DIR  where PNGs land (default %TEMP%\xtb-ui-smoke)
    /// </summary>
    public sealed class ConnectedWalkthroughTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();

        public ConnectedWalkthroughTest(ITestOutputHelper output) => _output = output;

        [Fact]
        public void OpenTool_ConnectsToTestEnvironment()
        {
            if (!IsEnabled())
            {
                _output.WriteLine("Skipped: set XTB_CONNECTED_TEST=1 (and connect XTS-CI-TEST once in XrmToolBox " +
                                  "so the token is warm) to run the connected walkthrough. See README.md Tier-3b.");
                return; // opt-in: a machine without a warm connection stays green
            }

            var connection = EnvOr("XTB_TEST_CONNECTION", "XTS-CI-TEST");
            var tool = EnvOr("XTB_WALKTHROUGH_TOOL", "Deployment Risk Analyzer");
            var attach = string.Equals(Environment.GetEnvironmentVariable("XTB_ATTACH"), "1", StringComparison.Ordinal);

            AssertNotProdConnection(connection);

            if (attach)
            {
                // ATTACH mode (recommended for interactive connections): the human already started XrmToolBox
                // and connected the test org, so we drive the live, connected session.
                _host.AttachToRunning(TimeSpan.FromSeconds(30));
                _host.Maximize();
                System.Threading.Thread.Sleep(1500);
            }
            else
            {
                // LAUNCH mode: only works if XrmToolBox auto-reconnects on startup (not the default) — otherwise
                // the tool opens disconnected. Prefer XTB_ATTACH=1 for the suite's interactive connections.
                _host.LaunchAndAttach(ResolveXtbExe(), TimeSpan.FromSeconds(60));
                _host.Maximize();
                System.Threading.Thread.Sleep(12000); // splash close + any auto-reconnect
            }
            _host.CloseToolLibraryTab();

            // Re-run resilience: a prior run may have left the tool's tab open. XrmToolBox titles a plugin tab
            // "<Tool> (<ConnectionName>)" when it opens WITH a live connection (just "<Tool>" when disconnected).
            // If that connected tab is already present, we're done — no need to reopen from the Tools list.
            var caption = _host.WaitForConnectedToolTab(tool, TimeSpan.FromSeconds(3));
            if (caption == null)
            {
                var opened = _host.OpenTool(tool);
                _host.Screenshot(ShotPath(tool, "01-opened"));
                Assert.True(opened, $"Could not find/open the '{tool}' tile in the XrmToolBox Tools list. " +
                                    "Is the tool deployed to the Plugins folder (-p:DeployToXTB=true), and is the " +
                                    "desktop UNLOCKED? A locked session makes the whole UI invisible to automation.");

                // The connected suffix appears immediately once the tool binds the active connection.
                caption = _host.WaitForConnectedToolTab(tool, TimeSpan.FromSeconds(45));
            }
            else
            {
                _host.Screenshot(ShotPath(tool, "01-opened"));
            }
            _host.Screenshot(ShotPath(tool, "02-connected"));

            Assert.False(caption == null,
                $"'{tool}' opened but its tab never showed a connection suffix ('{tool} (<conn>)'), so it opened " +
                "disconnected. In ATTACH mode (XTB_ATTACH=1), make sure XrmToolBox is running AND already " +
                $"connected before you start the test, and that the desktop stays UNLOCKED (a lock screen stops " +
                "UI Automation). See README.md Tier-3b.");

            // Sanity-check the tab is bound to the org we intended (name may be truncated in the caption).
            var stem = connection.Length > 6 ? connection.Substring(0, 6) : connection;
            _output.WriteLine($"'{tool}' opened connected. Tab caption: \"{caption}\" (expected connection '{connection}').");
            if (caption.IndexOf(stem, StringComparison.OrdinalIgnoreCase) < 0)
                _output.WriteLine($"NOTE: caption doesn't contain '{stem}' — the active connection may differ from '{connection}'.");
        }

        private static bool IsEnabled() =>
            string.Equals(Environment.GetEnvironmentVariable("XTB_CONNECTED_TEST"), "1", StringComparison.Ordinal);

        /// <summary>Guardrail: refuse to run against anything that looks like a production org.</summary>
        private static void AssertNotProdConnection(string connection)
        {
            var c = (connection ?? "").ToLowerInvariant();
            Assert.False(c.Contains("prod"),
                $"Refusing to run the connected walkthrough against '{connection}' — it looks like production. " +
                "Point XTB_TEST_CONNECTION at a dev/test org (e.g. XTS-CI-TEST).");
        }

        private static string EnvOr(string name, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        private static string ShotDir()
        {
            var dir = Environment.GetEnvironmentVariable("UISMOKE_SCREENSHOT_DIR");
            if (string.IsNullOrWhiteSpace(dir)) dir = Path.Combine(Path.GetTempPath(), "xtb-ui-smoke");
            return Path.Combine(dir, "connected");
        }

        private static string ShotPath(string tool, string step)
        {
            var slug = new string((tool ?? "").ToLowerInvariant().Replace(' ', '-').Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            return Path.Combine(ShotDir(), $"{slug}-{step}.png");
        }

        private static string ResolveXtbExe()
        {
            var fromEnv = Environment.GetEnvironmentVariable("XTB_EXE");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv)) return fromEnv;

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new[]
            {
                Path.Combine(local, "Programs", "XrmToolBox", "XrmToolBox.exe"),
                Path.Combine(local, "XrmToolBox", "XrmToolBox.exe"),
                @"C:\Program Files\XrmToolBox\XrmToolBox.exe",
                @"C:\Tools\XrmToolBox\XrmToolBox.exe",
                @"C:\devtools\XrmToolbox\XrmToolBox.exe",
            };
            var hit = candidates.FirstOrDefault(File.Exists);
            if (hit != null) return hit;

            throw new FileNotFoundException(
                "Could not find XrmToolBox.exe. Set XTB_EXE to its full path. See testing/UiSmokeTests/README.md.");
        }

        public void Dispose() => _host.Dispose();
    }
}
