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
    private readonly ScheduleViewModel _forecast = new();
    private readonly OperationViewModel _climate = new();
    private AppState? _observedState;
    private void StateChanged() => Dispatcher.InvokeAsync(() =>
    {
        if (CurrentPage is "home" or "material" or "compare" or "sim" or "report") Show(force: false);
        else if (CurrentPage == "project" && _page is ProjectPage project && App.State.AdaProyek)
            project.RefreshClimateDisplay(); // Update the climate card without overwriting form inputs.
    });
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
        if (CurrentPage != page) { _forecast.Cancel(); _climate.Cancel(); }
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
        if (CurrentPage == "weather") _ = RefreshForecastAsync(forceRefresh: false);
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
        if (!ReferenceEquals(_observedState, state))
        {
            if (_observedState is not null) _observedState.Changed -= StateChanged;
            _observedState = state;
            state.Changed += StateChanged;
        }
        if (!state.AdaProyek) return;
        var project = state.Aktif;
        foreach (RadioButton item in Nav.Children)
            item.IsChecked = (string)item.Tag == CurrentPage;
        StatusName.Text = project.Nama;
        StatusCity.Text = project.Kota;
        Context.Text = project.Nama;
        LocationText.Text = project.Kota;
        _window.Title = Titles[CurrentPage] + " | Wastu";

        // Indikator mode penyimpanan & banner luring
        StorageModeText.Text = state.LabelModePenyimpanan;
        if (state.ModeBacaSaja)
        {
            OfflineBanner.Visibility = Visibility.Visible;
            var waktuStr = state.SnapshotDiambilPadaUtc is { } dt
                ? Format.WaktuLokal(dt.LocalDateTime) + " WIB"
                : "-";
            var cadanganStr = state.DipulihkanDariCadangan ? " (Dipulihkan dari cadangan)" : "";
            OfflineBannerDescription.Text =
                $"Perubahan tidak disimpan ke database. Menampilkan snapshot per {waktuStr}.{cadanganStr}";
            StorageModeBorder.Background = (System.Windows.Media.Brush)FindResource("WarnBg");
            StorageModeText.Foreground = (System.Windows.Media.Brush)FindResource("Red");
        }
        else
        {
            OfflineBanner.Visibility = Visibility.Collapsed;
            if (state.PostgreSql)
            {
                StorageModeBorder.Background = (System.Windows.Media.Brush)FindResource("TagBg");
                StorageModeText.Foreground = (System.Windows.Media.Brush)FindResource("TagFg");
            }
            else
            {
                StorageModeBorder.Background = (System.Windows.Media.Brush)FindResource("TagPaleBg");
                StorageModeText.Foreground = (System.Windows.Media.Brush)FindResource("TagPaleFg");
            }
        }

        _page?.Show(force);
    }

    private async void Reconnect_Click(object sender, RoutedEventArgs e)
    {
        if (!ReconnectButton.IsEnabled) return;
        ReconnectButton.IsEnabled = false;
        ReconnectButtonText.Text = "Menghubungkan...";
        Toast("Mencoba menyambung ulang ke database PostgreSQL...");

        try
        {
            var newState = await Task.Run(() => AppState.MuatAsync());
            App.State = newState;

            if (newState.ModeBacaSaja)
            {
                Toast("Database masih belum dapat dihubungi. Mode luring tetap aktif.");
            }
            else
            {
                Toast("Berhasil terhubung kembali ke PostgreSQL.");
            }
            Show(force: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Toast("Gagal menyambung ulang ke database.");
        }
        finally
        {
            ReconnectButton.IsEnabled = true;
            ReconnectButtonText.Text = "Coba Sambung Ulang";
        }
    }

    public void Toast(string message) => _window.Toast(message);

    // getForecast() pada app.js
    public async Task RefreshForecastAsync(bool forceRefresh = true)
    {
        if (_forecast.IsBusy) { if (forceRefresh) _forecast.Cancel(); return; }
        var state = App.State;
        var project = state.Aktif;
        Toast("Mengambil prakiraan Open-Meteo… Tekan Perbarui lagi untuk membatalkan.");
        bool success = await _forecast.RefreshAsync(state.Workflows, forceRefresh);
        if (!ReferenceEquals(App.State, state) || !ReferenceEquals(state.Aktif, project)) return;
        Show(force: true);
        Toast(success ? state.PrakiraanDataLama
            ? "Data lama ditampilkan. Prakiraan belum berhasil diperbarui."
            : "Prakiraan harian tersedia."
            : _forecast.Error ?? "Prakiraan belum tersedia.");
    }

    public async Task RefreshClimateAsync()
    {
        if (App.State.ModeBacaSaja) return;
        if (_climate.IsBusy) { _climate.Cancel(); return; }
        var state = App.State;
        var project = state.Aktif;
        string? message = null;
        Toast("Mengambil iklim historis… Tekan Perbarui iklim lagi untuk membatalkan.");
        bool success = await _climate.RunAsync(async ct => message = await state.PerbaruiIklimAsync(project, ct));
        if (!ReferenceEquals(state, App.State) || !ReferenceEquals(project, state.Aktif)) return;
        Render();
        Toast(success ? message ?? "Profil iklim diperbarui dari Open-Meteo." : _climate.Error ?? "Iklim belum tersedia.");
    }

    public async Task RunProjectChangeAsync(Func<Task> action)
    {
        if (App.State.ModeBacaSaja)
        {
            Toast("Mode luring baca-saja: perubahan proyek tidak dapat disimpan karena database sedang tidak tersedia.");
            return;
        }
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

    public void NewProject()
    {
        if (App.State.ModeBacaSaja)
        {
            Toast("Mode luring baca-saja: tidak dapat membuat proyek baru saat database tidak tersedia.");
            return;
        }
        _window.ShowCreate();
    }

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
