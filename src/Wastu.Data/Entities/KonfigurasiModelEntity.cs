namespace Wastu.Data.Entities;

public sealed class KonfigurasiModelEntity
{
    public string VersiModel { get; set; } = "v2.0";

    public double BobotLembapHujanAlpha { get; set; } = 0.5;

    public double AcuanCurahHujanMm { get; set; } = 3000.0;

    public double AcuanJarakPesisirKm { get; set; } = 50.0;

    public double PitaTermalBatasBawah { get; set; } = 0.10;

    public double PitaTermalBatasAtasSejuk { get; set; } = 0.60;

    public double PitaTermalBatasAtasPanas { get; set; } = 0.30;

    public double SuhuSejukC { get; set; } = 26.0;

    public double SuhuPanasC { get; set; } = 32.0;

    public double FaktorPenuaanA { get; set; } = 0.5;

    public double AmbangTidakDisarankan { get; set; } = 55.0;

    public double AmbangCocok { get; set; } = 75.0;

    public bool Aktif { get; set; } = false;

    public DateTimeOffset DibuatPadaUtc { get; set; } = DateTimeOffset.UtcNow;
}
