using Wastu.Domain;
using Wastu.Domain.Climate;
using Wastu.Domain.Scoring;
using Wastu.Domain.Tests.Fixtures;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Scoring;

/// <summary>
/// Pengujian otomatis untuk WP04b (T14–T17, deduplikasi batch evaluasi, bobot per proyek, dan alasan Indonesia).
/// </summary>
public class ComparisonWeightsUiTests
{
    [Fact]
    public async Task T14_BahanSkorRendah_TetapTampilDenganAlasanKelemahanIndonesia()
    {
        // Bahan dengan ketahanan lembap & korosi rendah (Panel Gipsum)
        Bahan gipsum = DataContoh.PanelGipsum();
        Bahan beton = DataContoh.BetonBertulang();
        Bahan hebel = DataContoh.BetonRingan();

        var materialRepo = new MaterialRepositoryInMemory([gipsum, beton, hebel]);
        var proyekRepo = new ProyekRepositoryInMemory();
        var komponen = DataContoh.Dinding();
        await proyekRepo.AddKomponenAsync(komponen);
        await proyekRepo.SimpanProfilIklimAsync(DataContoh.ProyekId, DataContoh.IklimPesisirBandarLampung());

        var batchRepo = new BatchEvaluasiRepositoryPerekam();
        var service = new SuitabilityScoringService(materialRepo, proyekRepo, batchRepo, batchRepo);

        // Uji RankAsync dan RankMaterialsAsync
        var hasil = await service.RankMaterialsAsync(komponen.Id);

        // Seluruh bahan valid harus tetap tampil (tidak difilter)
        Assert.Equal(3, hasil.Count);

        var evalGipsum = hasil.Single(e => e.BahanId == gipsum.Id);
        // Gipsum di iklim pesisir berat harus mendapat skor < 55 ("Tidak Disarankan")
        Assert.True(evalGipsum.SkorKesesuaian < 55.0, $"Skor gipsum {evalGipsum.SkorKesesuaian} seharusnya < 55");
        Assert.Equal(LabelKesesuaian.TidakDisarankan, KonfigurasiModel.Bawaan.LabelUntuk(evalGipsum.SkorKesesuaian));

        // Alasan bahasa Indonesia harus ada dan menyebutkan kelemahan spesifik
        Assert.False(string.IsNullOrWhiteSpace(evalGipsum.Alasan));
        Assert.Contains("kurang sesuai", evalGipsum.Alasan);
        Assert.Contains("proksi risiko udara bergaram", evalGipsum.Alasan);

        // Bahan memadai (hebel/beton) mendapat alasan positif
        var evalHebel = hasil.Single(e => e.BahanId == hebel.Id);
        Assert.Contains("memadai", evalHebel.Alasan);
    }

    [Fact]
    public void T15_NormalisasiUsiaMemakaiSeluruhKategori_BukanHanyaSubsetPencarian()
    {
        Bahan b20 = new(Guid.NewGuid(), "Bahan20", JenisKomponen.Dinding, 0.20, 3, 3, 20, 50, "Sumber");
        Bahan b30 = new(Guid.NewGuid(), "Bahan30", JenisKomponen.Dinding, 0.20, 3, 3, 30, 50, "Sumber");
        Bahan b40 = new(Guid.NewGuid(), "Bahan40", JenisKomponen.Dinding, 0.20, 3, 3, 40, 50, "Sumber");

        Komponen komp = new(Guid.NewGuid(), Guid.NewGuid(), JenisKomponen.Dinding, 100);
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung();

        // Evaluasi katalog lengkap
        var hasilLengkap = SuitabilityScoringService.Rank(komp, iklim, [b20, b30, b40]);
        var eval30Lengkap = hasilLengkap.Single(e => e.BahanId == b30.Id);
        Assert.Equal(50.0, eval30Lengkap.SubSkor.Usia, 4);

        // Filter tampilan (misalnya user mencari "30" sehingga hanya 1 atau 2 bahan yang ditampilkan)
        // Normalisasi usia bahan 30 tetap 50 karena dievaluasi terhadap populasi penuh
        var filterTampilan = hasilLengkap.Where(e => e.BahanId == b30.Id).ToList();
        Assert.Single(filterTampilan);
        Assert.Equal(50.0, filterTampilan[0].SubSkor.Usia, 4);
    }

    [Fact]
    public void T15_LuasKomponenDuaKaliLipat_MenggandakanEmisiAwalTanpaMengubahSkorAtauUrutan()
    {
        Bahan b1 = DataContoh.BetonBertulang(); // faktor emisi 120
        Bahan b2 = DataContoh.BetonRingan();   // faktor emisi 60
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung();

        Komponen komp120 = new(Guid.NewGuid(), Guid.NewGuid(), JenisKomponen.Dinding, 120.0);
        Komponen komp240 = new(Guid.NewGuid(), Guid.NewGuid(), JenisKomponen.Dinding, 240.0);

        var hasil120 = SuitabilityScoringService.Rank(komp120, iklim, [b1, b2]);
        var hasil240 = SuitabilityScoringService.Rank(komp240, iklim, [b1, b2]);

        Assert.Equal(hasil120.Count, hasil240.Count);
        for (int i = 0; i < hasil120.Count; i++)
        {
            // Urutan bahan sama
            Assert.Equal(hasil120[i].BahanId, hasil240[i].BahanId);
            // Skor kesesuaian tidak berubah sama sekali
            Assert.Equal(hasil120[i].SkorKesesuaian, hasil240[i].SkorKesesuaian, 6);
            // Emisi awal tepat dua kali lipat
            Assert.Equal(hasil120[i].EmisiAwalKg * 2.0, hasil240[i].EmisiAwalKg, 6);
        }

        // Contoh konkret PRD: faktor 120 kgCO2e/m2, luas 120 -> 14.400, luas 240 -> 28.800
        var evalBeton120 = hasil120.Single(e => e.BahanId == b1.Id);
        var evalBeton240 = hasil240.Single(e => e.BahanId == b1.Id);
        Assert.Equal(14400.0, evalBeton120.EmisiAwalKg, 4);
        Assert.Equal(28800.0, evalBeton240.EmisiAwalKg, 4);
    }

