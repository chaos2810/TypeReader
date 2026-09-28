using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TypeReader.Core;
using WShapes = System.Windows.Shapes;

namespace TypeReader.App;

// Modal crop editor. The crop rectangle lives in image pixel coordinates
// (locked to the widget ratio); zoom and pan only change the viewport, so
// navigation never alters the saved crop.
public partial class ImageCropWindow : Wpf.Ui.Controls.FluentWindow
{
    private const double MinCropWidth = 24;
    private const double MaxZoom = 8.0;

    private readonly BitmapFrame _frame;
    private readonly Image _imageControl = new() { IsHitTestVisible = false };
    private readonly WShapes.Path _dimmer = new()
    {
        Fill = new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)),
        IsHitTestVisible = false,
    };
    private readonly WShapes.Rectangle _cropRect = new()
    {
        Stroke = Brushes.White,
        StrokeThickness = 1.5,
        Fill = Brushes.Transparent, // hit-testable so the body can be dragged
    };
    private readonly Dictionary<string, WShapes.Rectangle> _handles = new();

    private PixelRect _crop;
    private double _zoom = 1, _panX, _panY, _fitZoom = 1;
    private bool _spaceHeld, _confirmed;
    private DragState? _drag;

    private sealed record DragState(string Mode, double StartX, double StartY, double PanX, double PanY, PixelRect Start);

    private ImageCropWindow(BitmapFrame frame)
    {
        InitializeComponent();
        _frame = frame;
        Viewport.Loaded += (s, e) => InitView();
        Viewport.SizeChanged += (s, e) => { ClampPan(); Render(); };
        BuildCanvas();
    }

    public static bool Show(BitmapFrame frame, out ImageCrop crop)
    {
        var win = new ImageCropWindow(frame);
        win.ShowDialog();
        crop = win.CurrentCrop;
        return win._confirmed;
    }

    private ImageCrop CurrentCrop => CropMath.ToNormalized(_crop, _frame.PixelWidth, _frame.PixelHeight);

    private void BuildCanvas()
    {
        _imageControl.Source = _frame;
        CropCanvas.Children.Add(_imageControl);
        CropCanvas.Children.Add(_dimmer);
        CropCanvas.Children.Add(_cropRect);
        _cropRect.MouseLeftButtonDown += OnCropRectDown;
        var cursors = new Dictionary<string, Cursor>
        {
            ["NW"] = Cursors.SizeNWSE, ["SE"] = Cursors.SizeNWSE,
            ["NE"] = Cursors.SizeNESW, ["SW"] = Cursors.SizeNESW,
            ["N"] = Cursors.SizeNS, ["S"] = Cursors.SizeNS,
            ["E"] = Cursors.SizeWE, ["W"] = Cursors.SizeWE,
        };
        foreach (var (tag, cursor) in cursors)
        {
            var handle = new WShapes.Rectangle
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Cursor = cursor,
                Tag = tag,
            };
            handle.MouseLeftButtonDown += OnHandleDown;
            CropCanvas.Children.Add(handle);
            _handles[tag] = handle;
        }
    }

    private void InitView()
    {
        _fitZoom = Math.Min(Viewport.ActualWidth / _frame.PixelWidth, Viewport.ActualHeight / _frame.PixelHeight);
        if (_fitZoom <= 0 || double.IsInfinity(_fitZoom) || double.IsNaN(_fitZoom)) _fitZoom = 1;
        _fitZoom = Math.Min(_fitZoom, MaxZoom);
        var centered = CropMath.CenterCropToRatio(_frame.PixelWidth, _frame.PixelHeight, TaskbarEmbedder.WidgetRatio);
        _crop = CropMath.ToPixels(centered, _frame.PixelWidth, _frame.PixelHeight);
        ResetZoom();
    }

    private void ResetZoom()
    {
        _zoom = Math.Min(_fitZoom, MaxZoom);
        CenterPan();
        Render();
    }

    private void CenterPan()
    {
        _panX = (Viewport.ActualWidth - _frame.PixelWidth * _zoom) / 2;
        _panY = (Viewport.ActualHeight - _frame.PixelHeight * _zoom) / 2;
    }

    // keep the image visible: centered when smaller than the viewport,
    // edges clamped when larger
    private void ClampPan()
    {
        double iw = _frame.PixelWidth * _zoom, ih = _frame.PixelHeight * _zoom;
        double vw = Viewport.ActualWidth, vh = Viewport.ActualHeight;
        _panX = iw <= vw ? (vw - iw) / 2 : Math.Clamp(_panX, vw - iw, 0);
        _panY = ih <= vh ? (vh - ih) / 2 : Math.Clamp(_panY, vh - ih, 0);
    }

    private void Render()
    {
        double iw = _frame.PixelWidth * _zoom, ih = _frame.PixelHeight * _zoom;
        _imageControl.Width = iw;
        _imageControl.Height = ih;
        Canvas.SetLeft(_imageControl, _panX);
        Canvas.SetTop(_imageControl, _panY);

        double cx = _panX + _crop.X * _zoom, cy = _panY + _crop.Y * _zoom;
        double cw = _crop.Width * _zoom, ch = _crop.Height * _zoom;
        Canvas.SetLeft(_cropRect, cx);
        Canvas.SetTop(_cropRect, cy);
        _cropRect.Width = cw;
        _cropRect.Height = ch;

        // dim everything outside the crop (EvenOdd: outer rect + crop hole)
        _dimmer.Data = Geometry.Parse(string.Format(
            "M{0:0.###},{1:0.###} H{2:0.###} V{3:0.###} H{0:0.###} Z M{4:0.###},{5:0.###} H{6:0.###} V{7:0.###} H{4:0.###} Z",
            _panX, _panY, _panX + iw, _panY + ih, cx, cy, cx + cw, cy + ch));

        PositionHandle("NW", cx - 5, cy - 5);
        PositionHandle("N", cx + cw / 2 - 5, cy - 5);
        PositionHandle("NE", cx + cw - 5, cy - 5);
        PositionHandle("E", cx + cw - 5, cy + ch / 2 - 5);
        PositionHandle("SE", cx + cw - 5, cy + ch - 5);
        PositionHandle("S", cx + cw / 2 - 5, cy + ch - 5);
        PositionHandle("SW", cx - 5, cy + ch - 5);
        PositionHandle("W", cx - 5, cy + ch / 2 - 5);

        PreviewImage.Source = WidgetImage.CropFrame(_frame, CurrentCrop);
    }

    private void PositionHandle(string tag, double left, double top)
    {
        Canvas.SetLeft(_handles[tag], left);
        Canvas.SetTop(_handles[tag], top);
    }

    // ---- input ----

    private void OnViewportLeftDown(object sender, MouseButtonEventArgs e)
    {
        Viewport.Focus();
        BeginDrag("pan", e);
        e.Handled = true;
    }

    private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && e.MiddleButton == MouseButtonState.Pressed)
        {
            BeginDrag("pan", e);
            e.Handled = true;
        }
    }

    private void OnCropRectDown(object sender, MouseButtonEventArgs e)
    {
        Viewport.Focus();
        BeginDrag(_spaceHeld ? "pan" : "move", e);
        e.Handled = true;
    }

    private void OnHandleDown(object sender, MouseButtonEventArgs e)
    {
        Viewport.Focus();
        BeginDrag("resize:" + (string)((FrameworkElement)sender).Tag!, e);
        e.Handled = true;
    }

    private void BeginDrag(string mode, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(Viewport);
        _drag = new DragState(mode, p.X, p.Y, _panX, _panY, _crop);
        Viewport.CaptureMouse();
    }

    private void OnViewportMouseMove(object sender, MouseEventArgs e)
    {
        if (_drag is not DragState drag) return;
        var p = e.GetPosition(Viewport);
        switch (drag.Mode)
        {
            case "pan":
                _panX = drag.PanX + (p.X - drag.StartX);
                _panY = drag.PanY + (p.Y - drag.StartY);
                ClampPan();
                break;
            case "move":
                double dxImg = (p.X - drag.StartX) / _zoom;
                double dyImg = (p.Y - drag.StartY) / _zoom;
                _crop = CropMath.ClampCropRect(
                    drag.Start.X + dxImg, drag.Start.Y + dyImg,
                    drag.Start.Width, drag.Start.Height,
                    _frame.PixelWidth, _frame.PixelHeight, TaskbarEmbedder.WidgetRatio, MinCropWidth);
                break;
            case var mode when mode.StartsWith("resize:"):
                _crop = ResizeFromHandle(mode["resize:".Length..], MouseToImage(p), drag.Start);
                break;
        }
        Render();
    }

    private void OnViewportMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_drag == null) return;
        _drag = null;
        Viewport.ReleaseMouseCapture();
    }

    private (double X, double Y) MouseToImage(Point p)
        => ((p.X - _panX) / _zoom, (p.Y - _panY) / _zoom);

    // Resizes keep the opposite edge/corner anchored. Corner and E/W handles
    // are width-driven; N/S handles are height-driven. ClampCropRect enforces
    // the widget ratio and image bounds either way.
    private PixelRect ResizeFromHandle(string tag, (double X, double Y) m, PixelRect s)
    {
        double ax, ay;
        switch (tag)
        {
            case "NW": ax = s.X + s.Width; ay = s.Y + s.Height; break;
            case "NE": ax = s.X; ay = s.Y + s.Height; break;
            case "SE": ax = s.X; ay = s.Y; break;
            case "SW": ax = s.X + s.Width; ay = s.Y; break;
            case "E": ax = s.X; ay = s.Y + s.Height / 2.0; break;
            case "W": ax = s.X + s.Width; ay = s.Y + s.Height / 2.0; break;
            case "N": ax = s.X + s.Width / 2.0; ay = s.Y + s.Height; break;
            default: ax = s.X + s.Width / 2.0; ay = s.Y; break; // "S"
        }
        bool widthDriven = tag is not ("N" or "S");
        double w = widthDriven ? Math.Abs(ax - m.X) : Math.Abs(ay - m.Y) * TaskbarEmbedder.WidgetRatio;
        double h = w / TaskbarEmbedder.WidgetRatio;
        double x = tag is "N" or "S" ? ax - w / 2.0 : Math.Min(ax, m.X);
        double y = tag is "E" or "W"
            ? ay - h / 2.0
            : m.Y >= ay ? ay : ay - h;
        return CropMath.ClampCropRect(x, y, w, h,
            _frame.PixelWidth, _frame.PixelHeight, TaskbarEmbedder.WidgetRatio, MinCropWidth);
    }

    private void OnViewportWheel(object sender, MouseWheelEventArgs e)
    {
        var p = e.GetPosition(Viewport);
        double old = _zoom;
        _zoom = Math.Clamp(old * (e.Delta > 0 ? 1.15 : 1.0 / 1.15), _fitZoom, MaxZoom);
        if (_zoom == old) return;
        _panX = p.X - (p.X - _panX) * (_zoom / old);
        _panY = p.Y - (p.Y - _panY) * (_zoom / old);
        ClampPan();
        Render();
        e.Handled = true;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _spaceHeld = true;
            e.Handled = true; // Space+drag pans instead of moving the crop
        }
        else if (e.Key is Key.OemPlus or Key.Add) { ZoomAroundCenter(1.15); e.Handled = true; }
        else if (e.Key is Key.OemMinus or Key.Subtract) { ZoomAroundCenter(1.0 / 1.15); e.Handled = true; }
        else if (e.Key == Key.D0) { ResetZoom(); e.Handled = true; }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) { _spaceHeld = false; e.Handled = true; }
    }

    private void ZoomAroundCenter(double factor)
    {
        double old = _zoom;
        _zoom = Math.Clamp(old * factor, _fitZoom, MaxZoom);
        if (_zoom == old) return;
        var c = new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2);
        _panX = c.X - (c.X - _panX) * (_zoom / old);
        _panY = c.Y - (c.Y - _panY) * (_zoom / old);
        ClampPan();
        Render();
    }

    // ---- buttons ----

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        _confirmed = true;
        Close();
    }

    private void OnSkipCrop(object sender, RoutedEventArgs e)
    {
        var full = CropMath.CenterCropToRatio(_frame.PixelWidth, _frame.PixelHeight, TaskbarEmbedder.WidgetRatio);
        _crop = CropMath.ToPixels(full, _frame.PixelWidth, _frame.PixelHeight);
        _confirmed = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
