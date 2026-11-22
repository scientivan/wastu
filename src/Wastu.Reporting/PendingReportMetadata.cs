namespace Wastu.Reporting;

/// <summary>
/// Metadata laporan yang disimpan secara lokal jika penyimpanan ke database gagal (T35).
/// Digunakan oleh proses rekonsiliasi idempoten.
/// </summary>
public sealed record PendingReportMetadata
{
    public Guid Id { get; init; }
    public Guid ProyekId { get; init; }
    public string PathBerkas { get; init; } = string.Empty;
    public DateTimeOffset DibuatPada { get; init; }
    public string ChecksumSha256 { get; init; } = string.Empty;
    public string? SnapshotHash { get; init; }
    public string? SnapshotJson { get; init; }
    public long UkuranByte { get; init; }
    public string Status { get; init; } = "MenungguPencatatan";
    public DateTimeOffset DicatatLokalPadaUtc { get; init; } = DateTimeOffset.UtcNow;
    public string? ErrorPesanTerakhir { get; init; }
}
