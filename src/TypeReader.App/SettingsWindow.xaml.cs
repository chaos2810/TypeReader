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