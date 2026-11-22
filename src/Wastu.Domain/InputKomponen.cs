namespace Wastu.Domain;

/// <summary>
/// Masukan komponen bangunan untuk pembuatan atau pembaruan proyek (PRD §F1).
/// Mendukung proyek berdinding saja, beratap saja, atau keduanya.
/// </summary>
public sealed record InputKomponen
{
    public InputKomponen(JenisKomponen jenis, double luasM2)
    {
        if (!Enum.IsDefined(typeof(JenisKomponen), jenis))
        {
            throw new ArgumentOutOfRangeException(nameof(jenis), jenis, "Jenis komponen tidak dikenal.");
        }

        if (!double.IsFinite(luasM2) || luasM2 <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(luasM2), luasM2, "Luas komponen harus bernilai finite dan lebih besar dari 0 m².");
        }

        Jenis = jenis;
        LuasM2 = luasM2;
    }

    public JenisKomponen Jenis { get; init; }

    public double LuasM2 { get; init; }
}
