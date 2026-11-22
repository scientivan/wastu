using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

public sealed class SumberBahanRepositoryInMemory : ISumberBahanRepository
{
    private readonly ConcurrentDictionary<Guid, List<SumberPropertiBahan>> _sumber = new();

    public Task<IReadOnlyList<SumberPropertiBahan>> ListByBahanIdAsync(Guid bahanId, CancellationToken ct = default)
    {
        if (_sumber.TryGetValue(bahanId, out var list))
        {
            return Task.FromResult<IReadOnlyList<SumberPropertiBahan>>(list.OrderBy(x => x.Properti).ToList());
        }

        return Task.FromResult<IReadOnlyList<SumberPropertiBahan>>([]);
    }

    public Task SimpanAsync(Guid bahanId, IReadOnlyList<SumberPropertiBahan> sumber, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sumber);
        _sumber[bahanId] = [.. sumber];
        return Task.CompletedTask;
    }
}
