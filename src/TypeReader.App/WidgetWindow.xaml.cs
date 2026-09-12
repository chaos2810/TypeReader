using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TypeReader.Core;

namespace TypeReader.App;

public partial class WidgetWindow : Window
{
    private readonly KeyboardHook _hook = new();
    private readonly KeyFormatter _formatter = new(new ToUnicodeCharMapper());
    private TaskbarEmbedder? _embedder;
    private System.Windows.Threading.DispatcherTimer? _reembedTimer;
    private bool _detached;
    private readonly TrayIcon _tray = new();

    // raised when the widget HWND is destroyed (Explorer restart destroys
    // children with the taskbar); App recreates the widget
    public event Action? EmbedderLost;

    public WidgetWindow()
    {
        InitializeComponent();
        _tray.SettingsRequested += () => Dispatcher.Invoke(() => OnSettings(null!, null!));
        _tray.QuitRequested += () => Dispatcher.Invoke(() => OnQuit(null!, null!));
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
        _embedder.Position = new SettingsStore().Load().Position;
        ApplyThemeForeground();
        _embedder.Embed();               // WS_CHILD + SetParent + position + acrylic
        StartReembedWatch();
    }

    private void ApplyThemeForeground()
    {
        bool light = TaskbarEmbedder.LightTheme;
        KeyText.Foreground = light ? Brushes.Black : Brushes.White;
        KeyMenu.Foreground = light ? Brushes.Black : Brushes.White;
        // WPF-UI theme resources drive the menu chrome; text color must match
        App.ApplySystemTheme();
    }

    // hover glass effect (FluentFlyout recipe): translucent white overlay
    // animated over 200ms, plus a subtle top highlight border
    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        bool isDark = !TaskbarEmbedder.LightTheme;

        var targetColor = isDark
            ? System.Windows.Media.Color.FromArgb(197, 255, 255, 255)
            : System.Windows.Media.Color.FromArgb(255, 255, 255, 255);
        double targetOpacity = isDark ? 0.075 : 0.6;

        TopBorder.BorderBrush = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(93, 255, 255, 255))
        { Opacity = isDark ? 0.25 : 1 };

        AnimateHover(targetColor, targetOpacity, System.Windows.Media.Animation.EasingMode.EaseOut);
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        TopBorder.BorderBrush = Brushes.Transparent;
        AnimateHover(System.Windows.Media.Colors.Transparent, 0, System.Windows.Media.Animation.EasingMode.EaseInOut);
    }

    private void AnimateHover(System.Windows.Media.Color color, double opacity, System.Windows.Media.Animation.EasingMode mode)
    {
        if (MainBorder.Background is not SolidColorBrush brush)
        {
            brush = new SolidColorBrush(System.Windows.Media.Colors.Transparent) { Opacity = 0 };
            MainBorder.Background = brush;
        }
        // note: only this overlay brush animates. The Grid's alpha-1
        // background (the OS hit layer) is never touched, so hit-testing
        // stays intact after any number of hover cycles.

        brush.BeginAnimation(SolidColorBrush.ColorProperty, new System.Windows.Media.Animation.ColorAnimation
        {
            To = color,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = mode }
        });
        brush.BeginAnimation(SolidColorBrush.OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation
        {
            To = opacity,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = mode }
        });
    }

    private void OnKeyPressed(int vkCode, bool[] keyboardState, bool isKeyDown, byte[] rawKeyboardState)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var text = _formatter.Format(vkCode, keyboardState, isKeyDown, rawKeyboardState);
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
        _reembedTimer.Tick += (s, e) =>
        {
            if (_detached) { _reembedTimer?.Stop(); return; }
            if (_embedder != null && !_embedder.IsWidgetWindowAlive())
            {
                // HWND destroyed with the taskbar — this window is gone; signal App
                _reembedTimer.Stop();
                _tray.Dispose();
                _hook.Dispose();
                EmbedderLost?.Invoke();
                return;
            }
            _embedder?.CheckAndReembed();
        };
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
        if (_embedder != null)
        {
            _embedder.Position = s.Position;
            _embedder.CheckAndReembed();
        }
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
        _tray.Dispose();
        base.OnClosed(e);
    }
}