namespace Wastu.Domain;

/// <summary>
/// Hasil simulasi emisi horizon panjang untuk satu evaluasi (PRD §11.3).
/// </summary>
public sealed record SimulasiEmisi(
    Guid EvaluasiId,
    int HorizonTahun,
    double UsiaPakaiEfektifTahun,
    int JumlahPenggantian,
    double EmisiKumulatifKg,
    string VersiModel = "v2.0",
    double FaktorPenuaanA = 0.5,
    DateTimeOffset? DihitungPadaUtc = null);