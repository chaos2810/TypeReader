namespace TypeReader.Core;

public enum WidgetPosition { Left, Center, Right }

public enum WidgetDisplayMode { Text, Image }

public sealed class ImageCrop
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public static ImageCrop Full => new() { X = 0, Y = 0, Width = 1, Height = 1 };

    // NaN/Infinity fail the range comparisons, so no explicit checks needed
    public bool IsValid() =>
        X >= 0 && X <= 1 && Y >= 0 && Y <= 1 &&
        Width > 0 && Height > 0 &&
        X + Width <= 1.0000001 && Y + Height <= 1.0000001;
}

public sealed class Settings
{
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 14;
    public WidgetPosition Position { get; set; } = WidgetPosition.Center;
    public bool HideTrayIcon { get; set; } = false;
    public WidgetDisplayMode DisplayMode { get; set; } = WidgetDisplayMode.Text;
    public string ImagePath { get; set; } = "";
    public ImageCrop ImageCrop { get; set; } = ImageCrop.Full;

    // how large the cropped image renders inside the widget rectangle
    // (1.0 = fill, smaller = centered with background around it)
    public double ImageScale { get; set; } = 1.0;
}
