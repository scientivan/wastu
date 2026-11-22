namespace Wastu.Data.Entities;

public sealed class SimulasiEmisiEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EvaluasiId { get; set; }

    public int HorizonTahun { get; set; }

    public double UsiaPakaiEfektifTahun { get; set; }

    public int JumlahPenggantian { get; set; }

    public double EmisiKumulatifKg { get; set; }

    public string VersiModel { get; set; } = "v2.0";

    public double FaktorPenuaanA { get; set; } = 0.5;

    public DateTimeOffset? DihitungPadaUtc { get; set; }

    public EvaluasiEntity? Evaluasi { get; set; }
}
