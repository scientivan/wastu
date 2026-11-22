namespace Wastu.Domain.Weather;

/// <summary>
/// Status siklus hidup pekerjaan lapangan yang dijadwalkan (PRD §12, PRD-R10).
/// </summary>
public enum StatusPekerjaan
{
    Draft,
    Terkunci,
    PerluDitinjau,
    Selesai
}

/// <summary>
/// Konfirmasi persetujuan pengguna saat mengunci tanggal dengan risiko cuaca atau data lama (PRD §12.2, PRD-R08, PRD-R10).
/// </summary>
public sealed record KonfirmasiPenguncian(
    bool SetujuiRisiko = false,
    bool SetujuiDataLama = false,
    string? Catatan = null);

/// <summary>
/// Catatan keputusan penetapan tanggal dan status kelayakan cuaca saat penguncian (PRD §12.2).
/// </summary>
public sealed record KeputusanJadwal(
    DateOnly Tanggal,
    StatusKelayakanCuaca Status,
    IReadOnlyList<string> Catatan,
    bool Terkunci = false,
    DateTimeOffset? DikunciPadaUtc = null,
    KonfirmasiPenguncian? Konfirmasi = null);

/// <summary>
/// Pekerjaan lapangan sensitif cuaca yang direncanakan pada suatu proyek (PRD §F6, §12).
/// </summary>
public sealed record Pekerjaan
{
    public Pekerjaan(
        Guid id,
        Guid proyekId,
        string nama,
        JenisPekerjaan jenis,
        DateOnly tanggalMulai,
        DateOnly tanggalSelesai,
        StatusPekerjaan status = StatusPekerjaan.Draft,
        DateOnly? tanggalDikunci = null,
        Guid? komponenId = null,
        Guid? bahanId = null,
        AmbangPekerjaan? overrideAmbang = null,
        KonfirmasiPenguncian? konfirmasi = null,
        DateTimeOffset? terakhirDiperbaruiUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nama);

        if (!Enum.IsDefined(typeof(JenisPekerjaan), jenis))
        {
            throw new ArgumentOutOfRangeException(nameof(jenis), jenis, "Jenis pekerjaan tidak dikenal.");
        }

        if (!Enum.IsDefined(typeof(StatusPekerjaan), status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Status pekerjaan tidak dikenal.");
        }

        if (tanggalMulai > tanggalSelesai)
        {
            throw new ArgumentException(
                $"Tanggal mulai ({tanggalMulai}) tidak boleh lebih besar dari tanggal selesai ({tanggalSelesai}).",
                nameof(tanggalMulai));
        }

        if (tanggalDikunci.HasValue && (tanggalDikunci.Value < tanggalMulai || tanggalDikunci.Value > tanggalSelesai))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tanggalDikunci), tanggalDikunci,
                $"Tanggal dikunci ({tanggalDikunci.Value}) harus berada dalam rentang tanggal kerja ({tanggalMulai}..{tanggalSelesai}).");
        }

        overrideAmbang?.PastikanValid();

        Id = id;
        ProyekId = proyekId;
        Nama = nama;
        Jenis = jenis;
        TanggalMulai = tanggalMulai;
        TanggalSelesai = tanggalSelesai;
        Status = status;
        TanggalDikunci = tanggalDikunci;
        KomponenId = komponenId;
        BahanId = bahanId;
        OverrideAmbang = overrideAmbang;
        Konfirmasi = konfirmasi;
        TerakhirDiperbaruiUtc = terakhirDiperbaruiUtc ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; init; }
    public Guid ProyekId { get; init; }
    public string Nama { get; init; }
    public JenisPekerjaan Jenis { get; init; }
    public DateOnly TanggalMulai { get; init; }
    public DateOnly TanggalSelesai { get; init; }
    public StatusPekerjaan Status { get; init; }
    public DateOnly? TanggalDikunci { get; init; }
    public Guid? KomponenId { get; init; }
    public Guid? BahanId { get; init; }
    public AmbangPekerjaan? OverrideAmbang { get; init; }
    public KonfirmasiPenguncian? Konfirmasi { get; init; }
    public DateTimeOffset TerakhirDiperbaruiUtc { get; init; }
}
