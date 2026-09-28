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
    // center on screen instead.
    public static bool TryPickAndCrop(out string path, out ImageCrop crop)
    {
        path = "";
        crop = ImageCrop.Full;
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

        if (ImageCropWindow.Show(frame, out crop))
        {
            path = dlg.FileName;
            return true;
        }
        return false;
    }
}
