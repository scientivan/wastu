using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Wastu.Domain.Climate;

namespace Wastu.Services;

internal static class JarakPesisir
{
    private static readonly Lazy<List<List<(double Lat, double Lon)>>?> GarisPantai = new(Muat);

    public static double? CobaHitungKm(double lintang, double bujur)
    {
        try
        {
            var polylines = GarisPantai.Value;
            if (polylines is null)
                return null;
            return JarakPesisirCalculator.HitungJarakMinimumKm(lintang, bujur, polylines);
        }
        catch
        {
            return null;
        }
    }

    public static double HitungKm(double lintang, double bujur)
    {
        var jarak = CobaHitungKm(lintang, bujur);
        if (jarak is null)
        {
            throw new InvalidOperationException("Data aset garis pantai tidak tersedia atau kosong.");
        }
        return jarak.Value;
    }

    private static List<List<(double Lat, double Lon)>>? Muat()
    {
        try
        {
            using Stream? arsip = typeof(JarakPesisir).Assembly.GetManifestResourceStream("Assets.coastline-indonesia.json.gz");
            if (arsip is null) return null;
            using var data = new GZipStream(arsip, CompressionMode.Decompress);
            var raw = JsonSerializer.Deserialize<double[][][]>(data);
            if (raw is null || raw.Length == 0) return null;

            var hasil = new List<List<(double Lat, double Lon)>>(raw.Length);
            foreach (var line in raw)
            {
                if (line is null || line.Length == 0) continue;
                var polyline = new List<(double Lat, double Lon)>(line.Length);
                foreach (var pt in line)
                {
                    if (pt is not null && pt.Length >= 2)
                    {
                        // GeoJSON format is [lon, lat]
                        polyline.Add((Lat: pt[1], Lon: pt[0]));
                    }
                }
                if (polyline.Count > 0)
                {
                    hasil.Add(polyline);
                }
            }
            return hasil.Count > 0 ? hasil : null;
        }
        catch
        {
            return null;
        }
    }
}
