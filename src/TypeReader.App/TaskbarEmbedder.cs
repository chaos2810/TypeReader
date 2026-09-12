using System.Windows;
using System.Windows.Interop;
using TypeReader.Core;

namespace TypeReader.App;

internal sealed class TaskbarEmbedder
{
    private const int WidgetWidth = 120;
    private const int WidgetHeight = 48;

    private readonly Window _window;
    private IntPtr _widgetHwnd;
    private IntPtr _lastTaskbarHandle;

    public TaskbarEmbedder(Window window)
    {
        _window = window;
        _widgetHwnd = new WindowInteropHelper(window).Handle;
    }

    public void Embed()
    {
        IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero) return;

        long style = NativeMethods.GetWindowLongPtr(_widgetHwnd, NativeMethods.GWL_STYLE);
        style = (style & ~NativeMethods.WS_POPUP) | NativeMethods.WS_CHILD;
        NativeMethods.SetWindowLongPtr(_widgetHwnd, NativeMethods.GWL_STYLE, style);
        NativeMethods.SetParent(_widgetHwnd, taskbar);
        EnableAcrylic();               // FF WindowBlurHelper recipe — frosted at rest
        _lastTaskbarHandle = taskbar;
        Reposition(taskbar);
    }

    // R2: detach before close — restore popup style, unparent, no residue
    public void Detach()
    {
        // hide first so unparenting a visible window doesn't flash top-level
        NativeMethods.ShowWindow(_widgetHwnd, NativeMethods.SW_HIDE);
        NativeMethods.SetParent(_widgetHwnd, IntPtr.Zero);
        long style = NativeMethods.GetWindowLongPtr(_widgetHwnd, NativeMethods.GWL_STYLE);
        style = (style & ~NativeMethods.WS_CHILD) | NativeMethods.WS_POPUP;
        NativeMethods.SetWindowLongPtr(_widgetHwnd, NativeMethods.GWL_STYLE, style);
        _lastTaskbarHandle = IntPtr.Zero;
    }

    // R3: frosted box — FluentFlyout's WindowBlurHelper.EnableBlur recipe
    // verbatim (opacity 175, theme-aware background). Applied at rest so the
    // widget reads as native frosted glass, matching FF's taskbar widget.

    public void EnableAcrylic()
    {
        uint background = LightTheme ? 0xF3F3F3u : 0x202020u;
        SetAccent(new NativeMethods.AccentPolicy
        {
            AccentState = NativeMethods.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            GradientColor = (175u << 24) | (background & 0xFFFFFF)
        });
    }

    public void DisableAcrylic()
    {
        SetAccent(new NativeMethods.AccentPolicy
        {
            AccentState = NativeMethods.ACCENT_DISABLED
        });
    }

    private void SetAccent(NativeMethods.AccentPolicy accent)
    {
        int size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.AccentPolicy>();
        IntPtr accentPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        try
        {
            System.Runtime.InteropServices.Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new NativeMethods.WindowCompositionAttributeData
            {
                Attribute = NativeMethods.WCA_ACCENT_POLICY,
                Data = accentPtr,
                SizeOfData = size
            };
            NativeMethods.SetWindowCompositionAttribute(_widgetHwnd, ref data);
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(accentPtr);
        }
    }

    private static bool IsLightTheme()
    {
        try
        {
            var key = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", null) as int?;
            return key == 1;
        }
        catch
        {
            return false;
        }
    }

    public static bool LightTheme => IsLightTheme();

    public void CheckAndReembed()
    {
        IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero || taskbar != _lastTaskbarHandle)
            Embed();
        else
            Reposition(taskbar);
    }

    public WidgetPosition Position { get; set; } = WidgetPosition.Center;

    // true while the widget HWND still exists — false after Explorer restart
    // destroys the child windows along with the taskbar
    public bool IsWidgetWindowAlive()
    {
        return NativeMethods.IsWindow(_widgetHwnd);
    }

    private void Reposition(IntPtr taskbar)
    {
        NativeMethods.GetWindowRect(taskbar, out var tb);
        double dpi = NativeMethods.GetDpiForWindow(taskbar) / 96.0;
        double h = (tb.Bottom - tb.Top) / dpi;
        double y = (h - WidgetHeight) / 2;
        double taskbarW = (tb.Right - tb.Left) / dpi;

        // occupied intervals of the taskbar (other widgets, tray, discrete
        // icon containers) — our widget must not overlap any of them
        var occupied = OccupiedIntervals(taskbar, tb, dpi);

        double x = Position switch
        {
            WidgetPosition.Left => FindFreeX(occupied, 0, +1, taskbarW - WidgetWidth),
            // anchor near the system tray's left edge, sliding left past
            // anything already occupying that space
            WidgetPosition.Right => FindFreeX(occupied, RightAnchorX(taskbar, tb, dpi) - WidgetWidth - 4, -1, taskbarW - WidgetWidth),
            _ => FindFreeX(occupied, taskbarW / 2.0 - WidgetWidth / 2.0, +1, taskbarW - WidgetWidth),
        };
        NativeMethods.SetWindowPos(_widgetHwnd, IntPtr.Zero,
            (int)(x * dpi), (int)(y * dpi),
            (int)(WidgetWidth * dpi), (int)(WidgetHeight * dpi),
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

        // clip the HWND to the same rounded footprint as the XAML border
        // (2px inset, 6px corner radius) so the acrylic blur, the visible
        // box, and the mouse hitbox all match. On success the system OWNS
        // the region — deleting it corrupts the window's hit area; only
        // delete on failure.
        int left = (int)(2 * dpi), top = (int)(2 * dpi);
        int right = (int)((WidgetWidth - 2) * dpi), bottom = (int)((WidgetHeight - 2) * dpi);
        IntPtr rgn = NativeMethods.CreateRoundRectRgn(left, top, right, bottom,
            (int)(6 * dpi), (int)(6 * dpi));
        if (NativeMethods.SetWindowRgn(_widgetHwnd, rgn, true) == 0)
            NativeMethods.DeleteObject(rgn);
    }

    // scan from `start` in `direction` (±4px steps) until the widget fits
    // without overlapping any occupied interval, bounded to the taskbar
    private static double FindFreeX(List<(double Start, double End)> occupied, double start, int direction, double maxX)
    {
        bool Fits(double x) => occupied.All(o => x + WidgetWidth <= o.Start || x >= o.End);

        double x = Math.Clamp(start, 0, Math.Max(0, maxX));
        if (Fits(x)) return x;
        // walk right/left from the clamped start looking for the nearest gap
        double best = x;
        double bestDist = double.MaxValue;
        for (double probe = 0; probe <= maxX; probe += 4)
        {
            if (!Fits(probe)) continue;
            double dist = Math.Abs(probe - start);
            if (dist < bestDist) { bestDist = dist; best = probe; }
        }
        return best;
    }

    private List<(double Start, double End)> OccupiedIntervals(IntPtr taskbar, NativeMethods.RECT tb, double dpi)
    {
        double taskbarW = (tb.Right - tb.Left) / dpi;
        var list = new List<(double, double)>();

        // pass 1: child HWNDs (TrayNotifyWnd, other widgets' windows)
        NativeMethods.EnumChildWindows(taskbar, (h, l) =>
        {
            if (h == _widgetHwnd) return true; // skip ourselves
            if (!NativeMethods.IsWindowVisible(h)) return true;

            if (!NativeMethods.GetWindowRect(h, out var r)) return true;
            double start = (r.Left - tb.Left) / dpi;
            double end = (r.Right - tb.Left) / dpi;
            double w = end - start;

            if (w > taskbarW * 0.8)
            {
                // full-width child: it may be an embedded widget (FluentFlyout)
                // that clips itself with a window region — the region box is the
                // real footprint. Containers with no region stay non-obstacles.
                var rgn = NativeMethods.CreateRectRgn(0, 0, 0, 0);
                if (NativeMethods.GetWindowRgn(h, rgn) > 0) // success (SIMPLE=2/COMPLEX=3/NULL=1)
                {
                    if (NativeMethods.GetRgnBox(rgn, out var rb) != 0 && rb.Right > rb.Left)
                    {
                        double rs = (r.Left + rb.Left) / dpi - tb.Left / dpi;
                        double re = (r.Left + rb.Right) / dpi - tb.Left / dpi;
                        double rw = re - rs;
                        if (rw > 1 && rw <= taskbarW * 0.8)
                            list.Add((rs, re));
                    }
                }
                NativeMethods.DeleteObject(rgn);
                return true;
            }
            if (w > 1 && end > start)
                list.Add((start, end));
            return true;
        }, IntPtr.Zero);

        // pass 2: UI Automation — sees Win11 XAML taskbar buttons (pinned
        // icons, search, widgets, clock) that have no HWND. Cached + time-boxed
        // (FluentFlyout uses the same technique).
        foreach (var iv in UiaOccupiedIntervals(taskbar, tb, dpi))
        {
            double w = iv.End - iv.Start;
            if (w <= taskbarW * 0.8 && w > 1)
                list.Add(iv);
        }

        // merge overlapping intervals
        var merged = new List<(double, double)>();
        foreach (var iv in list.OrderBy(v => v.Item1))
        {
            if (merged.Count > 0 && iv.Item1 <= merged[^1].Item2)
                merged[^1] = (merged[^1].Item1, Math.Max(merged[^1].Item2, iv.Item2));
            else
                merged.Add(iv);
        }
        return merged;
    }

    private List<(double Start, double End)>? _uiaRaw;
    private IntPtr _uiaTaskbar;
    private DateTime _uiaTime = DateTime.MinValue;

    private List<(double Start, double End)> UiaOccupiedIntervals(IntPtr taskbar, NativeMethods.RECT tb, double dpi)
    {
        const double cacheSeconds = 5.0;
        if (_uiaRaw != null && _uiaTaskbar == taskbar
            && (DateTime.UtcNow - _uiaTime).TotalSeconds < cacheSeconds)
            return _uiaRaw.Select(v => ((v.Start - tb.Left) / dpi, (v.End - tb.Left) / dpi)).ToList();

        var raw = new List<(double Start, double End)>(); // physical screen px
        var task = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var root = System.Windows.Automation.AutomationElement.FromHandle(taskbar);
                var elements = root.FindAll(System.Windows.Automation.TreeScope.Descendants,
                    System.Windows.Automation.Condition.TrueCondition);
                int ourPid = Environment.ProcessId;
                foreach (System.Windows.Automation.AutomationElement el in elements)
                {
                    try
                    {
                        if (el.Current.ProcessId == ourPid) continue;
                        var r = el.Current.BoundingRectangle;
                        if (r.IsEmpty || r.Width < 1) continue;
                        raw.Add((r.Left, r.Right));
                    }
                    catch (System.Windows.Automation.ElementNotAvailableException)
                    {
                        // element vanished mid-enumeration
                    }
                }
            }
            catch
            {
                // UIA unavailable/slow — HWND pass still covers the basics
            }
        });
        if (!task.Wait(1000))
        {
            // time-boxed: keep whatever was collected before the deadline
        }
        _uiaRaw = raw;
        _uiaTaskbar = taskbar;
        _uiaTime = DateTime.UtcNow;
        return raw.Select(v => ((v.Start - tb.Left) / dpi, (v.End - tb.Left) / dpi)).ToList();
    }

    private static double RightAnchorX(IntPtr taskbar, NativeMethods.RECT taskbarRect, double dpi)
    {
        IntPtr tray = NativeMethods.FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (tray != IntPtr.Zero && NativeMethods.GetWindowRect(tray, out var trayRect))
            return (trayRect.Left - taskbarRect.Left) / dpi;
        // fallback: tray not found — estimate from the right edge
        return (taskbarRect.Right - taskbarRect.Left) / dpi - 20;
    }
}