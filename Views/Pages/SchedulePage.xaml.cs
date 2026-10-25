using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class SchedulePage : PageBase
{
    public SchedulePage() => InitializeComponent();

    protected override void Refresh()
    {
        // ≤1200 px: .scheduler-grid menjadi satu kolom.
        var stacked = Viewport.Width <= 1200;
        Grid.SetColumn(PlanPanel, stacked ? 0 : 2);
        Grid.SetRow(PlanPanel, stacked ? 2 : 0);
        Grid.SetColumnSpan(PlanPanel, stacked ? 3 : 1);
        Grid.SetColumnSpan(WeatherPanel, stacked ? 3 : 1);
        GapRow.Height = new GridLength(stacked ? 20 : 0);
        PlanColumn.MinWidth = stacked ? 0 : 310;
        PlanColumn.Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        GapColumn.Width = new GridLength(stacked ? 0 : 20);

        var painting = S.JenisKerja == JenisPekerjaan.Pengecatan;
        PaintTab.IsChecked = painting;
        CastTab.IsChecked = !painting;

        var forecast = S.PrakiraanAktif();
        JobName.Text = painting ? "Pengecatan eksterior" : "Pengecoran dinding";
        JobKind.SelectedIndex = painting ? 0 : 1;
        JobStart.SelectedDate = forecast.Count > 0 ? forecast[0].Tanggal : DateTime.Today;
        JobEnd.SelectedDate = forecast.Count > 0 ? forecast[^1].Tanggal : DateTime.Today.AddDays(6);
        DataContext = new ScheduleData(S, forecast);
    }

    private PrakiraanCuaca? SelectedDay() =>
        S.HariDipilih is { } day ? S.PrakiraanAktif().Find(d => d.Tanggal == day) : null;

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await Shell.RefreshForecastAsync();

    private void Work_Click(object sender, RoutedEventArgs e)
    {
        S.JenisKerja = Enum.Parse<JenisPekerjaan>(TagOf(sender));
        Shell.Render();
    }

    private void Day_Click(object sender, RoutedEventArgs e)
    {
        S.HariDipilih = DateTime.Parse(TagOf(sender), System.Globalization.CultureInfo.InvariantCulture);
        Shell.Render();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (JobName.Text.Length == 0 || JobStart.SelectedDate is not { } start || JobEnd.SelectedDate is not { } end)
        {
            Shell.Toast("Harap isi bidang ini.");
            return;
        }
        if (start > end)
        {
            Shell.Toast("Tanggal mulai harus sebelum tanggal selesai.");
            return;
        }
        var job = new Pekerjaan
        {
            Nama = JobName.Text,
            Jenis = Enum.Parse<JenisPekerjaan>((string)((ComboBoxItem)JobKind.SelectedItem).Tag),
            TanggalMulaiRencana = start.Date,
            TanggalSelesaiRencana = end.Date
        };
        job.PulihkanJadwal(null);
        S.Aktif.JadwalkanPekerjaan(job);
        Shell.Render();
        Shell.Toast("Pekerjaan ditambahkan. Pilih tanggal untuk mengunci.");
    }

    private void Lock_Click(object sender, RoutedEventArgs e)
    {
        var job = S.Aktif.Pekerjaan[(int)((FrameworkElement)sender).Tag];
        if (SelectedDay() is not { } day) return;
        if (day.Tanggal < job.TanggalMulaiRencana || day.Tanggal > job.TanggalSelesaiRencana)
        {
            Shell.Toast("Tanggal terpilih harus masuk rentang pekerjaan.");
            return;
        }
        var check = day.EvaluasiKelayakan(job.Jenis);
        if (check.Status == StatusKelayakan.TidakLayak && !Confirm("Cuaca tidak layak: " + check.Alasan + ". Tetap kunci tanggal?"))
            return;
        job.Jadwalkan(day, tetapKunci: true);
        Shell.Render();
        Shell.Toast("Jadwal " + job.Nama + " terkunci untuk " + Format.Iso(day.Tanggal));
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (!Confirm("Hapus pekerjaan ini?")) return;
        S.Aktif.HapusPekerjaan(S.Aktif.Pekerjaan[(int)((FrameworkElement)sender).Tag]);
        Shell.Render();
    }

    private bool Confirm(string message) =>
        MessageBox.Show(Window.GetWindow(this)!, message, "Wastu", MessageBoxButton.OKCancel) == MessageBoxResult.OK;
}

