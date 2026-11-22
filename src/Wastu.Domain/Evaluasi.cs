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
public sealed record Evaluasi(
    Guid Id,
    Guid BahanId,
    Guid KomponenId,
    double SkorKesesuaian,
    double EmisiAwalKg,
    SubSkorKesesuaian SubSkor,
    string Alasan,
    Guid? BatchId = null,
    string? VersiModel = null,
    string? HashInput = null,
    DateTimeOffset? DihitungPadaUtc = null);