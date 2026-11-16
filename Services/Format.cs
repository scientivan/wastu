using System.Globalization;

namespace Wastu.Services;

// Padanan Intl id-ID yang dipakai antarmuka referensi.
public static class Format
{
    public static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    // Intl.NumberFormat('id-ID', { maximumFractionDigits: 1 })
    public static string Angka(double nilai) => nilai.ToString("#,##0.#", Id);

    // toLocaleDateString('id-ID', { day: 'numeric', month: 'long', year: 'numeric' })
    public static string Tanggal(DateTime tanggal) => tanggal.ToString("d MMMM yyyy", Id);

    public static string HariSingkat(DateTime tanggal) => tanggal.ToString("ddd", Id);

    public static string TanggalSingkat(DateTime tanggal) => tanggal.ToString("d MMM", Id);

    public static string Iso(DateTime tanggal) => tanggal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // new Date().toLocaleString('id-ID')
    public static string WaktuLokal(DateTime waktu) => waktu.ToString("d/M/yyyy, HH.mm.ss", Id);

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
