namespace Wastu.Domain;

/// <summary>
/// Proyek bangunan milik pengguna beserta lokasinya (PRD §F1).
/// </summary>
/// <remarks>
/// Koordinat divalidasi terhadap batas kasar wilayah Indonesia via <see cref="ValidasiKoordinat"/>.
/// </remarks>
public sealed record Proyek
{
    public Proyek(Guid id, string nama, string kota, double latitude, double longitude)
        : this(id, nama, kota, latitude, longitude, 1, null)
    {
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public Proyek(
        Guid id,
        string nama,
        string kota,
        double latitude,
        double longitude,
        int revisiLokasi = 1,
        string? zonaWaktu = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nama);
        ArgumentException.ThrowIfNullOrWhiteSpace(kota);
        ValidasiKoordinat.Validasi(latitude, longitude);

        if (revisiLokasi < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revisiLokasi), revisiLokasi, "Revisi lokasi minimal bernilai 1.");
        }

        Id = id;
        Nama = nama;
        Kota = kota;
        Latitude = latitude;
        Longitude = longitude;
        RevisiLokasi = revisiLokasi;
        ZonaWaktu = zonaWaktu;
    }

    public Guid Id { get; init; }

    public string Nama { get; init; }

    public string Kota { get; init; }

    /// <summary>Lintang lokasi, derajat WGS84.</summary>
    public double Latitude { get; init; }

    /// <summary>Bujur lokasi, derajat WGS84.</summary>
    public double Longitude { get; init; }

    /// <summary>Nomor revisi koordinat lokasi proyek (default 1, bertambah setiap koordinat diubah).</summary>
    public int RevisiLokasi { get; init; } = 1;

    /// <summary>Zona waktu IANA untuk lokasi proyek (misal "Asia/Jakarta").</summary>
    public string? ZonaWaktu { get; init; }
}
