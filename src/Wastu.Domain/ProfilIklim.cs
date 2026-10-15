namespace Wastu.Domain;

/// <summary>
/// Profil iklim jangka panjang lokasi proyek (PRD §F3), hasil agregasi data
/// historis Open-Meteo lewat <c>IHistoricalClimateClient</c> (§8.3).
/// </summary>
/// <remarks>
/// Semua sub-skor kesesuaian (§11.1) dihitung dari objek ini, jadi inilah satu-satunya
/// tempat iklim masuk ke perhitungan domain. Batas nilai meniru CHECK constraint
/// tabel <c>profil_iklim</c>.
/// </remarks>
public sealed record ProfilIklim
{
    public ProfilIklim(
        double curahHujanTahunanMm,
        double kelembapanRataPct,
        double suhuRataC,
        double jarakPesisirKm,
        TingkatKorosi tingkatKorosi)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(curahHujanTahunanMm);
        ArgumentOutOfRangeException.ThrowIfNegative(jarakPesisirKm);

        if (kelembapanRataPct is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kelembapanRataPct), kelembapanRataPct,
                "Kelembapan rata-rata harus 0-100% (profil_iklim.kelembapan_wajar CHECK).");
        }

        if (suhuRataC is < -10 or > 60)
        {
            throw new ArgumentOutOfRangeException(
                nameof(suhuRataC), suhuRataC,
                "Suhu rata-rata harus -10 sampai 60 °C (profil_iklim.suhu_wajar CHECK).");
        }

        CurahHujanTahunanMm = curahHujanTahunanMm;
        KelembapanRataPct = kelembapanRataPct;
        SuhuRataC = suhuRataC;
        JarakPesisirKm = jarakPesisirKm;
        TingkatKorosi = tingkatKorosi;
    }

    /// <summary>Curah hujan tahunan, mm. Memakai-sub-skor lembap (§11.1).</summary>
    public double CurahHujanTahunanMm { get; init; }

    /// <summary>Kelembapan relatif rata-rata, %. Memakai sub-skor lembap (§11.1).</summary>
    public double KelembapanRataPct { get; init; }

    /// <summary>Suhu rata-rata, °C. Menggeser pita konduktivitas ideal (§11.1).</summary>
    public double SuhuRataC { get; init; }

    /// <summary>
    /// Jarak ke garis pantai terdekat, km. Dipakai sebagai proksi risiko korosi akibat
    /// udara bergaram — bukan pengukuran salinitas (PRD §14, R5).
    /// </summary>
    public double JarakPesisirKm { get; init; }

    /// <summary>Tingkat risiko korosi yang telah diklasifikasikan di lokasi.</summary>
    public TingkatKorosi TingkatKorosi { get; init; }
}