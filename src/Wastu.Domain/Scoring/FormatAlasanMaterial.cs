using System.Globalization;
using System.Text.RegularExpressions;

namespace Wastu.Domain.Scoring;

/// <summary>Menata alasan evaluasi untuk tampilan dan laporan dari teks yang sudah tersimpan.</summary>
public static class FormatAlasanMaterial
{
    private static readonly CultureInfo Budaya = CultureInfo.GetCultureInfo("id-ID");

    public static string[] KeBaris(string alasan)
    {
        // Evaluasi yang sudah tersimpan memakai titik koma sebagai pemisah kriteria.
        var ringkas = Regex.Replace(alasan, @"-?\d+,\d+", cocok =>
        {
            if (!double.TryParse(cocok.Value, NumberStyles.Float, Budaya, out double nilai) ||
                !double.IsFinite(nilai) || (nilai != 0 && Math.Round(nilai, 2) == 0))
                return cocok.Value;
            return nilai.ToString("#,##0.##", Budaya);
        });
        ringkas = Regex.Replace(ringkas, @"(?<=\d) C\b", " °C")
            .Replace(" W/mK", " W/(m·K)", StringComparison.Ordinal)
            .Replace(" persen", "%", StringComparison.Ordinal)
            .Replace("; jarak ke pantai sebagai proksi risiko udara bergaram",
                ". Jarak ke pantai dipakai untuk memperkirakan risiko udara bergaram", StringComparison.Ordinal);
        return Regex.Split(ringkas, @";\s*(?=Ketahanan lembap|Ketahanan korosi|Usia pakai nominal)")
            .Where(baris => !string.IsNullOrWhiteSpace(baris))
            .Select(baris => baris.Trim().TrimEnd('.') + ".")
            .ToArray();
    }
}