public sealed class ScheduleData : ViewData
{
    public ScheduleData(AppState s, List<PrakiraanCuaca> forecast)
    {
        var has = forecast.Count > 0;
        var day = s.HariDipilih is { } d ? forecast.Find(x => x.Tanggal == d) : null;
        Title = "Cuaca " + s.Aktif.Kota;
        HasForecast = has ? Visibility.Visible : Visibility.Collapsed;
        NoForecast = has ? Visibility.Collapsed : Visibility.Visible;
        Days = forecast.ConvertAll(x => new DayCell(x, x.EvaluasiKelayakan(s.JenisKerja).Status, x.Tanggal == s.HariDipilih));

        HasDay = day is null ? Visibility.Collapsed : Visibility.Visible;
        DayTitle = day is null ? "Pilih tanggal" : Format.Tanggal(day.Tanggal);
        DayText = day is null
            ? "Pilih hari untuk melihat kelayakan pekerjaan dan kondisi cuaca."
            : day.EvaluasiKelayakan(s.JenisKerja).Alasan;
        if (day is not null)
        {
            DayRain = Format.Angka(day.CurahHujan) + " mm";
            DayHumidity = Format.Angka(day.Kelembapan) + "%";
            DayTemperature = $"{Format.Angka(day.SuhuMin)} hingga {Format.Angka(day.SuhuMaks)}°C";
        }
        Updated = "Diperbarui " + s.PrakiraanDiperbarui;
        // Margin bawah .weather-facts (17) dan atas .data-caption (14) saling melebur.
        UpdatedMargin = new Thickness(0, day is null ? 14 : 17, 0, 0);

        var jobs = s.Aktif.Pekerjaan;
        JobCount = jobs.Count + " pekerjaan";
        Jobs = jobs.Select((j, i) => new JobRow(j, i, day is not null)).ToList();
        NoJobs = jobs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public string Title { get; }
    public Visibility HasForecast { get; }
    public Visibility NoForecast { get; }
    public List<DayCell> Days { get; }
    public Visibility HasDay { get; }
    public string DayTitle { get; }
    public string DayText { get; }
    public string DayRain { get; } = "";
    public string DayHumidity { get; } = "";
    public string DayTemperature { get; } = "";
    public string Updated { get; }
    public Thickness UpdatedMargin { get; }
    public string JobCount { get; }
    public List<JobRow> Jobs { get; }
    public Visibility NoJobs { get; }
}

public sealed class DayCell(PrakiraanCuaca day, StatusKelayakan status, bool selected) : ViewData
{
    public string Key { get; } = Format.Iso(day.Tanggal);
    public string Weekday { get; } = Format.HariSingkat(day.Tanggal);
    public string Date { get; } = Format.TanggalSingkat(day.Tanggal);
    public string Sky { get; } = day.ProbabilitasHujan > 40 ? "☁" : "☀";
    public string Rain { get; } = "Hujan " + Format.Angka(day.ProbabilitasHujan) + "%";
    public string Status { get; } = status switch
    {
        StatusKelayakan.TidakLayak => "Tidak layak",
        StatusKelayakan.Waspada => "Waspada",
        _ => "Layak"
    };
    public Brush Background { get; } = status switch
    {
        StatusKelayakan.TidakLayak => Palette.DayBadBg,
        StatusKelayakan.Waspada => Palette.DayWarnBg,
        _ => Palette.DayGoodBg
    };
    public Brush Border { get; } = status switch
    {
        StatusKelayakan.TidakLayak => Palette.DayBadBorder,
        StatusKelayakan.Waspada => Palette.DayWarnBorder,
        _ => Palette.DayGoodBorder
    };
    public Brush Ring { get; } = selected ? Palette.Green : Brushes.Transparent;
}

public sealed class JobRow(Pekerjaan job, int index, bool canLock) : ViewData
{
    public int Index { get; } = index;
    public string Name { get; } = job.Nama;
    public string Range { get; } = $"{Format.Tanggal(job.TanggalMulaiRencana)} hingga {Format.Tanggal(job.TanggalSelesaiRencana)}";
    public string Locked { get; } = job.TanggalDikunci is { } t ? "Tanggal terkunci: " + Format.Tanggal(t) : "Tanggal belum dikunci";
    public bool CanLock { get; } = canLock;
}
