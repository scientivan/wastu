namespace Wastu.Domain;

/// <summary>
/// Label kategori kesesuaian bahan terhadap iklim (PRD-R10).
/// </summary>
public enum LabelKesesuaian
{
    TidakDisarankan,
    PerluPertimbangan,
    Cocok
}

/// <summary>
/// Konfigurasi model perhitungan dan ambang batas berversi (PRD-R11, PRD-R10).
/// </summary>
public sealed record KonfigurasiModel
{
    public KonfigurasiModel(
        string versiModel = "v2.0",
        double bobotLembapHujanAlpha = 0.5,
        double acuanCurahHujanMm = 3000.0,
        double acuanJarakPesisirKm = 50.0,
        double pitaTermalBatasBawah = 0.10,
        double pitaTermalBatasAtasSejuk = 0.60,
        double pitaTermalBatasAtasPanas = 0.30,
        double suhuSejukC = 26.0,
        double suhuPanasC = 32.0,
        double faktorPenuaanA = 0.5,
        double ambangTidakDisarankan = 55.0,
        double ambangCocok = 75.0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versiModel);

        if (!double.IsFinite(faktorPenuaanA) || faktorPenuaanA <= 0 || faktorPenuaanA > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(faktorPenuaanA), faktorPenuaanA,
                "Faktor penuaan a harus bernilai finite dan berada dalam rentang 0 < a <= 1.");
        }

        if (!double.IsFinite(bobotLembapHujanAlpha) || bobotLembapHujanAlpha is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bobotLembapHujanAlpha), bobotLembapHujanAlpha,
                "Bobot lembap hujan alpha harus bernilai finite antara 0 dan 1.");
        }

        if (!double.IsFinite(acuanCurahHujanMm) || acuanCurahHujanMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acuanCurahHujanMm), acuanCurahHujanMm, "Acuan curah hujan harus bernilai finite positif.");
        }

        if (!double.IsFinite(acuanJarakPesisirKm) || acuanJarakPesisirKm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(acuanJarakPesisirKm), acuanJarakPesisirKm, "Acuan jarak pesisir harus bernilai finite positif.");
        }

        if (!double.IsFinite(pitaTermalBatasBawah) || pitaTermalBatasBawah < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pitaTermalBatasBawah), pitaTermalBatasBawah, "Pita termal batas bawah harus bernilai finite non-negatif.");
        }

        if (!double.IsFinite(pitaTermalBatasAtasSejuk) || pitaTermalBatasAtasSejuk < pitaTermalBatasBawah)
        {
            throw new ArgumentOutOfRangeException(nameof(pitaTermalBatasAtasSejuk), pitaTermalBatasAtasSejuk, "Pita termal batas atas sejuk harus >= batas bawah.");
        }

        if (!double.IsFinite(pitaTermalBatasAtasPanas) || pitaTermalBatasAtasPanas < pitaTermalBatasBawah)
        {
            throw new ArgumentOutOfRangeException(nameof(pitaTermalBatasAtasPanas), pitaTermalBatasAtasPanas, "Pita termal batas atas panas harus >= batas bawah.");
        }

        if (!double.IsFinite(suhuSejukC))
        {
            throw new ArgumentOutOfRangeException(nameof(suhuSejukC), suhuSejukC, "Suhu sejuk harus bernilai finite.");
        }

        if (!double.IsFinite(suhuPanasC) || suhuPanasC <= suhuSejukC)
        {
            throw new ArgumentOutOfRangeException(nameof(suhuPanasC), suhuPanasC, "Suhu panas harus bernilai finite dan lebih besar dari suhu sejuk.");
        }

        if (!double.IsFinite(ambangTidakDisarankan) || ambangTidakDisarankan is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(ambangTidakDisarankan), ambangTidakDisarankan, "Ambang tidak disarankan harus finite 0-100.");
        }

        if (!double.IsFinite(ambangCocok) || ambangCocok < ambangTidakDisarankan || ambangCocok > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(ambangCocok), ambangCocok, "Ambang cocok harus finite dan >= ambang tidak disarankan.");
        }

        VersiModel = versiModel;
        BobotLembapHujanAlpha = bobotLembapHujanAlpha;
        AcuanCurahHujanMm = acuanCurahHujanMm;
        AcuanJarakPesisirKm = acuanJarakPesisirKm;
        PitaTermalBatasBawah = pitaTermalBatasBawah;
        PitaTermalBatasAtasSejuk = pitaTermalBatasAtasSejuk;
        PitaTermalBatasAtasPanas = pitaTermalBatasAtasPanas;
        SuhuSejukC = suhuSejukC;
        SuhuPanasC = suhuPanasC;
        FaktorPenuaanA = faktorPenuaanA;
        AmbangTidakDisarankan = ambangTidakDisarankan;
        AmbangCocok = ambangCocok;
    }

    public string VersiModel { get; init; } = "v2.0";

    public double BobotLembapHujanAlpha { get; init; } = 0.5;

    public double AcuanCurahHujanMm { get; init; } = 3000.0;

    public double AcuanJarakPesisirKm { get; init; } = 50.0;

    public double PitaTermalBatasBawah { get; init; } = 0.10;

    public double PitaTermalBatasAtasSejuk { get; init; } = 0.60;

    public double PitaTermalBatasAtasPanas { get; init; } = 0.30;

    public double SuhuSejukC { get; init; } = 26.0;

    public double SuhuPanasC { get; init; } = 32.0;

    public double FaktorPenuaanA { get; init; } = 0.5;

    public double AmbangTidakDisarankan { get; init; } = 55.0;

    public double AmbangCocok { get; init; } = 75.0;

    /// <summary>Konfigurasi model default bawaan sistem.</summary>
    public static KonfigurasiModel Bawaan { get; } = new();

    /// <summary>
    /// Menentukan label kesesuaian berdasarkan skor floating-point asli (PRD-R10):
    /// - skor &lt; 55: TidakDisarankan
    /// - 55 &lt;= skor &lt; 75: PerluPertimbangan
    /// - skor &gt;= 75: Cocok
    /// </summary>
    public LabelKesesuaian LabelUntuk(double skor)
    {
        if (!double.IsFinite(skor))
        {
            throw new ArgumentException("Skor harus berupa angka finite.", nameof(skor));
        }

        if (skor < AmbangTidakDisarankan)
        {
            return LabelKesesuaian.TidakDisarankan;
        }

        if (skor < AmbangCocok)
        {
            return LabelKesesuaian.PerluPertimbangan;
        }

        return LabelKesesuaian.Cocok;
    }
}
