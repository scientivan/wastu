using Windows.Devices.Geolocation;

namespace Wastu.Services;

public sealed record PosisiPerangkat(double Lintang, double Bujur, double AkurasiMeter);

// Lokasi perangkat dari Windows Location Service: GPS bila tersedia, selain itu Wi-Fi atau jaringan.
public static class Lokasi
{
    private const string Ditolak = "Akses lokasi ditolak. Aktifkan di Pengaturan Windows › Privasi & keamanan › Lokasi.";

    // Pesan kesalahan dikemas sebagai InvalidOperationException agar halaman cukup menampilkan Message.
    public static async Task<PosisiPerangkat> DeteksiAsync()
    {
        try
        {
            if (await Geolocator.RequestAccessAsync() != GeolocationAccessStatus.Allowed)
                throw new InvalidOperationException(Ditolak);
            var locator = new Geolocator { DesiredAccuracy = PositionAccuracy.High };
            var posisi = await locator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(15));
            var c = posisi.Coordinate;
            return new PosisiPerangkat(c.Point.Position.Latitude, c.Point.Position.Longitude, c.Accuracy);
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException(Ditolak);
        }
        catch (Exception e) when (e is not InvalidOperationException)
        {
            throw new InvalidOperationException("Lokasi tidak dapat dideteksi. Pastikan layanan lokasi aktif lalu coba lagi.", e);
        }
    }
}
