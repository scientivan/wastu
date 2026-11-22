namespace Wastu.Domain;

/// <summary>
/// Catatan satu batch eksekusi evaluasi peringkat bahan (PRD §4.3, PRD-R10).
/// </summary>
public sealed record BatchEvaluasi
{
    public BatchEvaluasi(
        Guid id,
        Guid proyekId,
        Guid komponenId,
        int revisiLokasi,
        string hashInput,
        string versiModel,
        KriteriaBobot bobot,
        double luasM2,
        DateTimeOffset dihitungPadaUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashInput);
        ArgumentException.ThrowIfNullOrWhiteSpace(versiModel);
        ArgumentNullException.ThrowIfNull(bobot);

        if (revisiLokasi < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revisiLokasi), revisiLokasi, "Revisi lokasi minimal 1.");
        }

        if (!double.IsFinite(luasM2) || luasM2 <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(luasM2), luasM2, "Luas komponen harus bernilai finite positif.");
        }

        bobot.PastikanValid();

        Id = id;
        ProyekId = proyekId;
        KomponenId = komponenId;
        RevisiLokasi = revisiLokasi;
        HashInput = hashInput;
        VersiModel = versiModel;
        Bobot = bobot;
        LuasM2 = luasM2;
        DihitungPadaUtc = dihitungPadaUtc;
    }

    public Guid Id { get; init; }
    public Guid ProyekId { get; init; }
    public Guid KomponenId { get; init; }
    public int RevisiLokasi { get; init; }
    public string HashInput { get; init; }
    public string VersiModel { get; init; }
    public KriteriaBobot Bobot { get; init; }
    public double LuasM2 { get; init; }
    public DateTimeOffset DihitungPadaUtc { get; init; }
}
