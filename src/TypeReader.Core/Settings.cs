namespace TypeReader.Core;

public enum WidgetPosition { Left, Center, Right }

public sealed class Settings
{
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 14;
    public WidgetPosition Position { get; set; } = WidgetPosition.Center;
    public bool HideTrayIcon { get; set; } = false;
}