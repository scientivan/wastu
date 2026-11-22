namespace Wastu.Data.Entities;

public sealed class PrakiraanCacheEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? BangunanId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int RevisiLokasi { get; set; } = 1;

    public DateOnly TanggalCuaca { get; set; }

    public string ZonaWaktu { get; set; } = "Asia/Jakarta";

    public DateTimeOffset DiambilPadaUtc { get; set; }

    public string Penyedia { get; set; } = "Open-Meteo";

    public bool DataParsial { get; set; }

    public bool DataLengkap { get; set; } = true;

    public string? CatatanKualitas { get; set; }

    public double ProbabilitasHujanPct { get; set; }

    public double CurahHujanMm { get; set; }

    public double SuhuMinC { get; set; }

    public double SuhuMaksC { get; set; }

    public double KelembapanMaksPct { get; set; }

    public ProyekEntity? Bangunan { get; set; }
}
