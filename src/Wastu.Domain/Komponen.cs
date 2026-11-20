namespace Wastu.Domain;

/// <summary>
/// Bagian bangunan yang sedang dikerjakan pengguna beserta luasnya (PRD §F1).
/// Luas_area dibutuhkan karena emisi awal dihitung per satuan luas (PRD §11.2).
/// </summary>
public sealed record Komponen
{
    public Komponen(Guid id, Guid proyekId, JenisKomponen jenis, double luasM2)
    {
        if (!double.IsFinite(luasM2) || luasM2 <= 0)
            throw new ArgumentOutOfRangeException(nameof(luasM2));

        Id = id;
        ProyekId = proyekId;
        Jenis = jenis;
        LuasM2 = luasM2;
    }

    public Guid Id { get; init; }

    /// <summary>Proyek induk; dipakai untuk mencari profil iklim saat penilaian.</summary>
    public Guid ProyekId { get; init; }

    public JenisKomponen Jenis { get; init; }

    /// <summary>Luas area, m².</summary>
    public double LuasM2 { get; init; }
}
