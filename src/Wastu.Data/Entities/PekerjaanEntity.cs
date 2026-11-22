using Wastu.Domain.Weather;

namespace Wastu.Data.Entities;

public sealed class PekerjaanEntity
{
    public Guid Id { get; set; }

    public Guid BangunanId { get; set; }

    public string Nama { get; set; } = string.Empty;

    public JenisPekerjaan Jenis { get; set; }

    public DateOnly TanggalMulai { get; set; }

    public DateOnly TanggalSelesai { get; set; }

    public StatusPekerjaan Status { get; set; } = StatusPekerjaan.Draft;

    public DateOnly? TanggalDikunci { get; set; }

    public Guid? KomponenId { get; set; }

    public Guid? BahanId { get; set; }

    public int? RevisiLokasiSaatKunci { get; set; }

    public string? OverrideAmbangJson { get; set; }

    public string? KonfirmasiJson { get; set; }

    public DateTimeOffset TerakhirDiperbaruiUtc { get; set; }

    public ProyekEntity? Bangunan { get; set; }

    public KomponenEntity? Komponen { get; set; }

    public BahanEntity? Bahan { get; set; }
}
