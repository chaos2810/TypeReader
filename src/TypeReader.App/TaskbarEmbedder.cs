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
        ApplyAcrylic();
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

    // R3: FluentFlyout WindowBlurHelper recipe
    private void ApplyAcrylic()
    {
        bool lightTheme = IsLightTheme();
        uint background = lightTheme ? 0xF3F3F3u : 0x202020u;
        var accent = new NativeMethods.AccentPolicy
        {
            AccentState = NativeMethods.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            GradientColor = (175u << 24) | (background & 0xFFFFFF)
        };
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
        double taskbarW = (tb.Right - tb.Left) / dpi;
        double h = (tb.Bottom - tb.Top) / dpi;
        double y = (h - WidgetHeight) / 2;
        double x = Position switch
        {
            WidgetPosition.Left => 20,
            // FluentFlyout approach: anchor to the system tray's left edge so the
            // widget occupies its own space instead of overlapping clock/date/icons
            WidgetPosition.Right => RightAnchorX(taskbar, tb, dpi) - WidgetWidth - 4,
            _ => (taskbarW - WidgetWidth) / 2,
        };
        NativeMethods.SetWindowPos(_widgetHwnd, IntPtr.Zero,
            (int)(x * dpi), (int)(y * dpi),
            (int)(WidgetWidth * dpi), (int)(WidgetHeight * dpi),
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
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