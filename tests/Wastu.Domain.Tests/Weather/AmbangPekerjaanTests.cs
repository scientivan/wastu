using Wastu.Domain.Weather;

namespace Wastu.Domain.Tests.Weather;

public class AmbangPekerjaanTests
{
    [Fact]
    public void NegativeTemperaturesAreValidButNegativeRainIsRejected()
    {
        var threshold = new AmbangPekerjaan(40, 5, 85, -10, 35);
        threshold.PastikanValid();
        var adjusted = threshold with { SuhuMaksC = -5 };
        adjusted.PastikanValid();
        Assert.Equal(-10, adjusted.SuhuMinC);
        Assert.Throws<ArgumentOutOfRangeException>(() => threshold with { CurahHujanMaksMm = -1 });
    }
    [Fact]
    public void Pengecoran_SesuaiTabelPRD12()
    {
        AmbangPekerjaan ambang = AmbangLayakKerja.Pengecoran;

        Assert.Equal(60, ambang.ProbabilitasHujanMaksPct);
        Assert.Equal(5, ambang.CurahHujanMaksMm);
        Assert.Equal(35, ambang.SuhuMaksC);
        Assert.Equal(TingkatPelanggaran.Peringatan, ambang.TingkatSuhu);
    }

    [Fact]
    public void Pengecoran_TidakPunyaAmbangKelembapanDanSuhuMinimum()
    {
        // PRD §12 tidak menyebut kelembapan sebagai alasan kegagalan pengecoran, dan
        // tidak ada suhu minimum. Null berarti tidak berlaku, bukan diabaikan diam-diam.
        AmbangPekerjaan ambang = AmbangLayakKerja.Pengecoran;

        Assert.Null(ambang.KelembapanMaksPct);
        Assert.Null(ambang.SuhuMinC);
    }

    [Fact]
    public void Pengecatan_SesuaiTabelPRD12()
    {
        AmbangPekerjaan ambang = AmbangLayakKerja.Pengecatan;

        Assert.Equal(40, ambang.ProbabilitasHujanMaksPct);
        Assert.Equal(85, ambang.KelembapanMaksPct);
        Assert.Equal(10, ambang.SuhuMinC);
        Assert.Equal(35, ambang.SuhuMaksC);
        Assert.Equal(TingkatPelanggaran.Keras, ambang.TingkatSuhu);
    }

    [Fact]
    public void Pengecatan_TidakPunyaAmbangCurahHujan()
    {
        Assert.Null(AmbangLayakKerja.Pengecatan.CurahHujanMaksMm);
    }

    [Fact]
    public void Pengecatan_MemakaiAmbangHujanLebihKetatDaripadaPengecoran()
    {
        // PRD §12: pengecatan lebih rentan hujan (40 persen) karena cat yang belum
        // kering tersapu air, sementara pengecoran toleran sampai 60 persen.
        Assert.True(
            AmbangLayakKerja.Pengecatan.ProbabilitasHujanMaksPct
            < AmbangLayakKerja.Pengecoran.ProbabilitasHujanMaksPct);
    }

    [Fact]
    public void Untuk_MengembalikanAmbangYangTepat()
    {
        Assert.Same(AmbangLayakKerja.Pengecoran, AmbangLayakKerja.Untuk(JenisPekerjaan.Pengecoran));
        Assert.Same(AmbangLayakKerja.Pengecatan, AmbangLayakKerja.Untuk(JenisPekerjaan.Pengecatan));
    }

    [Fact]
    public void AmbangDapatDiubahTanpaMenyentuhKode()
    {
        // PRD §12 dan §14 R4 mewajibkan ambang dapat dikonfigurasi. Nilai kustom
        // harus diterima begitu saja.
        AmbangPekerjaan kustom = AmbangDisesuaikan with { ProbabilitasHujanMaksPct = 30 };

        Assert.Equal(30, kustom.ProbabilitasHujanMaksPct);
        Assert.Equal(AmbangDisesuaikan.CurahHujanMaksMm, kustom.CurahHujanMaksMm);
    }

    [Fact]
    public void AmbangNegatif_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmbangPekerjaan(-1, null, null, null, null));
    }

    [Fact]
    public void AmbangDiLuar100Persen_Ditolak()
    {
        // Probabilitas dan kelembapan adalah persen; nilai di atas 100 tidak mungkin.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmbangPekerjaan(101, null, null, null, null));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmbangPekerjaan(null, null, 150, null, null));
    }

    [Fact]
    public void SuhuMinimumLebihBesarDariMaksimum_Ditolak()
    {
        // Rentang yang tidak mungkin dipenuhi akan membuat setiap hari dianggap tidak
        // layak tanpa alasan yang masuk akal.
        Assert.Throws<ArgumentException>(
            () => new AmbangPekerjaan(null, null, null, 40, 10));
    }

    [Fact]
    public void SkorMinimumDisarankan_AdaUntukMenandaiBahanTidakDisarankan()
    {
        // PRD §F4: bahan di bawah ambang ditandai "tidak disarankan", bukan disembunyikan.
        // PRD-R10: angkanya terpusat di KonfigurasiModel, bukan konstanta 50 yang terpisah.
        Assert.Equal(55, AmbangLayakKerja.SkorMinimumDisarankan);
        Assert.Equal(KonfigurasiModel.Bawaan.AmbangTidakDisarankan, AmbangLayakKerja.SkorMinimumDisarankan);
        Assert.InRange(AmbangLayakKerja.SkorMinimumDisarankan, 0, 100);
    }

    [Fact]
    public void AmbangNaN_Ditolak()
    {
        Assert.Throws<ArgumentException>(() => new AmbangPekerjaan(double.NaN, null, null, null, null));
        Assert.Throws<ArgumentException>(() => new AmbangPekerjaan(null, double.NaN, null, null, null));
        Assert.Throws<ArgumentException>(() => new AmbangPekerjaan(null, null, double.NaN, null, null));
        Assert.Throws<ArgumentException>(() => new AmbangPekerjaan(null, null, null, double.NaN, null));
        Assert.Throws<ArgumentException>(() => new AmbangPekerjaan(null, null, null, null, double.NaN));
    }

    [Fact]
    public void AmbangInfinity_Ditolak()
    {
        Assert.Throws<ArgumentException>(() => new AmbangPekerjaan(double.PositiveInfinity, null, null, null, null));
        Assert.Throws<ArgumentException>(() => new AmbangPekerjaan(null, double.PositiveInfinity, null, null, null));
    }

    [Fact]
    public void WithBypass_NaNDitolak()
    {
        var ambang = AmbangLayakKerja.Pengecoran;
        Assert.Throws<ArgumentException>(() => ambang with { ProbabilitasHujanMaksPct = double.NaN });
    }

    [Fact]
    public void WithBypass_SuhuMinLebihBesarDariMaks_PastikanValidDitolak()
    {
        var ambang = AmbangLayakKerja.Pengecoran with { SuhuMinC = 40, SuhuMaksC = 30 };
        Assert.Throws<ArgumentException>(() => ambang.PastikanValid());
    }

    /// <summary>Salinan ambang pengecoran sebagai titik awal uji konfigurasi ulang.</summary>
    private static AmbangPekerjaan AmbangDisesuaikan => AmbangLayakKerja.Pengecoran;
}
