using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace XrmToolSuite.UiSmokeTests.Pages
{
    /// <summary>
    /// Captures a single top-level window's pixels via the Win32 <c>PrintWindow</c> API. Unlike a screen-region
    /// grab (BitBlt), this renders the target window's OWN content even when it is not foreground or is occluded
    /// by another app (e.g. the IDE running the test) — so every screenshot is the XrmToolBox window ONLY, never
    /// whatever happens to be on top. Returns false (so the caller can fall back) if the render came back blank.
    /// </summary>
    internal static class WindowCapture
    {
        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const uint WM_CLOSE = 0x0010;

        /// <summary>Ask a window to close (WM_CLOSE) — e.g. an opened report, to clear the foreground entirely.</summary>
        public static void Close(IntPtr hwnd) { try { if (hwnd != IntPtr.Zero) PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); } catch { } }

        /// <summary>Handle of whatever window is currently in the foreground (e.g. an opened report app).</summary>
        public static IntPtr ForegroundWindow() { try { return GetForegroundWindow(); } catch { return IntPtr.Zero; } }

        /// <summary>Maximize the given window (e.g. the opened report) so the screenshot shows it full-size.</summary>
        public static void Maximize(IntPtr hwnd) { try { if (hwnd != IntPtr.Zero) ShowWindow(hwnd, SW_MAXIMIZE); } catch { } }

        /// <summary>Minimize the given window (e.g. an opened report) to clear the foreground for the next step.</summary>
        public static void Minimize(IntPtr hwnd) { try { if (hwnd != IntPtr.Zero) ShowWindow(hwnd, SW_MINIMIZE); } catch { } }

        /// <summary>Owning process id of a window (0 on failure).</summary>
        public static int ProcessIdOf(IntPtr hwnd) { try { GetWindowThreadProcessId(hwnd, out uint pid); return (int)pid; } catch { return 0; } }
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private const uint PW_RENDERFULLCONTENT = 2; // include DWM/compositor content, not just GDI

        /// <summary>
        /// Find a visible top-level window owned by process <paramref name="pid"/> that is NOT the main window —
        /// i.e. the active dialog (connection selector, Connections Manager, Save As, message box). Uses Win32
        /// EnumWindows so it works even when FlaUI's GetAllTopLevelWindows throws COMException on this host.
        /// Returns the largest such window (dialogs are sizeable; tooltips/popups are tiny). Zero if none.
        /// </summary>
        public static IntPtr FindProcessDialog(int pid, IntPtr excludeHwnd)
            => FindProcessWindow(pid, excludeHwnd, 200, 120);

        /// <summary>
        /// Largest visible top-level window of process <paramref name="pid"/> (excluding <paramref name="excludeHwnd"/>)
        /// at least <paramref name="minW"/>×<paramref name="minH"/> in size. Use a small min for menu popups,
        /// a larger min for real dialogs. Zero if none.
        /// </summary>
        public static IntPtr FindProcessWindow(int pid, IntPtr excludeHwnd, int minW, int minH)
        {
            IntPtr best = IntPtr.Zero;
            long bestArea = 0;
            try
            {
                EnumWindows((h, l) =>
                {
                    try
                    {
                        if (h == excludeHwnd || !IsWindowVisible(h)) return true;
                        GetWindowThreadProcessId(h, out uint wpid);
                        if (wpid != (uint)pid) return true;
                        if (!GetWindowRect(h, out var r)) return true;
                        long w = r.Right - r.Left, ht = r.Bottom - r.Top;
                        if (w < minW || ht < minH) return true;
                        long area = w * ht;
                        if (area > bestArea) { bestArea = area; best = h; }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return best;
        }

        /// <summary>Save a PNG of just this window. Returns true on a good render, false if blank/failed.</summary>
        public static bool CaptureWindow(IntPtr hwnd, string path)
        {
            try
            {
                if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return false;
                if (!GetWindowRect(hwnd, out var r)) return false;
                int w = r.Right - r.Left, h = r.Bottom - r.Top;
                if (w <= 0 || h <= 0) return false;

                using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        var hdc = g.GetHdc();
                        bool ok;
                        try { ok = PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT); }
                        finally { g.ReleaseHdc(hdc); }
                        if (!ok) return false;
                    }
                    if (IsMostlyBlank(bmp)) return false; // PrintWindow can yield black for some surfaces — fall back
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    bmp.Save(path, ImageFormat.Png);
                    return true;
                }
            }
            catch { return false; }
        }

        /// <summary>Sample a grid of pixels; true if almost all are pure black (a failed PrintWindow render).</summary>
        private static bool IsMostlyBlank(Bitmap bmp)
        {
            int sample = 0, black = 0;
            int sx = Math.Max(1, bmp.Width / 40), sy = Math.Max(1, bmp.Height / 40);
            for (int y = 0; y < bmp.Height; y += sy)
                for (int x = 0; x < bmp.Width; x += sx)
                {
                    var c = bmp.GetPixel(x, y);
                    sample++;
                    if (c.R == 0 && c.G == 0 && c.B == 0) black++;
                }
            return sample > 0 && black * 100 / sample > 92;
        }
    }
}
