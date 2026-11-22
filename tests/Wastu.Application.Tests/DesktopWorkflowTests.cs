using System.Reflection;
using System.Text.Json;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;
using Wastu.Services;
using Xunit;

namespace Wastu.Application.Tests;

public sealed class DesktopWorkflowTests
{
    [Fact]
    public async Task CatalogRefreshCannotSilentlyChangeLocalStorageMode()
    {
        var state = CreateState();
        Assert.True(await state.SegarkanKatalogAsync());
        Assert.False(state.PostgreSql);
        Assert.Equal("Lokal (JSON)", state.LabelModePenyimpanan);
    }

    [Fact]
    public async Task SwitchingProjectsCancelsClimateWithoutLeavingPermanentLoadingState()
    {
        var state = CreateState();
        var first = state.Aktif;
        var client = new PendingClimate();
        state.HistoricalClient = client;
        var pending = state.PerbaruiIklimAsync(first);
        await client.Started.Task;
        state.TambahProyek("Kedua", "Bandung", -6.9, 107.6, [(Models.JenisKomponen.Atap, 90)], 80);
        state.PilihProyek(state.Aktif.Id);
        Assert.Equal("Pengambilan iklim dibatalkan.", await pending);
        Assert.NotEqual(Domain.StatusPengambilanIklim.Mengambil, first.ProfilIklim!.StatusPengambilan);
        Assert.NotSame(first.ProfilIklim, state.Aktif.ProfilIklim);
    }

    [Fact]
    public void LegacyImportIdsAreStableAndDistinctForDuplicateJobs()
    {
        var project = Guid.NewGuid();
        var job = new PekerjaanDto { Nama = "Cat", Mulai = new(2026, 11, 22), Selesai = new(2026, 11, 23) };
        var id = WorkflowMapper.LegacyJobId(project, 0, job);
        Assert.Equal(id, WorkflowMapper.LegacyJobId(project, 0, job));
        Assert.NotEqual(id, WorkflowMapper.LegacyJobId(project, 1, job));
        job.Id = id;
        Assert.Equal(id, WorkflowMapper.LegacyJobId(project, 99, job));
    }

    [Fact]
    public void SchedulingInputsSupportDecimalCommaAndNegativeTemperatures()
    {
        var threshold = ScheduleViewModel.ParseOverride(Models.JenisPekerjaan.Pengecatan,
            "30,5", "", "85", "-5", "35");
        Assert.Equal(30.5, threshold!.ProbabilitasHujanMaksPct);
        Assert.Equal(-5, threshold.SuhuMinC);
        Assert.Throws<FormatException>(() => ScheduleViewModel.ParseOverride(Models.JenisPekerjaan.Pengecatan,
            "NaN", "", "85", "10", "35"));
    }
    internal static AppState CreateState() => CreateState(new AppState());
    private static AppState CreateState(AppState state)
    {
        state.TambahProyek("Rumah uji", "Jakarta", -6.2, 106.8,
            [(Models.JenisKomponen.Dinding, 120)], 5);
        return state;
    }

