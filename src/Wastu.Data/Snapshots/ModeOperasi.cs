namespace Wastu.Data.Snapshots;

/// <summary>Keputusan startup berdasarkan ketersediaan database dan snapshot profil.</summary>
public enum ModeOperasi
{
    /// <summary>Database terjangkau; data hidup dimuat dan snapshot profil diperbarui (baca-tulis).</summary>
    DatabaseOnline,

    /// <summary>Database tidak tersedia dan snapshot valid; dibuka sebagai luring baca-saja.</summary>
    LuringBacaSaja,

    /// <summary>Database tidak tersedia dan tidak ada snapshot yang dapat digunakan; perlu pemulihan.</summary>
    ButuhPemulihan,

    /// <summary>Kompatibilitas JSON lokal (baca-tulis, disimpan di komputer ini, bukan PostgreSQL).</summary>
    ModeJsonLokal
}
