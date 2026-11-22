namespace Wastu.Data.Entities;

public sealed class BatchEvaluasiEntity
{
    public Guid Id { get; set; }

    public Guid ProyekId { get; set; }

    public Guid KomponenId { get; set; }

    public int RevisiLokasi { get; set; } = 1;

    public string HashInput { get; set; } = string.Empty;

    public string VersiModel { get; set; } = "v2.0";

    public double BobotTermal { get; set; }

    public double BobotLembap { get; set; }

    public double BobotKorosi { get; set; }

    public double BobotUsia { get; set; }

    public double LuasM2 { get; set; }

    public DateTimeOffset DihitungPadaUtc { get; set; }

    public ProyekEntity? Proyek { get; set; }

    public KomponenEntity? Komponen { get; set; }
}
