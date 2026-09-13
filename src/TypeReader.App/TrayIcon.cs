using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace TypeReader.App;

internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;

    public event Action? SettingsRequested;
    public event Action? QuitRequested;

    public TrayIcon()
    {
        _icon = new NotifyIcon
        {
            Text = "Type Reader",
            Icon = LoadAppIcon(),
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            // icon.ico is embedded as a WPF Resource (pack URI)
            var uri = new Uri("pack://application:,,,/icon.ico", UriKind.Absolute);
            var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream != null)
                return new Icon(stream);
        }
        catch
        {
            // fall through to the default icon
        }
        return SystemIcons.Application;
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (s, e) => SettingsRequested?.Invoke());
        menu.Items.Add("Quit", null, (s, e) => QuitRequested?.Invoke());
        return menu;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}