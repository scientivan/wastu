namespace Wastu.Data.Snapshots;

public enum SnapshotStatus
{
    Missing, ValidPrimary, RecoveredBackup, Corrupt, UnsupportedVersion, ProfileMismatch
}

public sealed record SnapshotLoadResult<T> where T : notnull
{
    internal SnapshotLoadResult(SnapshotStatus status, string message,
        SnapshotEnvelope<T>? snapshot = null, int? foundSchemaVersion = null)
        => (Status, Message, Snapshot, FoundSchemaVersion) = (status, message, snapshot, foundSchemaVersion);

    public SnapshotStatus Status { get; }
    public string Message { get; }
    public SnapshotEnvelope<T>? Snapshot { get; }
    public int? FoundSchemaVersion { get; }
}
