using Wastu.Domain.Abstractions;
using Wastu.Domain.Emissions;
using Wastu.Domain.Tests.Fixtures;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Emissions;

public class EmissionSimulationServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-20)]
    public async Task SimulateAsync_HorizonKurangDariAtauSamaDenganNol_MelemparArgumentOutOfRangeException(int invalidHorizon)
    {
        var evaluasiRepo = new EvaluasiRepositoryPerekam();
        var materialRepo = new MaterialRepositoryInMemory();
        var service = new EmissionSimulationService(evaluasiRepo, materialRepo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.SimulateAsync(Guid.NewGuid(), invalidHorizon));
    }

    [Fact]
    public async Task SimulateAsync_EvaluasiTidakDitemukan_MelemparKeyNotFoundException()
    {
        var evaluasiRepo = new EvaluasiRepositoryPerekam();
        var materialRepo = new MaterialRepositoryInMemory();
        var service = new EmissionSimulationService(evaluasiRepo, materialRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SimulateAsync(Guid.NewGuid(), 30));
    }

    [Fact]
    public async Task SimulateAsync_EvaluasiLegacyTanpaInput_TidakMenebakUsiaDariKatalog()
    {
        var evaluasiRepo = new EvaluasiRepositoryPerekam();
        var materialRepo = new MaterialRepositoryInMemory();
        var service = new EmissionSimulationService(evaluasiRepo, materialRepo);

        var eval = new Evaluasi(
            Id: Guid.NewGuid(),
            BahanId: Guid.NewGuid(), // non-existent bahan
            KomponenId: DataContoh.KomponenDindingId,
            SkorKesesuaian: 80,
            EmisiAwalKg: 1000,
            SubSkor: new SubSkorKesesuaian(80, 80, 80, 80),
            Alasan: "Uji");
        evaluasiRepo.EvaluasiMap[eval.Id] = eval;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SimulateAsync(eval.Id, 30));
    }

    [Fact]
    public async Task SimulateAsync_ConstructorBawaanDuaArg_MenyimpanSimulasiDenganBenar()
    {
        var evaluasiRepo = new EvaluasiRepositoryPerekam();
        var bahan = DataContoh.BetonBertulang();
        var materialRepo = new MaterialRepositoryInMemory([bahan]);

        var eval = new Evaluasi(
            Id: Guid.NewGuid(),
            BahanId: bahan.Id,
            KomponenId: DataContoh.KomponenDindingId,
            SkorKesesuaian: 100,
            EmisiAwalKg: 500,
            SubSkor: new SubSkorKesesuaian(100, 100, 100, 100),
            Alasan: "Uji", Input: Input(bahan));
        evaluasiRepo.EvaluasiMap[eval.Id] = eval;

        var service = new EmissionSimulationService(evaluasiRepo, materialRepo);
        SimulasiEmisi hasil = await service.SimulateAsync(eval.Id, 30);

        Assert.Equal(30, hasil.HorizonTahun);
        Assert.Equal("v2.0", hasil.VersiModel);
        Assert.Equal(0.5, hasil.FaktorPenuaanA);
        Assert.NotNull(hasil.DihitungPadaUtc);
        Assert.Single(evaluasiRepo.SimulasiTersimpan);
        Assert.Equal(hasil, evaluasiRepo.SimulasiTersimpan[0]);
    }

    [Fact]
    public async Task SimulateAsync_KonfigurasiDanTimeProviderKustom_TercatatPadaHasil()
    {
        var evaluasiRepo = new EvaluasiRepositoryPerekam();
        var bahan = DataContoh.BetonBertulang();
        var materialRepo = new MaterialRepositoryInMemory([bahan]);

        var eval = new Evaluasi(
            Id: Guid.NewGuid(),
            BahanId: bahan.Id,
            KomponenId: DataContoh.KomponenDindingId,
            SkorKesesuaian: 50,
            EmisiAwalKg: 1000,
            SubSkor: new SubSkorKesesuaian(50, 50, 50, 50),
            Alasan: "Uji kustom", Input: Input(bahan));
        evaluasiRepo.EvaluasiMap[eval.Id] = eval;

        var fixedTime = new DateTimeOffset(2026, 11, 22, 12, 34, 56, TimeSpan.Zero);
        var timeProvider = new UjiTimeProvider(fixedTime);
        var konfigurasi = new KonfigurasiModel(versiModel: "v2.0-custom", faktorPenuaanA: 0.8);

        var service = new EmissionSimulationService(evaluasiRepo, materialRepo, konfigurasi, timeProvider);
        SimulasiEmisi hasil = await service.SimulateAsync(eval.Id, 30);

        Assert.Equal("v2.0-custom", hasil.VersiModel);
        Assert.Equal(0.8, hasil.FaktorPenuaanA);
        Assert.Equal(fixedTime, hasil.DihitungPadaUtc);
    }

    [Fact]
    public async Task SimulateAsync_MultiHorizon_20_30_50_MenyimpanSemuaHasil()
    {
        // F5-01: pengguna dapat memilih horizon simulasi (20, 30, 50 tahun);
        // relasi menyimpan hasil per horizon tanpa menimpa.
        var evaluasiRepo = new EvaluasiRepositoryPerekam();
        var bahan = DataContoh.BetonBertulang();
        var materialRepo = new MaterialRepositoryInMemory([bahan]);

        var eval = new Evaluasi(
            Id: Guid.NewGuid(),
            BahanId: bahan.Id,
            KomponenId: DataContoh.KomponenDindingId,
            SkorKesesuaian: 75,
            EmisiAwalKg: 1200,
            SubSkor: new SubSkorKesesuaian(75, 75, 75, 75),
            Alasan: "Uji multi-horizon", Input: Input(bahan));
        evaluasiRepo.EvaluasiMap[eval.Id] = eval;

        var service = new EmissionSimulationService(evaluasiRepo, materialRepo);

        SimulasiEmisi hasil20 = await service.SimulateAsync(eval.Id, 20);
        SimulasiEmisi hasil30 = await service.SimulateAsync(eval.Id, 30);
        SimulasiEmisi hasil50 = await service.SimulateAsync(eval.Id, 50);

        Assert.Equal(20, hasil20.HorizonTahun);
        Assert.Equal(30, hasil30.HorizonTahun);
        Assert.Equal(50, hasil50.HorizonTahun);

        Assert.Equal(3, evaluasiRepo.SimulasiTersimpan.Count);
        Assert.Equal(20, evaluasiRepo.SimulasiTersimpan[0].HorizonTahun);
        Assert.Equal(30, evaluasiRepo.SimulasiTersimpan[1].HorizonTahun);
        Assert.Equal(50, evaluasiRepo.SimulasiTersimpan[2].HorizonTahun);
    }

    [Fact]
    public async Task SimulateAsync_KatalogBerubahAtauBahanDihapus_TetapMemakaiUsiaAsli()
    {
        var bahan = DataContoh.BetonBertulang() with { UsiaPakaiNominalTahun = 15 };
        var repo = new EvaluasiRepositoryPerekam();
        var eval = new Evaluasi(Guid.NewGuid(), bahan.Id, DataContoh.KomponenDindingId, 100,
            500, new(100, 100, 100, 100), "Uji", Input: Input(bahan));
        repo.EvaluasiMap[eval.Id] = eval;
        foreach (var katalog in new[] { new MaterialRepositoryInMemory([]),
            new MaterialRepositoryInMemory([bahan with { UsiaPakaiNominalTahun = 100 }]) })
        {
            var hasil = await new EmissionSimulationService(repo, katalog).SimulateAsync(eval.Id, 30);
            Assert.Equal(15, hasil.UsiaPakaiEfektifTahun);
            Assert.Equal(1, hasil.JumlahPenggantian);
            Assert.Equal(1000, hasil.EmisiKumulatifKg);
        }
    }

    private static InputEvaluasi Input(Bahan bahan) => new(
        new(DataContoh.KomponenDindingId, DataContoh.ProyekId, JenisKomponen.Dinding, 120),
        bahan, new ProfilIklim(700, 80, 26, 50, TingkatKorosi.Rendah),
        KriteriaBobot.Default, KonfigurasiModel.Bawaan, 15, 50);

    private sealed class UjiTimeProvider(DateTimeOffset fixedUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedUtc;
    }

    private sealed class EvaluasiRepositoryPerekam : IEvaluasiRepository
    {
        public List<SimulasiEmisi> SimulasiTersimpan { get; } = [];
        public Dictionary<Guid, Evaluasi> EvaluasiMap { get; } = [];

        public Task<IReadOnlyList<Evaluasi>> ListByKomponenAsync(Guid komponenId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Evaluasi>>(EvaluasiMap.Values.Where(e => e.KomponenId == komponenId).ToList());

        public Task<Evaluasi?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(EvaluasiMap.GetValueOrDefault(id));

        public Task SimpanAsync(IReadOnlyList<Evaluasi> evaluasi, CancellationToken ct = default)
        {
            foreach (var e in evaluasi) EvaluasiMap[e.Id] = e;
            return Task.CompletedTask;
        }

        public Task SimpanAsync(SimulasiEmisi simulasi, CancellationToken ct = default)
        {
            SimulasiTersimpan.Add(simulasi);
            return Task.CompletedTask;
        }
    }
}
