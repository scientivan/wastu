using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Emissions;

/// <summary>
/// Memproyeksikan emisi kumulatif satu evaluasi sepanjang horizon tertentu
/// (PRD §F5, §10).
/// </summary>
/// <remarks>
/// Kelas ini hanya mencari dua hal yang dibutuhkan perhitungan: skor kesesuaian
/// dan emisi awal dari evaluasi, serta usia pakai nominal dari bahan. Selebihnya
/// ada di <see cref="PerhitunganSimulasi"/> yang deterministik dan teruji tanpa
/// database.
/// </remarks>
public sealed class EmissionSimulationService : IEmissionSimulationService
{
    private readonly IEvaluasiRepository _evaluasi;
    private readonly IMaterialRepository _material;

    public EmissionSimulationService(IEvaluasiRepository evaluasi, IMaterialRepository material)
    {
        ArgumentNullException.ThrowIfNull(evaluasi);
        ArgumentNullException.ThrowIfNull(material);

        _evaluasi = evaluasi;
        _material = material;
    }

    public async Task<SimulasiEmisi> SimulateAsync(
        Guid evaluasiId, int horizonTahun, CancellationToken ct = default)
    {
        Evaluasi evaluasi = await _evaluasi.GetByIdAsync(evaluasiId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Evaluasi {evaluasiId} tidak ditemukan.");

        Bahan bahan = await _material.GetByIdAsync(evaluasi.BahanId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Bahan {evaluasi.BahanId} tidak ditemukan.");

        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            evaluasiId, evaluasi, bahan.UsiaPakaiNominalTahun, horizonTahun);

        await _evaluasi.SimpanAsync(hasil, ct).ConfigureAwait(false);

        return hasil;
    }
}