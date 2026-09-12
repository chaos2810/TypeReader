using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TypeReader.Core;

namespace TypeReader.App;

public partial class WidgetWindow : Window
{
    private readonly KeyboardHook _hook = new();
    private readonly KeyFormatter _formatter = new();
    private TaskbarEmbedder? _embedder;
    private System.Windows.Threading.DispatcherTimer? _reembedTimer;
    private bool _detached;

    public WidgetWindow()
    {
        InitializeComponent();
        _hook.KeyPressed += OnKeyPressed;
        try
        {
            _hook.Install();
        }
        catch (InvalidOperationException)
        {
            KeyText.Text = "⚠"; // capture unavailable; widget continues without capture
        }
        ApplySettings();
    }

    // R1: called by App.OnStartup after EnsureHandle(), before Show()
    public void EmbedBeforeShow()
    {
        _embedder = new TaskbarEmbedder(this);
        ApplyThemeForeground();
        _embedder.Embed();               // WS_CHILD + SetParent + position + acrylic
        StartReembedWatch();
    }

    private void ApplyThemeForeground()
    {
        bool light = TaskbarEmbedder.LightTheme;
        KeyText.Foreground = light ? Brushes.Black : Brushes.White;
        KeyMenu.Foreground = light ? Brushes.Black : Brushes.White;
    }

    private void OnKeyPressed(int vkCode, bool[] keyboardState, bool isKeyDown)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var text = _formatter.Format(vkCode, keyboardState, isKeyDown);
            if (text != null)
                KeyText.Text = text;
        });
    }

    private void StartReembedWatch()
    {
        _reembedTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _reembedTimer.Tick += (s, e) => _embedder?.CheckAndReembed();
        _reembedTimer.Start();
    }

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.SettingsChanged += ApplySettings;
        settingsWindow.Show();
    }

    private void ApplySettings()
    {
        var s = new SettingsStore().Load();
        KeyText.FontFamily = new System.Windows.Media.FontFamily(s.FontFamily);
        KeyText.FontSize = s.FontSize;
    }

    private void OnQuit(object sender, RoutedEventArgs e)
    {
        // R2: detach from taskbar BEFORE closing — no residue
        _reembedTimer?.Stop();
        if (!_detached)
        {
            _embedder?.Detach();
            _detached = true;
        }
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _reembedTimer?.Stop();
        if (!_detached)
        {
            _embedder?.Detach();
            _detached = true;
        }
        _hook.Dispose();
        base.OnClosed(e);
    }
}