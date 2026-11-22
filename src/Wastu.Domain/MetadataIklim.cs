namespace Wastu.Domain;

/// <summary>
/// Metadata lengkap penyedia, waktu pengambilan, zona waktu, dan provenance data iklim (PRD-R09).
/// </summary>
public sealed record MetadataIklim(
    DateOnly? PeriodeAwal,
    DateOnly? PeriodeAkhir,
    string Penyedia,
    string? Model,
    DateTimeOffset? DiambilPadaUtc,
    string? ZonaWaktu,
    string? MetodePesisir,
    ProvenanceIklim Provenance = ProvenanceIklim.LegacyUnknown,
    StatusPengambilanIklim StatusPengambilan = StatusPengambilanIklim.BelumTersedia)
{
    public MetadataIklim() : this(
        PeriodeAwal: null,
        PeriodeAkhir: null,
        Penyedia: "Open-Meteo",
        Model: null,
        DiambilPadaUtc: null,
        ZonaWaktu: "Asia/Jakarta",
        MetodePesisir: "Haversine Proximity",
        Provenance: ProvenanceIklim.LegacyUnknown,
        StatusPengambilan: StatusPengambilanIklim.BelumTersedia)
    {
    }
}
