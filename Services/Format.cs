using System.Globalization;
using System.Text.RegularExpressions;

namespace Wastu.Services;

// Padanan Intl id-ID yang dipakai antarmuka referensi.
public static class Format
{
    public static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    // Intl.NumberFormat('id-ID', { maximumFractionDigits: 1 })
    public static string Angka(double nilai) => nilai.ToString("#,##0.#", Id);

    public static string[] AlasanMaterial(string alasan)
    {
        // Evaluasi yang sudah tersimpan memakai titik koma sebagai pemisah kriteria.
        var ringkas = Regex.Replace(alasan, @"-?\d+,\d+", cocok =>
        {
            if (!double.TryParse(cocok.Value, NumberStyles.Float, Id, out double nilai) ||
                !double.IsFinite(nilai) || (nilai != 0 && Math.Round(nilai, 2) == 0))
                return cocok.Value;
            return nilai.ToString("#,##0.##", Id);
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

    // toLocaleDateString('id-ID', { day: 'numeric', month: 'long', year: 'numeric' })
    public static string Tanggal(DateTime tanggal) => tanggal.ToString("d MMMM yyyy", Id);

    public static string HariSingkat(DateTime tanggal) => tanggal.ToString("ddd", Id);

    public static string TanggalSingkat(DateTime tanggal) => tanggal.ToString("d MMM", Id);

    public static string Iso(DateTime tanggal) => tanggal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // new Date().toLocaleString('id-ID')
    public static string WaktuLokal(DateTime waktu) => waktu.ToString("d/M/yyyy, HH.mm.ss", Id);

    public static string WaktuProyek(DateTimeOffset waktu, string zonaWaktu)
    {
        var lokal = TimeZoneInfo.ConvertTime(waktu, TimeZoneInfo.FindSystemTimeZoneById(zonaWaktu));
        string zona = zonaWaktu switch { "Asia/Jayapura" => "WIT", "Asia/Makassar" => "WITA",
            "Asia/Jakarta" or "Asia/Pontianak" => "WIB", _ => zonaWaktu };
        return lokal.ToString("d MMMM yyyy, HH:mm", Id) + " " + zona;
    }

    public static string Masukan(double nilai) => nilai.ToString("0.##########", Id);

    // Menerima koma (id-ID) maupun titik desimal seperti input number di browser.
    public static bool CobaBaca(string? teks, out double nilai)
    {
        nilai = 0;
        var t = teks?.Trim();
        if (string.IsNullOrEmpty(t)) return false;
        if (t.Contains(',') && !t.Contains('.')) t = t.Replace(',', '.');
        else if (t.Contains(',') && t.Contains('.')) t = t.Replace(".", "").Replace(',', '.');
        return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out nilai) && double.IsFinite(nilai);
    }
}
