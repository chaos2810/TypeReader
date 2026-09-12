using System.Threading;
using System.Windows;
using Wpf.Ui.Appearance;

namespace TypeReader.App;

public partial class App : Application
{
    private static Mutex? _mutex;
    private static bool _ownsMutex;
    private static WidgetWindow? _widget;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "TypeReader.SingleInstance", out bool isNew);
        _ownsMutex = isNew;
        if (!isNew)
        {
            Shutdown();
            return;
        }
        // Explorer dying destroys our embedded child HWND; WPF's default
        // OnLastWindowClose would then quit the app. We manage lifetime
        // explicitly and recreate the widget instead.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);

        // match WPF-UI resources to the Windows theme so menus/settings
        // look native alongside the taskbar
        ApplySystemTheme();

        CreateWidget();
    }

    internal static void ApplySystemTheme()
    {
        bool light = TypeReader.App.TaskbarEmbedder.LightTheme;
        ApplicationThemeManager.Apply(light ? ApplicationTheme.Light : ApplicationTheme.Dark);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        // never let a background blip kill the widget; keep it alive
        e.Handled = true;
    }

    private static void CreateWidget()
    {
        _widget = new WidgetWindow();
        var helper = new System.Windows.Interop.WindowInteropHelper(_widget);
        helper.EnsureHandle();          // create HWND while still hidden
        _widget.EmbedBeforeShow();     // WS_CHILD + SetParent + position + acrylic (R1, R3)
        _widget.Show();                 // appears already inside the taskbar
        _widget.EmbedderLost += () =>
        {
            // old HWND destroyed with the taskbar (Explorer restart) — make a new one
            _widget = null;
            CreateWidget();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
            _mutex?.ReleaseMutex();
        base.OnExit(e);
    }
}