    internal static AppState Roundtrip(AppState state)
    {
        var method = typeof(AppState).GetMethod("KeDto", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var dto = (StateDto)method.Invoke(state, null)!;
        var json = JsonSerializer.Serialize(dto);
        var read = JsonSerializer.Deserialize<StateDto>(json)!;
        var restored = new AppState();
        typeof(AppState).GetMethod("TerapkanDto", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [restored, read]);
        return restored;
    }

    [Fact]
    public async Task HorizonsAndAgeingFactorSurviveSnapshotAndReportMatchesSimulation()
    {
        var state = CreateState();
        Assert.Null(state.AturFaktorPenuaan("0,7"));
        foreach (int horizon in new[] { 20, 50, 30, 20 })
        {
            state.Horizon = horizon;
            await state.Workflows.SimulateAsync();
        }
        Assert.Equal(6, state.Aktif.RiwayatSimulasi.Count);
        var restored = Roundtrip(state);
        Assert.Equal(0.7, restored.Aktif.FaktorPenuaanA);
        Assert.Equal(6, restored.Aktif.RiwayatSimulasi.Count);
        restored.Horizon = 50;
        var simulation = await restored.Workflows.SimulateAsync();
        var codes = restored.Terpilih().Select(i => i.Kode).ToHashSet();
        var report = ReportSnapshotFactory.Build(restored, restored.Jenis, 50, true, codes);
        Assert.Equal(simulation[0].Hasil.EmisiKumulatifKg, report.SimulasiHorizon[0].EmisiKumulatifKg);
        Assert.All(report.SimulasiHorizon, s => Assert.Equal("v2.0-a0.7", s.VersiModel));
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("0")]
    [InlineData("1,1")]
    public void InvalidAgeingDoesNotMutateState(string input)
    {
        var state = CreateState();
        int revision = state.Revisi;
        Assert.NotNull(state.AturFaktorPenuaan(input));
        Assert.Equal(0.5, state.Aktif.FaktorPenuaanA);
        Assert.Equal(revision, state.Revisi);
    }

    [Fact]
    public async Task RiskAndOverrideSurviveSnapshotAndAreNeverReportedAsSafe()
    {
        var state = CreateState();
        var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, "Asia/Jakarta");
        state.Aktif.PrakiraanCache = new([new(today, 35, 0, 20, 30, 70)],
            "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture");
        var job = new Models.Pekerjaan { Nama = "Cat uji", Jenis = Models.JenisPekerjaan.Pengecatan,
            TanggalMulaiRencana = today.ToDateTime(TimeOnly.MinValue),
            TanggalSelesaiRencana = today.AddDays(29).ToDateTime(TimeOnly.MinValue),
            OverrideAmbang = new(30, null, 85, 10, 35) };
        await state.Workflows.AddJobAsync(job);
        Assert.Equal(30, (await state.Workflows.EvaluateJobAsync(job.Id)).Count);
        Assert.Equal(30, state.Workflows.CalendarDays().Count);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            state.Workflows.LockJobAsync(job.Id, today, new()));
        Assert.Null(state.Aktif.Pekerjaan[0].TanggalDikunci);
        await state.Workflows.LockJobAsync(job.Id, today, new(SetujuiRisiko: true));
        var restored = Roundtrip(state);
        var saved = restored.Aktif.Pekerjaan[0];
        Assert.Equal(job.Id, saved.Id);
        Assert.Equal(30, saved.OverrideAmbang!.ProbabilitasHujanMaksPct);
        Assert.True(saved.PersetujuanRisiko);
        Assert.Equal(35, saved.Keputusan!.Cuaca!.ProbabilitasHujanPct);
        var codes = restored.Terpilih().Select(i => i.Kode).ToHashSet();
        var report = ReportSnapshotFactory.Build(restored, restored.Jenis, 30, true, codes);
        Assert.Equal("TidakLayak", report.JadwalTerkunci[0].StatusCuaca);
        Assert.True(report.JadwalTerkunci[0].KonfirmasiRisiko);
        Assert.Equal(job.Id, report.JadwalTerkunci[0].PekerjaanId);
    }

    [Fact]
    public async Task ForecastSurvivesProjectSwitchAndPartialValuesSurviveSerialization()
    {
        var state = CreateState();
        var first = state.Aktif;
        var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, "Asia/Jakarta");
        first.PrakiraanCache = new([new(today, double.NaN, 0, 20, 30, 70, false, "Fixture parsial")],
            "Asia/Jakarta", DateTimeOffset.UtcNow, "Fixture");
        state.TambahProyek("Atap saja", "Bandung", -6.9, 107.6, [(Models.JenisKomponen.Atap, 90)], 80);
        Assert.Empty(state.PrakiraanAktif());
        state.PilihProyek(first.Id);
        Assert.Single(state.PrakiraanAktif());
        var restored = Roundtrip(state);
        Assert.False(restored.PrakiraanAktif()[0].DataLengkap);
        Assert.True(double.IsNaN(restored.PrakiraanAktif()[0].ProbabilitasHujan));
        var service = new DesktopWorkflowService(restored, forecast: new ThrowingClient());
        await service.RefreshForecastAsync(); // Fresh cache: no network.
    }

    [Fact]
    public async Task ViewModelPreventsDoubleCommandAndRestoresStateAfterCancellationAndFailure()
    {
        var vm = new OperationViewModel();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int changed = 0;
        vm.PropertyChanged += (_, _) => changed++;
        var operation = vm.RunAsync(async ct => { started.SetResult(); await Task.Delay(Timeout.Infinite, ct); });
        await started.Task;
        Assert.True(vm.IsBusy);
        Assert.False(await vm.RunAsync(_ => throw new Exception("Must not execute")));
        vm.Cancel();
        Assert.False(await operation);
        Assert.False(vm.IsBusy);
        Assert.Equal("Operasi dibatalkan.", vm.Error);
        Assert.False(await vm.RunAsync(_ => throw new IOException("private detail")));
        Assert.DoesNotContain("private detail", vm.Error);
        Assert.True(await vm.RunAsync(_ => Task.CompletedTask));
        Assert.Null(vm.Error);
        Assert.True(changed >= 6);
    }

    [Fact]
    public async Task ReportHistorySurvivesSnapshotAndDeletedFileIsDetected()
    {
        var state = CreateState();
        var codes = state.Terpilih().Select(i => i.Kode).ToHashSet();
        var snapshot = ReportSnapshotFactory.Build(state, state.Jenis, 30, true, codes);
        var folder = Path.Combine(Path.GetTempPath(), "wastu-report-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "report.pdf");
            var report = await state.Workflows.ExportAsync(snapshot, path);
            var restored = Roundtrip(state);
            var history = Assert.Single(restored.Aktif.Laporan);
            Assert.Equal(report.Id, history.Id);
            Assert.Equal(Reporting.StatusBerkasLaporan.Tersedia, history.StatusBerkas);
            File.Delete(path);
            Assert.Equal(Reporting.StatusBerkasLaporan.Hilang, history.StatusBerkas);
        }
        finally { Directory.Delete(folder, true); }
    }

    private sealed class ThrowingClient : IDailyForecastClient
    {
        public Task<IReadOnlyList<DailyForecast>> GetForecastAsync(double lat, double lon, int days, CancellationToken ct = default)
            => throw new IOException("Network must not be called");
    }

    private sealed class PendingClimate : IHistoricalClimateClient
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<Domain.ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("Cancelled request must not return a profile.");
        }
    }
}
