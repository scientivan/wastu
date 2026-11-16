namespace Wastu.Domain.Weather;

/// <summary>Jenis pekerjaan konstruksi sensitif cuaca; cerminan CHECK <c>pekerjaan_jenis_enum</c>.</summary>
public enum JenisPekerjaan
{
    Pengecoran,
    Pengecatan
}

/// <summary>Sejauh mana sebuah ambang ditandai sebagai pelanggaran untuk sebuah hari kerja.</summary>
public enum TingkatPelanggaran
{
    /// <summary>Melewati ambang berarti hari tidak layak dikerjakan.</summary>
    Keras,

    /// <summary>
    /// Melewati ambang hanya menghasilkan peringatan. PRD §12 menetapkan suhu tinggi
    /// saat pengecoran sebagai peringatan, bukan larangan, karena Constructor bisa
    /// melakukan mitigasi.
    /// </summary>
    Peringatan
}