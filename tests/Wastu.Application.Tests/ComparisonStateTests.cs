using Wastu.Domain;
using Wastu.Models;
using Wastu.Services;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Scoring;
using Wastu.InMemory;
using Xunit;
using JenisKomponen = Wastu.Models.JenisKomponen;
using Komponen = Wastu.Models.Komponen;

namespace Wastu.Application.Tests;

public sealed class ComparisonStateTests
{
    [Fact]
    public async Task ReportCapturesCanonicalPersistedEvaluationIdsAndOnlyChosenComponent()
    {
        var evaluations = new EvaluasiRepositoryInMemory();
        var batches = new BatchEvaluasiRepositoryInMemory(evaluations);
        var scoring = new SuitabilityScoringService(new MaterialRepositoryInMemory([]),
            new ProyekRepositoryInMemory(), batches, evaluations);
        var state = BuatState(scoring);
        state.Aktif.TambahKomponen(new Komponen { Jenis = JenisKomponen.Atap, Luas = 90 });
        var ranking = state.Peringkat();
        var codes = ranking.Take(2).Select(e => state.Item(e.Bahan).Kode).ToHashSet();
        var snapshot = await ReportSnapshotFactory.BuildAsync(state, JenisKomponen.Dinding, 30, true, codes);
        Assert.Equal(Domain.JenisKomponen.Dinding, Assert.Single(snapshot.Komponen).Jenis);
        foreach (var simulation in snapshot.SimulasiHorizon)
            Assert.NotNull(await evaluations.GetByIdAsync(simulation.EvaluasiId));
    }

    [Fact]
    public void RenderUlangMemakaiEvaluasiYangSamaSampaiInputBerubah()
    {
        var state = BuatState();
        var awal = state.Peringkat();
        Assert.NotEmpty(awal);
        Assert.Same(awal[0], state.Peringkat()[0]);

        state.Aktif.AturLuasKomponen(JenisKomponen.Dinding, 240);
        var berubah = state.Peringkat();
        Assert.NotSame(awal[0], berubah[0]);
        Assert.Equal(awal[0].SkorKesesuaian, berubah[0].SkorKesesuaian);
        Assert.Equal(awal[0].EmisiAwal * 2, berubah[0].EmisiAwal);
    }

    [Fact]
    public void PemetaanKatalogMempertahankanVersiDataUntukHashDanRiwayat()
    {
        var bahan = new Domain.Bahan(Guid.NewGuid(), "Atap uji", Domain.JenisKomponen.Atap,
            0.2, 3, 4, 30, 50, "Referensi", versiData: 7);
        var item = ItemKatalog.DariDomain(bahan);
        Assert.Equal(7, item.DataDomain.VersiData);
        Assert.Equal(bahan.Id, item.DataDomain.Id);
        Assert.Equal(bahan.Id, item.Bahan.DomainId);
        Assert.Equal(bahan.Jenis, item.Bahan.JenisDomain);
    }

    [Fact]
    public void BobotTersimpanPerProyekDanInputInvalidTidakMengubahState()
    {
        var state = BuatState();
        var iklim = state.Iklim;
        Assert.Null(state.AturBobotPersentase(10, 10, 10, 70));
        var valid = state.BobotAktif;
        var revision = state.Revisi;
        Assert.NotNull(state.AturBobotPersentase(25, 25, 25, 24));
        Assert.Equal(valid, state.BobotAktif);
        Assert.Equal(revision, state.Revisi);
        Assert.Same(iklim, state.Iklim);

        state.Proyek.Add(new Bangunan { Id = 2 });
        state.ProyekAktifId = 2;
        Assert.False(state.Aktif.GunakanBobotKustom);
        Assert.Equal(KriteriaBobot.Default, state.BobotAktif);
        state.ProyekAktifId = 1;
        Assert.Equal(valid, state.BobotAktif);
        state.ResetBobotAktif();
        Assert.False(state.Aktif.GunakanBobotKustom);
        Assert.Equal(KriteriaBobot.Default, state.BobotAktif);
    }

    [Fact]
    public void IdentitasLegacyKosongDigantiSekaliDanTetapStabil()
    {
        var proyek = new Bangunan { DatabaseId = Guid.Empty };
        var komponen = new Komponen { DatabaseId = Guid.Empty };
        Assert.NotEqual(Guid.Empty, proyek.StableId);
        Assert.NotEqual(Guid.Empty, komponen.Id);
        Assert.Equal(proyek.StableId, proyek.StableId);
        Assert.Equal(komponen.Id, komponen.Id);
    }

