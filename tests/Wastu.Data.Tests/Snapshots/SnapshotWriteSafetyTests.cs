using System.Text.Json;
using System.Text.Json.Serialization;
using Wastu.Data.Snapshots;

namespace Wastu.Data.Tests.Snapshots;

public sealed class SnapshotWriteSafetyTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "wastu-snapshot-writes-" + Guid.NewGuid().ToString("N"));
    private readonly SnapshotProfile _profile = SnapshotProfile.LocalJson("test-writes");
    private static readonly DateTimeOffset DatabaseAt = new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    public SnapshotWriteSafetyTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task SavingAfterCorruptionKeepsLastValidBackupAndCorruptBytes()
    {
        var store = Store();
        await Seed(store);
        var backup = await File.ReadAllBytesAsync(store.BackupPath);
        const string corrupt = "corrupt-primary-for-diagnosis";
        await File.WriteAllTextAsync(store.SnapshotPath, corrupt);

        await store.SaveAsync(new WritePayload { Value = "fresh" }, DatabaseAt.AddDays(1));

        Assert.Equal("fresh", (await store.LoadAsync()).Snapshot!.Payload.Value);
        Assert.Equal(backup, await File.ReadAllBytesAsync(store.BackupPath));
        var retainedFiles = await Task.WhenAll(Directory.GetFiles(_directory).Select(path => File.ReadAllTextAsync(path)));
        Assert.Contains(corrupt, retainedFiles);
        File.Delete(store.SnapshotPath);
        Assert.Equal("older", (await store.LoadAsync()).Snapshot!.Payload.Value);
    }

    [Fact]
    public async Task PayloadThatCannotBeReloadedDoesNotReplaceGoodSnapshot()
    {
        var originalStore = Store();
        await Seed(originalStore);
        var primary = await File.ReadAllBytesAsync(originalStore.SnapshotPath);
        var backup = await File.ReadAllBytesAsync(originalStore.BackupPath);
        var incompatibleStore = new SnapshotStore<WriteOnlyPayload>(_directory, _profile);
        await Assert.ThrowsAsync<IOException>(() => incompatibleStore.SaveAsync(WriteOnlyPayload.Create(), DatabaseAt));
        Assert.Equal(primary, await File.ReadAllBytesAsync(originalStore.SnapshotPath));
        Assert.Equal(backup, await File.ReadAllBytesAsync(originalStore.BackupPath));
    }

    [Fact]
    public async Task CancellationBeforeSaveLeavesBothCopiesAndStoreUsable()
    {
        var store = Store();
        await Seed(store);
        var primary = await File.ReadAllBytesAsync(store.SnapshotPath);
        var backup = await File.ReadAllBytesAsync(store.BackupPath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.SaveAsync(new WritePayload { Value = "canceled" }, DatabaseAt, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadAsync(cancellation.Token));
        Assert.Equal(primary, await File.ReadAllBytesAsync(store.SnapshotPath));
        Assert.Equal(backup, await File.ReadAllBytesAsync(store.BackupPath));
        Assert.Equal("current", (await store.LoadAsync()).Snapshot!.Payload.Value);
    }

    [Fact]
    public async Task CancellationDuringSerializationPreservesCopiesAndOnlyCleansOwnTemporaryFile()
    {
        var store = Store();
        await Seed(store);
        var primary = await File.ReadAllBytesAsync(store.SnapshotPath);
        var backup = await File.ReadAllBytesAsync(store.BackupPath);
        var unrelated = Path.Combine(_directory, "another-writer.tmp");
        await File.WriteAllTextAsync(unrelated, "unrelated");
        using var cancellation = new CancellationTokenSource();
        var payload = new WritePayload { Value = new string('x', 100000), OnRead = cancellation.Cancel };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(payload, DatabaseAt, cancellation.Token));

        Assert.Equal(primary, await File.ReadAllBytesAsync(store.SnapshotPath));
        Assert.Equal(backup, await File.ReadAllBytesAsync(store.BackupPath));
        Assert.Equal("unrelated", await File.ReadAllTextAsync(unrelated));
        Assert.Single(Directory.GetFiles(_directory, "*.tmp"));
        await store.SaveAsync(new WritePayload { Value = "after-cancel" }, DatabaseAt);
        Assert.Equal("after-cancel", (await store.LoadAsync()).Snapshot!.Payload.Value);
    }

    [Fact]
    public async Task SerializationFailureHasSafeMessageAndLeavesBothCopiesUsable()
    {
        var store = Store();
        await Seed(store);
        var primary = await File.ReadAllBytesAsync(store.SnapshotPath);
        var backup = await File.ReadAllBytesAsync(store.BackupPath);
        var payload = new WritePayload
        {
            Value = "never-written",
            OnRead = () => throw new InvalidOperationException("synthetic-secret-test-token")
        };
        var error = await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(payload, DatabaseAt));
        Assert.DoesNotContain("synthetic-secret-test-token", error.ToString());
        Assert.Equal(primary, await File.ReadAllBytesAsync(store.SnapshotPath));
        Assert.Equal(backup, await File.ReadAllBytesAsync(store.BackupPath));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.Equal("current", (await store.LoadAsync()).Snapshot!.Payload.Value);
    }

    [Fact]
    public async Task ConcurrentCallsOnSameStoreSeeCompleteSnapshots()
    {
        var store = Store();
        await Seed(store);
        var operations = Enumerable.Range(0, 30).Select(async number =>
        {
            if (number % 2 == 0)
                await store.SaveAsync(new WritePayload { Value = "write-" + number }, DatabaseAt.AddMinutes(number));
            else
            {
                var loaded = await store.LoadAsync();
                Assert.Equal(SnapshotStatus.ValidPrimary, loaded.Status);
                Assert.Equal(_profile.Id, loaded.Snapshot!.ProfileId);
                Assert.False(string.IsNullOrEmpty(loaded.Snapshot.Payload.Value));
            }
        });
        await Task.WhenAll(operations);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        File.Delete(store.SnapshotPath);
        Assert.Equal(SnapshotStatus.RecoveredBackup, (await store.LoadAsync()).Status);
    }

    [Fact]
    public async Task FailedAtomicPromotionRetainsPrimaryAndCleansTemporaryFile()
    {
        var store = Store();
        await store.SaveAsync(new WritePayload { Value = "current" }, DatabaseAt);
        var primary = await File.ReadAllBytesAsync(store.SnapshotPath);
        // A directory cannot be used as the backup file on either Windows or Linux.
        Directory.CreateDirectory(store.BackupPath);
        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(new WritePayload { Value = "failed" }, DatabaseAt));
        Assert.Equal(primary, await File.ReadAllBytesAsync(store.SnapshotPath));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.Equal("current", (await store.LoadAsync()).Snapshot!.Payload.Value);
    }

    [Fact]
    public async Task DeserializationFailureDoesNotExposePayloadExceptions()
    {
        var store = new SnapshotStore<ThrowingConstructorPayload>(_directory, _profile);
        await File.WriteAllTextAsync(store.SnapshotPath, JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, ProfileId = _profile.Id, DatabaseSnapshotAtUtc = DatabaseAt,
            Payload = new { Value = "synthetic-secret-test-token" }
        }));
        var loaded = await store.LoadAsync();
        Assert.Equal(SnapshotStatus.Corrupt, loaded.Status);
        Assert.DoesNotContain("synthetic-secret-test-token", loaded.Message);
        Assert.Null(loaded.Snapshot);
    }

    [Fact]
    public async Task VersionBeyondIntegerRangeIsProtectedAsUnsupported()
    {
        var store = Store();
        const string future = "{\"SchemaVersion\":2147483648,\"Payload\":{}}";
        await File.WriteAllTextAsync(store.SnapshotPath, future);
        Assert.Equal(SnapshotStatus.UnsupportedVersion, (await store.LoadAsync()).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(new WritePayload { Value = "new" }, DatabaseAt));
        Assert.Equal(future, await File.ReadAllTextAsync(store.SnapshotPath));
    }

    [Fact]
    public void StoreArgumentsAreValidatedWithoutCreatingFiles()
    {
        Assert.Throws<ArgumentException>(() => new SnapshotStore<WritePayload>(" ", _profile));
        Assert.Throws<ArgumentNullException>(() => new SnapshotStore<WritePayload>(_directory, null!));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    private SnapshotStore<WritePayload> Store() => new(_directory, _profile);

    private static async Task Seed(SnapshotStore<WritePayload> store)
    {
        await store.SaveAsync(new WritePayload { Value = "older" }, DatabaseAt);
        await store.SaveAsync(new WritePayload { Value = "current" }, DatabaseAt.AddHours(1));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    public sealed class WritePayload
    {
        private readonly string _value = "";
        public string Value { get { OnRead?.Invoke(); return _value; } init => _value = value; }
        [JsonIgnore] public Action? OnRead { get; init; }
    }

    public sealed class ThrowingConstructorPayload
    {
        public ThrowingConstructorPayload(string value) => throw new InvalidOperationException(value);
        public string Value { get; } = "";
    }

    public sealed class WriteOnlyPayload
    {
        private WriteOnlyPayload() { }
        public string Value => "cannot-be-reloaded";
        public static WriteOnlyPayload Create() => new();
    }
}
