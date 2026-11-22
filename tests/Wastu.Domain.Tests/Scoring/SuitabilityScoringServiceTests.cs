using Wastu.Domain;
using Wastu.Domain.Scoring;
using Wastu.Domain.Tests.Fixtures;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Scoring;

public class SuitabilityScoringServiceTests
{
    [Fact]
    public async Task Rank_MenormalisasiUsiaTerhadapKategoriKomponen()
    {
        var pendek = DataContoh.PanelGipsum();
        var panjang = DataContoh.BetonBertulang();
        var service = Service(pendek, panjang, DataContoh.GentingKeramik());

        var hasil = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(0, hasil.Single(e => e.BahanId == pendek.Id).SubSkor.Usia);
        Assert.Equal(100, hasil.Single(e => e.BahanId == panjang.Id).SubSkor.Usia);
    }

    [Fact]
    public async Task Rank_T14_NormalisasiUsiaMemakaiSeluruhKategoriBukanSubset()
    {
        // T14: tiga bahan 20/30/40 tahun harus menjadi 0/50/100. Setelah hasil
        // disaring untuk menampilkan dua bahan, nilai acuannya tidak boleh berubah.
        Bahan pendek = Bahan(Guid.NewGuid(), "Pendek", usia: 20);
        Bahan sedang = Bahan(Guid.NewGuid(), "Sedang", usia: 30);
        Bahan panjang = Bahan(Guid.NewGuid(), "Panjang", usia: 40);

        IReadOnlyList<Evaluasi> hasil = await Service(pendek, sedang, panjang)
            .RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(0, hasil.Single(e => e.BahanId == pendek.Id).SubSkor.Usia);
        Assert.Equal(50, hasil.Single(e => e.BahanId == sedang.Id).SubSkor.Usia);
        Assert.Equal(100, hasil.Single(e => e.BahanId == panjang.Id).SubSkor.Usia);

        // Tampilan boleh menyaring dua baris; skor usia tetap memakai acuan penuh.
        Evaluasi[] subset = hasil.Where(e => e.BahanId != panjang.Id).ToArray();
        Assert.Equal(0, subset.Single(e => e.BahanId == pendek.Id).SubSkor.Usia);
        Assert.Equal(50, subset.Single(e => e.BahanId == sedang.Id).SubSkor.Usia);
    }

    [Fact]
    public void Rank_T14_UrutanKatalogTidakMengubahNormalisasiUsia()
    {
        Bahan pendek = Bahan(Guid.NewGuid(), "Pendek", usia: 20);
        Bahan sedang = Bahan(Guid.NewGuid(), "Sedang", usia: 30);
        Bahan panjang = Bahan(Guid.NewGuid(), "Panjang", usia: 40);

        IReadOnlyList<Evaluasi> normal = SuitabilityScoringService.Rank(
            DataContoh.Dinding(), DataContoh.IklimPesisirBandarLampung(), [pendek, sedang, panjang]);
        IReadOnlyList<Evaluasi> terbalik = SuitabilityScoringService.Rank(
            DataContoh.Dinding(), DataContoh.IklimPesisirBandarLampung(), [panjang, pendek, sedang]);

        foreach (Bahan bahan in new[] { pendek, sedang, panjang })
        {
            Assert.Equal(
                normal.Single(e => e.BahanId == bahan.Id).SubSkor.Usia,
                terbalik.Single(e => e.BahanId == bahan.Id).SubSkor.Usia);
        }
    }

    [Fact]
    public async Task Rank_MengurutkanDariSkorTertinggi()
    {
        // Pada kelompok ini, usia terpanjang dan ketahanan korosi mengunggulkan beton bertulang.
        SuitabilityScoringService service = Service(
            DataContoh.BetonBertulang(), DataContoh.BetonRingan(), DataContoh.PanelGipsum());

        IReadOnlyList<Evaluasi> hasil = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(3, hasil.Count);
        Assert.Equal(DataContoh.IdBetonBertulang, hasil[0].BahanId);
        Assert.Equal(DataContoh.BetonRingan().Id, hasil[1].BahanId);
        Assert.Equal(DataContoh.PanelGipsum().Id, hasil[2].BahanId);

        for (int i = 1; i < hasil.Count; i++)
        {
            Assert.True(
                hasil[i - 1].SkorKesesuaian >= hasil[i].SkorKesesuaian,
                $"Urutan tidak menurun pada indeks {i - 1} ke {i}.");
        }
    }

