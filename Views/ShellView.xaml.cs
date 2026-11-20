using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wastu.Controls;
using Wastu.Services;
using Wastu.Views.Pages;

namespace Wastu.Views;

// app.html: sidebar, topbar, dan halaman ruang kerja. Halaman dibuat sekali (lazy) lalu dipakai ulang;
// render hanya menyegarkan data halaman yang sedang tampil, dan hanya bila state berubah.
public partial class ShellView : UserControl
{
    private static readonly Dictionary<string, string> Titles = new()
    {
        ["home"] = "Beranda", ["project"] = "Profil Proyek", ["material"] = "Eksplor Material", ["compare"] = "Bandingkan",
        ["sim"] = "Simulasi Emisi", ["weather"] = "Jadwal Pekerjaan", ["report"] = "Laporan"
    };

    private readonly MainWindow _window;
    private readonly Dictionary<string, PageBase> _pages = [];
    private PageBase? _page;
    private (bool Compact, bool Wide) _tier;

    public ShellView(MainWindow window)
    {
        _window = window;
        InitializeComponent();
        Loaded += (_, _) => { Viewport.Changed += ApplyViewport; ApplyViewport(); };
        Unloaded += (_, _) => Viewport.Changed -= ApplyViewport;
    }

    public string CurrentPage { get; private set; } = "home";

    public void Go(string page)
    {
        CurrentPage = Titles.ContainsKey(page) ? page : "home";
        App.State.Cari = "";
        var view = Page(CurrentPage);
        // Menukar Visibility jauh lebih murah daripada melepas-pasang halaman: subtree yang
        // Collapsed tidak di-layout atau dirender, dan cache layout-nya tetap valid.
        if (_page is not null && _page != view) _page.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;
        _page = view;
        Show(force: false);
        Scroller.ScrollToTop();
    }

    private PageBase Page(string key)
    {
        if (_pages.TryGetValue(key, out var view)) return view;
        view = key switch
        {
            "project" => new ProjectPage(),
            "material" => new MaterialsPage(),
            "compare" => new ComparePage(),
            "sim" => new SimulationPage(),
            "weather" => new SchedulePage(),
            "report" => new ReportPage(),
            _ => new HomePage()
        };
        view.Shell = this;
        view.Visibility = Visibility.Collapsed;
        _pages[key] = view;
        PageHost.Children.Add(view);
        return view;
    }

    // Halaman yang belum dibuka dibangun saat aplikasi menganggur (satu halaman per giliran,
    // tidak mendahului input pengguna) sehingga kunjungan pertama pun tidak perlu membangun apa pun.
    public void Prewarm()
    {
        foreach (var key in Titles.Keys)
            Dispatcher.InvokeAsync(() =>
            {
                if (!App.State.AdaProyek || _pages.ContainsKey(key)) return;
                var view = Page(key);
                view.Visibility = Visibility.Hidden;
                view.Show(force: true);
                view.UpdateLayout();
                if (view != _page) view.Visibility = Visibility.Collapsed;
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    // Dipanggil setelah state berubah.
    public void Render()
    {
        App.State.Ubah();
        Show(force: false);
    }

    private void Show(bool force)
    {
        var state = App.State;
        if (!state.AdaProyek) return;
        var project = state.Aktif;
        foreach (RadioButton item in Nav.Children)
            item.IsChecked = (string)item.Tag == CurrentPage;
        StatusName.Text = project.Nama;
        StatusCity.Text = project.Kota;
        Context.Text = project.Nama;
        LocationText.Text = project.Kota;
        _window.Title = Titles[CurrentPage] + " | Wastu";
        _page?.Show(force);
    }

    public void Toast(string message) => _window.Toast(message);

    // getForecast() pada app.js
    public async Task RefreshForecastAsync()
    {
        var state = App.State;
        var project = state.Aktif;
        double lintang = project.Latitude;
        double bujur = project.Longitude;
        Toast("Mengambil prakiraan Open-Meteo…");
        try
        {
            var incoming = await OpenMeteo.AmbilPrakiraanAsync(lintang, bujur);
            if (!state.AdaProyek || state.Aktif != project || project.Latitude != lintang || project.Longitude != bujur) return;
            state.Prakiraan = incoming;
            state.PrakiraanProyekId = project.Id;
            state.PrakiraanDiperbarui = Format.WaktuLokal(DateTime.Now);
            state.HariDipilih = null;
            Render();
            Toast("Prakiraan harian berhasil dimuat.");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidDataException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Toast(state.PrakiraanAktif().Count > 0
                ? "Prakiraan belum dapat diperbarui. Data terakhir tetap tersedia."
                : "Prakiraan belum dapat diperbarui. Periksa koneksi lalu coba kembali.");
        }
    }

    public Task RefreshClimateAsync() => RunProjectChangeAsync(async () =>
    {
        Toast("Mengambil data iklim historis…");
        var pesan = await App.State.PerbaruiIklimAsync(App.State.Aktif);
        Render();
        Toast(pesan ?? "Profil iklim diperbarui dari Open-Meteo.");
    });

    public async Task RunProjectChangeAsync(Func<Task> action)
    {
        if (!IsEnabled) return;
        IsEnabled = false;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Toast("Perubahan proyek belum dapat diterapkan. Periksa koneksi dan coba lagi.");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    public void ProjectsEmptied() => _window.ShowWelcome();

    public void NewProject() => _window.ShowCreate();

    private void ApplyViewport()
    {
        // .content { padding: 30px clamp(24px, 3vw, 48px) 48px }
        var pad = Viewport.Clamp(24, 3, 48);
        var wide = Viewport.Width >= 1700;
        ContentArea.Padding = new Thickness(pad, wide ? 38 : 30, pad, 48);
        var tier = (Viewport.Width <= 1200, wide);
        if (tier == _tier) return;
        _tier = tier;
        _page?.Show(force: true);
    }

    private void Nav_Click(object sender, RoutedEventArgs e) => Go((string)((FrameworkElement)sender).Tag);

    private void Go_Click(object sender, RoutedEventArgs e) => Go((string)((FrameworkElement)sender).Tag);

    // Langkah gulir ~100 px per notch seperti Chromium (bawaan WPF 48 px).
    // Gulir di atas peta diteruskan ke peta untuk zoom, bukan menggulir halaman.
    private void Scroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || LocationMap.Contains(e.OriginalSource as DependencyObject)) return;
        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - e.Delta * 100.0 / 120);
        e.Handled = true;
    }
}
