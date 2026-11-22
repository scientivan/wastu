using Wastu.Domain.Abstractions;

namespace Wastu.Domain.Weather;

/// <summary>
/// Pembungkus hasil prakiraan cuaca harian ber-metadata lengkap (PRD §8.3, PRD-R08).
/// </summary>
public sealed record HasilPrakiraan(
    IReadOnlyList<DailyForecast> Hari,
    string ZonaWaktu,
    DateTimeOffset DiambilPadaUtc,
    string Penyedia,
    int RevisiLokasi = 1,
    bool DataParsial = false);
