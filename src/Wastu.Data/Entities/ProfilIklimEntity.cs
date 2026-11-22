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

    public int RevisiLokasi { get; set; } = 1;

    public string Provenance { get; set; } = "LegacyUnknown";

    public DateOnly? PeriodeAwal { get; set; }

    public DateOnly? PeriodeAkhir { get; set; }

    public string? Penyedia { get; set; }

    public string? ModelIklim { get; set; }

    public string? ZonaWaktu { get; set; }

    public string? MetodePesisir { get; set; }

    public string StatusPengambilan { get; set; } = "Tersedia";

    public ProyekEntity? Bangunan { get; set; }
}
