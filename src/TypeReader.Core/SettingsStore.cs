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
            var s = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
            return s ?? new Settings();
        }
        catch
        {
            return new Settings();
        }
    }

    public void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = System.Text.Json.JsonSerializer.Serialize(settings,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}