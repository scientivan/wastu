namespace Wastu.Domain.Tests;

public class ValidasiKoordinatTests
{
    [Theory]
    [InlineData(-11.0, 95.0)]
    [InlineData(6.0, 141.0)]
    [InlineData(-5.45, 105.26)]
    [InlineData(-6.20, 106.82)]
    [InlineData(0.0, 110.0)]
    public void ApakahValid_KoordinatDalamBatasKasarIndonesia_True(double lat, double lon)
    {
        Assert.True(ValidasiKoordinat.ApakahValid(lat, lon));
        ValidasiKoordinat.Validasi(lat, lon); // Tidak melempar pengecualian
    }

    [Theory]
    [InlineData(-11.001, 100.0)]
    [InlineData(6.001, 100.0)]
    [InlineData(0.0, 94.999)]
    [InlineData(0.0, 141.001)]
    [InlineData(48.85, 2.35)] // Paris
    public void Validasi_DiLuarBatasKasar_ThrowsArgumentOutOfRangeException(double lat, double lon)
    {
        Assert.False(ValidasiKoordinat.ApakahValid(lat, lon));
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ValidasiKoordinat.Validasi(lat, lon));
        Assert.Contains(ValidasiKoordinat.CatatanBatasKasar, ex.Message);
    }

    [Fact]
    public void Validasi_LatitudeNaN_ThrowsArgumentException()
    {
        Assert.False(ValidasiKoordinat.ApakahValid(double.NaN, 105.0));
        var ex = Assert.Throws<ArgumentException>(() => ValidasiKoordinat.Validasi(double.NaN, 105.0));
        Assert.Contains("finite", ex.Message);
    }

    [Fact]
    public void Validasi_LatitudeInfinity_ThrowsArgumentException()
    {
        Assert.False(ValidasiKoordinat.ApakahValid(double.PositiveInfinity, 105.0));
        var ex = Assert.Throws<ArgumentException>(() => ValidasiKoordinat.Validasi(double.PositiveInfinity, 105.0));
        Assert.Contains("finite", ex.Message);
    }

    [Fact]
    public void Validasi_LongitudeNaN_ThrowsArgumentException()
    {
        Assert.False(ValidasiKoordinat.ApakahValid(-6.0, double.NaN));
        var ex = Assert.Throws<ArgumentException>(() => ValidasiKoordinat.Validasi(-6.0, double.NaN));
        Assert.Contains("finite", ex.Message);
    }

    [Fact]
    public void Validasi_LongitudeInfinity_ThrowsArgumentException()
    {
        Assert.False(ValidasiKoordinat.ApakahValid(-6.0, double.NegativeInfinity));
        var ex = Assert.Throws<ArgumentException>(() => ValidasiKoordinat.Validasi(-6.0, double.NegativeInfinity));
        Assert.Contains("finite", ex.Message);
    }

    [Fact]
    public void Proyek_KonstruktorValidasiDanRevisiLokasiBawaan()
    {
        var proyek = new Proyek(Guid.NewGuid(), "Rumah Contoh", "Bandar Lampung", -5.45, 105.26);

        Assert.Equal(1, proyek.RevisiLokasi);
        Assert.Null(proyek.ZonaWaktu);
    }

    [Fact]
    public void Proyek_RevisiLokasiKurangDariSatu_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Proyek(Guid.NewGuid(), "Rumah", "Bandar Lampung", -5.45, 105.26, revisiLokasi: 0));
    }
}
