namespace Wastu.Domain.Weather;

/// <summary>
/// Ambang layak-kerja untuk satu jenis pekerjaan (PRD §12).
/// </summary>
public sealed record AmbangPekerjaan
{
    private readonly double? _probabilitasHujanMaksPct;
    private readonly double? _curahHujanMaksMm;
    private readonly double? _kelembapanMaksPct;
    private readonly double? _suhuMinC;
    private readonly double? _suhuMaksC;

    /// <summary>Ambang siap pakai untuk pekerjaan konstruksi; lihat <see cref="AmbangLayakKerja"/>.</summary>
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

        if (!Enum.IsDefined(typeof(TingkatPelanggaran), tingkatSuhu))
        {
            throw new ArgumentOutOfRangeException(nameof(tingkatSuhu), tingkatSuhu, "Tingkat pelanggaran suhu tidak dikenal.");
        }

        _probabilitasHujanMaksPct = probabilitasHujanMaksPct;
        _curahHujanMaksMm = curahHujanMaksMm;
        _kelembapanMaksPct = kelembapanMaksPct;
        _suhuMinC = suhuMinC;
        _suhuMaksC = suhuMaksC;
        TingkatSuhu = tingkatSuhu;
    }

    /// <summary>Batas atas probabilitas hujan, persen 0-100. Null berarti tidak berlaku.</summary>
    public double? ProbabilitasHujanMaksPct
    {
        get => _probabilitasHujanMaksPct;
        init
        {
            Validasi(value, nameof(ProbabilitasHujanMaksPct), 100);
            _probabilitasHujanMaksPct = value;
        }
    }

    /// <summary>Batas atas curah hujan, mm. Null berarti tidak berlaku.</summary>
    public double? CurahHujanMaksMm
    {
        get => _curahHujanMaksMm;
        init
        {
            Validasi(value, nameof(CurahHujanMaksMm));
            _curahHujanMaksMm = value;
        }
    }

    /// <summary>Batas atas kelembapan relatif, persen 0-100. Null berarti tidak berlaku.</summary>
    public double? KelembapanMaksPct
    {
        get => _kelembapanMaksPct;
        init
        {
            Validasi(value, nameof(KelembapanMaksPct), 100);
            _kelembapanMaksPct = value;
        }
    }

    /// <summary>Suhu minimum yang masih layak dikerjakan, derajat Celsius. Null berarti tidak berlaku.</summary>
    public double? SuhuMinC
    {
        get => _suhuMinC;
        init
        {
            Validasi(value, nameof(SuhuMinC));
            _suhuMinC = value;
        }
    }

    /// <summary>Suhu maksimum yang masih layak dikerjakan, derajat Celsius. Null berarti tidak berlaku.</summary>
    public double? SuhuMaksC
    {
        get => _suhuMaksC;
        init
        {
            Validasi(value, nameof(SuhuMaksC));
            _suhuMaksC = value;
        }
    }

    /// <summary>Apakah pelanggaran batas suhu bersifat keras atau hanya peringatan.</summary>
    public TingkatPelanggaran TingkatSuhu { get; init; }

    /// <summary>
    /// Memvalidasi ulang seluruh ambang batas terhadap NaN/Infinity dan konsistensi rentang suhu.
    /// </summary>
    public void PastikanValid()
    {
        Validasi(ProbabilitasHujanMaksPct, nameof(ProbabilitasHujanMaksPct), 100);
        Validasi(CurahHujanMaksMm, nameof(CurahHujanMaksMm));
        Validasi(KelembapanMaksPct, nameof(KelembapanMaksPct), 100);
        Validasi(SuhuMinC, nameof(SuhuMinC));
        Validasi(SuhuMaksC, nameof(SuhuMaksC));

        if (SuhuMinC is not null && SuhuMaksC is not null && SuhuMinC > SuhuMaksC)
        {
            throw new ArgumentException(
                $"Suhu minimum {SuhuMinC} lebih besar dari suhu maksimum {SuhuMaksC}; "
                + "rentang tersebut tidak mungkin dipenuhi.");
        }
    }

    private static void Validasi(double? nilai, string nama, double batasAtas = double.PositiveInfinity)
    {
        if (nilai is null)
        {
            return;
        }

        if (!double.IsFinite(nilai.Value))
        {
            throw new ArgumentException($"Ambang {nama} harus berupa angka finite.", nama);
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