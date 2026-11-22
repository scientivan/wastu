using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wastu.Domain.Weather;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class SchedulePage : PageBase
{
    private int? _lastProjectId;
    private readonly OperationViewModel _commands = new();

    public SchedulePage() => InitializeComponent();

    protected override void Refresh()
    {
        // ≤1200 px: .scheduler-grid menjadi satu kolom (Bug 1 layout stacked).
        var stacked = Viewport.Width <= 1200;
        Grid.SetColumn(PlanPanel, stacked ? 0 : 2);
        Grid.SetRow(PlanPanel, stacked ? 2 : 0);
        Grid.SetColumnSpan(PlanPanel, stacked ? 3 : 1);
        Grid.SetColumnSpan(WeatherPanel, stacked ? 3 : 1);
        GapRow.Height = new GridLength(stacked ? 20 : 0);
        PlanColumn.MinWidth = stacked ? 0 : 310;
        PlanColumn.Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        GapColumn.Width = new GridLength(stacked ? 0 : 20);

        var painting = S.JenisKerja == Models.JenisPekerjaan.Pengecatan;
        PaintTab.IsChecked = painting;
        CastTab.IsChecked = !painting;

        var forecast = S.Workflows.CalendarDays();
        if (_lastProjectId != S.Aktif.Id)
        {
            _lastProjectId = S.Aktif.Id;
            JobName.Text = painting ? "Pengecatan eksterior" : "Pengecoran dinding";
            JobKind.SelectedIndex = painting ? 0 : 1;
            var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, S.Aktif.ZonaWaktu)
                .ToDateTime(TimeOnly.MinValue);
            JobStart.SelectedDate = today;
            JobEnd.SelectedDate = today.AddDays(6);
            SyncOverrideInputs(painting ? Models.JenisPekerjaan.Pengecatan : Models.JenisPekerjaan.Pengecoran);
        }

        DataContext = new ScheduleData(S, forecast);
    }

    private void SyncOverrideInputs(Models.JenisPekerjaan jenis)
    {
        var ambang = AmbangLayakKerja.Untuk(jenis == Models.JenisPekerjaan.Pengecatan
            ? Domain.Weather.JenisPekerjaan.Pengecatan
            : Domain.Weather.JenisPekerjaan.Pengecoran);

        OverrideRainProb.Text = ambang.ProbabilitasHujanMaksPct?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        OverrideRainMm.Text = ambang.CurahHujanMaksMm?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        OverrideRhMax.Text = ambang.KelembapanMaksPct?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        OverrideTempMin.Text = ambang.SuhuMinC?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
        OverrideTempMax.Text = ambang.SuhuMaksC?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
    }

    private Models.PrakiraanCuaca? SelectedDay() =>
        S.HariDipilih is { } day ? S.Workflows.CalendarDays().Find(d => d.Tanggal == day) : null;

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await Shell.RefreshForecastAsync();

    private void Work_Click(object sender, RoutedEventArgs e)
    {
        S.JenisKerja = Enum.Parse<Models.JenisPekerjaan>(TagOf(sender));
        Shell.Render();
    }

    private void JobKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (JobKind.SelectedItem is ComboBoxItem item && item.Tag is string tagStr &&
            Enum.TryParse<Models.JenisPekerjaan>(tagStr, out var jenis))
        {
            SyncOverrideInputs(jenis);

        }
    }

    private void ResetOverride_Click(object sender, RoutedEventArgs e)
    {
        var jenis = JobKind.SelectedIndex == 0 ? Models.JenisPekerjaan.Pengecatan : Models.JenisPekerjaan.Pengecoran;
        SyncOverrideInputs(jenis);

        Shell.Toast("Ambang dikembalikan ke asumsi generik default.");
    }

    private void Day_Click(object sender, RoutedEventArgs e)
    {
        S.HariDipilih = DateTime.Parse(TagOf(sender), CultureInfo.InvariantCulture);
        Shell.Render();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (S.ModeBacaSaja)
        {
            Shell.Toast("Mode luring baca-saja: jadwal pekerjaan tidak dapat ditambah.");
            return;
        }
        if (string.IsNullOrWhiteSpace(JobName.Text) || JobStart.SelectedDate is not { } start || JobEnd.SelectedDate is not { } end)
        {
            Shell.Toast("Harap isi bidang ini.");
            return;
        }
        if (start > end)
        {
            Shell.Toast("Tanggal mulai harus sebelum tanggal selesai.");
            return;
        }

        var jenis = Enum.Parse<Models.JenisPekerjaan>((string)((ComboBoxItem)JobKind.SelectedItem).Tag);

        // Parsing override ambang jika diubah oleh pengguna (X11) dan validasi non-numerik (Bug 3)
        AmbangPekerjaan? overrideAmbang = null;
        try
        {
            overrideAmbang = ScheduleViewModel.ParseOverride(jenis, OverrideRainProb.Text,
                OverrideRainMm.Text, OverrideRhMax.Text, OverrideTempMin.Text, OverrideTempMax.Text);
        }
        catch (Exception ex)
        {
            Shell.Toast("Nilai ambang tidak valid: " + ex.Message);
            return;
        }

        var job = new Models.Pekerjaan
        {
            Nama = JobName.Text,
            Jenis = jenis,
            TanggalMulaiRencana = start.Date,
            TanggalSelesaiRencana = end.Date,
            OverrideAmbang = overrideAmbang
        };
        if (await _commands.RunAsync(ct => S.Workflows.AddJobAsync(job, ct)))
        {
            Shell.Render();
            Shell.Toast("Pekerjaan ditambahkan. Pilih tanggal untuk mengunci.");
        }
        else Shell.Toast(_commands.Error ?? "Pekerjaan belum tersimpan.");
    }

    private async void Lock_Click(object sender, RoutedEventArgs e)
    {
        if (_commands.IsBusy || S.ModeBacaSaja) return;
        var project = S.Aktif;
        var job = project.Pekerjaan[(int)((FrameworkElement)sender).Tag];
        if (SelectedDay() is not { } day)
        {
            Shell.Toast("Pilih tanggal terlebih dahulu pada kalender.");
            return;
        }
        var date = DateOnly.FromDateTime(day.Tanggal);
        if (await _commands.RunAsync(async ct =>
        {
            var days = await S.Workflows.EvaluateJobAsync(job.Id, ct);
            var evaluation = days.FirstOrDefault(d => d.Tanggal == date);
            if (evaluation is null || evaluation.Status == StatusKelayakanCuaca.DataTidakTersedia)
            {
                Shell.Toast("Tanggal ini belum memiliki prakiraan lengkap dalam rentang pekerjaan; hanya dapat disimpan sebagai draf.");
                return;
            }
            bool risk = evaluation.Status == StatusKelayakanCuaca.TidakLayak;
            if (risk && !Confirm("Risiko cuaca:\n" + string.Join("\n", evaluation.Alasan)
                + "\n\nTetap kunci tanggal dengan memahami risiko ini?")) return;
            if (evaluation.DataLama && !Confirm("Prakiraan sudah kedaluwarsa atau waktu pengambilannya tidak valid. Tetap kunci berdasarkan data lama?")) return;
            if (!ReferenceEquals(S.Aktif, project)) return;
            await S.Workflows.LockJobAsync(job.Id, date,
                new KonfirmasiPenguncian(risk, evaluation.DataLama), ct);
            Shell.Render();
            Shell.Toast("Jadwal terkunci untuk " + Format.Iso(day.Tanggal));
        })) return;
        Shell.Toast(_commands.Error ?? "Jadwal belum tersimpan.");
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (S.ModeBacaSaja)
        {
            Shell.Toast("Mode luring baca-saja: pekerjaan tidak dapat dihapus.");
            return;
        }
        if (!Confirm("Hapus pekerjaan ini?")) return;
        var id = S.Aktif.Pekerjaan[(int)((FrameworkElement)sender).Tag].Id;
        if (await _commands.RunAsync(ct => S.Workflows.DeleteJobAsync(id, ct))) Shell.Render();
        else Shell.Toast(_commands.Error ?? "Pekerjaan belum dihapus.");
    }

    private bool Confirm(string message) =>
        MessageBox.Show(Window.GetWindow(this)!, message, "Wastu", MessageBoxButton.OKCancel) == MessageBoxResult.OK;
}

