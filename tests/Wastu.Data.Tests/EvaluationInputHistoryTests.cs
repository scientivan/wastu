using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wastu.Data.Repositories;
using Wastu.Domain;
using Wastu.Domain.Emissions;
using Wastu.Domain.Scoring;
using Wastu.InMemory;

namespace Wastu.Data.Tests;

public sealed class EvaluationInputHistoryTests
{
    [PerluPostgreSql]
    public async Task UpgradeDariSkemaSebelumnyaMempertahankanHasilLegacyTanpaMengarangSnapshot()
    {
        await using var isolated = await PostgreSqlTestDatabase.CreateAsync();
        await using (var db = isolated.Factory.CreateDbContext())
        {
            var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions
                .GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db);
            await migrator.MigrateAsync("20261122110422_SeedCanonicalCatalogProvenance");
            var applied = await db.Database.GetAppliedMigrationsAsync();
            Assert.DoesNotContain(applied, m => m.Contains("PreserveEvaluationInputs"));
        }
        // Seed the legacy shape before the new nullable column exists.
        await isolated.ExecuteSqlScriptAsync(File.ReadAllText(FindSeed()).Replace(
            "ON CONFLICT (evaluasi_id, horizon_tahun, versi_model, faktor_penuaan_a)",
            "ON CONFLICT (evaluasi_id, horizon_tahun, versi_model)"));
        Guid[] ids;
        await using (var db = isolated.Factory.CreateDbContext())
        {
            ids = await db.Evaluasi.Select(e => e.Id).ToArrayAsync();
            await db.Database.MigrateAsync();
        }
        Assert.NotEmpty(ids);
        var repo = new EvaluasiRepositoryEf(isolated.Factory);
        foreach (var id in ids) Assert.Null((await repo.GetByIdAsync(id))!.Input);
        await using var check = isolated.Factory.CreateDbContext();
        Assert.Equal(ids.Order(), (await check.Evaluasi.Select(e => e.Id).ToArrayAsync()).Order());
    }

    private static string FindSeed()
    {
        var folder = AppContext.BaseDirectory;
        while (folder is not null)
        {
            var path = Path.Combine(folder, "database", "03_seed_supabase.sql");
            if (File.Exists(path)) return path;
            folder = Directory.GetParent(folder)?.FullName;
        }
        throw new FileNotFoundException("Seed fixture not found.");
    }

    [PerluPostgreSql]
    public async Task MasukanAsliBertahanSetelahKatalogLokasiDanAsumsiBerubah()
    {
        await using var isolated = await PostgreSqlTestDatabase.CreateAsync();
        await using (var db = isolated.Factory.CreateDbContext()) await db.Database.MigrateAsync();
        var projects = new ProyekRepositoryEf(isolated.Factory);
        var materials = new MaterialRepositoryEf(isolated.Factory);
        var evaluations = new EvaluasiRepositoryEf(isolated.Factory);
        var batches = new BatchEvaluasiRepositoryEf(isolated.Factory);
        var p = new Proyek(Guid.NewGuid(), "Riwayat asli", "Ambon", -3.7, 128.2, zonaWaktu: "Asia/Jayapura");
        var c = new Komponen(Guid.NewGuid(), p.Id, JenisKomponen.Dinding, 120);
        await projects.AddAsync(p, komponen: [c]);
        var climate = new ProfilIklim(700, 80, 26, 50, TingkatKorosi.Rendah,
            new MetadataIklim(new(2016, 1, 1), new(2025, 12, 31), "Fixture historis", "v1",
                new DateTimeOffset(2026, 11, 22, 0, 0, 0, TimeSpan.Zero), "Asia/Jayapura", "Segmen fixture",
                ProvenanceIklim.Contoh, StatusPengambilanIklim.Tersedia, 1), 1, ProvenanceIklim.Contoh);
        await projects.SimpanProfilIklimAsync(p.Id, climate);
        var service = new SuitabilityScoringService(materials, projects, batches, evaluations);
        var first = await service.RankAsync(c.Id);
        var original = first[0];
        Assert.NotNull(original.Input);

        await using (var db = isolated.Factory.CreateDbContext())
        {
            var item = await db.Bahan.SingleAsync(x => x.Id == original.BahanId);
            item.UsiaPakaiNominalTahun = 999;
            item.FaktorEmisi = 999;
            await db.SaveChangesAsync();
        }
        var newer = await service.RankAsync(c.Id);
        Assert.NotEqual(original.BatchId, newer[0].BatchId);
        var restored = (await evaluations.GetByIdAsync(original.Id))!;
        Assert.Equal(original.Input!.Bahan.UsiaPakaiNominalTahun, restored.Input!.Bahan.UsiaPakaiNominalTahun);
        Assert.Equal(original.Input.Bahan.FaktorEmisi, restored.Input.Bahan.FaktorEmisi);
        Assert.Equal("Asia/Jayapura", restored.Input.Iklim.Metadata!.ZonaWaktu);
        Assert.Equal("Fixture historis", restored.Input.Iklim.Metadata.Penyedia);
        Assert.Equal(ProvenanceIklim.Contoh, restored.Input.Iklim.Provenance);
        Assert.Equal(p, restored.Input.Proyek);
        Assert.Equal(120, restored.Input.Komponen.LuasM2);
        Assert.NotEmpty(restored.Input.Bahan.SumberProperti!);

        var simulation = new EmissionSimulationService(evaluations, materials);
        var expected = PerhitunganSimulasi.Simulasikan(original.Id, original,
            original.Input.Bahan.UsiaPakaiNominalTahun, 30);
        var actual = await simulation.SimulateAsync(original.Id, 30);
        Assert.Equal(expected.EmisiKumulatifKg, actual.EmisiKumulatifKg);
        await new EmissionSimulationService(evaluations, materials,
            new KonfigurasiModel(faktorPenuaanA: 0.7)).SimulateAsync(original.Id, 30);
        await using var check = isolated.Factory.CreateDbContext();
        Assert.Equal(2, await check.SimulasiEmisi.CountAsync(x => x.EvaluasiId == original.Id));
    }

    [Fact]
    public void PerubahanSumberDanProvenanceMembuatIdentitasBaruTanpaMengubahSkor()
    {
        var c = new Komponen(Guid.NewGuid(), Guid.NewGuid(), JenisKomponen.Dinding, 120);
        var b = KatalogDemo.Semua.First(x => x.Jenis == JenisKomponen.Dinding);
        var climate = new ProfilIklim(700, 80, 26, 50, TingkatKorosi.Rendah);
        var first = SuitabilityScoringService.Rank(c, climate, [b])[0];
        var changed = b with { SumberData = "Referensi revisi" };
        var second = SuitabilityScoringService.Rank(c, climate, [changed])[0];
        Assert.Equal(first.SkorKesesuaian, second.SkorKesesuaian);
        Assert.NotEqual(SuitabilityScoringService.HitungHash(c, climate, [b], KriteriaBobot.Default),
            SuitabilityScoringService.HitungHash(c, climate, [changed], KriteriaBobot.Default));
        Assert.NotEqual(SuitabilityScoringService.HitungHash(c, climate, [b], KriteriaBobot.Default),
            SuitabilityScoringService.HitungHash(c, climate with { Provenance = ProvenanceIklim.Contoh }, [b], KriteriaBobot.Default));
        var roundtrip = JsonSerializer.Deserialize<Evaluasi>(JsonSerializer.Serialize(first))!;
        Assert.Equal(b.Nama, roundtrip.Input!.Bahan.Nama);
        Assert.Equal(700, roundtrip.Input.Iklim.CurahHujanTahunanMm);
    }
}
