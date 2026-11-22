using System.Windows;
using System.Windows.Controls;
using Wastu.Data.Snapshots;
using Wastu.Services;

namespace Wastu.Views;

public partial class RecoveryView : UserControl
{
    private readonly MainWindow _window;

    public RecoveryView(MainWindow window)
    {
        _window = window;
        InitializeComponent();
        Loaded += (_, _) => PerbaruiTampilan();
    }

    public void PerbaruiTampilan()
    {
        var state = App.State;
        StatusDiagnosisText.Text = state.PesanStatusKoneksi ??
            "Database tidak dapat dihubungi dan belum ada snapshot yang dapat digunakan.";

        var statusRinci = state.StatusSnapshot switch
        {
            SnapshotStatus.Missing => "Status snapshot: Belum ada snapshot lokal untuk profil koneksi ini.",
            SnapshotStatus.Corrupt => "Status snapshot: Berkas snapshot rusak atau tidak terbaca (berkas tetap dipertahankan).",
            SnapshotStatus.UnsupportedVersion => "Status snapshot: Versi skema snapshot tidak kompatibel dengan aplikasi ini.",
            SnapshotStatus.ProfileMismatch => "Status snapshot: Snapshot milik profil database/koneksi yang berbeda.",
            _ => "Status snapshot: Tidak tersedia."
        };

        SnapshotStatusDetailText.Text = statusRinci;
        ActionStatusText.Text = "";
    }

    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        RetryButton.IsEnabled = false;
        LocalModeButton.IsEnabled = false;
        RetryButtonText.Text = "Menghubungi database...";
        ActionStatusText.Text = "Mencoba memuat ulang data dari database...";

        try
        {
            var state = await Task.Run(() => AppState.MuatAsync());
            App.State = state;

            if (state.ButuhPemulihan)
            {
                PerbaruiTampilan();
                ActionStatusText.Text = "Koneksi database masih belum berhasil. Silakan periksa kembali konfigurasi.";
            }
            else
            {
                if (state.AdaProyek)
                    _window.ShowShell("home");
                else
                    _window.ShowWelcome();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ActionStatusText.Text = "Terjadi kendala saat mencoba menyambung ulang.";
        }
        finally
        {
            RetryButton.IsEnabled = true;
            LocalModeButton.IsEnabled = true;
            RetryButtonText.Text = "Coba Lagi";
        }
    }

    private async void LocalMode_Click(object sender, RoutedEventArgs e)
    {
        RetryButton.IsEnabled = false;
        LocalModeButton.IsEnabled = false;
        ActionStatusText.Text = "Membuka mode kompatibilitas JSON lokal...";

        try
        {
            var state = await Task.Run(() => AppState.MuatAsync(paksaJsonLokal: true));
            App.State = state;

            if (state.ButuhPemulihan)
            {
                PerbaruiTampilan();
                ActionStatusText.Text = "Gagal memuat mode JSON lokal. Berkas snapshot atau state.json lokal mungkin rusak.";
            }
            else
            {
                if (state.AdaProyek)
                    _window.ShowShell("home");
                else
                    _window.ShowWelcome();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ActionStatusText.Text = "Terjadi kendala saat membuka mode JSON lokal.";
        }
        finally
        {
            RetryButton.IsEnabled = true;
            LocalModeButton.IsEnabled = true;
        }
    }
}
