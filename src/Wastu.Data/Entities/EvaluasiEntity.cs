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

    public KomponenEntity? Komponen { get; set; }

    public BahanEntity? Bahan { get; set; }
}
