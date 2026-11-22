namespace Wastu.Domain;

/// <summary>
/// Asal-usul data iklim yang digunakan (PRD-R09).
/// </summary>
public enum ProvenanceIklim
{
    /// <summary>Data yang benar-benar diambil dari layanan API iklim historis (Open-Meteo).</summary>
    ApiHistoris,

    /// <summary>Data cache iklim historis API yang tersimpan.</summary>
    CacheHistoris,

    /// <summary>Data contoh atau sintetis berlabel contoh.</summary>
    Contoh,

    /// <summary>Data estimasi default bawaan sistem.</summary>
    EstimasiBawaan,

    /// <summary>Data yang dimasukkan atau dioverride secara manual oleh pengguna.</summary>
    ManualOverride,

    /// <summary>Data historis lama yang asal-usulnya tidak dapat diverifikasi secara tuntas.</summary>
    LegacyUnknown
}

/// <summary>
/// Status operasional pengambilan data iklim saat ini (PRD-R09).
/// Dipisahkan tegas dari Provenance riwayat asal data.
/// </summary>
public enum StatusPengambilanIklim
{
    BelumTersedia,
    Mengambil,
    Tersedia,
    GagalMemperbarui
}

/// <summary>
/// Kualitas status cache profil iklim (PRD-R08, PRD-R09).
/// </summary>
public enum KualitasCacheIklim
{
    CocokLokasi,
    TidakCocokLokasi,
    Segar,
    CacheHistoris,
    DataLama,
    TidakTersedia
}
