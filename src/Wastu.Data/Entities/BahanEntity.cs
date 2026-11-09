using Wastu.Domain;

namespace Wastu.Data.Entities;

public sealed class BahanEntity
{
    public Guid Id { get; set; }

    public string Nama { get; set; } = string.Empty;

    public JenisKomponen Jenis { get; set; }

    public double KonduktivitasTermal { get; set; }

    public int KetahananLembap { get; set; }

    public int KetahananKorosi { get; set; }

    public double UsiaPakaiNominalTahun { get; set; }

    public double FaktorEmisi { get; set; }

    public string SumberData { get; set; } = string.Empty;
}
