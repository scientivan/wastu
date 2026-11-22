namespace Wastu.Domain;

/// <summary>
/// Validasi koordinat geografis wilayah Indonesia (PRD-R07).
/// </summary>
/// <remarks>
/// Menyatukan batas kasar lintang -11.0 sampai 6.0 dan bujur 95.0 sampai 141.0.
/// Pemeriksaan ini adalah batas kasar wilayah Indonesia, bukan verifikasi batas negara presisi.
/// </remarks>
public static class ValidasiKoordinat
{
    public const double LintangMin = -11.0;
    public const double LintangMaks = 6.0;
    public const double BujurMin = 95.0;
    public const double BujurMaks = 141.0;

    public const string CatatanBatasKasar = "Pemeriksaan ini adalah batas kasar wilayah Indonesia, bukan jaminan titik berada dalam batas administratif presisi.";

    public static bool ApakahValid(double latitude, double longitude) =>
        double.IsFinite(latitude) &&
        double.IsFinite(longitude) &&
        latitude is >= LintangMin and <= LintangMaks &&
        longitude is >= BujurMin and <= BujurMaks;

    public static void Validasi(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude))
        {
            throw new ArgumentException("Lintang harus berupa angka finite.", nameof(latitude));
        }

        if (latitude is < LintangMin or > LintangMaks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude), latitude,
                $"Lintang harus {LintangMin} sampai {LintangMaks}° agar berada di wilayah Indonesia. {CatatanBatasKasar}");
        }

        if (!double.IsFinite(longitude))
        {
            throw new ArgumentException("Bujur harus berupa angka finite.", nameof(longitude));
        }

        if (longitude is < BujurMin or > BujurMaks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude), longitude,
                $"Bujur harus {BujurMin} sampai {BujurMaks}° agar berada di wilayah Indonesia. {CatatanBatasKasar}");
        }
    }
}
