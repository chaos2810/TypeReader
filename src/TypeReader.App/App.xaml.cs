using System.Threading;
using System.Windows;

namespace TypeReader.App;

public partial class App : Application
{
    private static Mutex? _mutex;
    private static bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TypeReader.SingleInstance", out bool isNew);
        _ownsMutex = isNew;
        if (!isNew)
        {
            Shutdown();
            return;
        }
        base.OnStartup(e);

        var widget = new WidgetWindow();
        var helper = new System.Windows.Interop.WindowInteropHelper(widget);
        helper.EnsureHandle();          // create HWND while still hidden
        widget.EmbedBeforeShow();       // WS_CHILD + SetParent + position + acrylic (R1, R3)
        widget.Show();                  // appears already inside the taskbar
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
            _mutex?.ReleaseMutex();
        base.OnExit(e);
    }
}