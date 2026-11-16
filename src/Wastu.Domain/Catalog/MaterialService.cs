using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Catalog;

/// <summary>
/// Katalog bahan untuk ditampilkan dan dipilih pengguna (PRD §F2).
/// </summary>
/// <remarks>
/// Layanan ini sengaja tipis. Seluruh aturan penilaian ada di
/// <c>SuitabilityScoringService</c>; yang dijaga di sini hanya bentuk katalog
/// yang dilihat pengguna, yaitu filtering per jenis komponen dan sumber data yang
/// selalu ikut terbawa karena PRD §1 dan §11.4 menjadikan transparansi sumber
/// sebagai komitmen, bukan atribut opsional.
/// </remarks>
public sealed class MaterialService : IMaterialService
{
    private readonly IMaterialRepository _material;

    public MaterialService(IMaterialRepository material)
    {
        ArgumentNullException.ThrowIfNull(material);
        _material = material;
    }

    public Task<IReadOnlyList<Bahan>> ListByKindAsync(JenisKomponen jenis, CancellationToken ct = default)
        => _material.ListByKindAsync(jenis, ct);

    /// <summary>
    /// Mengambil satu bahan.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Bahan tidak ada. Kontrak <see cref="IMaterialService.GetByIdAsync"/> mengembalikan
    /// tipe non-nullable, jadi kondisi ini harus gagal dengan jelas dan bukan
    /// dikembalikan sebagai null yang nanti meledak di tempat lain.
    /// </exception>
    public async Task<Bahan> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        Bahan? bahan = await _material.GetByIdAsync(id, ct).ConfigureAwait(false);

        return bahan ?? throw new KeyNotFoundException(
            $"Bahan {id} tidak ada di katalog. Periksa kembali id yang dipakai.");
    }
}