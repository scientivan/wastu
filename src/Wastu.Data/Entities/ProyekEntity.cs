namespace Wastu.Data.Entities;

public sealed class ProyekEntity
{
    public Guid Id { get; set; }

    public string Nama { get; set; } = string.Empty;

    public string Kota { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTimeOffset DibuatPada { get; set; }

    public int RevisiLokasi { get; set; } = 1;

    public string? ZonaWaktu { get; set; }
}
