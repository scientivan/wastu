namespace Wastu.Data.Entities;

public sealed class SimulasiEmisiEntity
{
    public Guid EvaluasiId { get; set; }

    public int HorizonTahun { get; set; }

    public double UsiaPakaiEfektifTahun { get; set; }

    public int JumlahPenggantian { get; set; }

    public double EmisiKumulatifKg { get; set; }

    public EvaluasiEntity? Evaluasi { get; set; }
}
