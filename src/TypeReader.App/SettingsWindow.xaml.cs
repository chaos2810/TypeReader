using System.IO;
using System.Windows;
using System.Windows.Media;
using TypeReader.Core;
using Wpf.Ui.Controls;

namespace TypeReader.App;

public partial class SettingsWindow : FluentWindow
{
    private readonly Settings _settings = new SettingsStore().Load();

    public event Action? SettingsChanged;

    public SettingsWindow()
    {
        InitializeComponent();
        foreach (var size in new[] { "10", "12", "14", "16", "18", "20", "24" })
            SizeBox.Items.Add(size);
        SizeBox.SelectedItem = ((int)_settings.FontSize).ToString();
        switch (_settings.Position)
        {
            case WidgetPosition.Left: PosLeft.IsChecked = true; break;
            case WidgetPosition.Right: PosRight.IsChecked = true; break;
            default: PosCenter.IsChecked = true; break;
        }
        ModeText.IsChecked = _settings.DisplayMode == WidgetDisplayMode.Text;
        ModeImage.IsChecked = _settings.DisplayMode == WidgetDisplayMode.Image;
        UpdatePreview();
        ApplyPreview();
    }

    private void ApplyPreview()
    {
        PreviewText.FontFamily = new FontFamily(_settings.FontFamily);
        PreviewText.FontSize = _settings.FontSize;
    }

    private void OnSizeChanged(object sender, RoutedEventArgs e)
    {
        if (SizeBox.SelectedItem is string s && double.TryParse(s, out var size))
        {
            _settings.FontSize = size;
            ApplyPreview(); Save();
        }
    }

    private void OnPositionChanged(object sender, RoutedEventArgs e)
    {
        _settings.Position = PosLeft.IsChecked == true ? WidgetPosition.Left
            : PosRight.IsChecked == true ? WidgetPosition.Right
            : WidgetPosition.Center;
        Save();
    }

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        var mode = ModeImage.IsChecked == true ? WidgetDisplayMode.Image : WidgetDisplayMode.Text;
        if (mode == _settings.DisplayMode) return; // ctor initialization, no-op
        if (mode == WidgetDisplayMode.Image && !File.Exists(_settings.ImagePath))
        {
            if (!ImagePickFlow.TryPickAndCrop(_settings.ImageScale, out string path, out ImageCrop crop, out double scale))
            {
                ModeText.IsChecked = true; // cancelled: revert, handler saves nothing
                return;
            }
            _settings.ImagePath = path;
            _settings.ImageCrop = crop;
            _settings.ImageScale = scale;
        }
        _settings.DisplayMode = mode;
        UpdatePreview();
        Save();
    }

    private void OnChooseImage(object sender, RoutedEventArgs e)
    {
        if (!ImagePickFlow.TryPickAndCrop(_settings.ImageScale, out string path, out ImageCrop crop, out double scale)) return;
        _settings.ImagePath = path;
        _settings.ImageCrop = crop;
        _settings.ImageScale = scale;
        _settings.DisplayMode = WidgetDisplayMode.Image;
        ModeImage.IsChecked = true; // re-fires OnModeChanged, which no-ops (mode already set)
        UpdatePreview();
        Save();
    }

    private void UpdatePreview()
    {
        // fixed taskbar-shaped frame; image shrinks inside it, like the widget
        double sc = Math.Clamp(_settings.ImageScale, 0.25, 1.0);
        PreviewImage.MaxWidth = 180 * sc;
        PreviewImage.MaxHeight = 48 * sc;
        if (_settings.DisplayMode != WidgetDisplayMode.Image ||
            string.IsNullOrEmpty(_settings.ImagePath) || !File.Exists(_settings.ImagePath))
        {
            PreviewImage.Source = null;
            return;
        }
        try
        {
            var frame = WidgetImage.LoadFirstFrame(_settings.ImagePath);
            PreviewImage.Source = WidgetImage.CropFrame(frame, _settings.ImageCrop);
        }
        catch (Exception)
        {
            PreviewImage.Source = null;
        }
    }

    private void Save()
    {
        try
        {
            new SettingsStore().Save(_settings);
            SettingsChanged?.Invoke();
        }
        catch (Exception)
        {
            // settings write failed (disk/lock/ACL); keep running with in-memory settings
        }
    }
}