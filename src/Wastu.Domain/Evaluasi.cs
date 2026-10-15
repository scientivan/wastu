namespace Wastu.Domain;

/// <summary>
/// Empat sub-skor kesesuaian (§11.1), masing-masing sudah dinormalisasi ke rentang 0-100.
/// Dipisahkan dari <see cref="Evaluasi"/> supaya UI bisa menampilkan rincian penilaian
/// dan supaya pengujian dapat memeriksa tiap sub-skor secara terpisah.
/// </summary>
public sealed record SubSkorKesesuaian(
    double Termal,
    double Lembap,
    double Korosi,
    double Usia)
{
    /// <summary>Rata-rata keempat sub-skor, dipakai sebagai pembanding pembobotan equiponderal.</summary>
    public double RataRata => (Termal + Lembap + Korosi + Usia) / 4;
}

/// <summary>Hasil penilaian satu bahan pada satu komponen, siap disimpan ke tabel <c>evaluasi</c>.</summary>
/// <param name="BahanId">Bahan yang dinilai.</param>
/// <param name="KomponenId">Komponen yang dinilai.</param>
/// <param name="SkorKesesuaian">Skor akhir 0-100, hasil penjumlahan berbobot (§11.1).</param>
/// <param name="EmisiAwalKg">Emisi awal = faktor_emisi × luas_area, kgCO2e (§11.2).</param>
/// <param name="SubSkor">Rincian keempat sub-skor sebelum pembobotan.</param>
/// <param name="Alasan">Penjelasan tekstual untuk persona non-teknis (§11.1, §4.1).</param>
public sealed record Evaluasi(
    Guid BahanId,
    Guid KomponenId,
    double SkorKesesuaian,
    double EmisiAwalKg,
    SubSkorKesesuaian SubSkor,
    string Alasan);