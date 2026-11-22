using Wastu.Domain.Weather;

namespace Wastu.Domain.Tests.Weather;

public class KebijakanCachePrakiraanTests
{
    [Fact]
    public void T21_CacheTepatDiBawahEnamJam_Segar()
    {
        // 5 jam 59 menit 59 detik: masih segar (< 6 jam)
        var fetchedAtUtc = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
        var nowUtc = fetchedAtUtc.AddHours(5).AddMinutes(59).AddSeconds(59);

        Assert.True(KebijakanCachePrakiraan.ApakahSegar(fetchedAtUtc, nowUtc));
        Assert.False(KebijakanCachePrakiraan.PerluRefresh(fetchedAtUtc, nowUtc));
        Assert.Equal(TimeSpan.FromHours(5) + TimeSpan.FromMinutes(59) + TimeSpan.FromSeconds(59), KebijakanCachePrakiraan.HitungUmur(fetchedAtUtc, nowUtc));
    }

    [Fact]
    public void T21_CacheTepatEnamJam_StalePerluRefresh()
    {
        // Tepat 6:00:00 adalah stale (PRD §11.1, T21)
        var fetchedAtUtc = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
        var nowUtc = fetchedAtUtc.AddHours(6);

        Assert.False(KebijakanCachePrakiraan.ApakahSegar(fetchedAtUtc, nowUtc));
        Assert.True(KebijakanCachePrakiraan.PerluRefresh(fetchedAtUtc, nowUtc));
        Assert.Equal(TimeSpan.FromHours(6), KebijakanCachePrakiraan.HitungUmur(fetchedAtUtc, nowUtc));
    }

    [Fact]
    public void T21_TimestampMasaDepan_TidakSegarSelamanya_PerluRefresh()
    {
        // Clock mundur / fetchedAtUtc di masa depan: tidak boleh dianggap segar tanpa batas (T21)
        var fetchedAtUtc = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        var nowUtc = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

        Assert.False(KebijakanCachePrakiraan.ApakahSegar(fetchedAtUtc, nowUtc));
        Assert.True(KebijakanCachePrakiraan.PerluRefresh(fetchedAtUtc, nowUtc));
        Assert.Equal(TimeSpan.Zero, KebijakanCachePrakiraan.HitungUmur(fetchedAtUtc, nowUtc));
    }

    [Fact]
    public void CacheNull_PerluRefresh()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        Assert.True(KebijakanCachePrakiraan.PerluRefresh(null, nowUtc));
    }

    [Fact]
    public void T23_KomputerWibUtc_ProyekWit_PergantianTanggalMengikutiZonaProyek()
    {
        // Host pada UTC / WIB, proyek di Jayapura (WIT = UTC+9).
        // Pada jam 15:30:00 UTC (tanggal 20 November):
        // Di WIB (UTC+7): 22:30:00 tanggal 20 November.
        // Di WIT (UTC+9): 00:30:00 tanggal 21 November!
        var nowUtc = new DateTimeOffset(2026, 11, 20, 15, 30, 0, TimeSpan.Zero);

        var hariIniWib = KebijakanCachePrakiraan.HariIniPadaZona(nowUtc, "Asia/Jakarta");
        var hariIniWit = KebijakanCachePrakiraan.HariIniPadaZona(nowUtc, "Asia/Jayapura");

        Assert.Equal(new DateOnly(2026, 11, 20), hariIniWib);
        Assert.Equal(new DateOnly(2026, 11, 21), hariIniWit);
    }

    [Theory]
    [InlineData("Asia/Jakarta", 7)]
    [InlineData("Asia/Makassar", 8)]
    [InlineData("Asia/Jayapura", 9)]
    public void DapatkanTimeZoneInfo_MendukungIanaTigaZonaIndonesia(string zona, int expectedOffsetHours)
    {
        var tz = KebijakanCachePrakiraan.DapatkanTimeZoneInfo(zona);
        var offset = tz.GetUtcOffset(new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(TimeSpan.FromHours(expectedOffsetHours), offset);
    }
}
