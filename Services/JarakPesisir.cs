using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Wastu.Services;

internal static class JarakPesisir
{
    private static readonly Lazy<double[][][]> GarisPantai = new(Muat);

    public static double HitungKm(double lintang, double bujur)
    {
        const double kmPerDerajat = Math.PI * 6371 / 180;
        double skalaBujur = Math.Cos(lintang * Math.PI / 180);
        double terdekat = double.PositiveInfinity;

        // Proyeksi lokal pada lintang proyek, cukup untuk mengestimasi paparan pesisir.
        foreach (var garis in GarisPantai.Value)
        {
            for (int i = 1; i < garis.Length; i++)
            {
                double x = (garis[i - 1][0] - bujur) * skalaBujur;
                double y = garis[i - 1][1] - lintang;
                double dx = (garis[i][0] - garis[i - 1][0]) * skalaBujur;
                double dy = garis[i][1] - garis[i - 1][1];
                double panjangKuadrat = dx * dx + dy * dy;
                double posisi = panjangKuadrat == 0 ? 0 : Math.Clamp(-(x * dx + y * dy) / panjangKuadrat, 0, 1);
                double px = x + posisi * dx;
                double py = y + posisi * dy;
                terdekat = Math.Min(terdekat, px * px + py * py);
            }
        }
        return Math.Sqrt(terdekat) * kmPerDerajat;
    }

    private static double[][][] Muat()
    {
        using Stream arsip = typeof(JarakPesisir).Assembly.GetManifestResourceStream("Assets.coastline-indonesia.json.gz")!;
        using var data = new GZipStream(arsip, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<double[][][]>(data) ?? throw new InvalidDataException("Garis pantai kosong.");
    }
}