public sealed class ScheduleData : ViewData
{
    public ScheduleData(AppState s, List<Models.PrakiraanCuaca> forecast)
    {
        var has = forecast.Count > 0;
        var day = s.HariDipilih is { } d ? forecast.Find(x => x.Tanggal == d) : null;
        Title = "Cuaca " + s.Aktif.Kota;
        HasForecast = has ? Visibility.Visible : Visibility.Collapsed;
        NoForecast = has ? Visibility.Collapsed : Visibility.Visible;

        // Banner cache lama (>= 6 jam)
        bool isStale = s.PrakiraanDataLama && s.PrakiraanAktif().Count > 0;
        StaleBannerVisibility = isStale ? Visibility.Visible : Visibility.Collapsed;
        StaleBannerText = isStale
            ? $"Data prakiraan cuaca berusia ≥ 6 jam (terakhir dimuat {s.PrakiraanDiperbarui}). Perbarui prakiraan untuk akurasi terkini."
            : "";

        Days = forecast.ConvertAll(x =>
        {
            var domainEval = x.EvaluasiDomain(s.JenisKerja);
            return new DayCell(x, domainEval.Status, x.Tanggal == s.HariDipilih);
        });

        HasDay = day is null ? Visibility.Collapsed : Visibility.Visible;
        DayTitle = day is null ? "Pilih tanggal" : Format.Tanggal(day.Tanggal);

        if (day is not null)
        {
            var eval = day.EvaluasiDomain(s.JenisKerja);
            DayText = string.Join("; ", eval.Alasan);
            DayRain = day.DataLengkap ? Format.Angka(day.CurahHujan) + " mm" : "—";
            DayHumidity = day.DataLengkap ? Format.Angka(day.Kelembapan) + "%" : "—";
            DayTemperature = day.DataLengkap ? $"{Format.Angka(day.SuhuMin)} hingga {Format.Angka(day.SuhuMaks)}°C" : "—";
            DayOutOfRangeVisibility = Visibility.Collapsed;
        }
        else
        {
            DayText = "Pilih hari untuk melihat kelayakan pekerjaan dan kondisi cuaca.";
            DayOutOfRangeVisibility = Visibility.Collapsed;
        }

        Updated = "Diperbarui " + s.PrakiraanDiperbarui;
        UpdatedMargin = new Thickness(0, day is null ? 14 : 17, 0, 0);

        var jobs = s.Aktif.Pekerjaan;
        JobCount = jobs.Count + " pekerjaan";
        Jobs = jobs.Select((j, i) => new JobRow(j, i, day is not null, forecast)).ToList();
        NoJobs = jobs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public string Title { get; }
    public Visibility HasForecast { get; }
    public Visibility NoForecast { get; }
    public Visibility StaleBannerVisibility { get; }
    public string StaleBannerText { get; }
    public List<DayCell> Days { get; }
    public Visibility HasDay { get; }
    public Visibility DayOutOfRangeVisibility { get; }
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

public sealed class DayCell : ViewData
{
    public DayCell(Models.PrakiraanCuaca day, StatusKelayakanCuaca status, bool selected)
    {
        Key = Format.Iso(day.Tanggal);
        Weekday = Format.HariSingkat(day.Tanggal);
        Date = Format.TanggalSingkat(day.Tanggal);
        Sky = !day.DataLengkap ? "—" : day.ProbabilitasHujan > 40 ? "☁" : "☀";
        Rain = day.DataLengkap ? "Hujan " + Format.Angka(day.ProbabilitasHujan) + "%" : "Belum tersedia";

        Status = status switch
        {
            StatusKelayakanCuaca.TidakLayak => "Tidak layak",
            StatusKelayakanCuaca.Waspada => "Waspada",
            StatusKelayakanCuaca.DataTidakTersedia => "Data tdk ada",
            _ => "Layak"
        };

        Background = status switch
        {
            StatusKelayakanCuaca.TidakLayak => Palette.DayBadBg,
            StatusKelayakanCuaca.Waspada => Palette.DayWarnBg,
            StatusKelayakanCuaca.DataTidakTersedia => Palette.TagPaleBg,
            _ => Palette.DayGoodBg
        };

        Border = status switch
        {
            StatusKelayakanCuaca.TidakLayak => Palette.DayBadBorder,
            StatusKelayakanCuaca.Waspada => Palette.DayWarnBorder,
            StatusKelayakanCuaca.DataTidakTersedia => Palette.Line,
            _ => Palette.DayGoodBorder
        };

        Ring = selected ? Palette.Green : Brushes.Transparent;
    }

    public string Key { get; }
    public string Weekday { get; }
    public string Date { get; }
    public string Sky { get; }
    public string Rain { get; }
    public string Status { get; }
    public Brush Background { get; }
    public Brush Border { get; }
    public Brush Ring { get; }
}

public sealed class JobRow : ViewData
{
    public JobRow(Models.Pekerjaan job, int index, bool canLock, List<Models.PrakiraanCuaca> forecast)
    {
        Index = index;
        Name = job.Nama;
        Range = $"{Format.Tanggal(job.TanggalMulaiRencana)} hingga {Format.Tanggal(job.TanggalSelesaiRencana)}";
        Locked = job.TanggalDikunci is { } t ? "Tanggal terkunci: " + Format.Tanggal(t) : "Tanggal belum dikunci";
        CanLock = canLock && !App.State.ModeBacaSaja && !App.State.ButuhPemulihan;

        // Deteksi kondisi Perlu Ditinjau (T31)
        bool memburuk = false;
        string alasanTinjau = "";
        if (job.TanggalDikunci.HasValue)
        {
            var lockedDay = forecast.Find(d => d.Tanggal == job.TanggalDikunci.Value);
            if (lockedDay is not null)
            {
                var currentEval = lockedDay.EvaluasiDomain(job.Jenis, job.OverrideAmbang);
                if (currentEval.Status == StatusKelayakanCuaca.TidakLayak && !job.PersetujuanRisiko)
                {
                    memburuk = true;
                    alasanTinjau = "Prakiraan cuaca memburuk menjadi Tidak Layak (" + string.Join("; ", currentEval.Alasan) + ")";
                }
            }
        }

        bool perluTinjau = job.Status == "Perlu ditinjau" || memburuk;


        NeedsReviewVisibility = perluTinjau ? Visibility.Visible : Visibility.Collapsed;
        ReviewWarning = string.IsNullOrWhiteSpace(alasanTinjau)
            ? "Perlu ditinjau ulang: kondisi cuaca atau parameter pekerjaan telah berubah dari keputusan awal."
            : alasanTinjau;
    }

    public int Index { get; }
    public string Name { get; }
    public string Range { get; }
    public string Locked { get; }
    public bool CanLock { get; }
    public Visibility NeedsReviewVisibility { get; }
    public string ReviewWarning { get; }
}
