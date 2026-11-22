namespace Wastu.Domain;

/// <summary>
/// Satu bahan bangunan dengan sifat termal, ketahanan, usia pakai, dan jejak karbonnya
/// (PRD §F2). Objek ini adalah masukan utama <c>SuitabilityScoringService</c> dan
/// <c>EmissionSimulationService</c>.
/// </summary>
public sealed record Bahan
{
    public Bahan(
        Guid id,
        string nama,
        JenisKomponen jenis,
        double konduktivitasTermal,
        int ketahananLembap,
        int ketahananKorosi,
        double usiaPakaiNominalTahun,
        double faktorEmisi,
        string sumberData)
        : this(id, nama, jenis, konduktivitasTermal, ketahananLembap, ketahananKorosi, usiaPakaiNominalTahun, faktorEmisi, sumberData, null, 1, null)
    {
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public Bahan(
        Guid id,
        string nama,
        JenisKomponen jenis,
        double konduktivitasTermal,
        int ketahananLembap,
        int ketahananKorosi,
        double usiaPakaiNominalTahun,
        double faktorEmisi,
        string sumberData,
        string? susunanKetebalan = null,
        int versiData = 1,
        IReadOnlyList<SumberPropertiBahan>? sumberProperti = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nama);
        ArgumentException.ThrowIfNullOrWhiteSpace(sumberData);

        if (!Enum.IsDefined(typeof(JenisKomponen), jenis))
        {
            throw new ArgumentOutOfRangeException(nameof(jenis), jenis, "Jenis komponen tidak dikenal.");
        }

        if (!double.IsFinite(konduktivitasTermal))
        {
            throw new ArgumentException("Konduktivitas termal harus bernilai finite.", nameof(konduktivitasTermal));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(konduktivitasTermal);

        if (!double.IsFinite(usiaPakaiNominalTahun))
        {
            throw new ArgumentException("Usia pakai nominal harus bernilai finite.", nameof(usiaPakaiNominalTahun));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(usiaPakaiNominalTahun);

        if (!double.IsFinite(faktorEmisi))
        {
            throw new ArgumentException("Faktor emisi harus bernilai finite.", nameof(faktorEmisi));
        }
        ArgumentOutOfRangeException.ThrowIfNegative(faktorEmisi);

        if (ketahananLembap is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ketahananLembap), ketahananLembap,
                "Indeks ketahanan lembap harus 1-5 (bahan.ketahanan_lembap CHECK).");
        }

        if (ketahananKorosi is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ketahananKorosi), ketahananKorosi,
                "Indeks ketahanan korosi harus 1-5 (bahan.ketahanan_korosi CHECK).");
        }

        if (versiData < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(versiData), versiData, "Versi data minimal bernilai 1.");
        }

        Id = id;
        Nama = nama;
        Jenis = jenis;
        KonduktivitasTermal = konduktivitasTermal;
        KetahananLembap = ketahananLembap;
        KetahananKorosi = ketahananKorosi;
        UsiaPakaiNominalTahun = usiaPakaiNominalTahun;
        FaktorEmisi = faktorEmisi;
        SumberData = sumberData;
        SusunanKetebalan = susunanKetebalan;
        VersiData = versiData;
        SumberProperti = sumberProperti;
    }

    public Guid Id { get; init; }

    public string Nama { get; init; }

    public JenisKomponen Jenis { get; init; }

    /// <summary>Konduktivitas termal, W/m·K. Makin rendah makin baik sebagai isolasi.</summary>
    public double KonduktivitasTermal { get; init; }

    /// <summary>Indeks ketahanan lembap, 1-5. Makin tinggi makin baik.</summary>
    public int KetahananLembap { get; init; }

    /// <summary>Indeks ketahanan korosi, 1-5. Makin tinggi makin baik.</summary>
    public int KetahananKorosi { get; init; }

    /// <summary>Usia pakai nominal, tahun, sebelum disesuaikan iklim (PRD §11.3).</summary>
    public double UsiaPakaiNominalTahun { get; init; }

    /// <summary>Faktor emisi, kgCO2e per m² (PRD §11.2).</summary>
    public double FaktorEmisi { get; init; }

    /// <summary>
    /// Rujukan sumber data publik. Wajib diisi karena transparansi sumber adalah
    /// komitmen PRD §1 dan §11.4, bukan metadata opsional.
    /// </summary>
    public string SumberData { get; init; }

    /// <summary>Susunan ketebalan standar atau konfigurasi lapisan (opsional).</summary>
    public string? SusunanKetebalan { get; init; }

    /// <summary>Nomor versi katalog data bahan (default 1).</summary>
    public int VersiData { get; init; } = 1;

    /// <summary>Daftar sumber rujukan terverifikasi per properti (opsional).</summary>
    public IReadOnlyList<SumberPropertiBahan>? SumberProperti { get; init; }
}
