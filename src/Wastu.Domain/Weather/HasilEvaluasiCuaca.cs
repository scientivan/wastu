using Wastu.Domain.Abstractions;

namespace Wastu.Domain.Weather;

/// <summary>
/// Rincian hasil evaluasi kelayakan cuaca untuk satu hari pelaksanaan pekerjaan (PRD §12, PRD-R10).
/// </summary>
public sealed record HasilEvaluasiCuaca(
    DateOnly Tanggal,
    StatusKelayakanCuaca Status,
    IReadOnlyList<string> Alasan,
    bool DataLama = false,
    TimeSpan? UmurCache = null,
    AmbangPekerjaan? AmbangEfektif = null,
    DailyForecast? Cuaca = null,
    DateTimeOffset? DiambilPadaUtc = null);
