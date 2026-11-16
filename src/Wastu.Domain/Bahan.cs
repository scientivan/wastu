namespace Wastu.Domain;

/// <summary>
/// Satu bahan bangunan dengan sifat termal, ketahanan, usia pakai, dan jejak karbonnya
/// (PRD §F2). Objek ini adalah masukan utama <c>SuitabilityScoringService</c> dan
/// <c>EmissionSimulationService</c>.
/// </summary>
/// <remarks>
/// Batas nilai pada konstruktor sengaja meniru CHECK constraint tabel <c>bahan</c>
/// supaya data tidak valid tertangkap di domain, bukan baru saat menyentuh database.
/// Bersifat immutable: lapisan data (Wastu.Data) memetakannya ke baris tabel,
/// domain tidak tahu-menahu tentang tabel.
/// </remarks>
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
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nama);
        ArgumentException.ThrowIfNullOrWhiteSpace(sumberData);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(konduktivitasTermal);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(usiaPakaiNominalTahun);
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

        Id = id;
        Nama = nama;
        Jenis = jenis;
        KonduktivitasTermal = konduktivitasTermal;
        KetahananLembap = ketahananLembap;
        KetahananKorosi = ketahananKorosi;
        UsiaPakaiNominalTahun = usiaPakaiNominalTahun;
        FaktorEmisi = faktorEmisi;
        SumberData = sumberData;
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
}