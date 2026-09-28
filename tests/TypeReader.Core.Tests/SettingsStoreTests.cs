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
        var settings = new Settings { FontFamily = "Consolas", FontSize = 20, Position = WidgetPosition.Right, HideTrayIcon = true };
        store.Save(settings);

        var loaded = store.Load();
        Assert.Equal("Consolas", loaded.FontFamily);
        Assert.Equal(20, loaded.FontSize);
        Assert.Equal(WidgetPosition.Right, loaded.Position);
        Assert.True(loaded.HideTrayIcon);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var store = new SettingsStore(TestDir);
        var loaded = store.Load();
        Assert.Equal("Segoe UI", loaded.FontFamily);
        Assert.Equal(14, loaded.FontSize);
        Assert.Equal(WidgetPosition.Center, loaded.Position);
        Assert.False(loaded.HideTrayIcon);
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

    [Fact]
    public void SaveThenLoad_ImageMode_RoundTrips()
    {
        Directory.CreateDirectory(TestDir);
        var imgPath = Path.Combine(TestDir, "img.png");
        File.WriteAllText(imgPath, "x");
        var store = new SettingsStore(TestDir);
        var settings = new Settings
        {
            DisplayMode = WidgetDisplayMode.Image,
            ImagePath = imgPath,
            ImageCrop = new ImageCrop { X = 0.1, Y = 0.2, Width = 0.3, Height = 0.25 },
        };
        store.Save(settings);

        var loaded = store.Load();
        Assert.Equal(WidgetDisplayMode.Image, loaded.DisplayMode);
        Assert.Equal(imgPath, loaded.ImagePath);
        Assert.Equal(0.1, loaded.ImageCrop.X);
        Assert.Equal(0.2, loaded.ImageCrop.Y);
        Assert.Equal(0.3, loaded.ImageCrop.Width);
        Assert.Equal(0.25, loaded.ImageCrop.Height);
    }

    [Fact]
    public void Load_MissingImageFile_FallsBackToText()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": 14, "DisplayMode": 1, "ImagePath": "C:\\definitely-missing\\nope.png"}""");
        var store = new SettingsStore(TestDir);
        var loaded = store.Load();
        Assert.Equal(WidgetDisplayMode.Text, loaded.DisplayMode);
        Assert.Equal("", loaded.ImagePath);
    }

    [Fact]
    public void Load_InvalidDisplayMode_FallsBackToText()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": 14, "DisplayMode": 9}""");
        var store = new SettingsStore(TestDir);
        Assert.Equal(WidgetDisplayMode.Text, store.Load().DisplayMode);
    }

    [Fact]
    public void Load_ImageModeWithoutPath_FallsBackToText()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": 14, "DisplayMode": 1}""");
        Assert.Equal(WidgetDisplayMode.Text, new SettingsStore(TestDir).Load().DisplayMode);
    }

    [Fact]
    public void Load_InvalidCrop_ResetsToFull()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": 14, "ImageCrop": {"X": 0.9, "Y": 0, "Width": 0.5, "Height": 0.5}}""");
        var c = new SettingsStore(TestDir).Load().ImageCrop;
        Assert.Equal(0, c.X);
        Assert.Equal(0, c.Y);
        Assert.Equal(1, c.Width);
        Assert.Equal(1, c.Height);
    }

    [Fact]
    public void Load_NullCrop_ResetsToFull()
    {
        Directory.CreateDirectory(TestDir);
        File.WriteAllText(Path.Combine(TestDir, "settings.json"),
            """{"FontSize": 14, "ImageCrop": null}""");
        var c = new SettingsStore(TestDir).Load().ImageCrop;
        Assert.Equal(1, c.Width);
        Assert.Equal(1, c.Height);
    }
}