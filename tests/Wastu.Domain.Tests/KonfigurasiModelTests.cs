namespace Wastu.Domain.Tests;

public class KonfigurasiModelTests
{
    [Fact]
    public void Bawaan_MemilikiNilaiSesuaiSpesifikasiV2()
    {
        var config = KonfigurasiModel.Bawaan;

        Assert.Equal("v2.0", config.VersiModel);
        Assert.Equal(0.5, config.BobotLembapHujanAlpha);
        Assert.Equal(3000.0, config.AcuanCurahHujanMm);
        Assert.Equal(50.0, config.AcuanJarakPesisirKm);
        Assert.Equal(0.10, config.PitaTermalBatasBawah);
        Assert.Equal(0.60, config.PitaTermalBatasAtasSejuk);
        Assert.Equal(0.30, config.PitaTermalBatasAtasPanas);
        Assert.Equal(26.0, config.SuhuSejukC);
        Assert.Equal(32.0, config.SuhuPanasC);
        Assert.Equal(0.5, config.FaktorPenuaanA);
        Assert.Equal(55.0, config.AmbangTidakDisarankan);
        Assert.Equal(75.0, config.AmbangCocok);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void FaktorPenuaanA_Valid_Diterima(double a)
    {
        var config = new KonfigurasiModel(faktorPenuaanA: a);
        Assert.Equal(a, config.FaktorPenuaanA);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(1.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FaktorPenuaanA_DiLuarRentangNolSampaiSatu_Ditolak(double a)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KonfigurasiModel(faktorPenuaanA: a));
    }

    [Theory]
    [InlineData(0.0, LabelKesesuaian.TidakDisarankan)]
    [InlineData(54.999, LabelKesesuaian.TidakDisarankan)]
    [InlineData(55.0, LabelKesesuaian.PerluPertimbangan)]
    [InlineData(60.0, LabelKesesuaian.PerluPertimbangan)]
    [InlineData(74.999, LabelKesesuaian.PerluPertimbangan)]
    [InlineData(75.0, LabelKesesuaian.Cocok)]
    [InlineData(100.0, LabelKesesuaian.Cocok)]
    public void LabelUntuk_MengembalikanKategoriSesuaiSkor(double skor, LabelKesesuaian expected)
    {
        var config = KonfigurasiModel.Bawaan;
        Assert.Equal(expected, config.LabelUntuk(skor));
    }

    [Fact]
    public void LabelUntuk_SkorNonFinite_MelemparArgumentException()
    {
        var config = KonfigurasiModel.Bawaan;
        Assert.Throws<ArgumentException>(() => config.LabelUntuk(double.NaN));
        Assert.Throws<ArgumentException>(() => config.LabelUntuk(double.PositiveInfinity));
    }
}
