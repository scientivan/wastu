using Wastu.Domain;

namespace Wastu.Data.Entities;

public sealed class ProfilIklimEntity
{
    public Guid BangunanId { get; set; }

    public double CurahHujanTahunanMm { get; set; }

    public double KelembapanRataPct { get; set; }

    public double SuhuRataC { get; set; }

    public double JarakPesisirKm { get; set; }

    public TingkatKorosi TingkatKorosi { get; set; }

    public DateTimeOffset DiambilPada { get; set; }

    public ProyekEntity? Bangunan { get; set; }
}
