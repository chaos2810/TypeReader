using System.Drawing;
using System.IO;
using System.Reflection;
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
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var path = Path.Combine(dir, "trayicon.ico");
            if (File.Exists(path))
                return new Icon(path);
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