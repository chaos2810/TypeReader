using System.IO;
using System.Windows;
using System.Windows.Controls;
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
    private readonly BitmapFrame[] _frames;
    private readonly TimeSpan[] _delays;
    private readonly DispatcherTimer? _timer;
    private Image? _target;
    private ImageCrop _crop = ImageCrop.Full;
    private int _index;

    private WidgetImage(BitmapFrame[] frames, TimeSpan[] delays)
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

    private static BitmapFrame[] DecodeFrames(string path, out bool animated)
    {
        animated = false;
        if (string.Equals(Path.GetExtension(path), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            var decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count > 1)
            {
                animated = true;
                return decoder.Frames.ToArray();
            }
            return new[] { decoder.Frames[0] };
        }
        return new[] { BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad) };
    }

    public static BitmapFrame LoadFirstFrame(string path) => DecodeFrames(path, out _)[0];

    public static CroppedBitmap CropFrame(BitmapFrame frame, ImageCrop crop)
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