    [Fact]
    public async Task Rank_MenyertakanEmisiAwalDanAlasan()
    {
        SuitabilityScoringService service = Service(DataContoh.BetonBertulang());

        Evaluasi hasil = Assert.Single(await service.RankAsync(DataContoh.KomponenDindingId));

        // PRD §11.2 dan jangkar seed: 120 kgCO2e/m2 pada 120 m2.
        Assert.Equal(14400, hasil.EmisiAwalKg, 6);
        Assert.False(string.IsNullOrWhiteSpace(hasil.Alasan));
    }

    [Fact]
    public async Task Rank_HanyaMemakaiBahanSesuaiJenisKomponen()
    {
        // Atap tidak boleh muncul di peringkat dinding meski katalognya sama.
        SuitabilityScoringService service = Service(
            DataContoh.BetonBertulang(), DataContoh.GentingKeramik());

        IReadOnlyList<Evaluasi> hasil = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(DataContoh.IdBetonBertulang, Assert.Single(hasil).BahanId);
    }

    [Fact]
    public async Task Rank_BobotDefault_DipakaiSaatBobotTidakDiberikan()
    {
        SuitabilityScoringService service = Service(
            DataContoh.BetonBertulang(), DataContoh.BetonRingan(), DataContoh.PanelGipsum());

        IReadOnlyList<Evaluasi> tanpaBobot = await service.RankAsync(DataContoh.KomponenDindingId);
        IReadOnlyList<Evaluasi> denganBobotDefault = await service.RankAsync(
            DataContoh.KomponenDindingId, KriteriaBobot.Default);

        Assert.Equal(
            tanpaBobot.Select(e => e.BahanId),
            denganBobotDefault.Select(e => e.BahanId));
    }

    [Fact]
    public async Task Rank_MengubahBobotDapatMenggantiPeringkat()
    {
        // Beton ringan unggul di termal. Beton bertulang unggul di usia dan korosi.
        SuitabilityScoringService service = Service(
            DataContoh.BetonBertulang(), DataContoh.BetonRingan(), DataContoh.PanelGipsum());

        IReadOnlyList<Evaluasi> seimbang = await service.RankAsync(DataContoh.KomponenDindingId);
        IReadOnlyList<Evaluasi> utamakanTermal = await service.RankAsync(
            DataContoh.KomponenDindingId, new KriteriaBobot(0.70, 0.10, 0.10, 0.10));

        Assert.Equal(DataContoh.IdBetonBertulang, seimbang[0].BahanId);
        Assert.Equal(DataContoh.BetonRingan().Id, utamakanTermal[0].BahanId);
    }

