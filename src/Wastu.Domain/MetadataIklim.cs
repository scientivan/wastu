namespace Wastu.Domain;

/// <summary>
/// Metadata lengkap penyedia, waktu pengambilan, zona waktu, dan provenance data iklim (PRD-R09).
/// </summary>
[method: System.Text.Json.Serialization.JsonConstructor]
public sealed record MetadataIklim(
    DateOnly? PeriodeAwal,
    DateOnly? PeriodeAkhir,
    string Penyedia,
    string? Model,
    DateTimeOffset? DiambilPadaUtc,
    string? ZonaWaktu,
    string? MetodePesisir,
    ProvenanceIklim Provenance = ProvenanceIklim.LegacyUnknown,
    StatusPengambilanIklim StatusPengambilan = StatusPengambilanIklim.BelumTersedia,
    int? RevisiLokasi = null,
    KualitasCacheIklim KualitasCache = KualitasCacheIklim.CocokLokasi,
    string? VersiAlgoritmePesisir = null)
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
        StatusPengambilan: StatusPengambilanIklim.BelumTersedia,
        RevisiLokasi: null,
        KualitasCache: KualitasCacheIklim.CocokLokasi,
        VersiAlgoritmePesisir: null)
    {
    }

    public DateOnly? PeriodeMulai => PeriodeAwal;
    public DateOnly? PeriodeSelesai => PeriodeAkhir;
    public string Provider => Penyedia;
}
