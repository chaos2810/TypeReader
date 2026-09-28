using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TypeReader.Core;

namespace TypeReader.App;

// Decodes an image fully into memory (CacheOption.OnLoad — never locks the
// source file) and renders the user's crop into a WPF Image control.
// Animated GIFs advance through a DispatcherTimer using per-frame delays
// from Core's GifTiming.
internal sealed class WidgetImage : IDisposable
{
    private readonly BitmapSource[] _frames;
    private readonly TimeSpan[] _delays;
    private readonly DispatcherTimer? _timer;
    private Image? _target;
    private ImageCrop _crop = ImageCrop.Full;
    private int _index;

    private WidgetImage(BitmapSource[] frames, TimeSpan[] delays)
    {
        _frames = frames;
        _delays = delays;
        if (frames.Length > 1)
        {
            _timer = new DispatcherTimer { Interval = delays[0] };
            _timer.Tick += (s, e) =>
            {
                _index = (_index + 1) % _frames.Length;
                ShowFrame();
            };
        }
    }

    public static WidgetImage Load(string path)
    {
        var frames = DecodeFrames(path, out bool animated);
        var delays = new TimeSpan[frames.Length];
        if (animated)
        {
            var parsed = GifTiming.GetFrameDelays(File.ReadAllBytes(path));
            for (int i = 0; i < delays.Length; i++)
                delays[i] = i < parsed.Length ? parsed[i] : GifTiming.NormalizeDelay(0);
        }
        return new WidgetImage(frames, delays);
    }

    // WPF's GifBitmapDecoder returns each frame as a raw delta sub-image
    // (its own size, an offset within the logical screen, mostly transparent
    // pixels, plus a disposal method). Displaying deltas directly looks like
    // noise, so composite them onto full-canvas frames at load time using
    // the WIC metadata — the recipe browsers use.
    private static BitmapSource[] CompositeGifFrames(BitmapFrame[] raw, int w, int h)
    {
        int bpp = 4;
        var canvas = new byte[w * h * bpp];
        var saved = new byte[canvas.Length];
        bool hasSaved = false;
        byte prevDisposal = 0;
        int prevLeft = 0, prevTop = 0, prevW = 0, prevH = 0;
        var result = new BitmapSource[raw.Length];

        for (int i = 0; i < raw.Length; i++)
        {
            var f = raw[i];
            int left = 0, top = 0;
            byte disposal = 0;
            if (f.Metadata is BitmapMetadata meta)
            {
                var img = meta.GetQuery("/imgdesc") as BitmapMetadata;
                var gce = meta.GetQuery("/grctlext") as BitmapMetadata;
                if (img != null)
                {
                    left = Convert.ToInt32(img.GetQuery("/imgdesc/Left"));
                    top = Convert.ToInt32(img.GetQuery("/imgdesc/Top"));
                }
                if (gce != null) disposal = Convert.ToByte(gce.GetQuery("/grctlext/Disposal"));
            }

            if (i > 0)
            {
                if (prevDisposal == 2) // restore to background: clear prev rect
                {
                    int x0 = Math.Max(0, prevLeft), y0 = Math.Max(0, prevTop);
                    int x1 = Math.Min(w, prevLeft + prevW), y1 = Math.Min(h, prevTop + prevH);
                    for (int y = y0; y < y1; y++)
                        Array.Clear(canvas, y * w * bpp + x0 * bpp, (x1 - x0) * bpp);
                }
                else if (prevDisposal == 3 && hasSaved)
                    Array.Copy(saved, canvas, canvas.Length);
            }
            if (disposal == 3) { Array.Copy(canvas, saved, canvas.Length); hasSaved = true; }

            var conv = new FormatConvertedBitmap(f, PixelFormats.Bgra32, null, 0);
            var px = new byte[f.PixelWidth * f.PixelHeight * bpp];
            conv.CopyPixels(px, f.PixelWidth * bpp, 0);
            for (int y = 0; y < f.PixelHeight; y++)
                for (int x = 0; x < f.PixelWidth; x++)
                {
                    int src = (y * f.PixelWidth + x) * bpp;
                    if (px[src + 3] == 0) continue;
                    int cx = left + x, cy = top + y;
                    if (cx < 0 || cy < 0 || cx >= w || cy >= h) continue;
                    int dst = (cy * w + cx) * bpp;
                    canvas[dst] = px[src];
                    canvas[dst + 1] = px[src + 1];
                    canvas[dst + 2] = px[src + 2];
                    canvas[dst + 3] = 255;
                }

            prevDisposal = disposal;
            prevLeft = left; prevTop = top; prevW = f.PixelWidth; prevH = f.PixelHeight;

            var bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
            bmp.WritePixels(new Int32Rect(0, 0, w, h), canvas, w * bpp, 0);
            result[i] = bmp;
        }
        return result;
    }

    private static BitmapSource[] DecodeFrames(string path, out bool animated)
    {
        animated = false;
        if (string.Equals(Path.GetExtension(path), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            var decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count > 1)
            {
                animated = true;
                int w = decoder.Frames[0].PixelWidth, h = decoder.Frames[0].PixelHeight;
                var header = new byte[10];
                using (var fs = File.OpenRead(path))
                    if (fs.Read(header) == 10)
                    {
                        w = header[6] | (header[7] << 8); // logical screen size
                        h = header[8] | (header[9] << 8); // (frame 0 can be a sub-rect)
                    }
                return CompositeGifFrames(decoder.Frames.ToArray(), w, h);
            }
            return new BitmapSource[] { decoder.Frames[0] };
        }
        return new BitmapSource[] { BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad) };
    }

    public static BitmapFrame LoadFirstFrame(string path)
    {
        if (string.Equals(Path.GetExtension(path), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            var decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return decoder.Frames[0];
        }
        return BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
    }

    public static CroppedBitmap CropFrame(BitmapSource frame, ImageCrop crop)
    {
        var r = CropMath.ToPixels(crop, frame.PixelWidth, frame.PixelHeight);
        return new CroppedBitmap(frame, new Int32Rect(r.X, r.Y, r.Width, r.Height));
    }

    public void Attach(Image target, ImageCrop crop)
    {
        _target = target;
        _crop = crop;
        _index = 0;
        ShowFrame();
        _timer?.Start();
    }

    private void ShowFrame()
    {
        if (_target == null) return;
        _target.Source = CropFrame(_frames[_index], _crop);
        if (_timer != null) _timer.Interval = _delays[_index];
    }

    public void Dispose() => _timer?.Stop();
}