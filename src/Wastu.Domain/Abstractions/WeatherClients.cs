namespace Wastu.Domain.Abstractions;

/// <summary>
/// Data historis iklim di sebuah koordinat (PRD §8.3).
/// </summary>
/// <remarks>
/// Tanda tangan ini disalin apa adanya dari PRD §8.3 supaya domain dan dokumen tidak
/// berbeda.IMPLEMENTASI (pemanggilan Open-Meteo) adalah tanggung jawab software
/// architect; yang dikerjakan di sini hanya kontrak agar <c>ClimateAnalysisService</c>
/// tidak bergantung pada bentuk respons API mentah.
/// </remarks>
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
}

/// <summary>
/// Prakiraan satu hari (PRD §8.3). Semua nilai sudah dalam satuan yang sama dengan kolom
/// tabel <c>prakiraan_cuaca</c> agar tidak perlu konversi di lapisan mana pun.
/// </summary>
/// <param name="Tanggal">Tanggal prakiraan.</param>
/// <param name="ProbabilitasHujanPct">Probabilitas hujan, persen 0-100 — bukan pecahan 0-1.</param>
/// <param name="CurahHujanMm">Curah hujan perkiraan, mm.</param>
/// <param name="SuhuMinC">Suhu minimum, °C.</param>
/// <param name="SuhuMaksC">Suhu maksimum, °C.</param>
/// <param name="KelembapanPct">Kelembapan relatif, persen 0-100.</param>
public sealed record DailyForecast(
    DateOnly Tanggal,
    double ProbabilitasHujanPct,
    double CurahHujanMm,
    double SuhuMinC,
    double SuhuMaksC,
    double KelembapanPct);