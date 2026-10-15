namespace Wastu.Domain;

/// <summary>
/// Bagian bangunan yang sedang dikerjakan pengguna beserta luasnya (PRD §F1).
/// Luas_area dibutuhkan karena emisi awal dihitung per satuan luas (PRD §11.2).
/// </summary>
public sealed record Komponen
{
    public Komponen(Guid id, JenisKomponen jenis, double luasM2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(luasM2);

        Id = id;
        Jenis = jenis;
        LuasM2 = luasM2;
    }

    public Guid Id { get; init; }

    public JenisKomponen Jenis { get; init; }

    /// <summary>Luas area, m².</summary>
    public double LuasM2 { get; init; }
}