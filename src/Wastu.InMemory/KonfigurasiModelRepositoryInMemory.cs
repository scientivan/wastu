using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

public sealed class KonfigurasiModelRepositoryInMemory : IKonfigurasiModelRepository
{
    private readonly ConcurrentDictionary<string, KonfigurasiModel> _configs = new();
    private string _activeVersion = "v2.0";

    public KonfigurasiModelRepositoryInMemory()
    {
        _configs["v2.0"] = KonfigurasiModel.Bawaan;
    }

    public Task<KonfigurasiModel> GetActiveAsync(CancellationToken ct = default)
    {
        if (_configs.TryGetValue(_activeVersion, out var config))
        {
            return Task.FromResult(config);
        }

        return Task.FromResult(KonfigurasiModel.Bawaan);
    }

    public Task<KonfigurasiModel?> GetByVersiAsync(string versiModel, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versiModel);
        return Task.FromResult(_configs.GetValueOrDefault(versiModel));
    }

    public Task SimpanAsync(KonfigurasiModel konfigurasi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(konfigurasi);
        _configs[konfigurasi.VersiModel] = konfigurasi;
        _activeVersion = konfigurasi.VersiModel;
        return Task.CompletedTask;
    }
}
