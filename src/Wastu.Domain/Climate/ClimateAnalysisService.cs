using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Climate;

/// <summary>
/// Analisis iklim lokasi proyek (PRD §F3), dengan orkestrasi cache sesuai PRD §8.4.
/// </summary>
/// <remarks>
/// <para>
/// PRD §8.4 menetapkan dua perilaku cache yang berbeda dan tidak boleh dicampur.
/// Profil iklim historis <b>disimpan permanen</b> setelah diambil sekali, sehingga
/// pengguna tetap bisa melihat perbandingan bahan tanpa koneksi selama tidak
/// meminta pembaruan. Cache berumur enam jam yang disebut di §8.4 milik prakiraan
/// harian §F6, bukan profil iklim, jadi layanan ini sengaja tidak punya masa
/// berlaku sama sekali.
/// </para>
/// <para>
/// Yang dilayani di sini adalah orkestrasi, bukan pemanggilan HTTP.
/// <see cref="IHistoricalClimateClient"/> dipegang software architect karena
/// Open-Meteo adalah integrasi pihak ketiga miliknya (§8).
/// </para>
/// </remarks>
public sealed class ClimateAnalysisService : IClimateAnalysisService
{
    private readonly IProyekRepository _proyek;
    private readonly IHistoricalClimateClient _klien;

    public ClimateAnalysisService(IProyekRepository proyek, IHistoricalClimateClient klien)
    {
        ArgumentNullException.ThrowIfNull(proyek);
        ArgumentNullException.ThrowIfNull(klien);

        _proyek = proyek;
        _klien = klien;
    }

    /// <summary>
    /// Mengembalikan profil tersimpan, atau mengambilnya sekali bila belum ada.
    /// </summary>
    /// <remarks>
    /// Tidak pernah memanggil API bila profil sudah ada, karena §8.4 menyatakan
    /// profil disimpan permanen dan hanya diambil ulang atas permintaan pengguna.
    /// </remarks>
    public async Task<ProfilIklim> GetOrFetchProfileAsync(Guid proyekId, CancellationToken ct = default)
    {
        Proyek proyek = await AmbilProyekAsync(proyekId, ct).ConfigureAwait(false);

        ProfilIklim? tersimpan = await _proyek.GetProfilIklimAsync(proyekId, ct).ConfigureAwait(false);
        if (tersimpan is not null)
        {
            return tersimpan;
        }

        ProfilIklim baru = await AmbilDariKlienAsync(proyek, ct).ConfigureAwait(false);
        await _proyek.SimpanProfilIklimAsync(proyekId, baru, ct).ConfigureAwait(false);

        return baru;
    }

    /// <summary>
    /// Mengambil profil baru dari klien dan menimpa yang tersimpan, dengan
    /// fallback ke data lama bila panggilan gagal (PRD §8.4).
    /// </summary>
    public async Task<ProfilIklim> RefreshProfileAsync(Guid proyekId, CancellationToken ct = default)
    {
        Proyek proyek = await AmbilProyekAsync(proyekId, ct).ConfigureAwait(false);
        ProfilIklim? lama = await _proyek.GetProfilIklimAsync(proyekId, ct).ConfigureAwait(false);

        ProfilIklim baru;
        try
        {
            baru = await AmbilDariKlienAsync(proyek, ct).ConfigureAwait(false);
        }
        catch (Exception galat) when (galat is not OperationCanceledException)
        {
            if (lama is null)
            {
                throw new GagalMemperbaruiProfilIklimException(
                    $"Gagal mengambil data iklim untuk {proyek.Kota} dan belum ada data tersimpan "
                    + "sehingga tidak ada yang bisa ditampilkan. Periksa koneksi lalu coba lagi.",
                    profilTersimpan: null,
                    penyebab: galat);
            }

            // Data lama tetap dikembalikan bersama pesannya supaya antarmuka bisa
            // menampilkan keduanya sekaligus, bukan memilih salah satu.
            throw new GagalMemperbaruiProfilIklimException(
                $"Gagal mengambil data iklim terbaru untuk {proyek.Kota}. Data yang ditampilkan "
                + "adalah hasil analisis sebelumnya dan belum termasuk perubahan terbaru.",
                lama,
                galat);
        }

        await _proyek.SimpanProfilIklimAsync(proyekId, baru, ct).ConfigureAwait(false);

        return baru;
    }

    private async Task<Proyek> AmbilProyekAsync(Guid proyekId, CancellationToken ct)
        => await _proyek.GetByIdAsync(proyekId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek {proyekId} tidak ditemukan.");

    private Task<ProfilIklim> AmbilDariKlienAsync(Proyek proyek, CancellationToken ct)
        => _klien.GetAnnualProfileAsync(proyek.Latitude, proyek.Longitude, ct);
}