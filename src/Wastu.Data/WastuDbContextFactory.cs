using Microsoft.EntityFrameworkCore;

namespace Wastu.Data;

public sealed class WastuDbContextFactory : IDbContextFactory<WastuDbContext>
{
    private readonly DbContextOptions<WastuDbContext> _opsi;

    public WastuDbContextFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        string? schema = csb.SearchPath;

        var builder = new DbContextOptionsBuilder<WastuDbContext>();
        builder.UseNpgsql(connectionString, npgsql =>
        {
            if (!string.IsNullOrWhiteSpace(schema))
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            }
        });

        _opsi = builder.Options;
    }

    public WastuDbContext CreateDbContext() => new(_opsi);
}
