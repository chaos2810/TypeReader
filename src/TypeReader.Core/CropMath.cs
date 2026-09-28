namespace TypeReader.Core;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

public static class CropMath
{
    public static PixelRect ToPixels(ImageCrop crop, int imageWidth, int imageHeight)
    {
        int x = (int)Math.Round(Math.Clamp(crop.X, 0, 1) * imageWidth);
        int y = (int)Math.Round(Math.Clamp(crop.Y, 0, 1) * imageHeight);
        int w = Math.Clamp((int)Math.Round(crop.Width * imageWidth), 1, imageWidth - x);
        int h = Math.Clamp((int)Math.Round(crop.Height * imageHeight), 1, imageHeight - y);
        return new PixelRect(x, y, w, h);
    }

    public static ImageCrop ToNormalized(PixelRect rect, int imageWidth, int imageHeight)
    {
        return new ImageCrop
        {
            X = rect.X / (double)imageWidth,
            Y = rect.Y / (double)imageHeight,
            Width = rect.Width / (double)imageWidth,
            Height = rect.Height / (double)imageHeight,
        };
    }

    public static ImageCrop CenterCropToRatio(int imageWidth, int imageHeight, double ratio)
    {
        double w = Math.Min(imageWidth, imageHeight * ratio);
        double h = w / ratio;
        return new ImageCrop
        {
            X = (imageWidth - w) / 2 / imageWidth,
            Y = (imageHeight - h) / 2 / imageHeight,
            Width = w / imageWidth,
            Height = h / imageHeight,
        };
    }

    // Ratio is enforced with width driving height; the result always fits
    // inside the image. minWidth is ignored when the image itself is smaller.
    public static PixelRect ClampCropRect(double x, double y, double w, double h,
        int imageWidth, int imageHeight, double ratio, double minWidth)
    {
        double maxW = Math.Min(imageWidth, imageHeight * ratio);
        w = Math.Clamp(w, Math.Min(minWidth, maxW), maxW);
        h = w / ratio;
        x = Math.Clamp(x, 0, imageWidth - w);
        y = Math.Clamp(y, 0, imageHeight - h);
        return new PixelRect(
            (int)Math.Round(x), (int)Math.Round(y),
            Math.Max(1, (int)Math.Round(w)), Math.Max(1, (int)Math.Round(h)));
    }
}
