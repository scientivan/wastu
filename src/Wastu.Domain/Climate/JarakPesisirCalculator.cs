namespace Wastu.Domain.Climate;

/// <summary>
/// Penghitung jarak titik ke garis pantai terdekat dan klasifikasi paparan korosi (PRD §F3, §8.3, WP03).
/// </summary>
public static class JarakPesisirCalculator
{
    public const string VersiAlgoritme = "v1-point-to-segment-natural-earth";
    public const string CatatanProksiPesisir = "Perkiraan jarak ke garis pantai; proksi paparan udara bergaram.";

    private const double KmPerDerajat = Math.PI * 6371.0 / 180.0;

    /// <summary>
    /// Menghitung jarak terpendek dari titik (lat, lon) ke segmen garis A(latA, lonA) -> B(latB, lonB) dalam kilometer.
    /// Menggunakan proyeksi equirectangular lokal pada lintang titik kueri.
    /// </summary>
    public static double HitungJarakTitikKeSegmenKm(double lat, double lon, double latA, double lonA, double latB, double lonB)
    {
        double skalaBujur = Math.Cos(lat * Math.PI / 180.0);
        double xA = (lonA - lon) * skalaBujur;
        double yA = latA - lat;
        double dx = (lonB - lonA) * skalaBujur;
        double dy = latB - latA;
        double panjangKuadrat = dx * dx + dy * dy;

        double t = panjangKuadrat == 0.0 ? 0.0 : Math.Clamp(-(xA * dx + yA * dy) / panjangKuadrat, 0.0, 1.0);
        double px = xA + t * dx;
        double py = yA + t * dy;
        return Math.Sqrt(px * px + py * py) * KmPerDerajat;
    }

    /// <summary>
    /// Menghitung jarak minimum dari titik (lat, lon) ke seluruh polylines garis pantai.
    /// Mengembalikan null bila data garis pantai null, kosong, atau tidak memuat segmen valid (PRD §8.3, T10).
    /// Tidak pernah mengembalikan 0 atau PositiveInfinity saat data tidak tersedia.
    /// </summary>
    public static double? HitungJarakMinimumKm(
        double lat,
        double lon,
        IReadOnlyList<IReadOnlyList<(double Lat, double Lon)>>? polylines)
    {
        if (polylines is null || polylines.Count == 0)
            return null;

        double minJarak = double.PositiveInfinity;
        bool adaSegmen = false;

        for (int p = 0; p < polylines.Count; p++)
        {
            var garis = polylines[p];
            if (garis is null || garis.Count < 2)
                continue;

            for (int i = 1; i < garis.Count; i++)
            {
                var ptA = garis[i - 1];
                var ptB = garis[i];
                double jarak = HitungJarakTitikKeSegmenKm(lat, lon, ptA.Lat, ptA.Lon, ptB.Lat, ptB.Lon);
                if (jarak < minJarak)
                {
                    minJarak = jarak;
                }
                adaSegmen = true;
            }
        }

        if (!adaSegmen || double.IsPositiveInfinity(minJarak))
            return null;

        return minJarak;
    }

    /// <summary>
    /// Mengklasifikasikan tingkat korosi dari jarak pesisir:
    /// &lt; 10 km: Tinggi, &lt; 35 km: Sedang, lainnya: Rendah.
    /// </summary>
    public static TingkatKorosi KlasifikasiKorosi(double jarakKm)
    {
        if (jarakKm < 10.0) return TingkatKorosi.Tinggi;
        if (jarakKm < 35.0) return TingkatKorosi.Sedang;
        return TingkatKorosi.Rendah;
    }
}
