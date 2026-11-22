using System.Globalization;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Emissions;
using Wastu.Domain.Weather;
using Wastu.InMemory;
using Wastu.Reporting;

namespace Wastu.Services;

/// <summary>Composition and session persistence for the nonvisual desktop workflows.</summary>
public sealed class DesktopWorkflowService
{
    private readonly AppState _state;
    private readonly PostgreSqlBackend? _backend;
    private readonly ProyekRepositoryInMemory _localProjects = new();
    private readonly PekerjaanRepositoryInMemory _localJobs = new();
    private readonly PrakiraanCacheRepositoryInMemory _localCache = new();
    private readonly LaporanRepositoryInMemory _localReports = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IPekerjaanRepository _jobs;
    private readonly WorkSchedulingService _scheduling;
    private readonly ReportExportService _reports;

    internal DesktopWorkflowService(AppState state, PostgreSqlBackend? backend = null,
        IDailyForecastClient? forecast = null, TimeProvider? clock = null)
    {
        _state = state;
        _backend = backend;
        _jobs = backend?.PekerjaanRepository ?? (IPekerjaanRepository)_localJobs;
        _scheduling = backend?.WorkScheduling ?? new WorkSchedulingService(_jobs, _localProjects,
            _localCache, forecast ?? new OpenMeteo(), new AmbangPekerjaanRepositoryInMemory(_jobs), clock);
        _reports = backend is not null ? new ReportExportService(backend.LaporanRepository,
            pendingMetadataDirectory: PendingDirectory) : new ReportExportService(
            state.ModeBacaSaja ? new UnavailableReportRepository() : _localReports,
            pendingMetadataDirectory: PendingDirectory);
    }

    private string PendingDirectory => _state.PendingReportDirectory;

    public async Task LoadReportsAsync(CancellationToken ct = default)
    {
        if (_backend is not null) await _reports.RekonsiliasiPendingAsync(ct);
        foreach (var p in _state.Proyek)
        {
            foreach (var report in (await _reports.ReadPendingReportsAsync(ct)).Where(l => l.ProyekId == p.StableId))
                p.TambahLaporan(WorkflowMapper.DariDomain(report));
            if (_backend is not null)
                foreach (var report in await _reports.ListReportsAsync(p.StableId, ct))
                    p.TambahLaporan(WorkflowMapper.DariDomain(report));
        }
    }

