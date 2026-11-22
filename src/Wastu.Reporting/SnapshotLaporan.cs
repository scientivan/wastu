using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wastu.Domain;
using Wastu.Domain.Weather;

namespace Wastu.Reporting;

/// <summary>
/// Snapshot data DTO tidak dapat diubah (immutable) yang mencatat kondisi evaluasi
/// dan simulasi pada saat ekspor laporan (T38, PRD §F7).
/// Perubahan bobot, katalog, atau luas di kemudian hari tidak akan memengaruhi snapshot ini.
/// </summary>
public sealed record SnapshotLaporan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProyekId { get; init; }
    public string NamaProyek { get; init; } = string.Empty;
    public string Kota { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string KoordinatDisclaimer { get; init; } =
        "Koordinat berada dalam batas perkiraan kepulauan Indonesia (95°-141° BT, 11° LS - 6° LU).";

    public IReadOnlyList<SnapshotKomponen> Komponen { get; init; } = [];
    public SnapshotIklim Iklim { get; init; } = new();
    public IReadOnlyList<SnapshotBahan> BahanTerpilih { get; init; } = [];
    public IReadOnlyList<SnapshotSimulasi> SimulasiHorizon { get; init; } = [];
    public bool SertakanJadwal { get; init; }
    public IReadOnlyList<SnapshotPekerjaan> JadwalTerkunci { get; init; } = [];

    public DateTimeOffset DibuatPadaUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DibuatPadaWib { get; init; } =
        DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));

    public string VersiModel { get; init; } = "v2.0";
    public string AtribusiSumber { get; init; } =
        "Data iklim historis bersumber dari Open-Meteo Historical Weather API. " +
        "Faktor emisi tersemat dan properti bahan bersumber dari basis data publik internasional (ICE DB, dsb.) " +
        "serta estimasi literatur teknis.";

    public string? SnapshotHash { get; init; }

    /// <summary>
    /// Menghitung SHA-256 hash representasi kanonikal snapshot untuk audit dan integritas (PRD §4.3, T38).
    /// </summary>
    public string HitungHash()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        var copy = this with { SnapshotHash = null };
        var json = JsonSerializer.Serialize(copy, options);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed record SnapshotKomponen(
    JenisKomponen Jenis,
    double LuasM2);

public sealed record SnapshotIklim
{
    public double CurahHujanTahunanMm { get; init; }
    public double KelembapanRataPct { get; init; }
    public double SuhuRataC { get; init; }
    public double JarakPesisirKm { get; init; }
    public string TingkatKorosi { get; init; } = "Rendah";
    public string PesisirDisclaimer { get; init; } =
        "Jarak ke garis pantai adalah proksi risiko korosi atmosferik akibat udara bergaram, bukan pengukuran salinitas langsung.";
    public string Provider { get; init; } = "Open-Meteo Historical Climate API";
    public string Periode { get; init; } = "10 tahun historis (agregasi)";
    public string Provenance { get; init; } = "ApiHistoris";
    public DateTimeOffset? DiambilPada { get; init; }
    public bool IsOffline { get; init; }
    public string? CatatanOffline { get; init; }
}

public sealed record SnapshotBahan
{
    public Guid BahanId { get; init; }
    public Guid KomponenId { get; init; }
    public JenisKomponen Jenis { get; init; }
    public int Peringkat { get; init; }
    public string Nama { get; init; } = string.Empty;
    public double SkorKesesuaian { get; init; }
    public string LabelKesesuaian { get; init; } = "Cocok"; // Cocok, Perlu Pertimbangan, Tidak Disarankan
    public double EmisiAwalKg { get; init; }
    public double SubSkorTermal { get; init; }
    public double SubSkorLembap { get; init; }
    public double SubSkorKorosi { get; init; }
    public double SubSkorUsia { get; init; }
    public string Alasan { get; init; } = string.Empty;
    public IReadOnlyList<SnapshotSumberProperti> SumberProperti { get; init; } = [];
}

public sealed record SnapshotSumberProperti(
    string Properti,
    string Nilai,
    string Sumber,
    string? Tahun = null);

public sealed record SnapshotSimulasi
{
    public Guid EvaluasiId { get; init; }
    public string BahanNama { get; init; } = string.Empty;
    public JenisKomponen Jenis { get; init; }
    public int HorizonTahun { get; init; }
    public double UsiaPakaiNominal { get; init; }
    public double UsiaPakaiEfektif { get; init; }
    public int JumlahPenggantian { get; init; }
    public double EmisiAwalKg { get; init; }
    public double EmisiKumulatifKg { get; init; }
    public double FaktorPenuaanA { get; init; } = 0.5;
    public string VersiModel { get; init; } = "v2.0";
    public string EstimasiDisclaimer { get; init; } =
        "Hasil simulasi emisi adalah proyeksi estimasi model matematis, bukan hasil pengukuran langsung lapangan.";
}

public sealed record SnapshotPekerjaan
{
    public Guid PekerjaanId { get; init; }
    public string Nama { get; init; } = string.Empty;
    public JenisPekerjaan Jenis { get; init; }
    public DateOnly TanggalMulai { get; init; }
    public DateOnly TanggalSelesai { get; init; }
    public DateOnly? TanggalDikunci { get; init; }
    public string StatusCuaca { get; init; } = "Layak";
    public IReadOnlyList<string> AlasanCuaca { get; init; } = [];
    public bool KonfirmasiRisiko { get; init; }
    public bool KonfirmasiDataLama { get; init; }
    public string? Catatan { get; init; }
}
