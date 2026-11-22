using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using Wastu.Data.Snapshots;
using Wastu.Models;

namespace Wastu.Services;

// Bentuk berkas state.json. Model domain membungkus koleksinya, jadi disimpan lewat DTO ini.
public sealed class StateDto
{
    public List<ProyekDto> Proyek { get; set; } = [];
    public int? ProyekAktif { get; set; }
    public JenisKomponen Jenis { get; set; } = JenisKomponen.Dinding;
    public string[] Dipilih { get; set; } = ["m0", "m1"];
    public int Horizon { get; set; } = 30;
    public JenisPekerjaan JenisKerja { get; set; } = JenisPekerjaan.Pengecatan;
    public List<CuacaDto> Prakiraan { get; set; } = [];
    public string PrakiraanDiperbarui { get; set; } = "";
    public int? PrakiraanProyek { get; set; }
    public DateOnly? HariDipilih { get; set; }
}

public sealed class ProyekDto
{
    public int Id { get; set; }
    public Guid? DatabaseId { get; set; }
    public string Nama { get; set; } = "";
    public string Kota { get; set; } = "";
    public double Lintang { get; set; }
    public double Bujur { get; set; }
    public double Dinding { get; set; }
    public double Atap { get; set; }
    public double JarakPesisir { get; set; }
    public IklimDto? Iklim { get; set; }
    public List<PekerjaanDto> Pekerjaan { get; set; } = [];
}

public sealed class IklimDto
{
    public double CurahHujan { get; set; }
    public double Kelembapan { get; set; }
    public double Suhu { get; set; }
    public string Sumber { get; set; } = "";
    public string Diperbarui { get; set; } = "";
}

public sealed class PekerjaanDto
{
    public string Nama { get; set; } = "";
    public JenisPekerjaan Jenis { get; set; }
    public DateOnly Mulai { get; set; }
    public DateOnly Selesai { get; set; }
    public DateOnly? Dikunci { get; set; }
}

public sealed class CuacaDto
{
    public DateOnly Tanggal { get; set; }
    public double PeluangHujan { get; set; }
    public double CurahHujan { get; set; }
    public double SuhuMin { get; set; }
    public double SuhuMaks { get; set; }
    public double Kelembapan { get; set; }
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true, WriteIndented = false)]
[JsonSerializable(typeof(StateDto))]
internal sealed partial class StateJson : JsonSerializerContext;

public static class StateStore
{
    // WASTU_DATA_DIR memungkinkan mode portabel atau data uji terpisah.
    private static readonly string Folder = Environment.GetEnvironmentVariable("WASTU_DATA_DIR") is { Length: > 0 } custom
        ? custom
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wastu");

    private static readonly string SnapshotFolder = Path.Combine(Folder, "snapshots");

    public static string ProfileFolder => Folder;

    /// <summary>Berkas JSON legacy tanpa versi yang diimpor sekali ke snapshot berversi.</summary>
    public static string BerkasLegacyJson => Path.Combine(Folder, "state.json");

    /// <summary>Cache era sebelum snapshot berversi; hanya dibaca sebagai draf awal, tidak pernah ditimpa.</summary>
    private static string BerkasLegacyPostgres => Path.Combine(Folder, "state-postgres.json");

    /// <summary>
    /// Toko snapshot per profil koneksi. Profil hanya memakai host, port, database, dan pengguna —
    /// tidak pernah password atau string koneksi. Mengembalikan null bila koneksi tidak valid,
    /// tanpa pernah meneruskan detail konfigurasi.
    /// </summary>
    public static SnapshotStore<StateDto>? TokoPostgreSql(string koneksi)
    {
        try
        {
            var profil = SnapshotProfile.FromPostgreSql(new NpgsqlConnectionStringBuilder(koneksi));
            return new SnapshotStore<StateDto>(SnapshotFolder, profil);
        }
        catch (Exception e) when (e is ArgumentException or FormatException or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Toko mode kompatibilitas JSON lokal (bukan PostgreSQL).</summary>
    public static SnapshotStore<StateDto> TokoJsonLokal() => new(SnapshotFolder, SnapshotProfile.LocalJson("default"));

    /// <summary>Pembaca isi state.json legacy; null bila rusak, tanpa pernah melempar.</summary>
    public static StateDto? BacaLegacyPayload(string isi)
    {
        try
        {
            return JsonSerializer.Deserialize(isi, StateJson.Default.StateDto);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Draf snapshot yang ada untuk seed loader. Baca-saja: tidak mengubah satu pun berkas.</summary>
    public static async Task<StateDto?> BacaDrafAsync(SnapshotStore<StateDto> toko,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hasil = await toko.LoadAsync(cancellationToken);
            return hasil.Status is SnapshotStatus.ValidPrimary or SnapshotStatus.RecoveredBackup
                ? hasil.Snapshot!.Payload
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Cache state-postgres.json era lama sebagai draf awal; gagal baca diabaikan.</summary>
    public static StateDto? BacaLegacyPostgres()
    {
        try
        {
            return File.Exists(BerkasLegacyPostgres)
                ? JsonSerializer.Deserialize(File.ReadAllText(BerkasLegacyPostgres), StateJson.Default.StateDto)
                : null;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Penulisan sinkron atomik lewat <see cref="SnapshotStore{T}"/> (temporary lalu replace);
    /// tidak pernah memakai File.Move mentah dan tidak pernah menyentuh berkas profil lain.
    /// </summary>
    public static void Simpan(StateDto state, SnapshotStore<StateDto> toko)
        => toko.SaveAsync(state, DateTimeOffset.UtcNow).GetAwaiter().GetResult();
}
