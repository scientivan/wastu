using System.Text.Json;
using Wastu.Data.Snapshots;

namespace Wastu.Data.Tests.Snapshots;

public sealed class LegacyJsonImporterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "wastu-legacy-" + Guid.NewGuid().ToString("N"));
    private readonly string _legacy;
    private static readonly DateTimeOffset Jam = new(2026, 11, 22, 7, 30, 0, TimeSpan.Zero);

    public LegacyJsonImporterTests()
    {
        Directory.CreateDirectory(_directory);
        _legacy = Path.Combine(_directory, "state.json");
    }

    [Fact]
    public async Task ImporLegacySatuKaliLaluIdempotenPadaStartupBerikutnya()
    {
        var store = Store();
        var warisan = new Payload("warisan", 7);
        File.WriteAllText(_legacy, JsonSerializer.Serialize(warisan));

        var hasil = await Coordinator(store).JalankanModeJsonAsync(Importer(store), Kosong);

        Assert.Equal(ModeOperasi.ModeJsonLokal, hasil.Mode);
        Assert.True(hasil.ImporLegacyDilakukan);
        Assert.Equal(warisan, hasil.Data);
        Assert.NotNull(hasil.SnapshotDiambilPadaUtc);
        Assert.Equal(TimeSpan.Zero, hasil.SnapshotDiambilPadaUtc!.Value.Offset);
        Assert.Equal(SnapshotStatus.ValidPrimary, (await Store().LoadAsync()).Status);
        Assert.False(File.Exists(_legacy));
        Assert.True(File.Exists(Path.Combine(_directory, "state.json.legacy.bak")));

        // Startup kedua dengan legacy baru yang berbeda: snapshot menang, tidak ada impor ulang.
        File.WriteAllText(_legacy, JsonSerializer.Serialize(new Payload("pengganti", 99)));
        var ulang = await Coordinator(store).JalankanModeJsonAsync(Importer(store), Kosong);

        Assert.Equal(ModeOperasi.ModeJsonLokal, ulang.Mode);
        Assert.False(ulang.ImporLegacyDilakukan);
        Assert.Equal(warisan, ulang.Data);
        Assert.Equal(JsonSerializer.Serialize(new Payload("pengganti", 99)), await File.ReadAllTextAsync(_legacy));
    }

    [Fact]
    public async Task LegacyRusakTidakDitulisUlangDanTidakMembuatSnapshot()
    {
        var store = Store();
        const string rusak = "{ warisan tanpa penutup";
        File.WriteAllText(_legacy, rusak);

        var hasil = await Coordinator(store).JalankanModeJsonAsync(Importer(store), Kosong);

        Assert.Equal(ModeOperasi.ButuhPemulihan, hasil.Mode);
        Assert.Equal(SnapshotStatus.Corrupt, hasil.StatusSnapshot);
        Assert.Null(hasil.Data);
        Assert.False(hasil.ImporLegacyDilakukan);
        Assert.Equal(rusak, await File.ReadAllTextAsync(_legacy));
        Assert.False(File.Exists(store.SnapshotPath));
        Assert.False(File.Exists(store.BackupPath));
    }

    [Fact]
    public async Task TanpaLegacyMemulaiDenganDataKosongTanpaMembuatBerkas()
    {
        var store = Store();
        var hasil = await Coordinator(store).JalankanModeJsonAsync(Importer(store), Kosong);

        Assert.Equal(ModeOperasi.ModeJsonLokal, hasil.Mode);
        Assert.Equal(new Payload("kosong", 0), hasil.Data);
        Assert.False(hasil.ImporLegacyDilakukan);
        Assert.Equal(SnapshotStatus.Missing, hasil.StatusSnapshot);
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task SnapshotKorupTidakPernahDitimpaOlehImporLegacy()
    {
        var store = Store();
        const string snapshotRusak = "{ snapshot rusak";
        await File.WriteAllTextAsync(store.SnapshotPath, snapshotRusak);
        var legacy = JsonSerializer.Serialize(new Payload("warisan", 7));
        File.WriteAllText(_legacy, legacy);

        var hasil = await Coordinator(store).JalankanModeJsonAsync(Importer(store), Kosong);

        Assert.Equal(ModeOperasi.ButuhPemulihan, hasil.Mode);
        Assert.Equal(SnapshotStatus.Corrupt, hasil.StatusSnapshot);
        Assert.Null(hasil.Data);
        Assert.Equal(snapshotRusak, await File.ReadAllTextAsync(store.SnapshotPath));
        Assert.Equal(legacy, await File.ReadAllTextAsync(_legacy));
    }

    [Fact]
    public async Task PayloadNullPadaBerkasLegacyDilaporkanRusakTanpaMengubahBerkas()
    {
        var store = Store();
        File.WriteAllText(_legacy, "null");

        var hasil = await Importer(store).ImporAsync();

        Assert.Equal(LegacyImportStatus.Rusak, hasil.Status);
        Assert.Null(hasil.Data);
        Assert.Equal("null", await File.ReadAllTextAsync(_legacy));
        Assert.False(File.Exists(store.SnapshotPath));
    }

    [Fact]
    public async Task SnapshotBerversiSudahAdaMenggagalkanImporSecaraDefensif()
    {
        var store = Store();
        await store.SaveAsync(new Payload("tersimpan", 1), Jam);
        File.WriteAllText(_legacy, JsonSerializer.Serialize(new Payload("harus-tidak-dipakai", 5)));

        var hasil = await Importer(store).ImporAsync();

        Assert.Equal(LegacyImportStatus.SudahTersimpan, hasil.Status);
        Assert.Null(hasil.Data);
        Assert.Equal("tersimpan", (await Store().LoadAsync()).Snapshot!.Payload.Nama);
        Assert.True(File.Exists(_legacy));
    }

    [Fact]
    public async Task PenyimpananSnapshotGagalTidakMenghilangkanBerkasLegacy()
    {
        var store = Store();
        // Berkas di jalur snapshot menghalangi penulisan; legacy wajib tetap utuh.
        Directory.CreateDirectory(store.SnapshotPath);
        File.WriteAllText(_legacy, JsonSerializer.Serialize(new Payload("warisan", 7)));

        var hasil = await Importer(store).ImporAsync();

        Assert.Equal(LegacyImportStatus.GagalMenyimpan, hasil.Status);
        Assert.Null(hasil.Data);
        Assert.True(File.Exists(_legacy));
        Assert.False(File.Exists(Path.Combine(_directory, "state.json.legacy.bak")));
    }

    private SnapshotStore<Payload> Store() => new(_directory, SnapshotProfile.LocalJson("default"));

    private LegacyJsonImporter<Payload> Importer(SnapshotStore<Payload> store) => new(_legacy, store, Baca);

    private static StartupSnapshotCoordinator<Payload> Coordinator(SnapshotStore<Payload> store)
        => new(store, () => Jam);

    private static Payload? Baca(string isi) => JsonSerializer.Deserialize<Payload>(isi);

    private static Payload Kosong() => new("kosong", 0);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    public sealed record Payload(string Nama, int Jumlah);
}
