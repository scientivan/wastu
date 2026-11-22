using Wastu.Domain.Weather;

namespace Wastu.Domain.Abstractions;

/// <summary>
/// Data historis iklim di sebuah koordinat (PRD §8.3).
/// </summary>
public interface IHistoricalClimateClient
{
    /// <summary>Profil iklim tahunan di koordinat tersebut.</summary>
    Task<ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default);
}

/// <summary>Prakiraan harian hingga 16 hari ke depan (PRD §8.3).</summary>
public interface IDailyForecastClient
{
    /// <summary>
    /// Prakiraan harian untuk <paramref name="days"/> hari ke depan.
    /// Returns <see cref="DailyForecast"/> dengan <c>ProbabilitasHujanPct</c> dalam
    /// skala persen 0-100, mengikuti satuan kolom <c>prakiraan_cuaca.probabilitas_hujan_pct</c>.
    /// </summary>
    Task<IReadOnlyList<DailyForecast>> GetForecastAsync(
        double lat, double lon, int days, CancellationToken ct = default);

    /// <summary>
    /// Mengambil prakiraan cuaca harian lengkap dengan metadata penyedia, waktu pengambilan, dan zona waktu (PRD §8.3, PRD-R08).
    /// </summary>
    async Task<HasilPrakiraan> GetForecastWithMetadataAsync(
        double lat, double lon, int days, CancellationToken ct = default)
    {
        var hari = await GetForecastAsync(lat, lon, days, ct).ConfigureAwait(false);
        return new HasilPrakiraan(
            Hari: hari,
            ZonaWaktu: "Asia/Jakarta",
            DiambilPadaUtc: DateTimeOffset.UtcNow,
            Penyedia: "Open-Meteo",
            RevisiLokasi: 1,
            DataParsial: false);
    }
}

/// <summary>
/// Prakiraan satu hari (PRD §8.3). Semua nilai sudah dalam satuan yang sama dengan kolom
/// tabel <c>prakiraan_cuaca</c> agar tidak perlu konversi di lapisan mana pun.
/// </summary>
public sealed record DailyForecast(
    DateOnly Tanggal,
    double ProbabilitasHujanPct,
    double CurahHujanMm,
    double SuhuMinC,
    double SuhuMaksC,
    double KelembapanPct,
    bool DataLengkap = true,
    string? CatatanKualitas = null);