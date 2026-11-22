namespace Wastu.Domain;

/// <summary>
/// Status verifikasi properti bahan (PRD-R09).
/// </summary>
public enum StatusVerifikasiProperti
{
    Perkiraan,
    Terverifikasi,
    Manual
}

/// <summary>
/// Sumber rujukan per properti bahan bangunan untuk transparansi audit (PRD-R09).
/// </summary>
public sealed record SumberPropertiBahan
{
    public SumberPropertiBahan(
        Guid bahanId,
        string properti,
        string referensi,
        string? versi = null,
        string? tautan = null,
        StatusVerifikasiProperti status = StatusVerifikasiProperti.Perkiraan,
        string? catatanKonversi = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(properti);
        ArgumentException.ThrowIfNullOrWhiteSpace(referensi);

        if (!Enum.IsDefined(typeof(StatusVerifikasiProperti), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Status verifikasi properti tidak dikenal.");
        }

        BahanId = bahanId;
        Properti = properti;
        Referensi = referensi;
        Versi = versi;
        Tautan = tautan;
        Status = status;
        CatatanKonversi = catatanKonversi;
    }

    public Guid BahanId { get; init; }

    public string Properti { get; init; }

    public string Referensi { get; init; }

    public string? Versi { get; init; }

    public string? Tautan { get; init; }

    public StatusVerifikasiProperti Status { get; init; }

    public string? CatatanKonversi { get; init; }
}
