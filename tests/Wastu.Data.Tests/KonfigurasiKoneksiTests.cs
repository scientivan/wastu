using Npgsql;

namespace Wastu.Data.Tests;

public sealed class KonfigurasiKoneksiTests
{
    [Fact]
    public void VariabelTerpisahMenjagaPasswordDanMengaktifkanSsl()
    {
        var nilai = KonfigurasiLingkungan.Parse([
            "# contoh konfigurasi",
            "WASTU_CONNECTION_STRING=",
            "host=localhost",
            "port=5432",
            "database=postgres",
            "export user=postgres.contoh # komentar",
            "password='rahasia;=#$ spasi'"
        ]);
        var koneksi = new NpgsqlConnectionStringBuilder(KoneksiSupabase.Susun(nilai));
        Assert.Equal("localhost", koneksi.Host);
        Assert.Equal(5432, koneksi.Port);
        Assert.Equal("postgres", koneksi.Database);
        Assert.Equal("postgres.contoh", koneksi.Username);
        Assert.Equal("rahasia;=#$ spasi", koneksi.Password);
        Assert.Equal(SslMode.Require, koneksi.SslMode);
    }

    [Fact]
    public void ConnectionStringLengkapLebihDiutamakan()
    {
        var nilai = KonfigurasiLingkungan.Parse([
            "WASTU_CONNECTION_STRING=\"Host=localhost;Database=wastu\"",
            "host=diabaikan"
        ]);
        Assert.Equal("Host=localhost;Database=wastu", KoneksiSupabase.Susun(nilai));
    }

    [Fact]
    public void TanpaKonfigurasiTetapModeLokal()
        => Assert.Null(KoneksiSupabase.Susun(KonfigurasiLingkungan.Parse(["WASTU_CONNECTION_STRING="])));

    [Fact]
    public void KonfigurasiSebagianTidakDiamDiamMemakaiModeLokal()
    {
        var nilai = KonfigurasiLingkungan.Parse(["host=localhost", "password=rahasia-uji"]);
        var error = Assert.Throws<InvalidOperationException>(() => KoneksiSupabase.Susun(nilai));
        Assert.Contains("port", error.Message);
        Assert.DoesNotContain("rahasia-uji", error.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("65536")]
    public void PortTidakValidDitolak(string port)
    {
        var nilai = KonfigurasiLingkungan.Parse([
            "host=localhost", $"port={port}", "database=postgres", "user=postgres", "password=contoh"
        ]);
        Assert.Throws<InvalidOperationException>(() => KoneksiSupabase.Susun(nilai));
    }

    [Fact]
    public void KutipTidakLengkapDitolakTanpaMembocorkanNilai()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            KonfigurasiLingkungan.Parse(["password='rahasia-uji"]));
        Assert.DoesNotContain("rahasia-uji", error.Message);
    }
}
