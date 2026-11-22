using System.Text.Json;
using Npgsql;
using Wastu.Data.Snapshots;

namespace Wastu.Data.Tests.Snapshots;

public sealed class SnapshotProfileTests
{
    [Fact]
    public void PasswordRotationKeepsIdentityWithoutExposingConnectionValues()
    {
        var config = Config();
        var original = SnapshotProfile.FromPostgreSql(config);
        config.Password = "rotated-test-password";
        config.SslMode = SslMode.Require;
        config.ApplicationName = "different-client";
        var rotated = SnapshotProfile.FromPostgreSql(config);

        Assert.Equal(original, rotated);
        Assert.Matches("^postgres-[a-f0-9]{64}$", original.Id);
        var serialized = JsonSerializer.Serialize(original);
        foreach (var value in new[] { "db.example.test", "wastu_test", "tester", "initial-test-password", "rotated-test-password" })
        {
            Assert.DoesNotContain(value, serialized);
            Assert.DoesNotContain(value, original.ToString());
        }
    }

    [Theory]
    [InlineData("Host", "other.example.test")]
    [InlineData("Port", "5433")]
    [InlineData("Database", "other_database")]
    [InlineData("Username", "other_user")]
    [InlineData("Search Path", "tenant_b,public")]
    public void DifferentDatabaseScopeGetsDifferentIdentity(string property, string value)
    {
        var config = Config();
        var original = SnapshotProfile.FromPostgreSql(config);
        config[property] = value;
        Assert.NotEqual(original, SnapshotProfile.FromPostgreSql(config));
    }

    [Fact]
    public void LocalJsonHasExplicitSeparateNamespace()
    {
        var local = SnapshotProfile.LocalJson("portable");
        Assert.Equal(local, SnapshotProfile.LocalJson("portable"));
        Assert.NotEqual(local, SnapshotProfile.LocalJson("other"));
        Assert.NotEqual(local, SnapshotProfile.FromPostgreSql(Config()));
        Assert.Matches("^json-[a-f0-9]{64}$", local.Id);
        Assert.DoesNotContain("portable", JsonSerializer.Serialize(local));
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("Database")]
    [InlineData("Username")]
    public void IncompleteConfigurationFailsWithoutLeakingValues(string missing)
    {
        var config = Config();
        config.Remove(missing);
        var error = Assert.Throws<ArgumentException>(() => SnapshotProfile.FromPostgreSql(config));
        Assert.DoesNotContain("initial-test-password", error.ToString());
        Assert.DoesNotContain("db.example.test", error.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void LocalProfileNameIsRequired(string name)
        => Assert.Throws<ArgumentException>(() => SnapshotProfile.LocalJson(name));

    internal static NpgsqlConnectionStringBuilder Config() => new()
    {
        Host = "db.example.test", Port = 5432, Database = "wastu_test", Username = "tester",
        Password = "initial-test-password", SearchPath = "tenant_a,public"
    };
}
