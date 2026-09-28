using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TypeReader.Core;

namespace TypeReader.App;

internal static class ImagePickFlow
{
    private const string Filter =
        "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico;*.tiff;*.tif;*.wdp";

    // No owner window: the widget is embedded in the taskbar, so dialogs
    // center on screen instead. initialScale seeds the size slider so a
    // re-pick keeps the user's current image size.
    public static bool TryPickAndCrop(double initialScale, out string path, out ImageCrop crop, out double scale)
    {
        path = "";
        crop = ImageCrop.Full;
        scale = Math.Clamp(initialScale, 0.25, 1.0);
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = Filter };
        if (dlg.ShowDialog() != true) return false;

        BitmapFrame frame;
        try
        {
            frame = WidgetImage.LoadFirstFrame(dlg.FileName);
        }
        catch (Exception)
        {
            MessageBox.Show(
                $"TypeReader can't display '{Path.GetFileName(dlg.FileName)}'. The image format is not supported or the file is corrupt.",
                "Type Reader", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (ImageCropWindow.Show(frame, initialScale, out crop, out scale))
        {
            path = dlg.FileName;
            return true;
        }
        return false;
    }
}
