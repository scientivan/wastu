using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace Wastu.Data.Snapshots;

/// <summary>A stable, nonsecret storage namespace. Never retains the connection configuration.</summary>
public sealed record SnapshotProfile
{
    private SnapshotProfile(string id) => Id = id;

    public string Id { get; }

    /// <summary>
    /// Uses only the explicitly supplied host, port, database, username and search path.
    /// Authentication, transport options and environment defaults are not part of the identity.
    /// Database and username must be explicit, even when the provider could infer defaults.
    /// </summary>
    public static SnapshotProfile FromPostgreSql(NpgsqlConnectionStringBuilder configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(configuration.Host) ||
            string.IsNullOrWhiteSpace(configuration.Database) ||
            string.IsNullOrWhiteSpace(configuration.Username) || configuration.Port is < 1 or > 65535)
            throw new ArgumentException("Profil database memerlukan host, port, database, dan pengguna yang valid.", nameof(configuration));

        // JSON array framing prevents collisions when a field contains separators.
        var scope = JsonSerializer.Serialize(new object[]
        {
            configuration.Host, configuration.Port, configuration.Database,
            configuration.Username, configuration.SearchPath ?? ""
        });
        return new SnapshotProfile("postgres-" + Hash(scope));
    }

    /// <summary>An explicitly named compatibility profile, isolated from every database profile.</summary>
    public static SnapshotProfile LocalJson(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nama profil JSON lokal wajib diisi.", nameof(name));
        return new SnapshotProfile("json-" + Hash(name));
    }

    public override string ToString() => Id;

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