    [Fact]
    public void T16_BobotDefaultMvpDanKriteriaBobotValidation()
    {
        // MVP: 0.25 masing-masing
        Assert.Equal(0.25, KriteriaBobot.Default.Termal);
        Assert.Equal(0.25, KriteriaBobot.Default.Lembap);
        Assert.Equal(0.25, KriteriaBobot.Default.Korosi);
        Assert.Equal(0.25, KriteriaBobot.Default.Usia);
        Assert.Equal(1.0, KriteriaBobot.Default.Jumlah, 9);

        // V2 valid: 40, 30, 20, 10
        var v2 = KriteriaBobot.DariPersentase(40, 30, 20, 10);
        Assert.Equal(0.40, v2.Termal, 9);
        Assert.Equal(0.30, v2.Lembap, 9);
        Assert.Equal(0.20, v2.Korosi, 9);
        Assert.Equal(0.10, v2.Usia, 9);

        // Invalid: sum 99 atau 101 ditolak
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(25, 25, 25, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(25, 25, 25, 26));

        // Invalid: negatif, NaN, Infinity, all zero ditolak
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(-10, 50, 30, 30));
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(double.NaN, 50, 25, 25));
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(double.PositiveInfinity, 50, 25, 25));
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(0, 0, 0, 0));

        // With-bypass ditolak saat dipanggil PastikanValid atau saat dievaluasi
        var withBypass = KriteriaBobot.Default with { Termal = 0.50 };
        Assert.Throws<ArgumentOutOfRangeException>(() => withBypass.PastikanValid());
        var sub = new SubSkorKesesuaian(70, 70, 70, 70);
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSkor.SkorKesesuaian(sub, withBypass));
    }

    [Fact]
    public async Task T17_BobotKustom_ReRankTanpaPanggilanIklim_DanDeduplikasiBatch()
    {
        Bahan b1 = DataContoh.BetonBertulang();
        Bahan b2 = DataContoh.BetonRingan();
        var materialRepo = new MaterialRepositoryInMemory([b1, b2]);
        var proyekRepo = new ProyekRepositoryInMemory();

        Guid realProyekId = Guid.NewGuid();
        Guid realKompId = Guid.NewGuid();
        Komponen komp = new(realKompId, realProyekId, JenisKomponen.Dinding, 100.0);
        await proyekRepo.AddKomponenAsync(komp);
        await proyekRepo.SimpanProfilIklimAsync(realProyekId, DataContoh.IklimPesisirBandarLampung());

        var batchRepo = new BatchEvaluasiRepositoryPerekam();
        var service = new SuitabilityScoringService(materialRepo, proyekRepo, batchRepo, batchRepo);

        // Peringkat awal dengan bobot default MVP (0.25)
        var rankMvp = await service.RankMaterialsAsync(realKompId, KriteriaBobot.Default);
        Assert.Equal(1, batchRepo.JumlahSimpanBatch);
        Assert.All(rankMvp, e =>
        {
            Assert.NotEqual(Guid.Empty, e.KomponenId);
            Assert.NotNull(e.BatchId);
            Assert.NotEqual(Guid.Empty, e.BatchId.Value);
        });

        // Peringkat ulang dengan bobot yang sama -> DEDUPLIKASI (tidak membuat batch baru)
        var rankUlang = await service.RankMaterialsAsync(realKompId, KriteriaBobot.Default);
        Assert.Equal(1, batchRepo.JumlahSimpanBatch);
        Assert.Equal(rankMvp.Select(e => e.Id), rankUlang.Select(e => e.Id));

        // Ubah bobot V2 (misal fokus termal 70%, sub-skor lain 10%)
        var bobotTermal = new KriteriaBobot(0.70, 0.10, 0.10, 0.10);
        var rankTermal = await service.RankMaterialsAsync(realKompId, bobotTermal);

        // Batch baru dibuat karena hash input berubah
        Assert.Equal(2, batchRepo.JumlahSimpanBatch);

        // Skor berbeda karena bobot berubah
        Assert.NotEqual(rankMvp[0].SkorKesesuaian, rankTermal[0].SkorKesesuaian);

        // Pengulangan dengan bobot V2 yang sama -> DEDUPLIKASI
        await service.RankMaterialsAsync(realKompId, bobotTermal);
        Assert.Equal(2, batchRepo.JumlahSimpanBatch);
    }
}
