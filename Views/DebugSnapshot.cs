#if DEBUG || SNAPSHOT
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Wastu.Views;

// Alat verifikasi visual (hanya build Debug): WASTU_SNAPSHOT="halaman1,halaman2;folder;lebar;tinggi"
// merender tiap halaman ke PNG lalu menutup aplikasi.
internal static class DebugSnapshot
{
    private static readonly string? Spec = Environment.GetEnvironmentVariable("WASTU_SNAPSHOT");
    private static FrameworkElement? _root;
    private static DateTime _firstFrame;

    public static bool Requested => !string.IsNullOrEmpty(Spec);

    public static void Prepare(Window window)
    {
        if (!Requested) return;
        var parts = Spec!.Split(';');
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.Left = -30000;
        window.Top = 0;
        window.Width = 1100;
        window.Height = 700;

        // Tinggi jendela dibatasi layar; Canvas tidak memotong anaknya sehingga root bisa lebih besar.
        window.ContentRendered += (_, _) => _firstFrame = DateTime.Now;
        _root = (FrameworkElement)window.Content;
        window.Content = null;
        _root.Width = double.Parse(parts[2]);
        _root.Height = double.Parse(parts[3]);
        var canvas = new Canvas();
        canvas.Children.Add(_root);
        window.Content = canvas;
    }

    private static async Task Idle(Window window)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(150);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static void Save(string path)
    {
        var bitmap = new RenderTargetBitmap((int)_root!.ActualWidth, (int)_root.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(_root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    private static IEnumerable<T> All<T>(DependencyObject node) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is T match) yield return match;
            foreach (var inner in All<T>(child)) yield return inner;
        }
    }

