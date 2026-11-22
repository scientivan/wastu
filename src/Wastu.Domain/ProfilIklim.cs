namespace Wastu.Domain;

/// <summary>
/// Profil iklim jangka panjang sebuah lokasi (PRD §F3), hasil agregasi data historis
/// Open-Meteo lewat <c>IHistoricalClimateClient</c> (§8.3).
/// </summary>
public sealed record ProfilIklim
{
    public ProfilIklim(
        double curahHujanTahunanMm,
        double kelembapanRataPct,
        double suhuRataC,
        double jarakPesisirKm,
        TingkatKorosi tingkatKorosi)
        : this(curahHujanTahunanMm, kelembapanRataPct, suhuRataC, jarakPesisirKm, tingkatKorosi, null, 1, ProvenanceIklim.LegacyUnknown)
    {
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public ProfilIklim(
        double curahHujanTahunanMm,
        double kelembapanRataPct,
        double suhuRataC,
        double jarakPesisirKm,
        TingkatKorosi tingkatKorosi,
        MetadataIklim? metadata = null,
        int revisiLokasi = 1,
        ProvenanceIklim provenance = ProvenanceIklim.LegacyUnknown)
    {
        if (!double.IsFinite(curahHujanTahunanMm))
            throw new ArgumentException("Curah hujan tahunan harus bernilai finite.", nameof(curahHujanTahunanMm));
        ArgumentOutOfRangeException.ThrowIfNegative(curahHujanTahunanMm);

        if (!double.IsFinite(jarakPesisirKm))
            throw new ArgumentException("Jarak pesisir harus bernilai finite.", nameof(jarakPesisirKm));
        ArgumentOutOfRangeException.ThrowIfNegative(jarakPesisirKm);

        if (!double.IsFinite(kelembapanRataPct) || kelembapanRataPct is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kelembapanRataPct), kelembapanRataPct,
                "Kelembapan rata-rata harus bernilai finite 0-100% (profil_iklim.kelembapan_wajar CHECK).");
        }

        if (!double.IsFinite(suhuRataC) || suhuRataC is < -10 or > 60)
        {
            throw new ArgumentOutOfRangeException(
                nameof(suhuRataC), suhuRataC,
                "Suhu rata-rata harus bernilai finite -10 sampai 60 °C (profil_iklim.suhu_wajar CHECK).");
        }

        if (!Enum.IsDefined(typeof(TingkatKorosi), tingkatKorosi))
        {
            throw new ArgumentOutOfRangeException(nameof(tingkatKorosi), tingkatKorosi, "Tingkat korosi tidak dikenal.");
        }

        if (!Enum.IsDefined(typeof(ProvenanceIklim), provenance))
        {
            throw new ArgumentOutOfRangeException(nameof(provenance), provenance, "Provenance iklim tidak dikenal.");
        }

        if (revisiLokasi < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revisiLokasi), revisiLokasi, "Revisi lokasi minimal bernilai 1.");
        }

        CurahHujanTahunanMm = curahHujanTahunanMm;
        KelembapanRataPct = kelembapanRataPct;
        SuhuRataC = suhuRataC;
        JarakPesisirKm = jarakPesisirKm;
        TingkatKorosi = tingkatKorosi;
        Metadata = metadata;
        RevisiLokasi = revisiLokasi;
        Provenance = metadata?.Provenance ?? provenance;
        DiambilPada = metadata?.DiambilPadaUtc ?? DateTimeOffset.UtcNow;
    }

    /// <summary>Curah hujan tahunan, mm. Memakai sub-skor lembap (§11.1).</summary>
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

    public DateTimeOffset? DiambilPada { get; init; }

    /// <summary>Metadata penyedia, periode waktu, dan status provenance iklim.</summary>
    public MetadataIklim? Metadata { get; init; }

    /// <summary>Revisi koordinat lokasi proyek tempat iklim ini diambil.</summary>
    public int RevisiLokasi { get; init; } = 1;

    /// <summary>Asal-usul data iklim (ApiHistoris, Contoh, EstimasiBawaan, ManualOverride, LegacyUnknown).</summary>
    public ProvenanceIklim Provenance { get; init; } = ProvenanceIklim.LegacyUnknown;
}
