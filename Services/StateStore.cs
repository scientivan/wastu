using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public static readonly string FilePath = Path.Combine(Folder, "state.json");

    public static string ProfileFolder => Folder;

    public static StateDto? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            using var stream = File.OpenRead(FilePath);
            return JsonSerializer.Deserialize(stream, StateJson.Default.StateDto);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(StateDto state)
    {
        Directory.CreateDirectory(Folder);
        var temp = FilePath + ".tmp";
        using (var stream = File.Create(temp))
            JsonSerializer.Serialize(stream, state, StateJson.Default.StateDto);
        File.Move(temp, FilePath, overwrite: true);
    }
}
