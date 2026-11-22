namespace Wastu.Domain;

/// <summary>
/// Bobot keempat kriteria kesesuaian (PRD §11.1).
/// </summary>
/// <remarks>
/// Syarat PRD §11.1 adalah jumlah bobot tepat 1. Nilai menyimpang dari 1 ditolak
/// dalam toleransi pembulatan floating point (1e-9).
/// </remarks>
public sealed record KriteriaBobot
{
    private const double Toleransi = 1e-9;

    private readonly double _termal;
    private readonly double _lembap;
    private readonly double _korosi;
    private readonly double _usia;

    public KriteriaBobot(double termal, double lembap, double korosi, double usia)
    {
        Termal = termal;
        Lembap = lembap;
        Korosi = korosi;
        Usia = usia;
        PastikanValid();
    }

    /// <summary>Bobot sub-skor termal.</summary>
    public double Termal
    {
        get => _termal;
        init
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Bobot kriteria harus bernilai finite dan tidak boleh negatif.");
            }
            _termal = value;
        }
    }

    /// <summary>Bobot sub-skor lembap.</summary>
    public double Lembap
    {
        get => _lembap;
        init
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Bobot kriteria harus bernilai finite dan tidak boleh negatif.");
            }
            _lembap = value;
        }
    }

    /// <summary>Bobot sub-skor korosi.</summary>
    public double Korosi
    {
        get => _korosi;
        init
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Bobot kriteria harus bernilai finite dan tidak boleh negatif.");
            }
            _korosi = value;
        }
    }

    /// <summary>Bobot sub-skor usia pakai.</summary>
    public double Usia
    {
        get => _usia;
        init
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Bobot kriteria harus bernilai finite dan tidak boleh negatif.");
            }
            _usia = value;
        }
    }

    /// <summary>Jumlah bobot; selalu 1 setelah validasi konstruktor.</summary>
    public double Jumlah => Termal + Lembap + Korosi + Usia;

    /// <summary>
    /// Memastikan seluruh nilai bobot valid (finite, non-negatif, dan total = 1 dalam toleransi 1e-9).
    /// </summary>
    public void PastikanValid()
    {
        if (!double.IsFinite(Termal) || Termal < 0 ||
            !double.IsFinite(Lembap) || Lembap < 0 ||
            !double.IsFinite(Korosi) || Korosi < 0 ||
            !double.IsFinite(Usia) || Usia < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Termal), "Bobot kriteria harus bernilai finite dan tidak boleh negatif.");
        }

        if (Math.Abs(Jumlah - 1.0) > Toleransi)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Termal), Jumlah,
                $"Jumlah bobot termal + lembap + korosi + usia harus sama dengan 1 (PRD §11.1), aktual: {Jumlah}.");
        }
    }

    /// <summary>
    /// Membuat KriteriaBobot dari persentase (0-100%) dengan syarat total tepat 100%.
    /// </summary>
    public static KriteriaBobot DariPersentase(double termalPct, double lembapPct, double korosiPct, double usiaPct)
    {
        if (!double.IsFinite(termalPct) || termalPct < 0 ||
            !double.IsFinite(lembapPct) || lembapPct < 0 ||
            !double.IsFinite(korosiPct) || korosiPct < 0 ||
            !double.IsFinite(usiaPct) || usiaPct < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(termalPct), "Persentase bobot harus bernilai finite dan tidak boleh negatif.");
        }

        double total = termalPct + lembapPct + korosiPct + usiaPct;
        if (Math.Abs(total - 100.0) > Toleransi)
        {
            throw new ArgumentOutOfRangeException(
                nameof(termalPct), total,
                $"Total persentase bobot harus tepat 100%, aktual: {total}%.");
        }

        return new KriteriaBobot(termalPct / 100.0, lembapPct / 100.0, korosiPct / 100.0, usiaPct / 100.0);
    }

    /// <summary>Bobot equiponderal yang disarankan PRD §11.1.</summary>
    public static KriteriaBobot Default { get; } = new(0.25, 0.25, 0.25, 0.25);

    /// <summary>Alias bahasa Indonesia untuk bobot equiponderal/seimbang (PRD §11.1).</summary>
    public static KriteriaBobot Seimbang => Default;
}