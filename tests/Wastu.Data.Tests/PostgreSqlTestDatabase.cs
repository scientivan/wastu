using Npgsql;
using Wastu.Data;

namespace Wastu.Data.Tests;

/// <summary>
/// Menyediakan skema PostgreSQL terisolasi secara efemeral per eksekusi tes dan
/// membersihkannya secara bersih setelah pengujian selesai.
/// </summary>
public sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    public string BaseConnectionString { get; }
    public string SchemaName { get; }
    public string ConnectionString { get; }
    public WastuDbContextFactory Factory { get; }

    private PostgreSqlTestDatabase(string baseConnStr, string schemaName, string connStr, WastuDbContextFactory factory)
    {
        BaseConnectionString = baseConnStr;
        SchemaName = schemaName;
        ConnectionString = connStr;
        Factory = factory;
    }

    public static async Task<PostgreSqlTestDatabase> CreateAsync()
    {
        string? baseConnStr = Environment.GetEnvironmentVariable(PerluPostgreSqlAttribute.NamaVariabelLingkungan);
        if (string.IsNullOrWhiteSpace(baseConnStr))
        {
            throw new InvalidOperationException(
                $"{PerluPostgreSqlAttribute.NamaVariabelLingkungan} belum diatur.");
        }

        string schemaName = $"test_wp01b_{Guid.NewGuid():N}";

        await using (var conn = new NpgsqlConnection(baseConnStr))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE SCHEMA \"{schemaName}\";";
            await cmd.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(baseConnStr)
        {
            SearchPath = schemaName
        };
        string connStr = builder.ConnectionString;
        var factory = new WastuDbContextFactory(connStr);

        return new PostgreSqlTestDatabase(baseConnStr, schemaName, connStr, factory);
    }

    public async Task ExecuteSqlScriptAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(BaseConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{SchemaName}\" CASCADE;";
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Abaikan kesalahan penghapusan skema agar tidak menyembunyikan kegagalan assert utama
        }
    }
}
