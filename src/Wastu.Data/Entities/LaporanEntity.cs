namespace Wastu.Data.Entities;

public sealed class LaporanEntity
{
    public Guid Id { get; set; }

    public Guid BangunanId { get; set; }

    public string PathBerkas { get; set; } = string.Empty;

    public DateTimeOffset DibuatPadaUtc { get; set; }

    public string? HashSnapshot { get; set; }

    public string StatusBerkas { get; set; } = "Tersimpan";

    public string? ChecksumSha256 { get; set; }

    public long? UkuranByte { get; set; }

    public string? OpsiJson { get; set; }

    public ProyekEntity? Bangunan { get; set; }
}
