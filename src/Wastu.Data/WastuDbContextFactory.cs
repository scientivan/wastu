using Microsoft.EntityFrameworkCore;

namespace Wastu.Data;

public sealed class WastuDbContextFactory : IDbContextFactory<WastuDbContext>
{
    private readonly DbContextOptions<WastuDbContext> _opsi;

    public WastuDbContextFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _opsi = new DbContextOptionsBuilder<WastuDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public WastuDbContext CreateDbContext() => new(_opsi);
}
