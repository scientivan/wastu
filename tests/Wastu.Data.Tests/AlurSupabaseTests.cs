using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Data.Repositories;
using Wastu.Data.Tests.Fakes;
using Wastu.Domain;
using Wastu.Domain.Climate;
using Wastu.Domain.Emissions;
using Wastu.Domain.Projects;
using Wastu.Domain.Scoring;

namespace Wastu.Data.Tests;

public class AlurSupabaseTests
{
    [PerluSupabase]
    public async Task AlurProyekSampaiSimulasiBerjalanDiAtasSupabase()
    {
        string connectionString = KoneksiSupabase.Ambil();
        var configuration = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        var schema = Environment.GetEnvironmentVariable("WASTU_SUPABASE_TEST_SCHEMA");
        if (schema is null || !System.Text.RegularExpressions.Regex.IsMatch(schema, "^wastu_v2_test_[a-f0-9]{32}$")
            || configuration.SearchPath != schema)
            throw new InvalidOperationException("Uji Supabase hanya boleh menulis pada schema uji yang ditetapkan harness.");

        var pabrik = new WastuDbContextFactory(connectionString);
        var material = new MaterialRepositoryEf(pabrik);
        var proyekRepo = new ProyekRepositoryEf(pabrik);
        var evaluasiRepo = new EvaluasiRepositoryEf(pabrik);

        var iklim = new ClimateAnalysisService(proyekRepo, new KlienIklimUji());
        var proyekService = new ProjectService(proyekRepo, iklim);
        var skor = new SuitabilityScoringService(material, proyekRepo);
        var simulasi = new EmissionSimulationService(evaluasiRepo, material);

        Guid proyekId = Guid.Empty;

        try
        {
            // F1
            Proyek rumah = await proyekService.CreateProjectAsync(
                $"Uji Supabase {Guid.NewGuid():N}", "Bandar Lampung", -5.45, 105.26, 120, 90);
            proyekId = rumah.Id;

            Assert.Contains(await proyekService.ListAsync(), p => p.Id == rumah.Id);
            Komponen dinding = Assert.Single(await proyekRepo.ListKomponenAsync(rumah.Id),
                k => k.Jenis == JenisKomponen.Dinding);
            Assert.Equal(120, dinding.LuasM2);

            // F3
            ProfilIklim profil = await iklim.GetOrFetchProfileAsync(rumah.Id);
            ProfilIklim? tersimpan = await proyekRepo.GetProfilIklimAsync(rumah.Id);
            Assert.NotNull(tersimpan);
            Assert.Equal(profil.CurahHujanTahunanMm, tersimpan!.CurahHujanTahunanMm, 6);

            // F2
            IReadOnlyList<Bahan> katalog = await material.ListByKindAsync(JenisKomponen.Dinding);
            Assert.Equal(15, katalog.Count);

            // F4
            IReadOnlyList<Evaluasi> peringkat = await skor.RankAsync(dinding.Id);
            Assert.Equal(15, peringkat.Count);
            await evaluasiRepo.SimpanAsync(peringkat);

            IReadOnlyList<Evaluasi> dibacaUlang = await evaluasiRepo.ListByKomponenAsync(dinding.Id);
            Assert.Equal(15, dibacaUlang.Count);

            Evaluasi teratas = dibacaUlang.OrderByDescending(e => e.SkorKesesuaian).First();
            Assert.InRange(teratas.SkorKesesuaian, 0, 100);
            Assert.False(string.IsNullOrWhiteSpace(teratas.Alasan));
            Assert.True(teratas.SubSkor.Termal >= 0);

            // F5
            SimulasiEmisi hasil = await simulasi.SimulateAsync(teratas.Id, horizonTahun: 30);
            Assert.Equal(30, hasil.HorizonTahun);

            await using WastuDbContext db = pabrik.CreateDbContext();
            SimulasiEmisiEntity? baris = await db.SimulasiEmisi.AsNoTracking()
                .FirstOrDefaultAsync(x => x.EvaluasiId == teratas.Id);

            Assert.NotNull(baris);
            Assert.Equal(hasil.EmisiKumulatifKg, baris!.EmisiKumulatifKg, 6);

            var profilBaru = await proyekService.UpdateProjectAsync(rumah.Id, "Rumah Baru", "Jakarta",
                -6.20, 106.82, 150, 100);
            Assert.NotNull(profilBaru);
            Proyek? diedit = await proyekRepo.GetByIdAsync(rumah.Id);
            Assert.Equal("Rumah Baru", diedit!.Nama);
            Assert.Equal("Jakarta", diedit.Kota);
            Assert.Equal(150, (await proyekRepo.GetKomponenAsync(dinding.Id))!.LuasM2);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => proyekRepo.SimpanProfilIklimAsync(rumah.Id, profil, lokasi: rumah));

            await proyekService.DeleteAsync(rumah.Id);
            Assert.DoesNotContain(await proyekService.ListAsync(), p => p.Id == rumah.Id);
            Assert.Empty(await proyekRepo.ListKomponenAsync(rumah.Id));
            Assert.Null(await proyekRepo.GetProfilIklimAsync(rumah.Id));
        }
        finally
        {
            await BersihkanAsync(pabrik, proyekId);
        }
    }

    private static async Task BersihkanAsync(WastuDbContextFactory pabrik, Guid proyekId)
    {
        if (proyekId == Guid.Empty)
        {
            return;
        }

        await using WastuDbContext db = pabrik.CreateDbContext();
        ProyekEntity? baris = await db.Bangunan.FirstOrDefaultAsync(x => x.Id == proyekId);
        if (baris is null)
        {
            return;
        }

        db.Bangunan.Remove(baris);
        await db.SaveChangesAsync();
    }
}
