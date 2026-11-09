using Wastu.Domain;

namespace Wastu.Data.Entities;

public sealed class KomponenEntity
{
    public Guid Id { get; set; }

    public Guid BangunanId { get; set; }

    public JenisKomponen Jenis { get; set; }

    public double LuasM2 { get; set; }

    public ProyekEntity? Bangunan { get; set; }
}
