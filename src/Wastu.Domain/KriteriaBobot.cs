namespace Wastu.Domain;

/// <summary>
/// Bobot keempat kriteria kesesuaian (PRD §11.1).
/// </summary>
/// <remarks>
/// Syarat PRD §11.1 adalah jumlah bobot tepat 1. Nilai menyimpang dari 1 ditolak
/// dalam toleransi pembulatan floating point saja — bukan untuk membiarkan konfigurasi
/// bebas, karena bobot tak ternormalisasi akan membuat skor akhir tidak sebanding
/// dengan rentang 0-100 yang ditampilkan ke pengguna.
/// </remarks>
public sealed record KriteriaBobot
{
    private const double Toleransi = 1e-9;

    public KriteriaBobot(double termal, double lembap, double korosi, double usia)
    {
        if (new[] { termal, lembap, korosi, usia }.Any(w => w < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(termal), termal,
                "Bobot kriteria tidak boleh negatif (PRD §11.1 mensyaratkan jumlah bobot = 1).");
        }

        if (Math.Abs(termal + lembap + korosi + usia - 1) > Toleransi)
        {
            throw new ArgumentOutOfRangeException(
                nameof(termal), termal,
                "Jumlah bobot termal + lembap + korosi + usia harus sama dengan 1 (PRD §11.1).");
        }

        Termal = termal;
        Lembap = lembap;
        Korosi = korosi;
        Usia = usia;
    }

    /// <summary>Bobot sub-skor termal.</summary>
    public double Termal { get; init; }

    /// <summary>Bobot sub-skor lembap.</summary>
    public double Lembap { get; init; }

    /// <summary>Bobot sub-skor korosi.</summary>
    public double Korosi { get; init; }

    /// <summary>Bobot sub-skor usia pakai.</summary>
    public double Usia { get; init; }

    /// <summary>Jumlah bobot; selalu 1 setelah validasi konstruktor.</summary>
    public double Jumlah => Termal + Lembap + Korosi + Usia;

    /// <summary>Bobot equiponderal yang disarankan PRD §11.1.</summary>
    public static KriteriaBobot Default { get; } = new(0.25, 0.25, 0.25, 0.25);
}