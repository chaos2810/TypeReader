using TypeReader.Core;

namespace TypeReader.Core.Tests;

public class SettingsStoreTests
{
    // non-static: xUnit instantiates the class per test, isolating each test's temp dir
    private readonly string TestDir =
        Path.Combine(Path.GetTempPath(), "TypeReaderTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var store = new SettingsStore(TestDir);
        var settings = new Settings { FontFamily = "Consolas", FontSize = 20, Position = WidgetPosition.Right };
        store.Save(settings);

        var loaded = store.Load();
        Assert.Equal("Consolas", loaded.FontFamily);
        Assert.Equal(20, loaded.FontSize);
        Assert.Equal(WidgetPosition.Right, loaded.Position);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var store = new SettingsStore(TestDir);
        var loaded = store.Load();
        Assert.Equal("Segoe UI", loaded.FontFamily);
        Assert.Equal(14, loaded.FontSize);
        Assert.Equal(WidgetPosition.Center, loaded.Position);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaults()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"), "{ not json ]]");
        var store = new SettingsStore(TestDir);
        var loaded = store.Load();
        Assert.Equal("Segoe UI", loaded.FontFamily);
    }

    [Fact]
    public void Load_InvalidFontSize_FallsBackToDefault()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": -5, "FontFamily": "Consolas", "Position": 0}""");
        var store = new SettingsStore(TestDir);
        var loaded = store.Load();
        Assert.Equal(14, loaded.FontSize);
        Assert.Equal("Consolas", loaded.FontFamily);
    }

    [Fact]
    public void Load_EmptyFontFamily_FallsBackToDefault()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": 20, "FontFamily": "", "Position": 0}""");
        var store = new SettingsStore(TestDir);
        var loaded = store.Load();
        Assert.Equal("Segoe UI", loaded.FontFamily);
        Assert.Equal(20, loaded.FontSize);
    }

    [Fact]
    public void Load_OutOfRangePosition_FallsBackToDefault()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": 20, "FontFamily": "Consolas", "Position": 7}""");
        var store = new SettingsStore(TestDir);
        var loaded = store.Load();
        Assert.Equal(WidgetPosition.Center, loaded.Position);
    }
}