#if DEBUG || SNAPSHOT
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;
using Wastu.Services;

namespace Wastu.Views;

// Windows-only checks against the real WPF control tree, isolated from ordinary startup.
internal static partial class DebugSnapshot
{
    private static T VisibleNamed<T>(string name) where T : FrameworkElement =>
        All<T>(_root!).First(e => e.Name == name && e.IsVisible);

    private static async Task AcceptanceAsync(MainWindow window, string folder, string mode)
    {
        var log = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            log.Add("PASS " + name);
        }
        async Task Shot(string name)
        {
            await Idle(window);
            Save(Path.Combine(folder, name + ".png"));
        }
        try
        {
            if (mode == "acceptance-restart")
            {
                Check(App.State.Proyek.Count == 3, "Three projects persisted across process restart");
                Check(App.State.Aktif.FaktorPenuaanA == 0.7, "Ageing factor persisted");
                Check(App.State.Aktif.Laporan.Count > 0, "PDF history persisted");
                Check(App.State.Aktif.Pekerjaan.Any(j => j.Keputusan?.PersetujuanRisiko == true), "Risk decision persisted");
                Check(App.State.Aktif.RiwayatSimulasi.Select(s => s.Hasil.HorizonTahun).Distinct().Count() == 3,
                    "20/30/50 year history persisted");
                window.ShowShell("report");
                await Shot("restart-report");
                return;
            }
            if (mode == "acceptance-recovery")
            {
                Check(App.State.ButuhPemulihan, "Disconnected database without snapshot opens recovery");
                await Shot("recovery");
                return;
            }
            if (mode is "acceptance-offline" or "acceptance-backup")
            {
                Check(App.State.ModeBacaSaja, "Disconnected database opens snapshot read-only");
                if (mode == "acceptance-backup") Check(App.State.DipulihkanDariCadangan, "Corrupt primary recovers valid backup");
                window.ShowShell("compare");
                await Idle(window);
                Check(!VisibleNamed<RadioButton>("MvpWeightRadio").IsEnabled, "Read-only weight controls disabled");
                await Shot("offline-compare");
                var codes = App.State.Terpilih().Select(i => i.Kode).ToHashSet();
                var exported = await App.State.Workflows.ExportAsync(ReportSnapshotFactory.Build(
                    App.State, App.State.Jenis, 30, true, codes), Path.Combine(folder, "offline-report.pdf"));
                Check(exported.Status == "MenungguPencatatan", "Offline PDF is available with pending metadata");
                window.ShowShell("report");
                await Shot("offline-report");
                return;
            }
            if (mode == "acceptance-online")
            {
                Check(App.State.PostgreSql && !App.State.ModeBacaSaja, "Online database startup");
                Check(App.State.Aktif.Pekerjaan.Any(j => j.Keputusan is not null), "Database decision loaded by WPF");
                Check(App.State.Aktif.Laporan.Count > 0, "Database report history loaded by WPF");
                foreach (var page in new[] { "home", "sim", "weather", "report" })
                { window.ShowShell(page); await Shot("online-" + page); }
                return;
            }
            if (mode == "acceptance-live")
            {
                window.ShowCreate();
                await Idle(window);
                VisibleNamed<TextBox>("NameBox").Text = "Iklim nyata Windows";
                Click(VisibleNamed<Button>("NextButton"));
                VisibleNamed<ComboBox>("CityBox").SelectedItem = "Jakarta";
                Click(VisibleNamed<Button>("NextButton"));
                VisibleNamed<CheckBox>("RoofCheck").IsChecked = false;
                VisibleNamed<TextBox>("WallBox").Text = "120";
                var watch = Stopwatch.StartNew();
                Click(VisibleNamed<Button>("NextButton"));
                while (watch.Elapsed < TimeSpan.FromSeconds(75)
                    && (!App.State.AdaProyek || App.State.Aktif.ProfilIklim?.Provenance != ProvenanceIklim.ApiHistoris))
                    await Task.Delay(100);
                Check(App.State.Aktif.ProfilIklim?.Provenance == ProvenanceIklim.ApiHistoris, "Real historical API reaches WPF state");
                window.ShowShell("compare");
                await Idle(window);
                log.Add("Submit-to-real-climate-ranking-render-ms " + watch.Elapsed.TotalMilliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
                await Shot("live-ranking");
                return;
            }

            var climateClient = new AcceptanceClimate();
            App.State.HistoricalClient = climateClient;
            foreach (var kind in new[] { "wall", "roof", "both" })
            {
                int before = App.State.Proyek.Count;
                window.ShowCreate();
                await Idle(window);
                Click(VisibleNamed<Button>("NextButton"));
                Check(VisibleNamed<StackPanel>("Step0").IsVisible, kind + ": empty name rejected");
                VisibleNamed<TextBox>("NameBox").Text = "Rumah Windows " + kind;
                Click(VisibleNamed<Button>("NextButton"));
                VisibleNamed<TextBox>("LatBox").Text = "-12";
                Click(VisibleNamed<Button>("NextButton"));
                Check(VisibleNamed<StackPanel>("Step1").IsVisible, kind + ": unsupported latitude rejected");
                VisibleNamed<TextBox>("LatBox").Text = "-6,2";
                VisibleNamed<TextBox>("LonBox").Text = "106,8";
                Click(VisibleNamed<Button>("NextButton"));
                var wall = VisibleNamed<CheckBox>("WallCheck");
                var roof = VisibleNamed<CheckBox>("RoofCheck");
                wall.IsChecked = roof.IsChecked = false;
                Click(VisibleNamed<Button>("NextButton"));
                Check(App.State.Proyek.Count == before, kind + ": no components rejected");
                wall.IsChecked = kind != "roof";
                roof.IsChecked = kind != "wall";
                Click(wall); Click(roof);
                foreach (var invalid in new[] { "0", "-1", "NaN", "Infinity" })
                {
                    VisibleNamed<TextBox>(kind == "roof" ? "RoofBox" : "WallBox").Text = invalid;
                    Click(VisibleNamed<Button>("NextButton"));
                    Check(App.State.Proyek.Count == before, kind + ": area " + invalid + " rejected");
                }
                if (kind != "roof") VisibleNamed<TextBox>("WallBox").Text = "120";
                if (kind != "wall") VisibleNamed<TextBox>("RoofBox").Text = "90";
                Click(VisibleNamed<Button>("NextButton"));
                await Idle(window);
                Check(App.State.Proyek.Count == before + 1, kind + ": project created");
                Check(App.State.Aktif.Komponen.Count == (kind == "both" ? 2 : 1), kind + ": correct component count");
                foreach (var page in new[] { "home", "project", "material", "compare", "sim", "weather", "report" })
                {
                    if (page == "weather") SeedForecast();
                    window.ShowShell(page);
                    await Shot(kind + "-" + page);
                }
            }

            window.ShowShell("project");
            await Idle(window);
            foreach (var project in App.State.Proyek)
            {
                Click(Labeled(project.Nama + " " + project.Kota));
                await Idle(window);
                Check(App.State.Aktif.Id == project.Id && project.AdaKomponen(App.State.Jenis),
                    "Project switch selects an available component: " + project.Nama);
            }
            var activeProject = App.State.Aktif;
            int locationRevision = activeProject.RevisiLokasi;
            int climateCalls = climateClient.Calls;
            VisibleNamed<TextBox>("NameBox").Text = activeProject.Nama + " revisi";
            Click(Labeled("Simpan proyek"));
            await Idle(window);
            Check(activeProject.Nama.EndsWith("revisi") && activeProject.RevisiLokasi == locationRevision
                && climateClient.Calls == climateCalls, "Rename saves without fetching climate");
            VisibleNamed<TextBox>("LatBox").Text = "-6,3";
            VisibleNamed<TextBox>("LonBox").Text = "106,9";
            Click(Labeled("Simpan proyek"));
            await Idle(window);
            Check(activeProject.RevisiLokasi == locationRevision + 1 && climateClient.Calls > climateCalls,
                "Location change increments revision and fetches climate");
            VisibleNamed<TextBox>("LatBox").Text = "20";
            Click(Labeled("Simpan proyek"));
            Check(activeProject.Latitude == -6.3, "Invalid project edit preserves stored coordinates");
            window.ShowShell("home");
            window.ShowShell("project");
            await Idle(window);
            var pendingClimate = new PendingAcceptanceClimate();
            App.State.HistoricalClient = pendingClimate;
            Click(Labeled("Perbarui iklim"));
            await pendingClimate.Started.Task;
            Click(Labeled("Perbarui iklim"));
            await Idle(window);
            Check(activeProject.ProfilIklim!.StatusPengambilan != StatusPengambilanIklim.Mengambil,
                "Second refresh click cancels pending historical request without freezing WPF");
            App.State.HistoricalClient = new FailingAcceptanceClimate();
            var previousRain = activeProject.ProfilIklim.CurahHujanTahunan;
            Click(Labeled("Perbarui iklim"));
            await Idle(window);
            Check(activeProject.ProfilIklim.StatusPengambilan == StatusPengambilanIklim.GagalMemperbarui
                && activeProject.ProfilIklim.CurahHujanTahunan == previousRain,
                "Failed same-location refresh retains the previous climate profile");
            App.State.HistoricalClient = climateClient;

            window.ShowShell("material");
            VisibleNamed<TextBox>("SearchBox").Text = "tidak-ada-bahan-ini";
            Check(VisibleNamed<ItemsControl>("Cards").Items.Count == 0, "Catalog search empty state");
            VisibleNamed<TextBox>("SearchBox").Text = "";
            await Idle(window);
            Click(Labeled("Detail bahan"));
            Check(VisibleNamed<Grid>("DetailModal").IsVisible, "Material detail and provenance opened");
            await Shot("material-details");
            Click(Labeled("Tutup"));

            window.ShowShell("compare");
            Click(VisibleNamed<RadioButton>("V2WeightRadio"));
            VisibleNamed<TextBox>("ThermalWeightBox").Text = "10";
            VisibleNamed<TextBox>("MoistureWeightBox").Text = "10";
            VisibleNamed<TextBox>("CorrosionWeightBox").Text = "10";
            VisibleNamed<TextBox>("LifespanWeightBox").Text = "69";
            Click(VisibleNamed<Button>("ApplyWeightsBtn"));
            Check(App.State.BobotAktif.Usia == 0.25, "Invalid weight sum does not mutate project");
            VisibleNamed<TextBox>("LifespanWeightBox").Text = "70";
            Click(VisibleNamed<Button>("ApplyWeightsBtn"));
            Check(App.State.BobotAktif.Usia == 0.7, "Valid custom weights applied through UI");
            await Shot("custom-weights");
            Click(VisibleNamed<Button>("ResetWeightsBtn"));
            Check(!App.State.Aktif.GunakanBobotKustom, "Weight reset restores MVP");
            window.ShowShell("sim");
            foreach (int index in new[] { 0, 2, 1 })
            {
                VisibleNamed<ComboBox>("HorizonBox").SelectedIndex = index;
                await Idle(window);
            }
            VisibleNamed<TextBox>("FaktorABox").Text = "0,7";
            VisibleNamed<TextBox>("FaktorABox").RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            await Shot("simulation-a07");
            Check(App.State.Aktif.FaktorPenuaanA == 0.7, "Ageing factor applied through UI");
            Check(App.State.Aktif.RiwayatSimulasi.Select(s => s.Hasil.HorizonTahun).Distinct().Count() == 3,
                "Simulation UI persists all horizons");

            SeedForecast();
            window.ShowShell("weather");
            var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, "Asia/Jakarta");
            VisibleNamed<TextBox>("JobName").Text = "Cat risiko Windows";
            VisibleNamed<ComboBox>("JobKind").SelectedIndex = 0;
            VisibleNamed<DatePicker>("JobStart").SelectedDate = today.ToDateTime(TimeOnly.MinValue);
            VisibleNamed<DatePicker>("JobEnd").SelectedDate = today.AddDays(29).ToDateTime(TimeOnly.MinValue);
            Click(Labeled("Tambahkan pekerjaan"));
            await Idle(window);
            var job = App.State.Aktif.Pekerjaan.Last();
            var detail = await App.State.Workflows.EvaluateJobAsync(job.Id);
            Check(detail.Count == 30 && detail[^1].Status == StatusKelayakanCuaca.DataTidakTersedia,
                "30 day job keeps dates without forecast");
            // Risk confirmation dialogs are exercised separately with Windows UI Automation.
            await App.State.Workflows.LockJobAsync(job.Id, today, new KonfirmasiPenguncian(SetujuiRisiko: true));
            window.ShowShell("weather");
            await Shot("schedule-risk");

            var selected = App.State.Peringkat().Skip(9).Take(1).Select(e => App.State.Item(e.Bahan).Kode).ToHashSet();
            var snapshot = ReportSnapshotFactory.Build(App.State, App.State.Jenis, 30, true, selected);
            var report = await App.State.Workflows.ExportAsync(snapshot, Path.Combine(folder, "windows-report.pdf"));
            Check(File.ReadAllText(report.PathBerkas).StartsWith("%PDF-"), "Direct PDF export has PDF signature");
            Check(snapshot.BahanTerpilih.Single().Peringkat == 10, "Report includes rank 10 selection");
            Check(snapshot.JadwalTerkunci.Last().StatusCuaca == "TidakLayak", "Report preserves risky decision");
            window.ShowShell("report");
            await Shot("report-history");

            foreach (var width in new[] { 1024, 1200, 1440 })
            {
                _root!.Width = width;
                Services.Viewport.Update(width);
                window.ShowShell("weather");
                await Shot("resize-weather-" + width);
                window.ShowShell("report");
                await Shot("resize-report-" + width);
            }
            window.ShowShell("project");
            var name = VisibleNamed<TextBox>("NameBox");
            name.Focus();
            Check(name.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)), "Keyboard focus traverses next control");
            log.Add(await PerfAsync(window));
            App.State.SimpanSekarang();
        }
        catch (Exception ex) { log.Add("FAIL " + ex); }
        finally { await File.WriteAllLinesAsync(Path.Combine(folder, mode + ".log"), log); }
    }

    private static void SeedForecast()
    {
        var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, "Asia/Jakarta");
        App.State.Aktif.PrakiraanCache = new(Enumerable.Range(0, 14)
            .Select(i => new DailyForecast(today.AddDays(i), i == 0 ? 90 : 20, 0, 20, 30, 70)).ToArray(),
            "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture Windows; bukan API nyata", App.State.Aktif.RevisiLokasi);
        App.State.PulihkanPrakiraanAktif();
    }

    private sealed class AcceptanceClimate : IHistoricalClimateClient
    {
        public int Calls;
        public Task<Domain.ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(new Domain.ProfilIklim(2400, 80, 28, 5, TingkatKorosi.Tinggi,
                provenance: ProvenanceIklim.Contoh));
        }
    }

    private sealed class PendingAcceptanceClimate : IHistoricalClimateClient
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<Domain.ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("Expected cancellation.");
        }
    }

    private sealed class FailingAcceptanceClimate : IHistoricalClimateClient
    {
        public Task<Domain.ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
            => throw new System.Net.Http.HttpRequestException("Fixture failure; no network request.");
    }
}
#endif