    public List<Models.PrakiraanCuaca> CalendarDays()
    {
        var p = _state.Aktif;
        var forecast = _state.PrakiraanAktif();
        if (p is null || p.Pekerjaan.Count == 0) return forecast;
        var today = KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, p.ZonaWaktu)
            .ToDateTime(TimeOnly.MinValue);
        var first = p.Pekerjaan.Min(j => j.TanggalMulaiRencana);
        var last = p.Pekerjaan.Max(j => j.TanggalSelesaiRencana);
        first = first < today ? today : first;
        var days = forecast.ToDictionary(d => d.Tanggal.Date);
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            days.TryAdd(date, new Models.PrakiraanCuaca
            {
                Tanggal = date, DataLengkap = false,
                CatatanKualitas = "Prakiraan tanggal ini belum tersedia; hanya dapat disimpan sebagai draf."
            });
            if (date == DateTime.MaxValue.Date) break;
        }
        return days.Values.OrderBy(d => d.Tanggal).ToList();
    }
    private void EnsureWritable()
    {
        if (_state.ModeBacaSaja || _state.ButuhPemulihan)
            throw new InvalidOperationException("Mode luring baca-saja: perubahan tidak dapat disimpan.");
    }

    private async Task PrepareLocalAsync(Models.Bangunan p, CancellationToken ct)
    {
        if (_backend is not null) return;
        await _localProjects.AddAsync(WorkflowMapper.KeDomain(p), ct);
        foreach (var j in p.Pekerjaan) await _localJobs.AddAsync(WorkflowMapper.KeDomain(j, p.StableId), ct);
        if (p.PrakiraanCache is { } cache && cache.RevisiLokasi == p.RevisiLokasi)
            await _localCache.SimpanCacheAsync(p.Latitude, p.Longitude, cache, ct);
        foreach (var l in p.Laporan) await _localReports.SimpanAsync(WorkflowMapper.KeDomain(l), ct);
    }

    public async Task RefreshForecastAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        var p = _state.Aktif;
        if (p is null) return;
        if (_state.ModeBacaSaja) { _state.PulihkanPrakiraanAktif(); return; }
        int revision = p.RevisiLokasi;
        await _gate.WaitAsync(ct);
        try
        {
            await PrepareLocalAsync(p, ct);
            var result = await _scheduling.GetForecastAsync(p.StableId, forceRefresh, ct);
            ct.ThrowIfCancellationRequested();
            if (!_state.Proyek.Contains(p) || p.RevisiLokasi != revision) return;
            p.PrakiraanCache = result;
            p.ZonaWaktu = result.ZonaWaktu;
            if (ReferenceEquals(_state.Aktif, p)) _state.PulihkanPrakiraanAktif();
            foreach (var j in p.Pekerjaan.Where(j => j.TanggalDikunci.HasValue).ToArray())
            {
                var reviewed = await _scheduling.TinjauUlangPekerjaanAsync(j.Id, ct);
                if (reviewed is not null) p.JadwalkanPekerjaan(WorkflowMapper.DariDomain(reviewed));
            }
            _state.Ubah();
        }
        finally { _gate.Release(); }
    }

    public async Task AddJobAsync(Models.Pekerjaan job, CancellationToken ct = default)
    {
        EnsureWritable();
        var p = _state.Aktif;
        var domain = WorkflowMapper.KeDomain(job, p.StableId); // Validate before mutation.
        await _gate.WaitAsync(ct);
        try
        {
            await _jobs.AddAsync(domain, ct);
            p.JadwalkanPekerjaan(WorkflowMapper.DariDomain(domain));
            _state.Ubah();
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<HasilEvaluasiCuaca>> EvaluateJobAsync(Guid id, CancellationToken ct = default)
    {
        var p = _state.Aktif;
        await _gate.WaitAsync(ct);
        try
        {
            await PrepareLocalAsync(p, ct);
            return await _scheduling.EvaluateDaysDetailedAsync(id, ct);
        }
        finally { _gate.Release(); }
    }

    public async Task LockJobAsync(Guid id, DateOnly date, KonfirmasiPenguncian confirmation,
        CancellationToken ct = default)
    {
        EnsureWritable();
        var p = _state.Aktif;
        await _gate.WaitAsync(ct);
        try
        {
            await PrepareLocalAsync(p, ct);
            await _scheduling.LockDateAsync(id, date, confirmation, ct);
            var saved = await _jobs.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException("Jadwal belum tersimpan.");
            p.JadwalkanPekerjaan(WorkflowMapper.DariDomain(saved));
            _state.Ubah();
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteJobAsync(Guid id, CancellationToken ct = default)
    {
        EnsureWritable();
        var p = _state.Aktif;
        await _gate.WaitAsync(ct);
        try
        {
            await _jobs.DeleteAsync(id, ct);
            if (p.Pekerjaan.FirstOrDefault(j => j.Id == id) is { } job) p.HapusPekerjaan(job);
            _state.Ubah();
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<SimulationHistoryEntry>> SimulateAsync(CancellationToken ct = default)
    {
        var p = _state.Aktif;
        var selected = _state.Terpilih().Select(item => (Item: item, Eval: _state.Nilai(item))).ToArray();
        int horizon = _state.Horizon;
        double factor = p.FaktorPenuaanA;
        var config = new KonfigurasiModel(versiModel: "v2.0-a" + factor.ToString("R", CultureInfo.InvariantCulture),
            faktorPenuaanA: factor);
        var result = new List<SimulationHistoryEntry>();
        foreach (var (item, eval) in selected)
        {
            ct.ThrowIfCancellationRequested();
            var domain = _state.ModeBacaSaja ? eval.HasilDomain!
                : await _state.EvaluasiTersimpanAsync(eval, ct);
            var simulation = PerhitunganSimulasi.Simulasikan(domain.Id, domain,
                item.Bahan.UsiaPakaiTahun, horizon, config, DateTimeOffset.UtcNow);
            if (_backend is not null && !_state.ModeBacaSaja)
                simulation = await new EmissionSimulationService(_backend.EvaluasiRepository,
                    _backend.MaterialRepository, config).SimulateSnapshotAsync(domain, item.Bahan.UsiaPakaiTahun, horizon, ct);
            string hash = domain.HashInput ?? Domain.Scoring.SuitabilityScoringService.HitungHash(
                new Domain.Komponen(domain.KomponenId, p.StableId, item.DataDomain.Jenis,
                    p.LuasKomponen(item.Jenis)), p.ProfilIklim!.KeDomain(p.ZonaWaktu),
                _state.KatalogAktif.Where(k => k.Jenis == item.Jenis).Select(k => k.DataDomain).ToArray(), _state.BobotAktif);
            var entry = new SimulationHistoryEntry(hash, item.DataDomain.Id, item.Bahan.UsiaPakaiTahun, simulation, domain.EmisiAwalKg);
            result.Add(entry);
            if (!_state.ModeBacaSaja)
            {
                p.RiwayatSimulasi.RemoveAll(s => s.InputHash == hash && s.BahanId == entry.BahanId
                    && s.Hasil.HorizonTahun == horizon && s.Hasil.VersiModel == config.VersiModel);
                p.RiwayatSimulasi.Add(entry);
            }
        }
        if (!_state.ModeBacaSaja) _state.SimpanHasil();
        return result;
    }

    public async Task<Domain.Services.Laporan> ExportAsync(SnapshotLaporan snapshot, string path,
        CancellationToken ct = default)
    {
        // The project and inputs were captured before the dialog/await.
        var project = _state.Proyek.Single(p => p.StableId == snapshot.ProyekId);
        var report = await _reports.ExportSnapshotAsync(snapshot, path, ct);
        project.TambahLaporan(WorkflowMapper.DariDomain(report));
        if (!_state.ModeBacaSaja) _state.Ubah();
        return report;
    }

    public async Task<Domain.Services.Laporan> RegenerateReportAsync(Guid id, string path, CancellationToken ct = default)
    {
        var project = _state.Proyek.Single(p => p.Laporan.Any(l => l.Id == id));
        var original = project.Laporan.Single(l => l.Id == id);
        var report = await _reports.RegenerateAsync(WorkflowMapper.KeDomain(original), path, ct);
        project.TambahLaporan(WorkflowMapper.DariDomain(report));
        if (!_state.ModeBacaSaja) _state.Ubah();
        return report;
    }

    private sealed class UnavailableReportRepository : Domain.Abstractions.ILaporanRepository
    {
        public Task SimpanAsync(Domain.Services.Laporan l, CancellationToken ct = default)
            => throw new InvalidOperationException("Riwayat menunggu koneksi database.");
        public Task<IReadOnlyList<Domain.Services.Laporan>> ListByProyekAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Domain.Services.Laporan>>([]);
        public Task<Domain.Services.Laporan?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult<Domain.Services.Laporan?>(null);
        public Task DeleteAsync(Guid id, CancellationToken ct = default) => throw new InvalidOperationException();
    }
}
