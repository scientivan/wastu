using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Emissions;

/// <summary>
/// Memproyeksikan emisi kumulatif satu evaluasi sepanjang horizon tertentu
/// (PRD §F5, §10, PRD-R02, PRD-R11).
/// </summary>
public sealed class EmissionSimulationService : IEmissionSimulationService
{
    private readonly IEvaluasiRepository _evaluasi;
    private readonly KonfigurasiModel _konfigurasi;
    private readonly TimeProvider _timeProvider;

    public EmissionSimulationService(
        IEvaluasiRepository evaluasi,
        IMaterialRepository material)
        : this(evaluasi, material, null, null)
    {
    }

    public EmissionSimulationService(
        IEvaluasiRepository evaluasi,
        IMaterialRepository material,
        KonfigurasiModel? konfigurasi = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(evaluasi);
        ArgumentNullException.ThrowIfNull(material);

        _evaluasi = evaluasi;
        _konfigurasi = konfigurasi ?? KonfigurasiModel.Bawaan;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<SimulasiEmisi> SimulateAsync(
        Guid evaluasiId, int horizonTahun, CancellationToken ct = default)
    {
        if (horizonTahun <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizonTahun), horizonTahun,
                "Horizon simulasi harus bernilai finite dan lebih dari nol tahun.");
        }

        Evaluasi evaluasi = await _evaluasi.GetByIdAsync(evaluasiId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Evaluasi {evaluasiId} tidak ditemukan.");

        var input = evaluasi.Input ?? throw new InvalidOperationException(
            "Evaluasi legacy tidak memiliki masukan asli. Hitung evaluasi baru sebelum simulasi; riwayat lama tetap disimpan.");
        return await SimulateSnapshotAsync(evaluasi, input.Bahan.UsiaPakaiNominalTahun,
            horizonTahun, ct).ConfigureAwait(false);
    }
    /// <summary>Persists the same captured evaluation and nominal age displayed by the desktop.</summary>
    public async Task<SimulasiEmisi> SimulateSnapshotAsync(Evaluasi evaluasi, double usiaNominal,
        int horizonTahun, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var hasil = PerhitunganSimulasi.Simulasikan(evaluasi.Id, evaluasi, usiaNominal,
            horizonTahun, _konfigurasi, _timeProvider.GetUtcNow());
        await _evaluasi.SimpanAsync(hasil, ct).ConfigureAwait(false);
        return hasil;
    }

}
