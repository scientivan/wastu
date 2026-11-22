using System.Collections.Concurrent;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.InMemory;

public sealed class AmbangPekerjaanRepositoryInMemory : IAmbangPekerjaanRepository
{
    private readonly ConcurrentDictionary<Guid, AmbangPekerjaan> _overridePekerjaan = new();
    private readonly ConcurrentDictionary<JenisPekerjaan, AmbangPekerjaan> _ambangJenis = new();
    private readonly IPekerjaanRepository? _pekerjaanRepo;

    public AmbangPekerjaanRepositoryInMemory(IPekerjaanRepository? pekerjaanRepo = null)
    {
        _pekerjaanRepo = pekerjaanRepo;
    }

    public async Task<AmbangPekerjaan?> GetByPekerjaanIdAsync(Guid pekerjaanId, CancellationToken ct = default)
    {
        if (_overridePekerjaan.TryGetValue(pekerjaanId, out var ambang))
        {
            return ambang;
        }

        if (_pekerjaanRepo is not null)
        {
            var pekerjaan = await _pekerjaanRepo.GetByIdAsync(pekerjaanId, ct).ConfigureAwait(false);
            if (pekerjaan is not null)
            {
                if (pekerjaan.OverrideAmbang is not null)
                {
                    return pekerjaan.OverrideAmbang;
                }

                return await GetByJenisAsync(pekerjaan.Jenis, ct).ConfigureAwait(false);
            }
        }

        return null;
    }

    public Task<AmbangPekerjaan?> GetByJenisAsync(JenisPekerjaan jenis, CancellationToken ct = default)
    {
        if (_ambangJenis.TryGetValue(jenis, out var ambang))
        {
            return Task.FromResult<AmbangPekerjaan?>(ambang);
        }

        return Task.FromResult<AmbangPekerjaan?>(AmbangLayakKerja.Untuk(jenis));
    }

    public async Task SimpanOverrideAsync(Guid pekerjaanId, AmbangPekerjaan ambang, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ambang);
        ambang.PastikanValid();
        _overridePekerjaan[pekerjaanId] = ambang;

        if (_pekerjaanRepo is not null)
        {
            var p = await _pekerjaanRepo.GetByIdAsync(pekerjaanId, ct).ConfigureAwait(false);
            if (p is not null)
            {
                var updated = p with { OverrideAmbang = ambang,
                    Status = p.TanggalDikunci.HasValue ? StatusPekerjaan.PerluDitinjau : p.Status,
                    TerakhirDiperbaruiUtc = DateTimeOffset.UtcNow };

                await _pekerjaanRepo.UpdateAsync(updated, ct).ConfigureAwait(false);
            }
        }
    }
}
