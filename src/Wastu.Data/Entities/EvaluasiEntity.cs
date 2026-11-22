namespace Wastu.Data.Entities;

public sealed class EvaluasiEntity
{
    public Guid Id { get; set; }

    public Guid KomponenId { get; set; }

    public Guid BahanId { get; set; }

    public double SkorKesesuaian { get; set; }

    public double EmisiAwalKg { get; set; }

    public string Alasan { get; set; } = string.Empty;

    public double SkorTermal { get; set; }

    public double SkorLembap { get; set; }

    public double SkorKorosi { get; set; }

    public double SkorUsia { get; set; }

    public DateTimeOffset DihitungPada { get; set; }

    public Guid? BatchId { get; set; }

    public string VersiModel { get; set; } = "v2.0";

    public string? HashInput { get; set; }

    public DateTimeOffset? DihitungPadaUtc { get; set; }

    public string? InputSnapshotJson { get; set; }

    public KomponenEntity? Komponen { get; set; }

    public BahanEntity? Bahan { get; set; }

    public BatchEvaluasiEntity? Batch { get; set; }
}
