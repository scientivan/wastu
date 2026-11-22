using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;
using Wastu.Domain.Weather;

namespace Wastu.Domain.Tests;

public class BatchDanWeatherContractsTests
{
    [Fact]
    public void BatchEvaluasi_Valid_TersimpanSesuai()
    {
        var id = Guid.NewGuid();
        var proyekId = Guid.NewGuid();
        var komponenId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var batch = new BatchEvaluasi(
            id, proyekId, komponenId, 1, "hash123", "v2.0",
            KriteriaBobot.Default, 100.0, now);

        Assert.Equal(id, batch.Id);
        Assert.Equal(proyekId, batch.ProyekId);
        Assert.Equal(komponenId, batch.KomponenId);
        Assert.Equal(1, batch.RevisiLokasi);
        Assert.Equal("hash123", batch.HashInput);
        Assert.Equal("v2.0", batch.VersiModel);
        Assert.Equal(100.0, batch.LuasM2);
        Assert.Equal(now, batch.DihitungPadaUtc);
    }

    [Fact]
    public void BatchEvaluasi_RevisiAtauLuasTidakValid_MelemparPengecualian()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatchEvaluasi(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, "hash", "v2.0",
            KriteriaBobot.Default, 100.0, DateTimeOffset.UtcNow));

        Assert.Throws<ArgumentOutOfRangeException>(() => new BatchEvaluasi(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "hash", "v2.0",
            KriteriaBobot.Default, -10.0, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Evaluasi_AdditiveFields_BisaDiisiAtauDefaultNull()
    {
        var eDefault = new Evaluasi(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            80.0, 1500.0, new SubSkorKesesuaian(80, 80, 80, 80), "Cocok");

        Assert.Null(eDefault.BatchId);
        Assert.Null(eDefault.VersiModel);
        Assert.Null(eDefault.HashInput);
        Assert.Null(eDefault.DihitungPadaUtc);

        var batchId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var eExtended = new Evaluasi(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            85.0, 1200.0, new SubSkorKesesuaian(85, 85, 85, 85), "Sangat cocok",
            batchId, "v2.0", "hashabc", now);

        Assert.Equal(batchId, eExtended.BatchId);
        Assert.Equal("v2.0", eExtended.VersiModel);
        Assert.Equal("hashabc", eExtended.HashInput);
        Assert.Equal(now, eExtended.DihitungPadaUtc);
    }

    [Fact]
    public void SimulasiEmisi_AdditiveFields_BisaDiisiAtauDefault()
    {
        var evaluasiId = Guid.NewGuid();
        var sDefault = new SimulasiEmisi(evaluasiId, 30, 25.0, 1, 3000.0);

        Assert.Equal("v2.0", sDefault.VersiModel);
        Assert.Equal(0.5, sDefault.FaktorPenuaanA);
        Assert.Null(sDefault.DihitungPadaUtc);

        var now = DateTimeOffset.UtcNow;
        var sExtended = new SimulasiEmisi(evaluasiId, 50, 20.0, 2, 5000.0, "v2.1", 0.7, now);

        Assert.Equal("v2.1", sExtended.VersiModel);
        Assert.Equal(0.7, sExtended.FaktorPenuaanA);
        Assert.Equal(now, sExtended.DihitungPadaUtc);
    }

    [Fact]
    public void DailyForecast_AdditiveFields_BisaDiisiAtauDefault()
    {
        var tgl = new DateOnly(2026, 11, 23);
        var fDefault = new DailyForecast(tgl, 30, 2.0, 24, 32, 75);

        Assert.True(fDefault.DataLengkap);
        Assert.Null(fDefault.CatatanKualitas);

        var fCustom = new DailyForecast(tgl, 30, 2.0, 24, 32, 75, DataLengkap: false, CatatanKualitas: "Data parsial");
        Assert.False(fCustom.DataLengkap);
        Assert.Equal("Data parsial", fCustom.CatatanKualitas);
    }

    [Fact]
    public async Task DailyForecastClient_DefaultMetadataMethod_MengembalikanHasilPrakiraan()
    {
        IDailyForecastClient stub = new FakeForecastClient();
        HasilPrakiraan hasil = await stub.GetForecastWithMetadataAsync(-6.2, 106.8, 7);

        Assert.NotNull(hasil);
        Assert.Single(hasil.Hari);
        Assert.Equal("Open-Meteo", hasil.Penyedia);
        Assert.Equal("Asia/Jakarta", hasil.ZonaWaktu);
    }

    [Fact]
    public async Task WorkSchedulingService_DefaultMethods_BerfungsiTanpaError()
    {
        IWorkSchedulingService stub = new FakeSchedulingService();
        var id = Guid.NewGuid();
        var tgl = new DateOnly(2026, 11, 23);

        var detailed = await stub.EvaluateDaysDetailedAsync(id);
        Assert.Empty(detailed);

        var konfirmasi = new KonfirmasiPenguncian(SetujuiRisiko: true, SetujuiDataLama: false, "Persetujuan mandiri");
        await stub.LockDateAsync(id, tgl, konfirmasi); // Menjalankan delegasi bawaan
    }

    [Fact]
    public async Task ReportExportService_DefaultMethods_BerfungsiTanpaError()
    {
        IReportExportService stub = new FakeReportService();
        var id = Guid.NewGuid();

        var reports = await stub.ListReportsAsync(id);
        Assert.Empty(reports);

        var report = await stub.GetReportByIdAsync(id);
        Assert.Null(report);
    }

    [Fact]
    public void OpsiEksporDanLaporan_AdditiveFields_TersimpanLengkap()
    {
        var proyekId = Guid.NewGuid();
        var kompId = Guid.NewGuid();
        var bahanId = Guid.NewGuid();

        var opsi = new OpsiEkspor(30, true, "/tmp/out.pdf", [kompId], [bahanId], [30, 50], "snap123");
        Assert.Equal(30, opsi.HorizonTahun);
        Assert.True(opsi.SertakanJadwal);
        Assert.Equal(kompId, Assert.Single(opsi.KomponenDipilih!));
        Assert.Equal(bahanId, Assert.Single(opsi.BahanDipilih!));
        Assert.Equal(2, opsi.HorizonDipilih!.Count);
        Assert.Equal("snap123", opsi.SnapshotHash);

        var laporan = new Laporan(Guid.NewGuid(), proyekId, "/tmp/out.pdf", DateTimeOffset.UtcNow, "sha256abc", "snap123", 1024, "Tersimpan");
        Assert.Equal("sha256abc", laporan.ChecksumSha256);
        Assert.Equal("snap123", laporan.SnapshotHash);
        Assert.Equal(1024, laporan.UkuranByte);
        Assert.Equal("Tersimpan", laporan.Status);
    }

    private class FakeForecastClient : IDailyForecastClient
    {
        public Task<IReadOnlyList<DailyForecast>> GetForecastAsync(double lat, double lon, int days, CancellationToken ct = default)
        {
            IReadOnlyList<DailyForecast> list = [new DailyForecast(new DateOnly(2026, 11, 23), 20, 0, 24, 32, 70)];
            return Task.FromResult(list);
        }
    }

    private class FakeSchedulingService : IWorkSchedulingService
    {
        public Task<IReadOnlyList<PenilaianKesiapanHari>> EvaluateDaysAsync(Guid pekerjaanId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PenilaianKesiapanHari>>([]);

        public Task LockDateAsync(Guid pekerjaanId, DateOnly tanggal, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private class FakeReportService : IReportExportService
    {
        public Task<Laporan> ExportAsync(Guid proyekId, OpsiEkspor opsi, CancellationToken ct = default)
            => Task.FromResult(new Laporan(Guid.NewGuid(), proyekId, "dummy.pdf", DateTimeOffset.UtcNow));
    }
}
