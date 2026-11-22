using Wastu.Domain.Weather;

namespace Wastu.Domain.Tests.Weather;

public class PekerjaanTests
{
    [Fact]
    public void Pekerjaan_Valid_TersimpanLengkap()
    {
        var id = Guid.NewGuid();
        var proyekId = Guid.NewGuid();
        var mulai = new DateOnly(2026, 11, 20);
        var selesai = new DateOnly(2026, 11, 25);
        var dikunci = new DateOnly(2026, 11, 22);

        var pekerjaan = new Pekerjaan(
            id, proyekId, "Pengecoran Lantai 2", JenisPekerjaan.Pengecoran,
            mulai, selesai, StatusPekerjaan.Terkunci, dikunci);

        Assert.Equal(id, pekerjaan.Id);
        Assert.Equal(proyekId, pekerjaan.ProyekId);
        Assert.Equal("Pengecoran Lantai 2", pekerjaan.Nama);
        Assert.Equal(JenisPekerjaan.Pengecoran, pekerjaan.Jenis);
        Assert.Equal(StatusPekerjaan.Terkunci, pekerjaan.Status);
        Assert.Equal(dikunci, pekerjaan.TanggalDikunci);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Pekerjaan_NamaKosong_Ditolak(string nama)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Pekerjaan(
            Guid.NewGuid(), Guid.NewGuid(), nama, JenisPekerjaan.Pengecoran,
            new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 25)));
    }

    [Fact]
    public void Pekerjaan_TanggalMulaiLebihBesarDariSelesai_Ditolak()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Pekerjaan(
            Guid.NewGuid(), Guid.NewGuid(), "Pengecatan Fasad", JenisPekerjaan.Pengecatan,
            new DateOnly(2026, 11, 26), new DateOnly(2026, 11, 25)));

        Assert.Contains("tidak boleh lebih besar", ex.Message);
    }

    [Theory]
    [InlineData(2026, 11, 19)] // sebelum tanggal mulai
    [InlineData(2026, 11, 26)] // setelah tanggal selesai
    public void Pekerjaan_TanggalDikunciDiLuarRentang_Ditolak(int y, int m, int d)
    {
        var tanggalDikunci = new DateOnly(y, m, d);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pekerjaan(
            Guid.NewGuid(), Guid.NewGuid(), "Pengecoran Sloof", JenisPekerjaan.Pengecoran,
            new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 25),
            StatusPekerjaan.Terkunci, tanggalDikunci));
    }

    [Fact]
    public void Pekerjaan_TanggalDikunciDiBatasRentang_Diterima()
    {
        var mulai = new DateOnly(2026, 11, 20);
        var selesai = new DateOnly(2026, 11, 25);

        var p1 = new Pekerjaan(
            Guid.NewGuid(), Guid.NewGuid(), "Pengecoran", JenisPekerjaan.Pengecoran,
            mulai, selesai, StatusPekerjaan.Terkunci, mulai);
        Assert.Equal(mulai, p1.TanggalDikunci);

        var p2 = new Pekerjaan(
            Guid.NewGuid(), Guid.NewGuid(), "Pengecoran", JenisPekerjaan.Pengecoran,
            mulai, selesai, StatusPekerjaan.Terkunci, selesai);
        Assert.Equal(selesai, p2.TanggalDikunci);
    }

    [Fact]
    public void Pekerjaan_EnumTidakDikenal_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pekerjaan(
            Guid.NewGuid(), Guid.NewGuid(), "Pekerjaan", (JenisPekerjaan)999,
            new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 25)));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Pekerjaan(
            Guid.NewGuid(), Guid.NewGuid(), "Pekerjaan", JenisPekerjaan.Pengecoran,
            new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 25),
            (StatusPekerjaan)999));
    }
}
