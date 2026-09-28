namespace TypeReader.Core;

public sealed class SettingsStore
{
    private readonly string _path;

    public SettingsStore(string? directory = null)
    {
        var dir = directory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TypeReader");
        _path = Path.Combine(dir, "settings.json");
    }

    public Settings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new Settings();
            var json = File.ReadAllText(_path);
            var s = System.Text.Json.JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            return Validate(s);
        }
        catch
        {
            return new Settings();
        }
    }

    private static Settings Validate(Settings s)
    {
        if (double.IsNaN(s.FontSize) || double.IsInfinity(s.FontSize) || s.FontSize < 8 || s.FontSize > 72)
            s.FontSize = 14;
        if (string.IsNullOrWhiteSpace(s.FontFamily))
            s.FontFamily = "Segoe UI";
        if (s.Position < WidgetPosition.Left || s.Position > WidgetPosition.Right)
            s.Position = WidgetPosition.Center;
        if (s.DisplayMode < WidgetDisplayMode.Text || s.DisplayMode > WidgetDisplayMode.Image)
            s.DisplayMode = WidgetDisplayMode.Text;
        if (s.DisplayMode == WidgetDisplayMode.Image && !File.Exists(s.ImagePath))
        {
            s.DisplayMode = WidgetDisplayMode.Text;
            s.ImagePath = "";
        }
        if (s.ImageCrop is null || !s.ImageCrop.IsValid())
            s.ImageCrop = ImageCrop.Full;
        return s;
    }

    public void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = System.Text.Json.JsonSerializer.Serialize(settings,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}