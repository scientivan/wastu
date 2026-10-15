namespace Wastu.Domain.Weather;

/// <summary>
/// Ambang layak-kerja untuk satu jenis pekerjaan (PRD §12).
/// </summary>
/// <remarks>
/// <para>
/// Setiap jenis pekerjaan punya sensitivitas cuaca berbeda sehingga tidak bisa
/// memakai ambang yang sama. PRD §12 juga mewajibkan ambang ini dapat
/// dikonfigurasi, bukan konstanta tersembunyi, karena spesifikasi produk cat dan
/// semen berbeda antar merek (PRD §14, R4). Karena itu ambangnya berupa nilai
/// yang dikirim pemanggil, dengan <see cref="AmbangLayakKerja"/> hanya menyediakan
/// nilai awal.
/// </para>
/// <para>
/// Ambang yang null berarti tidak berlaku untuk jenis pekerjaan tersebut, bukan
/// berarti diabaikan diam-diam. Pengecoran tidak punya ambang kelembapan, dan
/// pengecatan tidak punya ambang curah hujan karena keduanya memang tidak jadi
/// alasan utama kegagalan pada pekerjaan itu.
/// </para>
/// </remarks>
public sealed record AmbangPekerjaan
{
    /// <summary>Ambang siap pakai untuk pengecoran; lihat <see cref="AmbangLayakKerja"/>.</summary>
    public AmbangPekerjaan(
        double? probabilitasHujanMaksPct,
        double? curahHujanMaksMm,
        double? kelembapanMaksPct,
        double? suhuMinC,
        double? suhuMaksC,
        TingkatPelanggaran tingkatSuhu = TingkatPelanggaran.Keras)
    {
        Validasi(probabilitasHujanMaksPct, nameof(probabilitasHujanMaksPct), 100);
        Validasi(curahHujanMaksMm, nameof(curahHujanMaksMm));
        Validasi(kelembapanMaksPct, nameof(kelembapanMaksPct), 100);
        Validasi(suhuMinC, nameof(suhuMinC));
        Validasi(suhuMaksC, nameof(suhuMaksC));

        if (suhuMinC is not null && suhuMaksC is not null && suhuMinC > suhuMaksC)
        {
            throw new ArgumentException(
                $"Suhu minimum {suhuMinC} lebih besar dari suhu maksimum {suhuMaksC}; "
                + "rentang tersebut tidak mungkin dipenuhi.");
        }

        ProbabilitasHujanMaksPct = probabilitasHujanMaksPct;
        CurahHujanMaksMm = curahHujanMaksMm;
        KelembapanMaksPct = kelembapanMaksPct;
        SuhuMinC = suhuMinC;
        SuhuMaksC = suhuMaksC;
        TingkatSuhu = tingkatSuhu;
    }

    /// <summary>Batas atas probabilitas hujan, persen 0-100. Null berarti tidak berlaku.</summary>
    public double? ProbabilitasHujanMaksPct { get; init; }

    /// <summary>Batas atas curah hujan, mm. Null berarti tidak berlaku.</summary>
    public double? CurahHujanMaksMm { get; init; }

    /// <summary>Batas atas kelembapan relatif, persen 0-100. Null berarti tidak berlaku.</summary>
    public double? KelembapanMaksPct { get; init; }

    /// <summary>Suhu minimum yang masih layak dikerjakan, derajat Celsius. Null berarti tidak berlaku.</summary>
    public double? SuhuMinC { get; init; }

    /// <summary>Suhu maksimum yang masih layak dikerjakan, derajat Celsius. Null berarti tidak berlaku.</summary>
    public double? SuhuMaksC { get; init; }

    /// <summary>Apakah pelanggaran batas suhu bersifat keras atau hanya peringatan.</summary>
    public TingkatPelanggaran TingkatSuhu { get; init; }

    private static void Validasi(double? nilai, string nama, double batasAtas = double.PositiveInfinity)
    {
        if (nilai is null)
        {
            return;
        }

        if (nilai < 0)
        {
            throw new ArgumentOutOfRangeException(nama, nilai, "Ambang tidak boleh negatif.");
        }

        if (nilai > batasAtas)
        {
            throw new ArgumentOutOfRangeException(nama, nilai, $"Ambang tidak boleh melebihi {batasAtas}.");
        }
    }
}