    private static void Click(System.Windows.Controls.Primitives.ButtonBase button) =>
        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, button));

    private static T Named<T>(string name) where T : FrameworkElement =>
        All<T>(_root!).First(e => e.Name == name);

    private static System.Windows.Controls.Primitives.ButtonBase Labeled(string content) =>
        All<System.Windows.Controls.Primitives.ButtonBase>(_root!).First(b => b.IsVisible && (b.Content as string) == content);

    // Skenario uji asap: menjalankan alur nyata lewat event UI dan merekam tiap langkah.
    private static async Task FlowAsync(MainWindow window, string folder)
    {
        var log = new System.Text.StringBuilder();
        var step = 0;
        async Task Shot(string name)
        {
            await Idle(window);
            Save(Path.Combine(folder, $"flow-{++step:00}-{name}.png"));
            log.AppendLine($"{step:00} {name} ok");
        }
        try
        {
            window.ShowWelcome(); await Shot("welcome");
            Click(Labeled("Buat proyek baru")); await Shot("create");
            Click(Named<Button>("NextButton")); await Shot("create-invalid");
            Named<TextBox>("NameBox").Text = "Rumah Uji"; Click(Named<Button>("NextButton")); await Shot("create-lokasi");
            Named<ComboBox>("CityBox").SelectedItem = "Lokasi lain"; await Shot("create-lokasi-lain");
            Named<ComboBox>("CityBox").SelectedItem = "Bandung";
            Click(Named<Button>("NextButton")); await Shot("create-luas");
            Click(Named<Button>("NextButton")); await Shot("create-luas-invalid");
            Named<TextBox>("WallBox").Text = "150"; Named<TextBox>("RoofBox").Text = "100,5";
            Click(Named<Button>("NextButton")); await Shot("home-baru");
            Click(All<RadioButton>(_root!).First(r => (r.Tag as string) == "material")); await Shot("material");
            Click(Named<RadioButton>("RoofTab")); await Shot("material-atap");
            Named<TextBox>("SearchBox").Text = "genteng"; await Shot("material-cari");
            Click(Labeled("Bandingkan material")); await Shot("compare-atap");
            Named<ComboBox>("SelectB").SelectedIndex = 4; await Shot("compare-ganti-b");
            Click(Labeled("Simulasikan emisi")); await Shot("sim");
            Named<ComboBox>("HorizonBox").SelectedIndex = 2; await Shot("sim-50");
            Click(All<RadioButton>(_root!).First(r => (r.Tag as string) == "weather")); await Shot("weather-kosong");
            Click(Labeled("Tambahkan pekerjaan")); await Shot("weather-tambah");
            Click(Named<RadioButton>("CastTab")); await Shot("weather-pengecoran");
            Click(All<RadioButton>(_root!).First(r => (r.Tag as string) == "report")); await Shot("report");
            Click(All<RadioButton>(_root!).First(r => (r.Tag as string) == "project")); await Shot("project");
            Named<TextBox>("NameBox").Text = "Rumah Uji Dua"; Click(Labeled("Simpan proyek")); await Shot("project-simpan");
            Named<TextBox>("LatBox").Text = "20"; Click(Labeled("Simpan proyek")); await Shot("project-koordinat-salah");
            Click(Labeled("Proyek baru")); await Shot("create-dari-proyek");
            Click(Named<Button>("BackButton")); await Shot("kembali-ke-beranda");
            App.State.HapusProyekAktif();
            if (!App.State.AdaProyek) window.ShowWelcome();
            await Shot("hapus-proyek-terakhir");
        }
        catch (Exception e)
        {
            log.AppendLine("FAIL step " + (step + 1) + ": " + e);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "flow.log"), log.ToString());
    }

    // Waktu UI thread untuk berpindah halaman (bangun/segarkan + layout), kunjungan pertama dan berikutnya.
    private static async Task<string> PerfAsync(MainWindow window)
    {
        var sb = new System.Text.StringBuilder();
        var started = System.Diagnostics.Process.GetCurrentProcess().StartTime;
        sb.AppendLine($"frame pertama (splash tampil): {(_firstFrame - started).TotalMilliseconds:0} ms");
        await Task.Delay(1500); // beri waktu prewarm halaman di latar
        sb.AppendLine($"startup (proses -> ruang kerja siap, termasuk splash minimum 600 ms): {(DateTime.Now - started).TotalMilliseconds:0} ms");
        string[] pages = ["home", "project", "material", "compare", "sim", "weather", "report"];
        foreach (var round in new[] { "pertama", "berikutnya" })
            foreach (var page in pages)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                window.ShowShell(page);
                _root!.UpdateLayout();
                sw.Stop();
                await Idle(window);
                sb.AppendLine($"{round,-10} {page,-9} {sw.Elapsed.TotalMilliseconds,7:0.0} ms");
            }
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 20; i++)
        {
            App.State.AturJenis(i % 2 == 0 ? Models.JenisKomponen.Atap : Models.JenisKomponen.Dinding);
            App.State.Ubah();
            window.ShowShell("material");
            _root!.UpdateLayout();
        }
        sb.AppendLine($"ganti Dinding/Atap di Eksplor material (rata-rata 20x): {sw2.Elapsed.TotalMilliseconds / 20:0.0} ms");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var proc = System.Diagnostics.Process.GetCurrentProcess();
        proc.Refresh();
        sb.AppendLine($"working set: {proc.WorkingSet64 / 1048576.0:0} MB, private: {proc.PrivateMemorySize64 / 1048576.0:0} MB, managed heap: {GC.GetTotalMemory(true) / 1048576.0:0.0} MB");
        return sb.ToString();
    }

    // Perhitungan model C# untuk skenario yang sama dengan verify.js (app.js asli).
    private static string Verify()
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        (string City, double Lat, double Lon, double Wall, double Roof, double Coastal, double[]? Climate)[] scenarios =
        [
            ("Bandung", -6.9175, 107.6191, 150, 100.5, 84, null),
            ("Yogyakarta", -7.7956, 110.3695, 120, 90, 26, [2310, 81, 27.1]),
            ("Semarang", -6.9667, 110.4167, 80, 64.5, 4, null),
            ("Lokasi lain", -2, 120, 33.3, 21.7, 12, [3100, 88, 26.4]),
        ];
        var lines = new List<string>();
        foreach (var sc in scenarios)
            foreach (var kind in new[] { Models.JenisKomponen.Dinding, Models.JenisKomponen.Atap })
                foreach (var horizon in new[] { 20, 30, 50 })
                {
                    var state = new Services.AppState();
                    var p = state.TambahProyek("X", sc.City, sc.Lat, sc.Lon, sc.Wall, sc.Roof, sc.Coastal);
                    if (sc.Climate is { } c)
                    {
                        p.ProfilIklim!.CurahHujanTahunan = c[0];
                        p.ProfilIklim.KelembapanRata = c[1];
                        p.ProfilIklim.SuhuRata = c[2];
                        p.ProfilIklim.DiperbaruiPada = "x";
                    }
                    state.Jenis = kind;
                    state.Horizon = horizon;
                    foreach (var e in state.Peringkat())
                    {
                        var item = Services.Katalog.Dari(e.Bahan);
                        var sim = state.Simulasikan(item).SimulasiEmisi!;
                        lines.Add(string.Join('|', sc.City, kind.ToString().ToLowerInvariant(), horizon, item.Kode, e.SkorKesesuaian,
                            Math.Round(e.EmisiAwal * 1000) / 1000, sim.JumlahPenggantian, Math.Round(sim.UsiaPakaiEfektif * 1e6) / 1e6,
                            Math.Round(sim.EmisiKumulatif * 1000) / 1000, e.Alasan).Replace(",", "."));
                    }
                }
        return string.Join(Environment.NewLine, lines);
    }

    private static string Dump(FrameworkElement root)
    {
        var sb = new System.Text.StringBuilder();
        var family = (FontFamily)Application.Current.Resources["Inter"];
        var face = Controls.GlyphFonts.Inter(FontWeights.Normal);
        sb.AppendLine($"family baseline={family.Baseline} spacing={family.LineSpacing} glyph baseline={face.Baseline} height={face.Height}");
        void Walk(DependencyObject node, int depth)
        {
            if (node is FrameworkElement fe && fe.IsVisible)
            {
                var p = fe.TransformToAncestor(root).Transform(new Point(0, 0));
                var text = fe switch { TextBlock t => " \"" + t.Text + "\"", Controls.TrackedText t => " \"" + t.Text + "\"", _ => "" };
                sb.Append(' ', depth).Append(fe.GetType().Name).Append(' ').Append(fe.Name)
                  .Append($" [{p.X:0.##},{p.Y:0.##} {fe.ActualWidth:0.##}x{fe.ActualHeight:0.##}]").AppendLine(text);
            }
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Walk(VisualTreeHelper.GetChild(node, i), depth + 1);
        }
        Walk(root, 0);
        return sb.ToString();
    }

    public static async Task RunAsync(MainWindow window)
    {
        var parts = Spec!.Split(';');
        Directory.CreateDirectory(parts[1]);
        if (parts[0] == "perf")
        {
            await File.WriteAllTextAsync(Path.Combine(parts[1], "perf.txt"), await PerfAsync(window));
            Application.Current.Shutdown();
            return;
        }
        if (parts[0] == "verify")
        {
            await File.WriteAllTextAsync(Path.Combine(parts[1], "actual.txt"), Verify());
            Application.Current.Shutdown();
            return;
        }
        if (parts[0] == "flow")
        {
            await FlowAsync(window, parts[1]);
            Application.Current.Shutdown();
            return;
        }
        foreach (var page in parts[0].Split(','))
        {
            switch (page)
            {
                case "welcome": window.ShowWelcome(); break;
                case "create": window.ShowCreate(); break;
                default: window.ShowShell(page); break;
            }
            await Idle(window);
            Save(Path.Combine(parts[1], page + ".png"));
            if (Environment.GetEnvironmentVariable("WASTU_DUMP") == "1")
                await File.WriteAllTextAsync(Path.Combine(parts[1], page + ".tree.txt"), Dump(_root!));
        }
        Application.Current.Shutdown();
    }
}
#endif
