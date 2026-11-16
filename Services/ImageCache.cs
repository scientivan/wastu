using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wastu.Services;

// Seluruh gambar didekode sekali di thread pool saat splash, dibekukan (Freeze),
// lalu dipakai bersama oleh semua kartu. Tidak ada dekode ulang saat berpindah halaman.
public static class ImageCache
{
    private static readonly string[] Materials = ["aac", "brick", "concrete", "fiber", "metal", "tile", "timber"];
    private static readonly Dictionary<string, ImageBrush> Covers = [];
    private static readonly Dictionary<string, ImageBrush> Thumbs = [];
    private static BitmapSource? _welcome, _planning;
    private static Task? _load;

    public static Task PreloadAsync() => _load ??= Task.Run(LoadAll);

    public static BitmapSource Welcome { get { Ready(); return _welcome!; } }
    public static BitmapSource Planning { get { Ready(); return _planning!; } }
    public static ImageBrush Cover(string name) { Ready(); return Covers[name]; }
    public static ImageBrush Thumb(string name) { Ready(); return Thumbs[name]; }

    // Normalnya sudah selesai sebelum splash ditutup; Wait hanya jaring pengaman.
    private static void Ready() => PreloadAsync().Wait();

    private static void LoadAll()
    {
        Parallel.Invoke(
            () => _welcome = Decode("welcome-architecture.jpg", 0),
            () => _planning = Decode("project-planning.png", 0),
            () =>
            {
                foreach (var name in Materials)
                {
                    Covers[name] = CoverBrush(Decode(name + ".jpg", 768));
                    Thumbs[name] = CoverBrush(Decode(name + ".jpg", 144));
                }
            });
    }

    private static ImageBrush CoverBrush(BitmapSource source)
    {
        // object-fit: cover
        var brush = new ImageBrush(source) { Stretch = Stretch.UniformToFill };
        RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);
        brush.Freeze();
        return brush;
    }

    private static BitmapSource Decode(string name, int width)
    {
        using var stream = typeof(ImageCache).Assembly.GetManifestResourceStream("Assets." + name)
            ?? throw new InvalidOperationException("Aset tidak ditemukan: " + name);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        image.StreamSource = stream;
        if (width > 0) image.DecodePixelWidth = width;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
