using System.Text.Json;
using UglyToad.PdfPig;
using Wastu.Domain;
using Wastu.Domain.Services;
using Wastu.InMemory;
using Xunit;

namespace Wastu.Reporting.Tests;

public sealed class ReportHistoryIntegrityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Wastu_ReportHistory_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ExportByProjectUsesOneOriginalBatchWithActualWeightsWithoutCurrentCatalog()
    {
        Directory.CreateDirectory(_root);
        var p = new Proyek(Guid.NewGuid(), "Rumah riwayat", "Ambon", -3.7, 128.2, zonaWaktu: "Asia/Jayapura");
        var c = new Komponen(Guid.NewGuid(), p.Id, JenisKomponen.Dinding, 120);
        var b = new Bahan(Guid.NewGuid(), "Nama asli", JenisKomponen.Dinding, 0.2, 3, 4, 15, 10,
            "Referensi asli", sumberProperti: [new(Guid.Empty, "usia_pakai_nominal", "Rujukan usia", "v1")]);
        var climate = new ProfilIklim(700, 80, 26, 50, TingkatKorosi.Rendah);
        var weights = new KriteriaBobot(.1, .2, .3, .4);
        var input = new InputEvaluasi(c, b, climate, weights, KonfigurasiModel.Bawaan, 15, 50, p, [b.Id]);
        var latest = new Evaluasi(Guid.NewGuid(), b.Id, c.Id, 20, 1000, new(20, 20, 20, 20),
            "Alasan asli", Guid.NewGuid(), "v2.0", "latest", DateTimeOffset.UtcNow, input);
        var old = latest with { Id = Guid.NewGuid(), BatchId = Guid.NewGuid(), HashInput = "old",
            SkorKesesuaian = 99, DihitungPadaUtc = DateTimeOffset.UtcNow.AddDays(-1) };
        var projects = new ProyekRepositoryInMemory();
        await projects.AddAsync(p, komponen: [c]);
        var evals = new EvaluasiRepositoryInMemory();
        await evals.SimpanAsync([old, latest]);
        var reports = new LaporanRepositoryInMemory();
        var service = new ReportExportService(reports, projects, evals,
            new MaterialRepositoryInMemory([]), pendingMetadataDirectory: Path.Combine(_root, "pending"));
        var result = await service.ExportAsync(p.Id, new(30, false, Path.Combine(_root, "original.pdf")));
        var snapshot = JsonSerializer.Deserialize<SnapshotLaporan>(result.SnapshotJson!)!;
        var material = Assert.Single(snapshot.BahanTerpilih);
        Assert.Equal(20, material.SkorKesesuaian);
        Assert.Equal("Nama asli", material.Nama);
        Assert.Equal(weights, snapshot.Bobot);
        Assert.Equal(3, Assert.Single(snapshot.SimulasiHorizon).JumlahPenggantian);
        Assert.Equal(4000, snapshot.SimulasiHorizon[0].EmisiKumulatifKg);
        Assert.Equal(15, snapshot.SimulasiHorizon[0].UsiaPakaiNominal);
        Assert.Equal("WIT", snapshot.LabelZona);
        Assert.Equal(snapshot.HitungHash(), result.SnapshotHash);
        using (var pdf = PdfDocument.Open(result.PathBerkas))
            Assert.Contains("WIT", string.Join(" ", pdf.GetPages().Select(x => x.Text)));

        // Restart from persisted metadata; current catalog/profile/project no longer participate.
        var restored = JsonSerializer.Deserialize<Laporan>(JsonSerializer.Serialize(result))!;
        var restarted = new ReportExportService(reports, pendingMetadataDirectory: Path.Combine(_root, "pending"));
        File.Delete(result.PathBerkas);
        var copy = await restarted.RegenerateAsync(restored, Path.Combine(_root, "copy.pdf"));
        Assert.True(File.Exists(copy.PathBerkas));
        Assert.Equal(result.Id, copy.Id);
        Assert.Equal(result.DibuatPada, copy.DibuatPada);
        Assert.Equal(result.SnapshotHash, copy.SnapshotHash);
        Assert.Equal(result.SnapshotJson, copy.SnapshotJson);
        Assert.Single(await reports.ListByProyekAsync(p.Id));
        Assert.Equal(copy.PathBerkas, (await reports.GetByIdAsync(copy.Id))!.PathBerkas);
        await Assert.ThrowsAsync<InvalidOperationException>(() => restarted.RegenerateAsync(
            restored with { SnapshotJson = restored.SnapshotJson!.Replace("Nama asli", "Nama berubah") }, Path.Combine(_root, "invalid.pdf")));
        Assert.False(File.Exists(Path.Combine(_root, "invalid.pdf")));
        await evals.SimpanAsync([latest with { Input = input with { BahanKategori = [b.Id, Guid.NewGuid()] } }]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(p.Id,
            new(30, false, Path.Combine(_root, "incomplete.pdf"))));
        Assert.False(File.Exists(Path.Combine(_root, "incomplete.pdf")));
    }

    [Fact]
    public async Task LegacyReportCannotBeRegeneratedFromGuessedCurrentValues()
    {
        var service = new ReportExportService(pendingMetadataDirectory: Path.Combine(_root, "pending"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegenerateAsync(
            new(Guid.NewGuid(), Guid.NewGuid(), "missing.pdf", DateTimeOffset.UtcNow), Path.Combine(_root, "legacy.pdf")));
        Assert.False(Directory.Exists(_root));
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
