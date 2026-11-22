namespace Wastu.Data.Snapshots;

/// <summary>
/// Hasil deterministik startup. <see cref="Pesan"/> selalu teks tetap berbahasa Indonesia yang aman
/// untuk UI: tanpa kredensial, alamat server, string koneksi, atau jalur berkas internal.
/// </summary>
public sealed record HasilStartup<T> where T : notnull
{
    public required ModeOperasi Mode { get; init; }

    /// <summary>Payload data; null hanya pada <see cref="ModeOperasi.ButuhPemulihan"/>.</summary>
    public T? Data { get; init; }

    /// <summary>Status snapshot yang mendasari keputusan, bila relevan.</summary>
    public SnapshotStatus? StatusSnapshot { get; init; }

    /// <summary>Waktu pengambilan data asli (UTC), bukan waktu pembacaan berkas.</summary>
    public DateTimeOffset? SnapshotDiambilPadaUtc { get; init; }

    public bool DipulihkanDariCadangan { get; init; }

    /// <summary>Ringkasan status untuk UI; tidak pernah memuat detail teknis mentah.</summary>
    public string Pesan { get; init; } = "";

    /// <summary>True bila impor one-time dari state.json legacy baru saja dilakukan.</summary>
    public bool ImporLegacyDilakukan { get; init; }
}
