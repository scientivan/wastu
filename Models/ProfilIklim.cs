using Wastu.Domain;
using Wastu.Domain.Climate;

namespace Wastu.Models;

public class ProfilIklim
{
    private double _curahHujanTahunan;
    private double _kelembapanRata;
    private double _suhuRata;
    private double _jarakPesisirKm;
    private string _tingkatKorosi = string.Empty;
    private string _sumber = string.Empty;
    private string? _diperbaruiPada;

    public double CurahHujanTahunan { get => _curahHujanTahunan; set => _curahHujanTahunan = value; }
    public double KelembapanRata { get => _kelembapanRata; set => _kelembapanRata = value; }
    public double SuhuRata { get => _suhuRata; set => _suhuRata = value; }
    public double JarakPesisirKm
    {
        get => _jarakPesisirKm;
        set
        {
            _jarakPesisirKm = value;
            _tingkatKorosi = value < 10 ? "Tinggi" : value < 35 ? "Sedang" : "Rendah";
        }
    }
    public string TingkatKorosi { get => _tingkatKorosi; set => _tingkatKorosi = value; }
    public string Sumber { get => _sumber; set => _sumber = value; }

    public ProvenanceIklim Provenance { get; set; } = ProvenanceIklim.EstimasiBawaan;
    public StatusPengambilanIklim StatusPengambilan { get; set; } = StatusPengambilanIklim.BelumTersedia;
    public KualitasCacheIklim KualitasCache { get; set; } = KualitasCacheIklim.CocokLokasi;
    public DateTimeOffset? DiambilPadaUtc { get; set; }
    public DateOnly? PeriodeMulai { get; set; }
    public DateOnly? PeriodeSelesai { get; set; }
    public string Provider { get; set; } = "";
    public string VersiAlgoritmePesisir { get; set; } = JarakPesisirCalculator.VersiAlgoritme;
    public int? RevisiLokasi { get; set; }
    public string? CatatanOverridePesisir { get; set; }
    public bool PesisirTersedia { get; set; } = true;

    // Display string untuk UI
    public string? DiperbaruiPada
    {
        get => _diperbaruiPada ?? (DiambilPadaUtc.HasValue ? Services.Format.WaktuLokal(DiambilPadaUtc.Value.LocalDateTime) : null);
        set => _diperbaruiPada = value;
    }

    public bool SudahDiperbarui =>
        (Provenance is ProvenanceIklim.ApiHistoris or ProvenanceIklim.CacheHistoris or ProvenanceIklim.ManualOverride && DiambilPadaUtc.HasValue) ||
        (Provenance is ProvenanceIklim.LegacyUnknown && !string.IsNullOrEmpty(_diperbaruiPada));

    internal Domain.ProfilIklim KeDomain()
    {
        var tingkat = JarakPesisirKm < 10 ? Domain.TingkatKorosi.Tinggi
            : JarakPesisirKm < 35 ? Domain.TingkatKorosi.Sedang : Domain.TingkatKorosi.Rendah;

        var meta = new Domain.MetadataIklim(
            PeriodeAwal: PeriodeMulai,
            PeriodeAkhir: PeriodeSelesai,
            Penyedia: !string.IsNullOrWhiteSpace(Provider) ? Provider : (Sumber.Length > 0 ? Sumber : "Open-Meteo"),
            Model: null,
            DiambilPadaUtc: DiambilPadaUtc,
            ZonaWaktu: "Asia/Jakarta",
            MetodePesisir: VersiAlgoritmePesisir,
            Provenance: Provenance,
            StatusPengambilan: StatusPengambilan,
            RevisiLokasi: RevisiLokasi,
            KualitasCache: KualitasCache,
            VersiAlgoritmePesisir: VersiAlgoritmePesisir);

        return new Domain.ProfilIklim(
            curahHujanTahunanMm: CurahHujanTahunan,
            kelembapanRataPct: KelembapanRata,
            suhuRataC: SuhuRata,
            jarakPesisirKm: JarakPesisirKm,
            tingkatKorosi: tingkat,
            metadata: meta,
            revisiLokasi: RevisiLokasi ?? 1,
            provenance: Provenance);
    }
}
