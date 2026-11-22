using System.Text.Json;

namespace Wastu.Data.Snapshots;

/// <summary>
/// Async, profile-scoped JSON persistence. Calls on this instance are serialized; callers must
/// share one instance per profile and payload type. Multiple instances/processes writing the
/// same namespace are outside this contract. The caller owns payload completeness and must
/// supply a DTO without credentials, plus the original successful database snapshot time.
/// </summary>
public sealed class SnapshotStore<T> where T : notnull
{
    public const int CurrentSchemaVersion = 1;
    private readonly SnapshotProfile _profile;
    private readonly Func<T, bool>? _validatePayload;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _directory;

    public SnapshotStore(string directory, SnapshotProfile profile, Func<T, bool>? validatePayload = null)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Folder snapshot wajib diisi.", nameof(directory));
        ArgumentNullException.ThrowIfNull(profile);
        _directory = Path.GetFullPath(directory);
        _profile = profile;
        _validatePayload = validatePayload;
        SnapshotPath = Path.Combine(_directory, profile.Id + ".json");
        BackupPath = SnapshotPath + ".bak";
    }

    public string SnapshotPath { get; }
    public string BackupPath { get; }

    /// <summary>
    /// Reads without modifying any file or timestamp. Only a corrupt/unreadable or missing
    /// primary permits backup recovery. Unsupported versions and foreign profiles never do.
    /// </summary>
    public async Task<SnapshotLoadResult<T>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var primary = await ReadFileAsync(SnapshotPath, cancellationToken).ConfigureAwait(false);
            if (primary.Status is not (SnapshotStatus.Corrupt or SnapshotStatus.Missing))
                return primary;

            var backup = await ReadFileAsync(BackupPath, cancellationToken).ConfigureAwait(false);
            if (backup.Status == SnapshotStatus.ValidPrimary)
                return new(SnapshotStatus.RecoveredBackup,
                    "Snapshot utama tidak tersedia atau rusak. Cadangan valid tersedia dengan waktu data aslinya.", backup.Snapshot);
            if (backup.Status is SnapshotStatus.UnsupportedVersion or SnapshotStatus.ProfileMismatch)
                return backup;
            return primary.Status == SnapshotStatus.Missing ? backup : primary;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Saves after a successful database operation; never substitutes the time of file I/O.
    /// Unsupported/legacy files or profile mismatches require explicit caller recovery before
    /// another save. The final promotion is atomic and is the cancellation commit point.
    /// </summary>
    public async Task SaveAsync(T payload, DateTimeOffset databaseSnapshotAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        if (!ValidTimestamp(databaseSnapshotAtUtc))
            throw new ArgumentException("Waktu snapshot database harus berupa timestamp UTC yang valid.", nameof(databaseSnapshotAtUtc));
        if (!ValidPayload(payload))
            throw new ArgumentException("Isi snapshot tidak memenuhi validasi pemanggil.", nameof(payload));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        string? temporary = null;
        try
        {
            var primary = await ReadFileAsync(SnapshotPath, cancellationToken).ConfigureAwait(false);
            var backup = await ReadFileAsync(BackupPath, cancellationToken).ConfigureAwait(false);
            ProtectExisting(primary);
            ProtectExisting(backup);
            try
            {
                Directory.CreateDirectory(_directory);
                temporary = SnapshotPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 81920, FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(stream, new SnapshotEnvelope<T>
                    {
                        SchemaVersion = CurrentSchemaVersion, ProfileId = _profile.Id,
                        DatabaseSnapshotAtUtc = databaseSnapshotAtUtc, Payload = payload
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (primary.Status == SnapshotStatus.ValidPrimary)
                    File.Replace(temporary, SnapshotPath, BackupPath);
                else
                    File.Move(temporary, SnapshotPath, overwrite: true);
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            catch (Exception)
            {
                // File paths, serializer messages and inner exceptions can contain caller data.
                throw new IOException("Snapshot belum dapat disimpan. Salinan sebelumnya tetap dipertahankan.");
            }
        }
        finally
        {
            if (temporary is not null)
            {
                try { File.Delete(temporary); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            }
            _gate.Release();
        }
    }

    private async Task<SnapshotLoadResult<T>> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Corrupt();
            if (!root.TryGetProperty(nameof(SnapshotEnvelope<T>.SchemaVersion), out var schema))
                return new(SnapshotStatus.UnsupportedVersion,
                    "Snapshot belum memiliki versi skema. Diperlukan impor legacy secara eksplisit.");
            if (schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out var version))
                return Corrupt();
            if (version != CurrentSchemaVersion)
                return new(SnapshotStatus.UnsupportedVersion,
                    $"Versi skema snapshot {version} belum didukung. Berkas tetap dipertahankan.", foundSchemaVersion: version);
            if (!root.TryGetProperty(nameof(SnapshotEnvelope<T>.ProfileId), out var identity) ||
                identity.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(identity.GetString()))
                return Corrupt();
            if (!string.Equals(identity.GetString(), _profile.Id, StringComparison.Ordinal))
                return new(SnapshotStatus.ProfileMismatch,
                    "Snapshot berasal dari profil penyimpanan lain dan tidak dapat digunakan.");
            var snapshot = root.Deserialize<SnapshotEnvelope<T>>();
            if (snapshot is null || !ValidTimestamp(snapshot.DatabaseSnapshotAtUtc) ||
                snapshot.Payload is null || !ValidPayload(snapshot.Payload))
                return Corrupt();
            return new(SnapshotStatus.ValidPrimary, "Snapshot valid tersedia dengan waktu data aslinya.", snapshot);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return new(SnapshotStatus.Missing, "Snapshot belum tersedia untuk profil penyimpanan ini.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return Corrupt();
        }
    }

    private bool ValidPayload(T payload)
    {
        try { return _validatePayload?.Invoke(payload) ?? true; }
        catch (Exception) { return false; }
    }

    private static bool ValidTimestamp(DateTimeOffset timestamp)
        => timestamp != default && timestamp.Offset == TimeSpan.Zero;

    private static SnapshotLoadResult<T> Corrupt()
        => new(SnapshotStatus.Corrupt, "Snapshot kosong, rusak, tidak dapat dibaca, atau isinya tidak valid. Berkas tetap dipertahankan.");

    private static void ProtectExisting(SnapshotLoadResult<T> result)
    {
        if (result.Status is SnapshotStatus.UnsupportedVersion or SnapshotStatus.ProfileMismatch)
            throw new InvalidOperationException("Snapshot tidak ditimpa karena versi skema atau profil berbeda. Diperlukan pemulihan secara eksplisit.");
    }
}
