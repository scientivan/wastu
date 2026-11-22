using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Climate;

/// <summary>
/// Analisis iklim lokasi proyek (PRD §F3), dengan orkestrasi cache sesuai PRD §8.4.
/// </summary>
public sealed class ClimateAnalysisService : IClimateAnalysisService
{
    private readonly IProyekRepository _proyek;
    private readonly IHistoricalClimateClient _klien;
    private readonly TimeProvider _timeProvider;

    public ClimateAnalysisService(IProyekRepository proyek, IHistoricalClimateClient klien)
        : this(proyek, klien, TimeProvider.System)
    {
    }

    public ClimateAnalysisService(
        IProyekRepository proyek,
        IHistoricalClimateClient klien,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(proyek);
        ArgumentNullException.ThrowIfNull(klien);

        _proyek = proyek;
        _klien = klien;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Mengembalikan profil tersimpan, atau mengambilnya sekali bila belum ada.
    /// Bila data tersimpan berasal dari revisi koordinat lama, mengambil ulang profil baru.
    /// </summary>
    public async Task<ProfilIklim> GetOrFetchProfileAsync(Guid proyekId, CancellationToken ct = default)
    {
        Proyek proyekAwal = await AmbilProyekAsync(proyekId, ct).ConfigureAwait(false);

        ProfilIklim? tersimpan = await _proyek.GetProfilIklimAsync(proyekId, ct).ConfigureAwait(false);
        if (tersimpan is not null)
        {
            int? revTersimpan = tersimpan.Metadata?.RevisiLokasi ?? (tersimpan.RevisiLokasi > 0 ? tersimpan.RevisiLokasi : null);
            if (revTersimpan is null || revTersimpan == proyekAwal.RevisiLokasi)
            {
                return tersimpan;
            }
        }

        ProfilIklim mentah = await AmbilDariKlienAsync(proyekAwal, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        Proyek? proyekTerkini = await _proyek.GetByIdAsync(proyekId, ct).ConfigureAwait(false);
        if (proyekTerkini is null ||
            proyekTerkini.RevisiLokasi != proyekAwal.RevisiLokasi ||
            proyekTerkini.Latitude != proyekAwal.Latitude ||
            proyekTerkini.Longitude != proyekAwal.Longitude)
        {
            throw new OperationCanceledException(
                "Pengambilan profil iklim dibatalkan karena revisi lokasi proyek telah berubah atau proyek telah dihapus.");
        }

        ProfilIklim baru = BangunProfilDenganMetadata(mentah, proyekAwal);
        await _proyek.SimpanProfilIklimAsync(proyekId, baru, ct, proyekTerkini).ConfigureAwait(false);

        return baru;
    }

    /// <summary>
    /// Mengambil profil baru dari klien dan menimpa yang tersimpan, dengan
    /// fallback ke data lama bila panggilan gagal (PRD §8.4).
    /// </summary>
    public async Task<ProfilIklim> RefreshProfileAsync(Guid proyekId, CancellationToken ct = default)
    {
        Proyek proyekAwal = await AmbilProyekAsync(proyekId, ct).ConfigureAwait(false);
        ProfilIklim? lama = await _proyek.GetProfilIklimAsync(proyekId, ct).ConfigureAwait(false);

        ProfilIklim mentah;
        try
        {
            mentah = await AmbilDariKlienAsync(proyekAwal, ct).ConfigureAwait(false);
        }
        catch (Exception galat) when (galat is not OperationCanceledException)
        {
            if (lama is null)
            {
                throw new GagalMemperbaruiProfilIklimException(
                    $"Gagal mengambil data iklim untuk {proyekAwal.Kota} dan belum ada data tersimpan "
                    + "sehingga tidak ada yang bisa ditampilkan. Periksa koneksi lalu coba lagi.",
                    profilTersimpan: null,
                    penyebab: galat);
            }

            int? revLama = lama.Metadata?.RevisiLokasi ?? (lama.RevisiLokasi > 0 ? lama.RevisiLokasi : null);
            bool lokasiSama = revLama is null || revLama == proyekAwal.RevisiLokasi;

            if (lokasiSama)
            {
                // Fallback untuk lokasi yang SAMA (T08): profil ApiHistoris lama tetap utuh
                throw new GagalMemperbaruiProfilIklimException(
                    $"Gagal mengambil data iklim terbaru untuk {proyekAwal.Kota}. Data yang ditampilkan "
                    + "adalah hasil analisis sebelumnya dan belum termasuk perubahan terbaru.",
                    lama,
                    galat);
            }
            else
            {
                // Profil lama berasal dari lokasi SEBELUMNYA (T07):
                // Tandai dengan TidakCocokLokasi dan sertakan peringatan eksplisit
                var metaMismatched = (lama.Metadata ?? new MetadataIklim()) with
                {
                    KualitasCache = KualitasCacheIklim.TidakCocokLokasi,
                    StatusPengambilan = StatusPengambilanIklim.GagalMemperbarui
                };
                var fallbackMismatched = new ProfilIklim(
                    lama.CurahHujanTahunanMm,
                    lama.KelembapanRataPct,
                    lama.SuhuRataC,
                    lama.JarakPesisirKm,
                    lama.TingkatKorosi,
                    metaMismatched,
                    lama.RevisiLokasi,
                    lama.Provenance);

                throw new GagalMemperbaruiProfilIklimException(
                    $"Gagal mengambil data iklim terbaru untuk {proyekAwal.Kota}. Data tersimpan berasal dari lokasi sebelumnya dan tidak mewakili koordinat baru. Periksa koneksi lalu coba lagi.",
                    fallbackMismatched,
                    galat);
            }
        }

        ct.ThrowIfCancellationRequested();

        Proyek? proyekTerkini = await _proyek.GetByIdAsync(proyekId, ct).ConfigureAwait(false);
        if (proyekTerkini is null ||
            proyekTerkini.RevisiLokasi != proyekAwal.RevisiLokasi ||
            proyekTerkini.Latitude != proyekAwal.Latitude ||
            proyekTerkini.Longitude != proyekAwal.Longitude)
        {
            throw new OperationCanceledException(
                "Pengambilan profil iklim dibatalkan karena revisi lokasi proyek telah berubah atau proyek telah dihapus.");
        }

        ProfilIklim baru = BangunProfilDenganMetadata(mentah, proyekAwal);
        await _proyek.SimpanProfilIklimAsync(proyekId, baru, ct, proyekTerkini).ConfigureAwait(false);

        return baru;
    }

    private ProfilIklim BangunProfilDenganMetadata(ProfilIklim mentah, Proyek proyek)
    {
        var meta = mentah.Metadata;
        var penyedia = meta?.Penyedia;
        string provider = string.IsNullOrWhiteSpace(penyedia) || penyedia == "Open-Meteo"
            ? "Open-Meteo Historical Weather API"
            : penyedia;

        var metadataLengkap = new MetadataIklim(
            PeriodeAwal: meta?.PeriodeAwal,
            PeriodeAkhir: meta?.PeriodeAkhir,
            Penyedia: provider,
            Model: meta?.Model,
            DiambilPadaUtc: meta?.DiambilPadaUtc ?? _timeProvider.GetUtcNow(),
            ZonaWaktu: meta?.ZonaWaktu ?? proyek.ZonaWaktu ?? "Asia/Jakarta",
            MetodePesisir: meta?.MetodePesisir ?? "Haversine Proximity",
            Provenance: ProvenanceIklim.ApiHistoris,
            StatusPengambilan: StatusPengambilanIklim.Tersedia,
            RevisiLokasi: proyek.RevisiLokasi,
            KualitasCache: KualitasCacheIklim.CocokLokasi,
            VersiAlgoritmePesisir: JarakPesisirCalculator.VersiAlgoritme);

        return new ProfilIklim(
            curahHujanTahunanMm: mentah.CurahHujanTahunanMm,
            kelembapanRataPct: mentah.KelembapanRataPct,
            suhuRataC: mentah.SuhuRataC,
            jarakPesisirKm: mentah.JarakPesisirKm,
            tingkatKorosi: mentah.TingkatKorosi,
            metadata: metadataLengkap,
            revisiLokasi: proyek.RevisiLokasi,
            provenance: ProvenanceIklim.ApiHistoris);
    }

    private async Task<Proyek> AmbilProyekAsync(Guid proyekId, CancellationToken ct)
        => await _proyek.GetByIdAsync(proyekId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek {proyekId} tidak ditemukan.");

    private Task<ProfilIklim> AmbilDariKlienAsync(Proyek proyek, CancellationToken ct)
        => _klien.GetAnnualProfileAsync(proyek.Latitude, proyek.Longitude, ct);
}
