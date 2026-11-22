using System.Text.Json;
using Npgsql;
using Wastu.Data.Snapshots;

namespace Wastu.Data.Tests.Snapshots;

public sealed class StartupSnapshotCoordinatorTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "wastu-startup-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTimeOffset WaktuData = new(2026, 11, 20, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset Jam = new(2026, 11, 22, 7, 30, 0, TimeSpan.Zero);

    public StartupSnapshotCoordinatorTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task DatabaseOnlineMenyimpanSnapshotBerversiDenganWaktuUtc()
    {
        var coordinator = Coordinator();
        var hasil = await coordinator.JalankanAsync(_ => Task.FromResult(new Payload("hidup", WaktuData)));

        Assert.Equal(ModeOperasi.DatabaseOnline, hasil.Mode);
        Assert.Equal("hidup", hasil.Data!.Nama);
        Assert.False(hasil.DipulihkanDariCadangan);
        Assert.False(string.IsNullOrWhiteSpace(hasil.Pesan));

        var tersimpan = await Store().LoadAsync();
        Assert.Equal(SnapshotStatus.ValidPrimary, tersimpan.Status);
        Assert.Equal(Jam, tersimpan.Snapshot!.DatabaseSnapshotAtUtc);
        Assert.Equal(TimeSpan.Zero, tersimpan.Snapshot.DatabaseSnapshotAtUtc.Offset);
        Assert.Equal(Profile().Id, tersimpan.Snapshot.ProfileId);
        Assert.Equal(SnapshotStore<Payload>.CurrentSchemaVersion, tersimpan.Snapshot.SchemaVersion);
        foreach (var rahasia in new[] { "initial-test-password", "db.example.test", "tester", "Host=" })
        {
            Assert.DoesNotContain(rahasia, await File.ReadAllTextAsync(Store().SnapshotPath));
            Assert.DoesNotContain(rahasia, Path.GetFileName(Store().SnapshotPath));
        }
    }

    [Fact]
    public async Task PenulisanSnapshotGagalTetapMengembalikanDataHidupDenganPeringatan()
    {
        var store = Store();
        // Berkas di jalur snapshot menghalangi penulisan; data hidup tidak boleh hilang.
        Directory.CreateDirectory(store.SnapshotPath);

        var hasil = await Coordinator().JalankanAsync(_ => Task.FromResult(new Payload("hidup", WaktuData)));

        Assert.Equal(ModeOperasi.DatabaseOnline, hasil.Mode);
        Assert.Equal("hidup", hasil.Data!.Nama);
        Assert.Contains("belum dapat disimpan", hasil.Pesan);
        Assert.Contains("tetap utuh", hasil.Pesan);
    }

    [Fact]
    public async Task DatabaseGagalDenganSnapshotValidMembukaLuringBacaSaja()
    {
        var store = Store();
        await store.SaveAsync(new Payload("simpanan", WaktuData), WaktuData);

        var hasil = await Coordinator().JalankanAsync(_ => throw new InvalidOperationException("koneksi ditolak"));

        Assert.Equal(ModeOperasi.LuringBacaSaja, hasil.Mode);
        Assert.Equal("simpanan", hasil.Data!.Nama);
        Assert.Equal(WaktuData, hasil.SnapshotDiambilPadaUtc);
        Assert.False(hasil.DipulihkanDariCadangan);
        Assert.Equal(SnapshotStatus.ValidPrimary, hasil.StatusSnapshot);
        Assert.Contains("baca-saja", hasil.Pesan);
        Assert.Contains("2026-11-20 03:04 UTC", hasil.Pesan);
    }

    [Fact]
    public async Task SnapshotRusakMemulihkanCadanganTanpaMenghapusBerkasRusak()
    {
        var store = Store();
        await store.SaveAsync(new Payload("lama", WaktuData), WaktuData);
        await store.SaveAsync(new Payload("baru", WaktuData.AddHours(1)), WaktuData.AddHours(1));
        const string rusak = "{ primer rusak tanpa penutup";
        await File.WriteAllTextAsync(store.SnapshotPath, rusak);

        var hasil = await Coordinator().JalankanAsync(_ => throw new IOException("database mati"));

        Assert.Equal(ModeOperasi.LuringBacaSaja, hasil.Mode);
        Assert.True(hasil.DipulihkanDariCadangan);
        Assert.Equal("lama", hasil.Data!.Nama);
        Assert.Equal(WaktuData, hasil.SnapshotDiambilPadaUtc);
        Assert.Equal(SnapshotStatus.RecoveredBackup, hasil.StatusSnapshot);
        Assert.Equal(rusak, await File.ReadAllTextAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task DatabaseGagalTanpaSnapshotMemintaPemulihanTanpaMembuatBerkas()
    {
        var hasil = await Coordinator().JalankanAsync(_ => throw new IOException("database mati"));

        Assert.Equal(ModeOperasi.ButuhPemulihan, hasil.Mode);
        Assert.Equal(SnapshotStatus.Missing, hasil.StatusSnapshot);
        Assert.Null(hasil.Data);
        Assert.False(hasil.DipulihkanDariCadangan);
        Assert.Empty(Directory.GetFiles(_directory));
        Assert.Contains("Coba Lagi", hasil.Pesan);
        Assert.Contains("Tidak ada data contoh", hasil.Pesan);
        Assert.Contains("WASTU_CONNECTION_STRING", hasil.Pesan);
    }

    [Fact]
    public async Task DuaBerkasRusakMemintaPemulihanDanMempertahankanKeduanya()
    {
        var store = Store();
        const string primer = "primer-rusak";
        const string cadangan = "cadangan-rusak";
        await File.WriteAllTextAsync(store.SnapshotPath, primer);
        await File.WriteAllTextAsync(store.BackupPath, cadangan);

        var hasil = await Coordinator().JalankanAsync(_ => throw new IOException("database mati"));

        Assert.Equal(ModeOperasi.ButuhPemulihan, hasil.Mode);
        Assert.Equal(SnapshotStatus.Corrupt, hasil.StatusSnapshot);
        Assert.Null(hasil.Data);
        Assert.Equal(primer, await File.ReadAllTextAsync(store.SnapshotPath));
        Assert.Equal(cadangan, await File.ReadAllTextAsync(store.BackupPath));
    }

    [Fact]
    public async Task VersiSkemaTakDidukungMemintaPemulihanTanpaMenyentuhBerkas()
    {
        var store = Store();
        await TulisEnvelope(store.SnapshotPath, Profile().Id, "masa-depan", WaktuData, versi: 99);
        var asli = await File.ReadAllBytesAsync(store.SnapshotPath);

        var hasil = await Coordinator().JalankanAsync(_ => throw new IOException("database mati"));

        Assert.Equal(ModeOperasi.ButuhPemulihan, hasil.Mode);
        Assert.Equal(SnapshotStatus.UnsupportedVersion, hasil.StatusSnapshot);
        Assert.Null(hasil.Data);
        Assert.Equal(asli, await File.ReadAllBytesAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task ProfilBerbedaMemintaPemulihanTanpaMemuatDataProfilLain()
    {
        var store = Store();
        await TulisEnvelope(store.SnapshotPath, SnapshotProfile.LocalJson("profil-lain").Id, "milik-lain", WaktuData);
        var asli = await File.ReadAllBytesAsync(store.SnapshotPath);

        var hasil = await Coordinator().JalankanAsync(_ => throw new IOException("database mati"));

        Assert.Equal(ModeOperasi.ButuhPemulihan, hasil.Mode);
        Assert.Equal(SnapshotStatus.ProfileMismatch, hasil.StatusSnapshot);
        Assert.Null(hasil.Data);
        Assert.Equal(asli, await File.ReadAllBytesAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task GantiKoneksiMemisahkanSnapshotAntarDatabase()
    {
        await Store("db_a").SaveAsync(new Payload("milik-a", WaktuData), WaktuData);

        // Koneksi ke database lain tidak boleh pernah memuat snapshot database pertama.
        var gagal = await Coordinator("db_b").JalankanAsync(_ => throw new IOException("database B mati"));
        Assert.Equal(ModeOperasi.ButuhPemulihan, gagal.Mode);
        Assert.Equal(SnapshotStatus.Missing, gagal.StatusSnapshot);
        Assert.Null(gagal.Data);

        var daring = await Coordinator("db_b").JalankanAsync(_ => Task.FromResult(new Payload("milik-b", WaktuData)));
        Assert.Equal(ModeOperasi.DatabaseOnline, daring.Mode);
        Assert.Equal("milik-b", (await Store("db_b").LoadAsync()).Snapshot!.Payload.Nama);
        Assert.Equal("milik-a", (await Store("db_a").LoadAsync()).Snapshot!.Payload.Nama);
        Assert.Equal(2, Directory.GetFiles(_directory).Length);
    }

    [Fact]
    public async Task PesanStatusTidakPernahMembocorkanStringKoneksiAtauPassword()
    {
        var koneksi = "Host=db.example.test;Port=5432;Database=wastu_test;Username=tester;Password=rahasia-tersembunyi-999";

        var hasil = await Coordinator().JalankanAsync(_ => throw new InvalidOperationException("gagal: " + koneksi));

        Assert.Equal(ModeOperasi.ButuhPemulihan, hasil.Mode);
        foreach (var sisi in new[] { "rahasia-tersembunyi-999", "db.example.test", "tester", "wastu_test", koneksi })
            Assert.DoesNotContain(sisi, hasil.Pesan);
    }

    [Fact]
    public async Task PembatalanTidakDianggapKegagalanDatabase()
    {
        using var sumber = new CancellationTokenSource();
        sumber.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Coordinator().JalankanAsync(_ => Task.FromCanceled<Payload>(sumber.Token), sumber.Token));
    }

    private static NpgsqlConnectionStringBuilder Config(string database) => new()
    {
        Host = "db.example.test",
        Port = 5432,
        Database = database,
        Username = "tester",
        Password = "initial-test-password"
    };

    private SnapshotProfile Profile(string database = "wastu_test") => SnapshotProfile.FromPostgreSql(Config(database));

    private SnapshotStore<Payload> Store(string database = "wastu_test") => new(_directory, Profile(database));

    private StartupSnapshotCoordinator<Payload> Coordinator(string database = "wastu_test")
        => new(Store(database), () => Jam);

    private static Task TulisEnvelope(string path, string profileId, string nama, DateTimeOffset waktuData, int versi = 1)
        => File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            SchemaVersion = versi,
            ProfileId = profileId,
            DatabaseSnapshotAtUtc = waktuData,
            Payload = new Payload(nama, waktuData)
        }));

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    public sealed record Payload(string Nama, DateTimeOffset Waktu);
}
