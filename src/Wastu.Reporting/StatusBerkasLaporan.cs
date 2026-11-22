namespace Wastu.Reporting;

/// <summary>
/// Status ketersediaan dan integritas berkas PDF laporan pada media penyimpanan lokal (PRD §F7, T34).
/// </summary>
public enum StatusBerkasLaporan
{
    /// <summary>Berkas ada dan checksum SHA-256 cocok.</summary>
    Tersedia,

    /// <summary>Berkas tidak ditemukan pada lokasi path yang tercatat.</summary>
    Hilang,

    /// <summary>Berkas ada namun checksum SHA-256 berbeda dari catatan metadata.</summary>
    BerubahChecksum
}
