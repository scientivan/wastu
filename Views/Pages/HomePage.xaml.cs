using System.Windows;
using System.Windows.Media;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class HomePage : PageBase
{
    public HomePage() => InitializeComponent();

    protected override void Refresh()
    {
        var compact = Viewport.Width <= 1200;
        var wide = Viewport.Width >= 1700;
        LeftColumn.Width = new GridLength(compact ? 1.15 : 1.2, GridUnitType.Star);
        RightColumn.MinWidth = compact ? 260 : 290;
        GapColumn.Width = new GridLength(compact ? 23 : 30);
        NextActions.Padding = new Thickness(compact ? 22 : 28, 0, 0, 0);
        DataContext = new HomeData(S, compact, wide);
    }

    private async void Forecast_Click(object sender, RoutedEventArgs e)
    {
        if (S.PrakiraanAktif().Count > 0) Shell.Go("weather");
        else await Shell.RefreshForecastAsync();
    }
}

public sealed class HomeData : ViewData
{
    public HomeData(AppState s, bool compact, bool wide)
    {
        var p = s.Aktif;
        var jobs = p.Pekerjaan;
        var today = DateTime.Today;
        var next = jobs.Where(j => j.TanggalDikunci >= today).MinBy(j => j.TanggalDikunci);
        var forecast = s.PrakiraanAktif();

        Summary = "Ringkasan " + p.Nama;
        Wall = Format.Angka(p.LuasKomponen(JenisKomponen.Dinding)) + " ";
        Roof = Format.Angka(p.LuasKomponen(JenisKomponen.Atap)) + " ";
        Locked = jobs.Count(j => j.TanggalDikunci is not null).ToString();
        LockedSuffix = $" dari {jobs.Count} pekerjaan";
        AreaLabel = (s.Jenis == JenisKomponen.Dinding ? "Dinding " : "Atap ") + Format.Angka(s.Luas) + " m²";

        var imageHeight = wide ? 215 : compact ? 130 : 155;
        Materials = s.Terpilih().ConvertAll(item =>
        {
            var e = s.Nilai(item);
            return new ActiveMaterial(ImageCache.Cover(item.Gambar), item.Bahan.Nama, Format.Angka(e.SkorKesesuaian),
                Format.Angka(e.EmisiAwal) + " ", imageHeight, compact);
        });

        ClimateHint = p.ProfilIklim?.SudahDiperbarui == true
            ? "Profil iklim sudah tersedia."
            : "Perbarui kondisi iklim untuk lokasi proyek.";
        SimHint = $"Tinjau dampak material dalam {s.Horizon} tahun.";
        ScheduleHint = next is not null
            ? $"{next.Nama} pada {Format.Tanggal(next.TanggalDikunci!.Value)}"
            : "Susun rencana pengecoran dan pengecatan.";
        ForecastTitle = forecast.Count > 0 ? "Prakiraan tersedia" : "Prakiraan belum tersedia";
        ForecastText = forecast.Count > 0
            ? $"{Format.Tanggal(forecast[0].Tanggal)} hingga {Format.Tanggal(forecast[^1].Tanggal)}"
            : "Perbarui data untuk memeriksa cuaca di lokasi proyek.";
        ForecastAction = forecast.Count > 0 ? "Lihat prakiraan" : "Perbarui prakiraan";

        MetricPadding = compact ? new Thickness(16, 18, 16, 18) : new Thickness(23, 20, 23, 20);
        MetricIconMargin = new Thickness(0, 0, compact ? 12 : 19, 0);
        MetricSize = compact ? 25 : 29;
        MetricLineHeight = MetricSize * 1.1;
        MetricMinHeight = wide ? 111 : 101;
        WorkflowPadding = new Thickness(0, wide ? 25 : 20, 0, wide ? 25 : 20);
    }

    public string Summary { get; }
    public string Wall { get; }
    public string Roof { get; }
    public string Locked { get; }
    public string LockedSuffix { get; }
    public string AreaLabel { get; }
    public List<ActiveMaterial> Materials { get; }
    public string ClimateHint { get; }
    public string SimHint { get; }
    public string ScheduleHint { get; }
    public string ForecastTitle { get; }
    public string ForecastText { get; }
    public string ForecastAction { get; }
    public Thickness MetricPadding { get; }
    public Thickness MetricIconMargin { get; }
    public double MetricSize { get; }
    public double MetricLineHeight { get; }
    public double MetricMinHeight { get; }
    public Thickness WorkflowPadding { get; }
}

public sealed class ActiveMaterial(Brush image, string name, string score, string emission, double imageHeight, bool stacked) : ViewData
{
    public Brush Image { get; } = image;
    public string Name { get; } = name;
    public string Score { get; } = score;
    public string Emission { get; } = emission;
    public double ImageHeight { get; } = imageHeight;

    // ≤1200 px: nilai turun ke bawah label (dl > div { display: block }).
    public int ValueRow { get; } = stacked ? 1 : 0;
    public HorizontalAlignment ValueAlign { get; } = stacked ? HorizontalAlignment.Left : HorizontalAlignment.Right;
    public Thickness ValueMargin { get; } = stacked ? new Thickness(0, 6, 0, 0) : new Thickness(0);
    public double ValueSize => 16;
    public double SuffixSize => 11;
}
