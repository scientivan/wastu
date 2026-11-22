namespace Wastu.Data.Entities;

public sealed class KeputusanJadwalEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PekerjaanId { get; set; }

    public DateOnly Tanggal { get; set; }

    public string Status { get; set; } = string.Empty;

    public string CatatanJson { get; set; } = "[]";

    public bool Terkunci { get; set; }

    public DateTimeOffset? DikunciPadaUtc { get; set; }

    public bool PersetujuanRisiko { get; set; }

    public bool KonfirmasiDataLama { get; set; }

    public string? CatatanKonfirmasi { get; set; }

    public string? SnapshotCuacaJson { get; set; }

    public string? SnapshotAmbangJson { get; set; }

    public double? UmurDataJam { get; set; }

    public DateTimeOffset DibuatPadaUtc { get; set; } = DateTimeOffset.UtcNow;

    public PekerjaanEntity? Pekerjaan { get; set; }
}
