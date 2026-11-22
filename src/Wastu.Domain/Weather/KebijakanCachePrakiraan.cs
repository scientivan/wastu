namespace Wastu.Domain.Weather;

/// <summary>
/// Kebijakan cache prakiraan cuaca 6 jam UTC dan penanganan zona waktu proyek (PRD §8.4, §PRD-R08, T21, T23).
/// </summary>
public static class KebijakanCachePrakiraan
{
    /// <summary>TTL cache prakiraan cuaca adalah 6 jam (PRD §8.4, X04).</summary>
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    /// <summary>
    /// Memeriksa apakah cache prakiraan masih segar.
    /// Segar iff: 0 &lt;= (nowUtc - fetchedAtUtc) &lt; 6 jam.
    /// Tepat 6:00:00 adalah stale (false); timestamp masa depan (fetchedAtUtc &gt; nowUtc) adalah stale (false).
    /// </summary>
    public static bool ApakahSegar(DateTimeOffset fetchedAtUtc, DateTimeOffset nowUtc)
    {
        var selisih = nowUtc - fetchedAtUtc;
        return selisih >= TimeSpan.Zero && selisih < Ttl;
    }

    /// <summary>
    /// Menentukan apakah cache memerlukan refresh dari penyedia (PRD §8.4, T21).
    /// </summary>
    public static bool PerluRefresh(DateTimeOffset? fetchedAtUtc, DateTimeOffset nowUtc)
    {
        if (!fetchedAtUtc.HasValue)
        {
            return true;
        }

        return !ApakahSegar(fetchedAtUtc.Value, nowUtc);
    }

    /// <summary>
    /// Menghitung umur cache non-negatif.
    /// </summary>
    public static TimeSpan HitungUmur(DateTimeOffset fetchedAtUtc, DateTimeOffset nowUtc)
    {
        var selisih = nowUtc - fetchedAtUtc;
        return selisih >= TimeSpan.Zero ? selisih : TimeSpan.Zero;
    }

    /// <summary>
    /// Mendapatkan tanggal kalender "hari ini" pada zona waktu proyek (PRD §12.2, PRD-R08, T23).
    /// </summary>
    public static DateOnly HariIniPadaZona(DateTimeOffset nowUtc, string? zonaWaktu)
    {
        var tz = DapatkanTimeZoneInfo(zonaWaktu);
        var waktuLokal = TimeZoneInfo.ConvertTime(nowUtc, tz);
        return DateOnly.FromDateTime(waktuLokal.Date);
    }

    /// <summary>
    /// Memetakan string nama zona waktu (IANA atau Windows) ke objek <see cref="TimeZoneInfo"/>.
    /// </summary>
    public static TimeZoneInfo DapatkanTimeZoneInfo(string? zonaWaktu)
    {
        if (string.IsNullOrWhiteSpace(zonaWaktu))
        {
            zonaWaktu = "Asia/Jakarta";
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(zonaWaktu);
        }
        catch (TimeZoneNotFoundException)
        {
            return zonaWaktu switch
            {
                "Asia/Jakarta" => CariAtauCustom("SE Asia Standard Time", TimeSpan.FromHours(7), "WIB"),
                "Asia/Makassar" => CariAtauCustom("Singapore Standard Time", TimeSpan.FromHours(8), "WITA"),
                "Asia/Jayapura" => CariAtauCustom("Tokyo Standard Time", TimeSpan.FromHours(9), "WIT"),
                "SE Asia Standard Time" => CariAtauCustom("Asia/Jakarta", TimeSpan.FromHours(7), "WIB"),
                "Singapore Standard Time" => CariAtauCustom("Asia/Makassar", TimeSpan.FromHours(8), "WITA"),
                "Tokyo Standard Time" => CariAtauCustom("Asia/Jayapura", TimeSpan.FromHours(9), "WIT"),
                _ => TimeZoneInfo.Utc
            };
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static TimeZoneInfo CariAtauCustom(string idAlternatif, TimeSpan offset, string displayName)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(idAlternatif);
        }
        catch
        {
            return TimeZoneInfo.CreateCustomTimeZone(displayName, offset, displayName, displayName);
        }
    }
}
