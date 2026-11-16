namespace Wastu.Domain;

/// <summary>
/// Proyek bangunan milik pengguna beserta lokasinya (PRD §F1).
/// </summary>
/// <remarks>
/// Koordinat wajib berada dalam wilayah Indonesia pada MVP — batas ini divalidasi
/// di sini karena CHECK constraint <c>bangunan_berada_di_indonesia</c> hanya menyala
/// setelah data menyentuh database, sedangkan userspace bisa keliru memasukkan koordinat.
/// </remarks>
public sealed record Proyek
{
    private const double LatitudeMin = -11;
    private const double LatitudeMaks = 6;
    private const double LongitudeMin = 94;
    private const double LongitudeMaks = 142;

    public Proyek(Guid id, string nama, string kota, double latitude, double longitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nama);
        ArgumentException.ThrowIfNullOrWhiteSpace(kota);

        if (latitude is < LatitudeMin or > LatitudeMaks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude), latitude,
                $"Lintang harus {LatitudeMin} sampai {LatitudeMaks}° agar berada di wilayah Indonesia.");
        }

        if (longitude is < LongitudeMin or > LongitudeMaks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude), longitude,
                $"Bujur harus {LongitudeMin} sampai {LongitudeMaks}° agar berada di wilayah Indonesia.");
        }

        Id = id;
        Nama = nama;
        Kota = kota;
        Latitude = latitude;
        Longitude = longitude;
    }

    public Guid Id { get; init; }

    public string Nama { get; init; }

    public string Kota { get; init; }

    /// <summary>Lintang lokasi, derajat WGS84.</summary>
    public double Latitude { get; init; }

    /// <summary>Bujur lokasi, derajat WGS84.</summary>
    public double Longitude { get; init; }
}