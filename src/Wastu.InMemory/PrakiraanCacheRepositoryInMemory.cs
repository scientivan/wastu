using System.Collections.Concurrent;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.InMemory;

public sealed class PrakiraanCacheRepositoryInMemory : IPrakiraanCacheRepository
{
    private sealed record CacheKey(double LatRounded, double LonRounded, int RevisiLokasi);

    private readonly ConcurrentDictionary<CacheKey, HasilPrakiraan> _cache = new();

    private static CacheKey BuatKunci(double lat, double lon, int revisiLokasi)
        => new(Math.Round(lat, 3), Math.Round(lon, 3), revisiLokasi);

    public Task<HasilPrakiraan?> GetCacheAsync(double lat, double lon, int revisiLokasi = 1, CancellationToken ct = default)
    {
        var key = BuatKunci(lat, lon, revisiLokasi);
        return Task.FromResult(_cache.GetValueOrDefault(key));
    }

    public Task SimpanCacheAsync(double lat, double lon, HasilPrakiraan hasil, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(hasil);
        var key = BuatKunci(lat, lon, hasil.RevisiLokasi);
        _cache[key] = hasil;
        return Task.CompletedTask;
    }

    public Task PruneExpiredAsync(DateTimeOffset sebelumUtc, CancellationToken ct = default)
    {
        foreach (var (k, v) in _cache)
        {
            if (v.DiambilPadaUtc < sebelumUtc)
            {
                _cache.TryRemove(k, out _);
            }
        }
        return Task.CompletedTask;
    }
}
