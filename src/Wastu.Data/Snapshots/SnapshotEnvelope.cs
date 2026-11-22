namespace Wastu.Data.Snapshots;

public sealed record SnapshotEnvelope<T> where T : notnull
{
    public required int SchemaVersion { get; init; }
    public required string ProfileId { get; init; }
    public required DateTimeOffset DatabaseSnapshotAtUtc { get; init; }
    public required T Payload { get; init; }
}
