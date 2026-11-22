using Wastu.Domain.Weather;

namespace Wastu.Data.Entities;

public sealed class AmbangPekerjaanEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? PekerjaanId { get; set; }

    public JenisPekerjaan? JenisPekerjaan { get; set; }

    public double? ProbabilitasHujanMaksPct { get; set; }

    public double? CurahHujanMaksMm { get; set; }

    public double? SuhuMinC { get; set; }

    public double? SuhuMaksC { get; set; }

    public double? KelembapanMaksPct { get; set; }

    public TingkatPelanggaran TingkatSuhu { get; set; } = TingkatPelanggaran.Peringatan;

    public string SumberAmbang { get; set; } = "Default Asumsi";

    public PekerjaanEntity? Pekerjaan { get; set; }
}
