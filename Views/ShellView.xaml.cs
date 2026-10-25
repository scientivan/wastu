using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wastu.Services;
using Wastu.Views.Pages;

namespace Wastu.Views;

// app.html: sidebar, topbar, dan halaman ruang kerja. Halaman dibuat sekali (lazy) lalu dipakai ulang;
// render menyegarkan data halaman yang sedang tampil.
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
        _page = view;
        PageHost.Child = view;
        Show();
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
        _pages[key] = view;
        return view;
    }

    // Dipanggil setelah state berubah.
    public void Render()
    {
        App.State.Ubah();
        Show();
    }

    private void Show()
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
        _page?.Refresh();
    }

    public void Toast(string message) => _window.Toast(message);

    // getForecast() pada app.js
    public async Task RefreshForecastAsync()
    {
        var state = App.State;
        var project = state.Aktif;
        Toast("Mengambil prakiraan Open-Meteo…");
        try
        {
            var incoming = await OpenMeteo.AmbilPrakiraanAsync(project.Latitude, project.Longitude);
            if (!state.AdaProyek || state.Aktif != project) return;
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

    // getClimate() pada app.js
    public async Task RefreshClimateAsync()
    {
        var project = App.State.Aktif;
        Toast("Mengambil data iklim historis…");
        try
        {
            var climate = await OpenMeteo.AmbilIklimAsync(project.Latitude, project.Longitude);
            var profile = project.ProfilIklim ??= Katalog.ProfilBawaan(project.Kota, 30);
            profile.CurahHujanTahunan = climate.CurahHujan;
            profile.SuhuRata = climate.Suhu;
            profile.KelembapanRata = climate.Kelembapan;
            profile.Sumber = "Open-Meteo Historical Weather API (arsip model/reanalisis), jarak pesisir input manual";
            profile.DiperbaruiPada = Format.WaktuLokal(DateTime.Now);
            Render();
            Toast("Profil iklim diperbarui dari Open-Meteo.");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidDataException or JsonException)
        {
            var reason = e switch
            {
                TaskCanceledException => "signal timed out",
                HttpRequestException { StatusCode: { } code } => "HTTP " + (int)code,
                HttpRequestException => "Failed to fetch",
                _ => e.Message
            };
            Toast("Gagal mengambil iklim: " + reason + ". Data sebelumnya tetap tersedia.");
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
        _page?.Refresh();
    }

    private void Nav_Click(object sender, RoutedEventArgs e) => Go((string)((FrameworkElement)sender).Tag);

    private void Go_Click(object sender, RoutedEventArgs e) => Go((string)((FrameworkElement)sender).Tag);

    // Langkah gulir ~100 px per notch seperti Chromium (bawaan WPF 48 px).
    private void Scroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - e.Delta * 100.0 / 120);
        e.Handled = true;
    }
}
