using Wastu.Domain;
using Wastu.Domain.Scoring;
using Wastu.Domain.Tests.Fixtures;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Scoring;

/// <summary>
/// Pemeriksaan penerimaan WP04a yang merangkai T11-T17 melalui API publik, bukan
/// hanya menguji satu fungsi terpisah. Fokusnya adalah invariant yang mudah
/// terlewat: bobot MVP/V2, snapshot batch, label, dan alasan untuk semua bahan.
/// </summary>
public class ScoringAcceptanceTests
{
    [Fact]
    public void T16_Mvp_MemakaiEmpatBobotSeperempat()
    {
        Assert.Equal(0.25, KriteriaBobot.Default.Termal);
        Assert.Equal(0.25, KriteriaBobot.Default.Lembap);
        Assert.Equal(0.25, KriteriaBobot.Default.Korosi);
        Assert.Equal(0.25, KriteriaBobot.Default.Usia);
        Assert.Equal(1.0, KriteriaBobot.Default.Jumlah, 12);
    }

    [Fact]
    public void T16_V2_BobotValidSepuluhSepuluhSepuluhTujuhPuluh_Diterima()
    {
        var bobot = new KriteriaBobot(0.10, 0.10, 0.10, 0.70);

        bobot.PastikanValid();
        Assert.Equal(1.0, bobot.Jumlah, 12);
    }

    [Theory]
    [InlineData(0.50, 0.50, 0.50, 0.50)]
    [InlineData(0.40, 0.30, 0.20, 0.20)]
    [InlineData(double.NaN, 0.25, 0.25, 0.50)]
    [InlineData(0.25, double.PositiveInfinity, 0.25, 0.50)]
    public void T16_BobotTidakValid_Ditolak(double termal, double lembap, double korosi, double usia)
    {
        Assert.ThrowsAny<ArgumentException>(() => new KriteriaBobot(termal, lembap, korosi, usia));
    }

    [Fact]
    public void T16_BobotHasilWithYangJumlahnyaSalah_DitolakSaatDipakai()
    {
        KriteriaBobot bobot = KriteriaBobot.Default with { Usia = 0.50 };
        var sub = new SubSkorKesesuaian(80, 80, 80, 80);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PerhitunganSkor.SkorKesesuaian(sub, bobot));
    }

    [Fact]
    public void T16_BobotSemuaNol_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => KriteriaBobot.DariPersentase(0, 0, 0, 0));
    }

    [Fact]
    public async Task WP04a_AlurLengkap_MenghasilkanSkorAlasanLabelDanSnapshot()
    {
        Bahan beton = DataContoh.BetonBertulang();
        Bahan ringan = DataContoh.BetonRingan();
        Bahan gipsum = DataContoh.PanelGipsum();

        var material = new MaterialRepositoryInMemory([beton, ringan, gipsum]);
        var proyek = new ProyekRepositoryInMemory();
        await proyek.AddKomponenAsync(DataContoh.Dinding());
        await proyek.SimpanProfilIklimAsync(DataContoh.ProyekId, DataContoh.IklimPesisirBandarLampung());

        var batch = new BatchEvaluasiRepositoryPerekam();
        var service = new SuitabilityScoringService(material, proyek, batch, batch);

        IReadOnlyList<Evaluasi> hasil = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(3, hasil.Count);
        Assert.All(hasil, item =>
        {
            Assert.InRange(item.SkorKesesuaian, 0, 100);
            Assert.False(string.IsNullOrWhiteSpace(item.Alasan));
            Assert.NotNull(item.BatchId);
            Assert.Equal("v2.0", item.VersiModel);
            Assert.False(string.IsNullOrWhiteSpace(item.HashInput));
        });

        // Setiap bahan punya label yang dapat diturunkan dari skor aslinya.
        foreach (Evaluasi item in hasil)
        {
            LabelKesesuaian label = KonfigurasiModel.Bawaan.LabelUntuk(item.SkorKesesuaian);
            Assert.True(Enum.IsDefined(label));
        }

        // Render ulang tidak menambah batch atau evaluasi.
        int jumlahEvaluasi = batch.Evaluasi.Count;
        IReadOnlyList<Evaluasi> ulang = await service.RankAsync(DataContoh.KomponenDindingId);

        Assert.Equal(1, batch.JumlahSimpanBatch);
        Assert.Equal(jumlahEvaluasi, batch.Evaluasi.Count);
        Assert.Equal(hasil.Select(e => e.Id), ulang.Select(e => e.Id));
    }
}
