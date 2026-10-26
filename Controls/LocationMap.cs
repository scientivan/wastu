using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wastu.Services;

namespace Wastu.Controls;

// Peta OpenStreetMap (Web Mercator, ubin 256 px) untuk menandai titik lokasi proyek.
// Seret untuk menggeser, roda tetikus atau tombol +/− untuk memperbesar, klik untuk memindahkan penanda.
public sealed class LocationMap : Grid
{
    private const int Tile = 256, MinZoom = 3, MaxZoom = 19, PinZoom = 14, CacheLimit = 160;
    private const double Radius = 8;

    private static readonly HttpClient Http = CreateClient();
    private static readonly string TileFolder = Path.Combine(StateStore.ProfileFolder, "tiles");

    // Ubin dibagi antarinstans dan hanya disentuh dari UI thread.
    private static readonly Dictionary<(int Z, int X, int Y), BitmapSource> Cache = [];
    private static readonly HashSet<(int Z, int X, int Y)> Pending = [];
    private static readonly Dictionary<(int Z, int X, int Y), DateTime> Failed = [];
    private static event Action? TileLoaded;

    private static readonly Brush Paper = Freeze(new SolidColorBrush(Color.FromRgb(0xEE, 0xEC, 0xE3)));
    private static readonly Pen Outline = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromRgb(0xDF, 0xE5, 0xDB))), 1));
    private static readonly Brush PinFill = Freeze(new SolidColorBrush(Color.FromRgb(0x19, 0x4F, 0x38)));
    private static readonly Pen PinStroke = Freeze(new Pen(Brushes.White, 1.6));
    private static readonly Brush PinShadow = Freeze(new SolidColorBrush(Color.FromArgb(0x55, 0x0B, 0x22, 0x18)));
    // IcoPin tanpa lubang tengah; ujung bawah (12,22) menunjuk titik lokasi.
    private static readonly Geometry PinShape = Freeze(Geometry.Parse("M12 22 C12 22 4 16 4 10 A8 8 0 1 1 20 10 C20 16 12 22 12 22 Z"));

    // Pusat tampilan dan penanda dalam koordinat Mercator ternormalisasi [0,1].
    private double _cx = X(118), _cy = Y(-2.5);
    private int _zoom = 4;
    private Point? _pin;
    private Point _press, _pressCenter;
    private bool _dragging;
    private int _wheel;

    // Titik dipilih pengguna (klik peta atau Place); lintang, bujur dalam derajat.
    public event Action<double, double>? Picked;

    public LocationMap()
    {
        Focusable = true;
        ClipToBounds = true;
        Cursor = Cursors.Hand;
        SetResourceReference(FocusVisualStyleProperty, "FocusOutline");
        AutomationProperties.SetName(this, "Peta lokasi proyek");
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

        var zoom = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(10) };
        zoom.Children.Add(ZoomButton("+", "Perbesar", 1));
        zoom.Children.Add(ZoomButton("−", "Perkecil", -1));
        Children.Add(zoom);
        // Atribusi wajib menurut kebijakan ubin OpenStreetMap.
        Children.Add(new Border
        {
            Background = Freeze(new SolidColorBrush(Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF))),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 1, 1), Padding = new Thickness(6, 2, 6, 2), CornerRadius = new CornerRadius(0, 0, Radius - 1, 0),
            IsHitTestVisible = false,
            Child = new CssText { Text = "© OpenStreetMap contributors", FontSize = 10, Foreground = Freeze(new SolidColorBrush(Color.FromRgb(0x56, 0x6B, 0x5D))) }
        });

        Loaded += (_, _) => TileLoaded += InvalidateVisual;
        Unloaded += (_, _) => TileLoaded -= InvalidateVisual;
    }

    public bool HasPin => _pin is not null;

    // Menandai titik dari luar (misalnya isian form). Peta hanya berpindah bila titik keluar dari layar.
    public void SetPin(double lintang, double bujur)
    {
        var first = _pin is null;
        _pin = new Point(X(bujur), Y(lintang));
        if (first) Center(_pin.Value, Math.Max(_zoom, PinZoom));
        else if (!InView(_pin.Value)) Center(_pin.Value, _zoom);
        InvalidateVisual();
    }

    public void ClearPin()
    {
        _pin = null;
        InvalidateVisual();
    }

    // Menandai, memusatkan, lalu memberi tahu pendengar seperti klik pengguna (dipakai deteksi GPS).
    public void Place(double lintang, double bujur, int zoom)
    {
        _pin = new Point(X(bujur), Y(lintang));
        Center(_pin.Value, zoom);
        InvalidateVisual();
        Picked?.Invoke(lintang, bujur);
    }

    // Sinkron dua arah dengan isian lintang/bujur: ketik → penanda bergeser, klik peta → isian terisi.
    public void Bind(TextBox latBox, TextBox lonBox)
    {
        var writing = false;
        void Sync()
        {
            if (writing) return;
            if (Format.CobaBaca(latBox.Text, out var lat) && Format.CobaBaca(lonBox.Text, out var lon) && Math.Abs(lat) <= 85 && Math.Abs(lon) <= 180)
                SetPin(lat, lon);
            else if (latBox.Text.Length == 0 && lonBox.Text.Length == 0)
                ClearPin();
        }
        latBox.TextChanged += (_, _) => Sync();
        lonBox.TextChanged += (_, _) => Sync();
        Picked += (lat, lon) =>
        {
            writing = true;
            // Enam desimal ≈ 0,1 m, lebih dari cukup untuk lokasi lahan.
            latBox.Text = Format.Masukan(Math.Round(lat, 6));
            lonBox.Text = Format.Masukan(Math.Round(lon, 6));
            writing = false;
        };
    }

    private Button ZoomButton(string glyph, string name, int delta)
    {
        var button = new Button { Content = glyph, Width = 34, MinHeight = 34, Padding = new Thickness(0), FontSize = 17, Margin = new Thickness(0, 0, 0, 6), ToolTip = name };
        button.SetResourceReference(StyleProperty, "BtnLightSm");
        AutomationProperties.SetName(button, name);
        button.Click += (_, _) => Zoom(delta, new Point(ActualWidth / 2, ActualHeight / 2));
        return button;
    }

    private double Scale => Tile * (double)(1 << _zoom);

    private void Center(Point world, int zoom)
    {
        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        _cx = world.X;
        _cy = Math.Clamp(world.Y, 0, 1);
    }

    private bool InView(Point world)
    {
        var p = ToScreen(world);
        return p.X >= 0 && p.Y >= 0 && p.X <= ActualWidth && p.Y <= ActualHeight;
    }

    private Point ToScreen(Point world) =>
        new((world.X - _cx) * Scale + ActualWidth / 2, (world.Y - _cy) * Scale + ActualHeight / 2);

    private Point ToWorld(Point screen) =>
        new(_cx + (screen.X - ActualWidth / 2) / Scale, _cy + (screen.Y - ActualHeight / 2) / Scale);

    // Perbesaran berjangkar: titik di bawah kursor tetap di tempatnya.
    private void Zoom(int delta, Point anchor)
    {
        var next = Math.Clamp(_zoom + delta, MinZoom, MaxZoom);
        if (next == _zoom) return;
        var world = ToWorld(anchor);
        _zoom = next;
        _cx = world.X - (anchor.X - ActualWidth / 2) / Scale;
        _cy = Math.Clamp(world.Y - (anchor.Y - ActualHeight / 2) / Scale, 0, 1);
        InvalidateVisual();
    }

    private void Pan(double dx, double dy)
    {
        _cx -= dx / Scale;
        _cy = Math.Clamp(_cy - dy / Scale, 0, 1);
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.Handled) return;
        Focus();
        CaptureMouse();
        _press = e.GetPosition(this);
        _pressCenter = new Point(_cx, _cy);
        _dragging = false;
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!IsMouseCaptured) return;
        var delta = e.GetPosition(this) - _press;
        if (!_dragging && delta.Length < 4) return;
        _dragging = true;
        Cursor = Cursors.SizeAll;
        _cx = _pressCenter.X;
        _cy = _pressCenter.Y;
        Pan(delta.X, delta.Y);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!IsMouseCaptured) return;
        ReleaseMouseCapture();
        Cursor = Cursors.Hand;
        e.Handled = true;
        if (_dragging) return;
        var world = ToWorld(e.GetPosition(this));
        _pin = new Point(world.X, world.Y);
        InvalidateVisual();
        Picked?.Invoke(Lat(Math.Clamp(world.Y, 0, 1)), Lon(world.X));
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        Cursor = Cursors.Hand;
    }

    // Trackpad mengirim delta kecil berkali-kali; dikumpulkan agar satu level zoom = satu notch roda (120).
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Math.Sign(e.Delta) != Math.Sign(_wheel)) _wheel = 0;
        _wheel += e.Delta;
        var anchor = e.GetPosition(this);
        if (anchor.X < 0 || anchor.Y < 0 || anchor.X > ActualWidth || anchor.Y > ActualHeight)
            anchor = new Point(ActualWidth / 2, ActualHeight / 2);
        while (Math.Abs(_wheel) >= Mouse.MouseWheelDeltaForOneLine)
        {
            var step = Math.Sign(_wheel);
            Zoom(step, anchor);
            _wheel -= step * Mouse.MouseWheelDeltaForOneLine;
        }
        e.Handled = true;
    }

    // Untuk ScrollViewer induk yang menangani PreviewMouseWheel: apakah sumber event berada di dalam peta.
    public static bool Contains(DependencyObject? source)
    {
        for (var d = source; d is not null; d = d is Visual ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d))
            if (d is LocationMap) return true;
        return false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        switch (e.Key)
        {
            case Key.Left: Pan(80, 0); break;
            case Key.Right: Pan(-80, 0); break;
            case Key.Up: Pan(0, 80); break;
            case Key.Down: Pan(0, -80); break;
            case Key.OemPlus or Key.Add: Zoom(1, center); break;
            case Key.OemMinus or Key.Subtract: Zoom(-1, center); break;
            default: return;
        }
        e.Handled = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        var frame = new Rect(0.5, 0.5, w - 1, h - 1);
        dc.DrawRoundedRectangle(Paper, null, frame, Radius, Radius);
        dc.PushClip(new RectangleGeometry(frame, Radius, Radius));

        var n = 1 << _zoom;
        var left = _cx * Scale - w / 2;
        var top = _cy * Scale - h / 2;
        for (var ty = Math.Max(0, (int)Math.Floor(top / Tile)); ty <= Math.Min(n - 1, (int)Math.Floor((top + h) / Tile)); ty++)
            for (var tx = (int)Math.Floor(left / Tile); tx <= (int)Math.Floor((left + w) / Tile); tx++)
            {
                var rect = new Rect(Math.Round(tx * Tile - left), Math.Round(ty * Tile - top), Tile, Tile);
                DrawTile(dc, rect, _zoom, ((tx % n) + n) % n, ty);
            }

        if (_pin is { } pin)
        {
            var p = ToScreen(new Point(((pin.X - _cx + 0.5) % 1 + 1) % 1 + _cx - 0.5, pin.Y));
            dc.DrawEllipse(PinShadow, null, p, 7, 2.6);
            const double size = 34.0 / 24;
            dc.PushTransform(new MatrixTransform(size, 0, 0, size, p.X - 12 * size, p.Y - 22 * size));
            dc.DrawGeometry(PinFill, PinStroke, PinShape);
            dc.DrawEllipse(Brushes.White, null, new Point(12, 10), 3.2, 3.2);
            dc.Pop();
        }

        dc.Pop();
        dc.DrawRoundedRectangle(null, Outline, frame, Radius, Radius);
    }

    // Selama ubin belum termuat, induknya (zoom lebih kecil) yang sudah ada diperbesar sebagai pengganti.
    private static void DrawTile(DrawingContext dc, Rect rect, int z, int x, int y)
    {
        if (Get((z, x, y)) is { } image)
        {
            dc.DrawImage(image, rect);
            return;
        }
        for (var d = 1; d <= 4 && z - d >= 0; d++)
        {
            if (!Cache.TryGetValue((z - d, x >> d, y >> d), out var parent)) continue;
            var size = Tile << d;
            dc.PushClip(new RectangleGeometry(rect));
            dc.DrawImage(parent, new Rect(rect.X - (x & ((1 << d) - 1)) * Tile, rect.Y - (y & ((1 << d) - 1)) * Tile, size, size));
            dc.Pop();
            return;
        }
    }

    private static BitmapSource? Get((int Z, int X, int Y) key)
    {
        if (Cache.TryGetValue(key, out var image)) return image;
        if (!Pending.Contains(key) && !(Failed.TryGetValue(key, out var at) && DateTime.UtcNow - at < TimeSpan.FromSeconds(30)))
            _ = LoadAsync(key);
        return null;
    }

    // Ubin disimpan di disk 30 hari (sesuai kebijakan ubin OSM); bila jaringan gagal, salinan lama tetap dipakai.
    private static async Task LoadAsync((int Z, int X, int Y) key)
    {
        Pending.Add(key);
        try
        {
            var image = await Task.Run(async () =>
            {
                var file = Path.Combine(TileFolder, key.Z.ToString(CultureInfo.InvariantCulture), key.X.ToString(CultureInfo.InvariantCulture), key.Y.ToString(CultureInfo.InvariantCulture) + ".png");
                var saved = File.Exists(file);
                if (!saved || DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > TimeSpan.FromDays(30))
                {
                    try
                    {
                        var bytes = await Http.GetByteArrayAsync($"https://tile.openstreetmap.org/{key.Z}/{key.X}/{key.Y}.png").ConfigureAwait(false);
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                            await File.WriteAllBytesAsync(file, bytes).ConfigureAwait(false);
                        }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
                        return Decode(bytes);
                    }
                    catch (Exception e) when (saved && e is HttpRequestException or TaskCanceledException) { }
                }
                return Decode(await File.ReadAllBytesAsync(file).ConfigureAwait(false));
            });
            if (Cache.Count >= CacheLimit) Cache.Clear();
            Cache[key] = image;
            Failed.Remove(key);
            TileLoaded?.Invoke();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or NotSupportedException or FormatException)
        {
            Failed[key] = DateTime.UtcNow;
        }
        finally
        {
            Pending.Remove(key);
        }
    }

    private static BitmapSource Decode(byte[] bytes)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = new MemoryStream(bytes);
        image.EndInit();
        image.Freeze();
        return image;
    }

    // Kebijakan ubin OSM mewajibkan User-Agent yang mengidentifikasi aplikasi dan membatasi koneksi paralel.
    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new SocketsHttpHandler { MaxConnectionsPerServer = 2, PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
        { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Wastu/1.0 (+https://github.com/scientivan/wastu)");
        return client;
    }

    private static double X(double lon) => (lon + 180) / 360;

    private static double Y(double lat)
    {
        var s = Math.Sin(Math.Clamp(lat, -85.0511, 85.0511) * Math.PI / 180);
        return 0.5 - Math.Log((1 + s) / (1 - s)) / (4 * Math.PI);
    }

    private static double Lon(double x) => (((x % 1) + 1) % 1) * 360 - 180;

    private static double Lat(double y) => 90 - 360 * Math.Atan(Math.Exp((y - 0.5) * 2 * Math.PI)) / Math.PI;

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
