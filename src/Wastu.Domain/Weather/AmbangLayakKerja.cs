namespace Wastu.Domain.Weather;

/// <summary>
/// Nilai awal ambang layak-kerja untuk tiap jenis pekerjaan, persis seperti
/// ditabulasi PRD §12.
/// </summary>
/// <remarks>
/// Angka di sini adalah nilai awal berbasis praktik umum konstruksi dan
/// spesifikasi cat generik, bukan hasil pengukuran lapangan. PRD §12 mewajibkan
/// nilainya dapat dikonfigurasi karena spesifikasi produk aktual berbeda per
/// merek (PRD §14, R4), dan PRD §14 R5 menyatakan jarak ke laut adalah proksi
/// risiko, bukan pengukuran. Siklus kalibrasi lanjutan ada di §14 dan §15.
/// </remarks>
public static class AmbangLayakKerja
{
    /// <summary>
    /// Ambang pengecoran (PRD §12). Curah hujan dan probabilitas hujan bersifat
    /// keras karena hujan saat curing awal beton mengganggu hidrasi semen dan
    /// merusak permukaan. Suhu di atas 35 C hanya peringatan karena bisa dimitigasi.
    /// </summary>
    public static AmbangPekerjaan Pengecoran { get; } = new(
        probabilitasHujanMaksPct: 60,
        curahHujanMaksMm: 5,
        kelembapanMaksPct: null,
        suhuMinC: null,
        suhuMaksC: 35,
        tingkatSuhu: TingkatPelanggaran.Peringatan);

    /// <summary>
    /// Ambang pengecatan (PRD §12). Semua bersifat keras: cat yang belum kering
    /// tersapu hujan, kelembapan di atas 85 persen menghambat pengeringan, dan di
    /// luar rentang 10-35 C daya rekat menurun.
    /// </summary>
    public static AmbangPekerjaan Pengecatan { get; } = new(
        probabilitasHujanMaksPct: 40,
        curahHujanMaksMm: null,
        kelembapanMaksPct: 85,
        suhuMinC: 10,
        suhuMaksC: 35,
        tingkatSuhu: TingkatPelanggaran.Keras);

    /// <summary>Ambang awal untuk jenis pekerjaan tertentu.</summary>
    public static AmbangPekerjaan Untuk(JenisPekerjaan jenis) => jenis switch
    {
        JenisPekerjaan.Pengecoran => Pengecoran,
        JenisPekerjaan.Pengecatan => Pengecatan,
        _ => throw new ArgumentOutOfRangeException(
            nameof(jenis), jenis, "Jenis pekerjaan belum punya ambang.")
    };

    /// <summary>
    /// Skor kesesuaian di bawah nilai ini ditandai sebagai tidak disarankan, bukan
    /// disembunyikan (PRD §F4).
    /// </summary>
    public const double SkorMinimumDisarankan = 50;
}