    [Fact]
    public void BobotDanIdentitasBertahanDalamJsonDanDtoLegacyTetapDapatDibuka()
    {
        var state = BuatState();
        Assert.Null(state.AturBobotPersentase(40, 30, 20, 10));
        var simpan = typeof(AppState).GetMethod("KeDto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var pulihkan = typeof(AppState).GetMethod("TerapkanDto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var json = System.Text.Json.JsonSerializer.Serialize((StateDto)simpan.Invoke(state, null)!);
        var dto = System.Text.Json.JsonSerializer.Deserialize<StateDto>(json)!;
        var hasil = new AppState();
        pulihkan.Invoke(null, [hasil, dto]);
        Assert.Equal(state.Aktif.StableId, hasil.Aktif.StableId);
        Assert.Equal(state.Aktif.Komponen[0].Id, hasil.Aktif.Komponen[0].Id);
        Assert.True(hasil.Aktif.GunakanBobotKustom);
        Assert.Equal(state.BobotAktif, hasil.BobotAktif);

        var legacy = System.Text.Json.JsonSerializer.Deserialize<StateDto>(
            """{"Proyek":[{"Id":1,"Kota":"Jakarta","Dinding":120}]}""")!;
        pulihkan.Invoke(null, [hasil, legacy]);
        Assert.Equal(KriteriaBobot.Default, hasil.BobotAktif);
        Assert.False(hasil.Aktif.GunakanBobotKustom);
        Assert.NotEqual(Guid.Empty, hasil.Aktif.StableId);
        Assert.NotEqual(Guid.Empty, hasil.Aktif.Komponen[0].Id);
    }

    [Fact]
    public async Task PenyimpananGagalDicobaLagiDanMemakaiInputYangDitampilkan()
    {
        var evaluasi = new EvaluasiRepositoryInMemory();
        var repo = new BatchPerekam(evaluasi) { GagalPertama = true };
        // Empty backing sources prove that persistence uses the UI snapshot, not a later reload.
        var scoring = new SuitabilityScoringService(new MaterialRepositoryInMemory([]),
            new ProyekRepositoryInMemory(), repo, evaluasi);
        var state = BuatState(scoring);
        Assert.Null(state.AturBobotPersentase(40, 30, 20, 10));
        var iklim = state.Iklim;
        var awal = state.Peringkat();
        Assert.Equal(1, repo.Percobaan);
        var ulang = state.Peringkat();
        await repo.Selesai.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, repo.Percobaan);
        Assert.Same(awal[0], ulang[0]);
        Assert.Same(iklim, state.Iklim);

        var batch = Assert.Single(await repo.ListBatchesByProyekAsync(state.Aktif.StableId));
        Assert.Equal(state.BobotAktif, batch.Bobot);
        Assert.Equal(state.Aktif.Komponen[0].Id, batch.KomponenId);
        Assert.Equal(120, batch.LuasM2);
        var hasil = await evaluasi.ListByKomponenAsync(batch.KomponenId);
        Assert.Equal(awal.Select(e => e.SkorKesesuaian).Order(), hasil.Select(e => e.SkorKesesuaian).Order());
        Assert.Equal(awal.Select(e => e.EmisiAwal).Order(), hasil.Select(e => e.EmisiAwalKg).Order());
        state.Peringkat();
        Assert.Equal(2, repo.Percobaan);
    }

    [Fact]
    public void ModeLuringMenolakBobotDanTidakMenyimpanEvaluasi()
    {
        var evaluasi = new EvaluasiRepositoryInMemory();
        var repo = new BatchPerekam(evaluasi);
        var scoring = new SuitabilityScoringService(new MaterialRepositoryInMemory([]),
            new ProyekRepositoryInMemory(), repo, evaluasi);
        var state = BuatState(scoring);
        typeof(AppState).GetProperty(nameof(AppState.ModeBacaSaja))!.SetValue(state, true);
        Assert.Throws<InvalidOperationException>(() => state.AturBobotPersentase(40, 30, 20, 10));
        Assert.Throws<InvalidOperationException>(() => state.AturModeBobot(true));
        Assert.Throws<InvalidOperationException>(() => state.ResetBobotAktif());
        Assert.NotEmpty(state.Peringkat());
        Assert.Equal(0, repo.Percobaan);
    }

    private static AppState BuatState(SuitabilityScoringService? scoring = null)
    {
        var state = scoring is null ? new AppState() : new AppState(scoring);
        var proyek = new Bangunan { Id = 1, Nama = "Proyek uji", Kota = "Jakarta",
            Latitude = -6.2, Longitude = 106.8, DatabaseId = Guid.NewGuid() };
        proyek.TambahKomponen(new Komponen { Jenis = JenisKomponen.Dinding, Luas = 120, DatabaseId = Guid.NewGuid() });
        state.Proyek.Add(proyek);
        state.ProyekAktifId = 1;
        return state;
    }

    private sealed class BatchPerekam(IEvaluasiRepository evaluasi) : IBatchEvaluasiRepository
    {
        private readonly BatchEvaluasiRepositoryInMemory _repo = new(evaluasi);
        public bool GagalPertama { get; init; }
        public int Percobaan { get; private set; }
        public TaskCompletionSource Selesai { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task SimpanBatchAsync(BatchEvaluasi batch, IReadOnlyList<Domain.Evaluasi> hasil, CancellationToken ct = default)
        {
            Percobaan++;
            if (GagalPertama && Percobaan == 1) throw new IOException("Koneksi uji terputus.");
            await _repo.SimpanBatchAsync(batch, hasil, ct);
            Selesai.TrySetResult();
        }
        public Task<BatchEvaluasi?> GetBatchAsync(Guid id, CancellationToken ct = default) => _repo.GetBatchAsync(id, ct);
        public Task<BatchEvaluasi?> GetLatestBatchByKomponenAsync(Guid id, CancellationToken ct = default) => _repo.GetLatestBatchByKomponenAsync(id, ct);
        public Task<IReadOnlyList<BatchEvaluasi>> ListBatchesByProyekAsync(Guid id, CancellationToken ct = default) => _repo.ListBatchesByProyekAsync(id, ct);
    }
}