    [Fact]
    public async Task Rank_KomponenTidakAda_Throws()
    {
        SuitabilityScoringService service = Service(DataContoh.BetonBertulang());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RankAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Rank_ProfilIklimBelumAda_ThrowsDenganPetunjukYangJelas()
    {
        // PRD §F3 mensyaratkan analisis iklim dijalankan lebih dulu; pesan galat harus
        // mengarahkan pengguna ke langkah itu, bukan hanya melaporkan null.
        SuitabilityScoringService service = Service(
            (ProfilIklim?)null, DataContoh.BetonBertulang());

        InvalidOperationException galat = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RankAsync(DataContoh.KomponenDindingId));

        Assert.Contains("iklim", galat.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rank_SkorSelaluDalamRentangNolSampaiSeratus()
    {
        SuitabilityScoringService service = Service(
            DataContoh.IklimPesisirBandarLampung(),
            DataContoh.BetonBertulang(), DataContoh.BetonRingan(), DataContoh.PanelGipsum());

        foreach (Evaluasi hasil in await service.RankAsync(DataContoh.KomponenDindingId))
        {
            Assert.InRange(hasil.SkorKesesuaian, 0, 100);
            Assert.InRange(hasil.SubSkor.Termal, 0, 100);
            Assert.InRange(hasil.SubSkor.Lembap, 0, 100);
            Assert.InRange(hasil.SubSkor.Korosi, 0, 100);
            Assert.InRange(hasil.SubSkor.Usia, 0, 100);
        }
    }

    // ------------------------------------------------------------------ tie-break

    [Fact]
    public void Rank_TieBreak_SkorSama_EmisiLebihRendahDidahulukan()
    {
        Bahan mahal = Bahan(Guid.NewGuid(), "Mahal", emisi: 200);
        Bahan murah = Bahan(Guid.NewGuid(), "Murah", emisi: 20);

        IReadOnlyList<Evaluasi> hasil = SuitabilityScoringService.Rank(
            DataContoh.Dinding(), DataContoh.IklimPesisirBandarLampung(), [mahal, murah]);

        Assert.Equal(murah.Id, hasil[0].BahanId);
        Assert.Equal(mahal.Id, hasil[1].BahanId);
    }

    [Fact]
    public void Rank_TieBreak_SkorDanEmisiSama_NamaAscendingCaseInsensitive()
    {
        // "alpha" harus mendahului "Zeta" meski ordinal case-sensitive menaruh Z lebih dulu.
        Bahan zeta = Bahan(Guid.NewGuid(), "Zeta", emisi: 50);
        Bahan alpha = Bahan(Guid.NewGuid(), "alpha", emisi: 50);

        IReadOnlyList<Evaluasi> hasil = SuitabilityScoringService.Rank(
            DataContoh.Dinding(), DataContoh.IklimPesisirBandarLampung(), [zeta, alpha]);

        Assert.Equal(alpha.Id, hasil[0].BahanId);
        Assert.Equal(zeta.Id, hasil[1].BahanId);
    }

    [Fact]
    public void Rank_TieBreak_SkorEmisiNamaSama_IdAscending()
    {
        var idKecil = new Guid("aaaaaaaa-0000-4000-8000-000000000001");
        var idBesar = new Guid("bbbbbbbb-0000-4000-8000-000000000002");
        Bahan besar = Bahan(idBesar, "Bahan Sama", emisi: 50);
        Bahan kecil = Bahan(idKecil, "Bahan Sama", emisi: 50);

        IReadOnlyList<Evaluasi> hasil = SuitabilityScoringService.Rank(
            DataContoh.Dinding(), DataContoh.IklimPesisirBandarLampung(), [besar, kecil]);

        Assert.Equal(idKecil, hasil[0].BahanId);
        Assert.Equal(idBesar, hasil[1].BahanId);
    }

    [Fact]
    public void Rank_TieBreak_MemakaiPresisiPenuhBukanAngkaBulatTampilan()
    {
        // Dua skor yang sama bila dibulatkan ke bilangan bulat, tetapi berbeda
        // pada presisi penuh. Rentang usia kategori ditahan oleh dua bahan lain
        // supaya selisih kecil ini benar-benar menjadi pembeda skor.
        Bahan pendek = Bahan(Guid.NewGuid(), "Pendek", usia: 20);
        Bahan panjang = Bahan(Guid.NewGuid(), "Panjang", usia: 40);
        Bahan rendah = Bahan(Guid.NewGuid(), "Rendah", usia: 30.0000001);
        Bahan tinggi = Bahan(Guid.NewGuid(), "Tinggi", usia: 30.0000002);

        IReadOnlyList<Evaluasi> hasil = SuitabilityScoringService.Rank(
            DataContoh.Dinding(),
            DataContoh.IklimPesisirBandarLampung(),
            [rendah, tinggi, pendek, panjang]);

        int indeksRendah = hasil.ToList().FindIndex(e => e.BahanId == rendah.Id);
        int indeksTinggi = hasil.ToList().FindIndex(e => e.BahanId == tinggi.Id);

        Assert.True(indeksTinggi < indeksRendah, "Skor lebih tinggi harus tampil lebih dulu.");
        Assert.Equal(
            Math.Round(hasil[indeksTinggi].SkorKesesuaian),
            Math.Round(hasil[indeksRendah].SkorKesesuaian));
        Assert.True(hasil[indeksTinggi].SkorKesesuaian > hasil[indeksRendah].SkorKesesuaian);
    }

    // ------------------------------------------------------------- emisi dan validasi

    [Fact]
    public void Rank_T15_LuasDuaKaliLipat_EmisiGanda_SkorTetap()
    {
        Bahan bahan = DataContoh.BetonBertulang();
        var kecil = new Komponen(Guid.NewGuid(), DataContoh.ProyekId, JenisKomponen.Dinding, 120);
        var besar = new Komponen(Guid.NewGuid(), DataContoh.ProyekId, JenisKomponen.Dinding, 240);

        Evaluasi hasilKecil = Assert.Single(SuitabilityScoringService.Rank(
            kecil, DataContoh.IklimPesisirBandarLampung(), [bahan]));
        Evaluasi hasilBesar = Assert.Single(SuitabilityScoringService.Rank(
            besar, DataContoh.IklimPesisirBandarLampung(), [bahan]));

        Assert.Equal(hasilKecil.SkorKesesuaian, hasilBesar.SkorKesesuaian, 6);
        Assert.Equal(hasilKecil.EmisiAwalKg * 2, hasilBesar.EmisiAwalKg, 6);
    }

    [Fact]
    public void Rank_BobotTidakValidHasilWith_DitolakTanpaMenghasilkanNilai()
    {
        KriteriaBobot bobot = KriteriaBobot.Default with { Termal = 0.50 };

        Assert.Throws<ArgumentOutOfRangeException>(() => SuitabilityScoringService.Rank(
            DataContoh.Dinding(),
            DataContoh.IklimPesisirBandarLampung(),
            [DataContoh.BetonBertulang()],
            bobot));
    }

    [Fact]
    public void Rank_IklimTidakFiniteHasilWith_Ditolak()
    {
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung() with { CurahHujanTahunanMm = double.NaN };

        Assert.Throws<ArgumentOutOfRangeException>(() => SuitabilityScoringService.Rank(
            DataContoh.Dinding(), iklim, [DataContoh.BetonBertulang()]));
    }

    [Fact]
    public async Task Rank_KonfigurasiKustom_MemakaiParameterModelYangDiberikan()
    {
        var konfigurasi = new KonfigurasiModel(bobotLembapHujanAlpha: 0.0);
        SuitabilityScoringService service = Service(konfigurasi, DataContoh.BetonBertulang());

        Evaluasi hasil = Assert.Single(await service.RankAsync(DataContoh.KomponenDindingId));

        // Bandar Lampung RH 82%, alpha 0 -> beban 0,82, kebutuhan 4,28, 4/4,28 = 93,457944.
        Assert.Equal(93.457944, hasil.SubSkor.Lembap, 6);
    }

    // ------------------------------------------------------- batch dan deduplikasi

    [Fact]
    public async Task RankAsync_MenyimpanBatchDenganIdentitasDanSnapshotLengkap()
    {
        var batch = new BatchEvaluasiRepositoryPerekam();
        var jam = new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
        SuitabilityScoringService service = Service(
            batch, null, new JamTetap(jam), DataContoh.BetonBertulang());

        IReadOnlyList<Evaluasi> hasil = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(1, batch.JumlahSimpanBatch);
        BatchEvaluasi tersimpan = Assert.Single(batch.Batch);

        Assert.Equal(DataContoh.KomponenDindingId, tersimpan.KomponenId);
        Assert.Equal(DataContoh.ProyekId, tersimpan.ProyekId);
        Assert.Equal(DataContoh.LuasDinding, tersimpan.LuasM2, 6);
        Assert.Equal("v2.0", tersimpan.VersiModel);
        Assert.False(string.IsNullOrWhiteSpace(tersimpan.HashInput));
        Assert.Equal(jam, tersimpan.DihitungPadaUtc);

        foreach (Evaluasi item in hasil)
        {
            Assert.Equal(tersimpan.Id, item.BatchId);
            Assert.Equal(tersimpan.VersiModel, item.VersiModel);
            Assert.Equal(tersimpan.HashInput, item.HashInput);
            Assert.Equal(tersimpan.DihitungPadaUtc, item.DihitungPadaUtc);
            Assert.Equal(DataContoh.KomponenDindingId, item.KomponenId);
        }
    }

    [Fact]
    public async Task RankAsync_RenderUlangInputSama_TidakMembuatBatchBaru()
    {
        var batch = new BatchEvaluasiRepositoryPerekam();
        SuitabilityScoringService service = Service(
            batch, null, new JamTetap(DateTimeOffset.UnixEpoch), DataContoh.BetonBertulang(), DataContoh.BetonRingan());

        IReadOnlyList<Evaluasi> pertama = await service.RankAsync(DataContoh.KomponenDindingId);
        IReadOnlyList<Evaluasi> kedua = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(1, batch.JumlahSimpanBatch);
        Assert.Single(batch.Batch);
        Assert.Equal(pertama.Select(e => e.Id), kedua.Select(e => e.Id));
        Assert.Equal(pertama.Select(e => e.BahanId), kedua.Select(e => e.BahanId));
        Assert.Equal(pertama.Select(e => e.SkorKesesuaian), kedua.Select(e => e.SkorKesesuaian));
    }

    [Fact]
    public async Task RankAsync_BobotBerubah_MembuatBatchBaru()
    {
        var batch = new BatchEvaluasiRepositoryPerekam();
        SuitabilityScoringService service = Service(
            batch, null, new JamTetap(DateTimeOffset.UnixEpoch), DataContoh.BetonBertulang());

        await service.RankAsync(DataContoh.KomponenDindingId);
        await service.RankAsync(
            DataContoh.KomponenDindingId,
            new KriteriaBobot(0.10, 0.10, 0.10, 0.70));

        Assert.Equal(2, batch.JumlahSimpanBatch);
        Assert.Equal(2, batch.Batch.Select(b => b.HashInput).Distinct().Count());
    }

    [Fact]
    public async Task RankAsync_IklimBerubah_MembuatBatchBaru()
    {
        var material = new MaterialRepositoryInMemory([DataContoh.BetonBertulang()]);
        var proyek = new ProyekRepositoryInMemory();
        await proyek.AddKomponenAsync(DataContoh.Dinding());
        await proyek.SimpanProfilIklimAsync(DataContoh.ProyekId, DataContoh.IklimPesisirBandarLampung());

        var batch = new BatchEvaluasiRepositoryPerekam();
        var service = new SuitabilityScoringService(material, proyek, batch, batch);

        await service.RankAsync(DataContoh.KomponenDindingId);
        await proyek.SimpanProfilIklimAsync(DataContoh.ProyekId, DataContoh.IklimDinginInland());
        await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(2, batch.JumlahSimpanBatch);
        Assert.Equal(2, batch.Batch.Select(b => b.HashInput).Distinct().Count());
    }

    [Fact]
    public async Task RankAsync_BobotTidakValid_TidakMenyimpanBatch()
    {
        var batch = new BatchEvaluasiRepositoryPerekam();
        SuitabilityScoringService service = Service(
            batch, null, new JamTetap(DateTimeOffset.UnixEpoch), DataContoh.BetonBertulang());
        KriteriaBobot bobot = KriteriaBobot.Default with { Lembap = 0.50 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.RankAsync(DataContoh.KomponenDindingId, bobot));

        Assert.Equal(0, batch.JumlahSimpanBatch);
    }

    private static SuitabilityScoringService Service(params Bahan[] bahan)
        => Service(DataContoh.IklimPesisirBandarLampung(), bahan);

    private static SuitabilityScoringService Service(ProfilIklim? profil, params Bahan[] bahan)
    {
        var material = new MaterialRepositoryInMemory(bahan);
        var proyek = new ProyekRepositoryInMemory();

        proyek.AddKomponenAsync(DataContoh.Dinding());
        if (profil is not null)
        {
            proyek.SimpanProfilIklimAsync(DataContoh.ProyekId, profil);
        }

        return new SuitabilityScoringService(material, proyek);
    }

    private static SuitabilityScoringService Service(
        BatchEvaluasiRepositoryPerekam batch,
        KonfigurasiModel? konfigurasi,
        TimeProvider clock,
        params Bahan[] bahan)
    {
        var material = new MaterialRepositoryInMemory(bahan);
        var proyek = new ProyekRepositoryInMemory();
        proyek.AddKomponenAsync(DataContoh.Dinding());
        proyek.SimpanProfilIklimAsync(DataContoh.ProyekId, DataContoh.IklimPesisirBandarLampung());

        return new SuitabilityScoringService(material, proyek, batch, batch, konfigurasi, clock);
    }

    private static SuitabilityScoringService Service(
        BatchEvaluasiRepositoryPerekam batch,
        KonfigurasiModel? konfigurasi,
        Bahan bahan)
        => Service(batch, konfigurasi, TimeProvider.System, bahan);

    private static SuitabilityScoringService Service(KonfigurasiModel konfigurasi, params Bahan[] bahan)
    {
        var material = new MaterialRepositoryInMemory(bahan);
        var proyek = new ProyekRepositoryInMemory();
        proyek.AddKomponenAsync(DataContoh.Dinding());
        proyek.SimpanProfilIklimAsync(DataContoh.ProyekId, DataContoh.IklimPesisirBandarLampung());

        return new SuitabilityScoringService(material, proyek, konfigurasi: konfigurasi);
    }

    private static Bahan Bahan(
        Guid id,
        string nama,
        double konduktivitas = 0.30,
        int ketahananLembap = 3,
        int ketahananKorosi = 3,
        double usia = 30,
        double emisi = 50) => new(
        id,
        nama,
        JenisKomponen.Dinding,
        konduktivitas,
        ketahananLembap,
        ketahananKorosi,
        usia,
        emisi,
        "Data uji");

    private sealed class JamTetap(DateTimeOffset sekarang) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => sekarang;
    }
}
