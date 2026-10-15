using System.Collections.Concurrent;
using Wastu.Domain;
using Wastu.Domain.Abstractions;

namespace Wastu.InMemory;

/// <summary>
/// Sumber bahan di dalam memori proses.
/// </summary>
/// <remarks>
/// <para>
/// Adapter ini bukan pengganti <c>Wastu.Data</c> yang memakai EF Core + Npgsql.
/// Gunanya supaya alur F2 dan F4 bisa dijalankan, didemo, dan diuji tanpa
/// database, dan supaya ada contoh konkret port yang harus dipenuhi software
/// architect sebelum menulis adapter EF Core-nya.
/// </para>
/// <para>
/// Setiap port repository punya kelasnya sendiri karena
/// <c>IMaterialRepository.GetByIdAsync</c> dan
/// <c>IEvaluasiRepository.GetByIdAsync</c> bertanda tangan sama tetapi mengembalikan
/// tipe berbeda, jadi keduanya tidak bisa diimplementasi dalam satu kelas.
/// </para>
/// <para>
/// Kamus memakai <see cref="ConcurrentDictionary{TKey,TValue}"/> karena kontrak
/// port memakai Task dan pemanggil bisa berjalan paralel. Tidak ada transaction dan
/// tidak ada persistensi: keluar dari proses berarti semua hilang.
/// </para>
/// </remarks>
public sealed class MaterialRepositoryInMemory : IMaterialRepository
{
    private readonly ConcurrentDictionary<Guid, Bahan> _bahan = new();

    public MaterialRepositoryInMemory()
    {
    }

    /// <summary>Membuat adapter yang sudah memuat katalog bahan.</summary>
    public MaterialRepositoryInMemory(IEnumerable<Bahan> katalog) => Seed(katalog);

    /// <summary>Menambah atau mengganti bahan pada katalog.</summary>
    public MaterialRepositoryInMemory Seed(IEnumerable<Bahan> katalog)
    {
        ArgumentNullException.ThrowIfNull(katalog);

        foreach (Bahan item in katalog)
        {
            _bahan[item.Id] = item;
        }

        return this;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Bahan>> ListByKindAsync(JenisKomponen jenis, CancellationToken ct = default)
    {
        IReadOnlyList<Bahan> hasil = _bahan.Values
            .Where(b => b.Jenis == jenis)
            .OrderBy(b => b.Nama, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(hasil);
    }

    /// <inheritdoc />
    public Task<Bahan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_bahan.GetValueOrDefault(id));
}