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
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
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