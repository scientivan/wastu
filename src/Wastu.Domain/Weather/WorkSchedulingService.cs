using System.Collections.Concurrent;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Weather;

/// <summary>
/// Layanan penjadwalan pekerjaan lapangan sensitif cuaca (PRD §F6, §12, T21–T31).
/// </summary>
public sealed class WorkSchedulingService : IWorkSchedulingService
{
    private readonly IPekerjaanRepository _pekerjaanRepo;
    private readonly IProyekRepository _proyekRepo;
    private readonly IPrakiraanCacheRepository _cacheRepo;
    private readonly IDailyForecastClient _forecastClient;
    private readonly IAmbangPekerjaanRepository _ambangRepo;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, Task<HasilPrakiraan>> _pendingRequests = new(StringComparer.Ordinal);

    public WorkSchedulingService(
        IPekerjaanRepository pekerjaanRepo,
        IProyekRepository proyekRepo,
        IPrakiraanCacheRepository cacheRepo,
        IDailyForecastClient forecastClient,
        IAmbangPekerjaanRepository ambangRepo,
        TimeProvider? timeProvider = null)
    {
        _pekerjaanRepo = pekerjaanRepo ?? throw new ArgumentNullException(nameof(pekerjaanRepo));
        _proyekRepo = proyekRepo ?? throw new ArgumentNullException(nameof(proyekRepo));
        _cacheRepo = cacheRepo ?? throw new ArgumentNullException(nameof(cacheRepo));
        _forecastClient = forecastClient ?? throw new ArgumentNullException(nameof(forecastClient));
        _ambangRepo = ambangRepo ?? throw new ArgumentNullException(nameof(ambangRepo));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PenilaianKesiapanHari>> EvaluateDaysAsync(
        Guid pekerjaanId,
        CancellationToken ct = default)
    {
        var detail = await EvaluateDaysDetailedAsync(pekerjaanId, ct).ConfigureAwait(false);
        return detail.Select(d => new PenilaianKesiapanHari(
            Tanggal: d.Tanggal,
            Layak: d.Status == StatusKelayakanCuaca.Layak,
            Alasan: d.Alasan.Count > 0 ? string.Join("; ", d.Alasan) : null)).ToList();
    }

    /// <inheritdoc />
    public Task LockDateAsync(
        Guid pekerjaanId,
        DateOnly tanggal,
        CancellationToken ct = default)
    {
        return LockDateAsync(pekerjaanId, tanggal, new KonfirmasiPenguncian(), ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HasilEvaluasiCuaca>> EvaluateDaysDetailedAsync(
        Guid pekerjaanId,
        CancellationToken ct = default)
    {
        var pekerjaan = await _pekerjaanRepo.GetByIdAsync(pekerjaanId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Pekerjaan dengan ID {pekerjaanId} tidak ditemukan.");

        var proyek = await _proyekRepo.GetByIdAsync(pekerjaan.ProyekId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek dengan ID {pekerjaan.ProyekId} tidak ditemukan.");

        var ambangRepo = await _ambangRepo.GetByPekerjaanIdAsync(pekerjaan.Id, ct).ConfigureAwait(false)
            ?? await _ambangRepo.GetByJenisAsync(pekerjaan.Jenis, ct).ConfigureAwait(false);

        var ambangEfektif = EvaluatorCuaca.TentukanAmbangEfektif(pekerjaan, ambangRepo);
        var nowUtc = _timeProvider.GetUtcNow();

        // 1. Periksa cache yang sesuai revisi lokasi proyek (T22, X04)
        var cache = await _cacheRepo.GetCacheAsync(
            proyek.Latitude, proyek.Longitude, proyek.RevisiLokasi, ct).ConfigureAwait(false);

        HasilPrakiraan? hasil = null;
        bool isStale = false;
        TimeSpan? umurCache = null;

        if (cache is not null && KebijakanCachePrakiraan.ApakahSegar(cache.DiambilPadaUtc, nowUtc))
        {
            // Cache segar (< 6 jam)
            hasil = cache;
            isStale = false;
            umurCache = KebijakanCachePrakiraan.HitungUmur(cache.DiambilPadaUtc, nowUtc);
        }
        else
        {
            // Cache tidak ada atau sudah kedaluwarsa (>= 6 jam).
            // Lakukan pengambilan async dengan coalescing request serentak per (ProyekId, RevisiLokasi) (PRD §11.1)
            string coalesceKey = $"{proyek.Id}:{proyek.RevisiLokasi}";
            try
            {
                hasil = await _pendingRequests.GetOrAdd(
                    coalesceKey,
                    _ => AmbilDanSimpanAsync(proyek, ct)).ConfigureAwait(false);

                isStale = false;
                umurCache = KebijakanCachePrakiraan.HitungUmur(hasil.DiambilPadaUtc, nowUtc);
            }
            catch
            {
                // Bila pengambilan gagal: fallback ke cache lama milik (Proyek, RevisiLokasi) yang sama jika ada (T22)
                if (cache is not null)
                {
                    hasil = cache;
                    isStale = true;
                    umurCache = KebijakanCachePrakiraan.HitungUmur(cache.DiambilPadaUtc, nowUtc);
                }
                else
                {
                    hasil = null;
                }
            }
            finally
            {
                _pendingRequests.TryRemove(coalesceKey, out _);
            }
        }

        // 2. Evaluasi seluruh rentang tanggal pekerjaan (bahkan jika >14 hari, T29)
        var daftarHasil = new List<HasilEvaluasiCuaca>();
        for (var tgl = pekerjaan.TanggalMulai; tgl <= pekerjaan.TanggalSelesai; tgl = tgl.AddDays(1))
        {
            var daily = hasil?.Hari.FirstOrDefault(h => h.Tanggal == tgl);
            var evaluasi = EvaluatorCuaca.EvaluasiHari(
                tanggal: tgl,
                cuaca: daily,
                jenis: pekerjaan.Jenis,
                ambangEfektif: ambangEfektif,
                dataLama: isStale,
                umurCache: umurCache);

            daftarHasil.Add(evaluasi);
        }

        return daftarHasil;
    }

    /// <inheritdoc />
    public async Task LockDateAsync(
        Guid pekerjaanId,
        DateOnly tanggal,
        KonfirmasiPenguncian konfirmasi,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(konfirmasi);

        var pekerjaan = await _pekerjaanRepo.GetByIdAsync(pekerjaanId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Pekerjaan dengan ID {pekerjaanId} tidak ditemukan.");

        var proyek = await _proyekRepo.GetByIdAsync(pekerjaan.ProyekId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek dengan ID {pekerjaan.ProyekId} tidak ditemukan.");

        // 1. Validasi rentang tanggal rencana
        if (tanggal < pekerjaan.TanggalMulai || tanggal > pekerjaan.TanggalSelesai)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tanggal), tanggal,
                $"Tanggal yang dikunci ({tanggal}) berada di luar rentang rencana pekerjaan ({pekerjaan.TanggalMulai}..{pekerjaan.TanggalSelesai}).");
        }

        // 2. Validasi tanggal lampau pada zona waktu proyek (T23)
        var nowUtc = _timeProvider.GetUtcNow();
        var hariIniZonaProyek = KebijakanCachePrakiraan.HariIniPadaZona(nowUtc, proyek.ZonaWaktu);
        if (tanggal < hariIniZonaProyek)
        {
            throw new InvalidOperationException(
                $"Tanggal yang dikunci ({tanggal}) tidak boleh merupakan tanggal lampau pada zona waktu proyek ({proyek.ZonaWaktu ?? "Asia/Jakarta"}). Hari ini adalah {hariIniZonaProyek}.");
        }

        // 3. Evaluasi hari terkait
        var evaluasiList = await EvaluateDaysDetailedAsync(pekerjaanId, ct).ConfigureAwait(false);
        var evaluasi = evaluasiList.FirstOrDefault(e => e.Tanggal == tanggal)
            ?? throw new InvalidOperationException($"Evaluasi cuaca untuk tanggal {tanggal} tidak ditemukan.");

        // 4. Tolak penguncian cuaca bila data tidak tersedia (F6-05, T29)
        if (evaluasi.Status == StatusKelayakanCuaca.DataTidakTersedia)
        {
            throw new InvalidOperationException(
                $"Tanggal {tanggal} tidak dapat dikunci karena prakiraan cuaca tidak tersedia atau di luar cakupan.");
        }

        // 5. Konfirmasi persetujuan risiko jika TidakLayak (T30)
        if (evaluasi.Status == StatusKelayakanCuaca.TidakLayak && !konfirmasi.SetujuiRisiko)
        {
            throw new InvalidOperationException(
                $"Tanggal {tanggal} berstatus TidakLayak ({string.Join("; ", evaluasi.Alasan)}). Diperlukan persetujuan risiko eksplisit untuk mengunci.");
        }

        // 6. Konfirmasi persetujuan data lama jika cache >= 6 jam (T30)
        if (evaluasi.DataLama && !konfirmasi.SetujuiDataLama)
        {
            throw new InvalidOperationException(
                $"Prakiraan cuaca untuk tanggal {tanggal} berumur >= 6 jam. Diperlukan konfirmasi persetujuan data lama untuk mengunci.");
        }

        // 7. Simpan snapshot keputusan jadwal permanen dan perbarui status pekerjaan (PRD §12.2, T30)
        var snapshot = new KeputusanJadwal(
            Tanggal: tanggal,
            Status: evaluasi.Status,
            Catatan: evaluasi.Alasan,
            Terkunci: true,
            DikunciPadaUtc: nowUtc,
            Konfirmasi: konfirmasi,
            PersetujuanRisiko: konfirmasi.SetujuiRisiko,
            KonfirmasiDataLama: konfirmasi.SetujuiDataLama,
            UmurDataSaatKunci: evaluasi.UmurCache,
            AmbangEfektif: evaluasi.AmbangEfektif,
            DiambilPadaUtc: evaluasi.UmurCache.HasValue ? nowUtc - evaluasi.UmurCache.Value : null,
            DiputuskanPadaUtc: nowUtc,
            RevisiLokasiSaatKunci: proyek.RevisiLokasi);

        var pekerjaanDiperbarui = pekerjaan with
        {
            TanggalDikunci = tanggal,
            Status = StatusPekerjaan.Terkunci,
            Konfirmasi = konfirmasi,
            TerakhirDiperbaruiUtc = nowUtc,
            RevisiLokasiSaatKunci = proyek.RevisiLokasi,
            KeputusanTerakhir = snapshot
        };

        await _pekerjaanRepo.UpdateAsync(pekerjaanDiperbarui, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Meninjau ulang status pekerjaan yang terkunci (T31).
    /// Bila revisi lokasi berubah, ambang override berubah, atau cuaca baru memburuk,
    /// pekerjaan ditandai <see cref="StatusPekerjaan.PerluDitinjau"/> tanpa menghapus/memindahkan
    /// tanggal terkunci atau mengubah snapshot keputusan terakhir.
    /// </summary>
    public async Task<Pekerjaan?> TinjauUlangPekerjaanAsync(
        Guid pekerjaanId,
        CancellationToken ct = default)
    {
        var pekerjaan = await _pekerjaanRepo.GetByIdAsync(pekerjaanId, ct).ConfigureAwait(false);
        if (pekerjaan is null || pekerjaan.Status != StatusPekerjaan.Terkunci || !pekerjaan.TanggalDikunci.HasValue)
        {
            return pekerjaan;
        }

        var proyek = await _proyekRepo.GetByIdAsync(pekerjaan.ProyekId, ct).ConfigureAwait(false);
        if (proyek is null)
        {
            return pekerjaan;
        }

        bool lokasiBerubah = pekerjaan.RevisiLokasiSaatKunci.HasValue
            && proyek.RevisiLokasi != pekerjaan.RevisiLokasiSaatKunci.Value;

        var repoAmbang = await _ambangRepo.GetByPekerjaanIdAsync(pekerjaan.Id, ct).ConfigureAwait(false)
            ?? await _ambangRepo.GetByJenisAsync(pekerjaan.Jenis, ct).ConfigureAwait(false);
        var ambangSekarang = EvaluatorCuaca.TentukanAmbangEfektif(pekerjaan, repoAmbang);

        bool ambangBerubah = pekerjaan.KeputusanTerakhir?.AmbangEfektif is not null
            && !pekerjaan.KeputusanTerakhir.AmbangEfektif.Equals(ambangSekarang);

        // Periksa evaluasi cuaca terkini pada tanggal terkunci
        var evaluasiList = await EvaluateDaysDetailedAsync(pekerjaanId, ct).ConfigureAwait(false);
        var evaluasiHariKunci = evaluasiList.FirstOrDefault(e => e.Tanggal == pekerjaan.TanggalDikunci.Value);

        bool cuacaMemburuk = false;
        if (evaluasiHariKunci is not null && pekerjaan.KeputusanTerakhir is not null)
        {
            static int TingkatKeparahan(StatusKelayakanCuaca s) => s switch
            {
                StatusKelayakanCuaca.Layak => 0,
                StatusKelayakanCuaca.Waspada => 1,
                StatusKelayakanCuaca.TidakLayak => 2,
                StatusKelayakanCuaca.DataTidakTersedia => 3,
                _ => 4
            };

            cuacaMemburuk = TingkatKeparahan(evaluasiHariKunci.Status) > TingkatKeparahan(pekerjaan.KeputusanTerakhir.Status);
        }

        if (lokasiBerubah || ambangBerubah || cuacaMemburuk)
        {
            var pekerjaanPerluDitinjau = pekerjaan with
            {
                Status = StatusPekerjaan.PerluDitinjau,
                TerakhirDiperbaruiUtc = _timeProvider.GetUtcNow()
            };

            await _pekerjaanRepo.UpdateAsync(pekerjaanPerluDitinjau, ct).ConfigureAwait(false);
            return pekerjaanPerluDitinjau;
        }

        return pekerjaan;
    }

    private async Task<HasilPrakiraan> AmbilDanSimpanAsync(Proyek proyek, CancellationToken ct)
    {
        var raw = await _forecastClient.GetForecastWithMetadataAsync(
            proyek.Latitude, proyek.Longitude, 14, ct).ConfigureAwait(false);

        var hasil = new HasilPrakiraan(
            Hari: raw.Hari,
            ZonaWaktu: raw.ZonaWaktu,
            DiambilPadaUtc: raw.DiambilPadaUtc,
            Penyedia: raw.Penyedia,
            RevisiLokasi: proyek.RevisiLokasi,
            DataParsial: raw.DataParsial);

        await _cacheRepo.SimpanCacheAsync(proyek.Latitude, proyek.Longitude, hasil, ct).ConfigureAwait(false);
        return hasil;
    }
}
