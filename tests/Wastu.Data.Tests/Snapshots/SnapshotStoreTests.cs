using System.Text.Json;
using System.Text.Json.Serialization;
using Wastu.Data.Snapshots;

namespace Wastu.Data.Tests.Snapshots;

public sealed class SnapshotStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "wastu-snapshots-" + Guid.NewGuid().ToString("N"));
    private readonly SnapshotProfile _profile = SnapshotProfile.FromPostgreSql(SnapshotProfileTests.Config());
    private static readonly DateTimeOffset FetchedAt = new(2026, 11, 20, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset DatabaseAt = FetchedAt.AddHours(1);

    public SnapshotStoreTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task RoundtripAcrossInstancesPreservesPayloadAndOriginalTimestamps()
    {
        var store = Store();
        await store.SaveAsync(new Payload("rumah", FetchedAt), DatabaseAt);
        var loaded = await Store().LoadAsync();

        Assert.Equal(SnapshotStatus.ValidPrimary, loaded.Status);
        Assert.Equal(new Payload("rumah", FetchedAt), loaded.Snapshot!.Payload);
        Assert.Equal(DatabaseAt, loaded.Snapshot.DatabaseSnapshotAtUtc);
        Assert.Equal(_profile.Id, loaded.Snapshot.ProfileId);
        Assert.Equal(SnapshotStore<Payload>.CurrentSchemaVersion, loaded.Snapshot.SchemaVersion);
        Assert.False(string.IsNullOrWhiteSpace(loaded.Message));
        var bytes = await File.ReadAllTextAsync(store.SnapshotPath);
        foreach (var secret in new[] { "initial-test-password", "db.example.test", "wastu_test", "tester", "Host=" })
        {
            Assert.DoesNotContain(secret, bytes);
            Assert.DoesNotContain(secret, Path.GetFileName(store.SnapshotPath));
        }
    }

    [Fact]
    public async Task ProfilesAreIsolatedAndPasswordRotationCanReload()
    {
        await Store().SaveAsync(new Payload("original", FetchedAt), DatabaseAt);
        var config = SnapshotProfileTests.Config();
        config.Password = "rotated-test-password";
        Assert.Equal(SnapshotStatus.ValidPrimary,
            (await Store(SnapshotProfile.FromPostgreSql(config)).LoadAsync()).Status);
        config.Database = "other_database";
        Assert.Equal(SnapshotStatus.Missing,
            (await Store(SnapshotProfile.FromPostgreSql(config)).LoadAsync()).Status);
        Assert.Equal(SnapshotStatus.Missing, (await Store(SnapshotProfile.LocalJson("compatibility")).LoadAsync()).Status);
    }

    [Fact]
    public async Task MissingFilesHaveExplicitOutcomeWithoutCreatingFiles()
    {
        var loaded = await Store().LoadAsync();
        Assert.Equal(SnapshotStatus.Missing, loaded.Status);
        Assert.Null(loaded.Snapshot);
        Assert.False(string.IsNullOrWhiteSpace(loaded.Message));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ broken secret-test-token")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"SchemaVersion\":\"one\"}")]
    public async Task InvalidFileIsReportedAndRetained(string content)
    {
        var store = Store();
        await File.WriteAllTextAsync(store.SnapshotPath, content);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.Corrupt, loaded.Status);
        Assert.Null(loaded.Snapshot);
        Assert.DoesNotContain("secret-test-token", loaded.Message);
        Assert.Equal(content, await File.ReadAllTextAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task UnreadablePrimaryHasSafeExplicitOutcome()
    {
        var store = Store();
        Directory.CreateDirectory(store.SnapshotPath);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.Corrupt, loaded.Status);
        Assert.Null(loaded.Snapshot);
        Assert.False(string.IsNullOrWhiteSpace(loaded.Message));
    }

    [Fact]
    public async Task CorruptPrimaryRecoversOnlyValidBackupAndKeepsCorruptBytes()
    {
        var store = Store();
        await store.SaveAsync(new Payload("older", FetchedAt), DatabaseAt);
        await store.SaveAsync(new Payload("newer", FetchedAt.AddHours(2)), DatabaseAt.AddHours(2));
        const string damaged = "{ diagnosis: corrupt primary }";
        await File.WriteAllTextAsync(store.SnapshotPath, damaged);

        var loaded = await Store().LoadAsync();
        Assert.Equal(SnapshotStatus.RecoveredBackup, loaded.Status);
        Assert.Equal("older", loaded.Snapshot!.Payload.Name);
        Assert.Equal(FetchedAt, loaded.Snapshot.Payload.FetchedAtUtc);
        Assert.Equal(DatabaseAt, loaded.Snapshot.DatabaseSnapshotAtUtc);
        Assert.Equal(damaged, await File.ReadAllTextAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task MissingPrimaryCanRecoverBackup()
    {
        var store = Store();
        await WriteEnvelope(store.BackupPath, _profile.Id);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.RecoveredBackup, loaded.Status);
        Assert.Equal(DatabaseAt, loaded.Snapshot!.DatabaseSnapshotAtUtc);
        Assert.False(File.Exists(store.SnapshotPath));
    }

    [Fact]
    public async Task BothInvalidFilesRemainAvailableForDiagnosis()
    {
        var store = Store();
        await File.WriteAllTextAsync(store.SnapshotPath, "bad-primary");
        await File.WriteAllTextAsync(store.BackupPath, "bad-backup");
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.Corrupt, loaded.Status);
        Assert.Null(loaded.Snapshot);
        Assert.Equal("bad-primary", await File.ReadAllTextAsync(store.SnapshotPath));
        Assert.Equal("bad-backup", await File.ReadAllTextAsync(store.BackupPath));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(999)]
    public async Task UnsupportedSchemaNeverFallsBackOrGetsOverwritten(int version)
    {
        var store = Store();
        // Unknown payload shapes must not be deserialized using today's DTO.
        await File.WriteAllTextAsync(store.SnapshotPath,
            JsonSerializer.Serialize(new { SchemaVersion = version, ProfileId = _profile.Id, Payload = "future-format" }));
        await WriteEnvelope(store.BackupPath, _profile.Id);
        var original = await File.ReadAllBytesAsync(store.SnapshotPath);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.UnsupportedVersion, loaded.Status);
        Assert.Equal(version, loaded.FoundSchemaVersion);
        Assert.Null(loaded.Snapshot);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new Payload("new", FetchedAt), DatabaseAt));
        Assert.Equal(original, await File.ReadAllBytesAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task LegacyUnversionedFileNeedsExplicitImporter()
    {
        var store = Store();
        const string legacy = "{\"Proyek\":[],\"Horizon\":30}";
        await File.WriteAllTextAsync(store.SnapshotPath, legacy);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.UnsupportedVersion, loaded.Status);
        Assert.Null(loaded.FoundSchemaVersion);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new Payload("new", FetchedAt), DatabaseAt));
        Assert.Equal(legacy, await File.ReadAllTextAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task CopiedForeignPrimaryNeverFallsBackOrGetsOverwritten()
    {
        var store = Store();
        await WriteEnvelope(store.SnapshotPath, SnapshotProfile.LocalJson("other").Id);
        await WriteEnvelope(store.BackupPath, _profile.Id);
        var original = await File.ReadAllBytesAsync(store.SnapshotPath);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.ProfileMismatch, loaded.Status);
        Assert.Null(loaded.Snapshot);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new Payload("new", FetchedAt), DatabaseAt));
        Assert.Equal(original, await File.ReadAllBytesAsync(store.SnapshotPath));
    }

    [Fact]
    public async Task ForeignBackupCannotRecoverCorruptPrimary()
    {
        var store = Store();
        await File.WriteAllTextAsync(store.SnapshotPath, "bad-primary");
        await WriteEnvelope(store.BackupPath, SnapshotProfile.LocalJson("other").Id);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.ProfileMismatch, loaded.Status);
        Assert.Null(loaded.Snapshot);
    }

    [Fact]
    public async Task UnsupportedBackupIsReportedAndProtectedEvenWhenPrimaryIsValid()
    {
        var store = Store();
        await WriteEnvelope(store.SnapshotPath, _profile.Id);
        await WriteEnvelope(store.BackupPath, _profile.Id, version: 99);
        var original = await File.ReadAllBytesAsync(store.BackupPath);
        Assert.Equal(SnapshotStatus.ValidPrimary, (await store.LoadAsync()).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new Payload("new", FetchedAt), DatabaseAt));
        File.Delete(store.SnapshotPath);
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.UnsupportedVersion, loaded.Status);
        Assert.Equal(99, loaded.FoundSchemaVersion);
        Assert.Equal(original, await File.ReadAllBytesAsync(store.BackupPath));
    }

    [Theory]
    [InlineData("timestamp")]
    [InlineData("default-timestamp")]
    [InlineData("payload")]
    [InlineData("profile")]
    public async Task InvalidEnvelopeCannotYieldPayload(string invalid)
    {
        var store = Store();
        var timestamp = invalid == "timestamp" ? DatabaseAt.ToOffset(TimeSpan.FromHours(7))
            : invalid == "default-timestamp" ? default : DatabaseAt;
        await WriteEnvelope(store.SnapshotPath, invalid == "profile" ? "" : _profile.Id,
            timestamp: timestamp, nullPayload: invalid == "payload");
        Assert.Equal(SnapshotStatus.Corrupt, (await store.LoadAsync()).Status);
    }

    [Fact]
    public async Task CallerValidationAppliesToBothSaveAndLoad()
    {
        var store = new SnapshotStore<Payload>(_directory, _profile, p => p.Name.Length > 0);
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(new Payload("", FetchedAt), DatabaseAt));
        Assert.Empty(Directory.GetFiles(_directory));
        await Store().SaveAsync(new Payload("", FetchedAt), DatabaseAt);
        Assert.Equal(SnapshotStatus.Corrupt, (await store.LoadAsync()).Status);
    }

    [Fact]
    public async Task SaveRequiresNonNullPayloadAndExplicitUtcTimestamp()
    {
        var store = Store();
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!, DatabaseAt));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(new Payload("new", FetchedAt), default));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(new Payload("new", FetchedAt), DatabaseAt.ToOffset(TimeSpan.FromHours(7))));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    private SnapshotStore<Payload> Store(SnapshotProfile? profile = null) => new(_directory, profile ?? _profile);

    private static Task WriteEnvelope(string path, string profile, int version = 1,
        DateTimeOffset? timestamp = null, bool nullPayload = false)
        => File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            SchemaVersion = version, ProfileId = profile, DatabaseSnapshotAtUtc = timestamp ?? DatabaseAt,
            Payload = nullPayload ? null : new Payload("backup", FetchedAt)
        }));

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    public sealed record Payload(string Name, DateTimeOffset FetchedAtUtc);
}
