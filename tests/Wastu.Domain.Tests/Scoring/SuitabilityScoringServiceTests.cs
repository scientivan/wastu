using Wastu.Domain.Scoring;
using Wastu.InMemory;
using Wastu.Domain.Tests.Fixtures;

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
}
