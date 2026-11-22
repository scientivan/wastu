namespace Wastu.Data.Entities;

public sealed class SumberPropertiBahanEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BahanId { get; set; }

    public string Properti { get; set; } = string.Empty;

    public string Referensi { get; set; } = string.Empty;

    public string? Versi { get; set; }

    public string? Tautan { get; set; }

    public string Status { get; set; } = "perkiraan";

    public string? CatatanKonversi { get; set; }

    public BahanEntity? Bahan { get; set; }
}
