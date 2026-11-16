using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Projects;

/// <summary>
/// Manajemen profil proyek dan komponennya (PRD §F1).
/// </summary>
/// <remarks>
/// Mengganti lokasi adalah satu-satunya operasi di sini yang menyentuh layanan lain:
/// PRD §F1 mensyaratkan perubahan lokasi memicu pengambilan ulang profil iklim,
/// karena seluruh sub-skor §11.1 dihitung dari iklim. Kalau langkah itu dilewati,
/// pengguna akan melihat peringkat bahan untuk lokasi lama yang tidak lagi relevan.
/// </remarks>
public sealed class ProjectService : IProjectService
{
    private readonly IProyekRepository _proyek;
    private readonly IClimateAnalysisService _iklim;

    public ProjectService(IProyekRepository proyek, IClimateAnalysisService iklim)
    {
        ArgumentNullException.ThrowIfNull(proyek);
        ArgumentNullException.ThrowIfNull(iklim);

        _proyek = proyek;
        _iklim = iklim;
    }

    public async Task<Proyek> CreateProjectAsync(
        string nama, string kota, double latitude, double longitude, CancellationToken ct = default)
    {
        // Konstruktor Proyek menolak nama kosong, koordinat kosong, dan koordinat
        // di luar wilayah Indonesia. Semua itu dicek sebelum apa pun disimpan.
        var proyek = new Proyek(Guid.NewGuid(), nama, kota, latitude, longitude);

        await _proyek.AddAsync(proyek, ct).ConfigureAwait(false);

        return proyek;
    }

    /// <summary>
    /// Mengganti lokasi proyek lalu mengambil ulang profil iklimnya (PRD §F1).
    /// </summary>
    /// <remarks>
    /// Urutannya penting: lokasi baru disimpan lebih dulu, sehingga saat
    /// <see cref="IClimateAnalysisService"/> membaca koordinat untuk pengambilan data,
    /// yang terbaca adalah koordinat yang baru dan bukan yang lama.
    /// </remarks>
    public async Task UpdateLocationAsync(
        Guid proyekId, double latitude, double longitude, CancellationToken ct = default)
    {
        Proyek lama = await _proyek.GetByIdAsync(proyekId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Proyek {proyekId} tidak ditemukan.");

        // Sengaja membuat Proyek baru lewat konstruktor, bukan memakai ekspresi with.
        // Ekspresi with pada record menyalin field apa adanya dan TIDAK menjalankan
        // ulang validasi konstruktor, sehingga batas wilayah Indonesia akan lolos
        // tanpa diperiksa setiap kali lokasi diubah.
        Proyek baru = new(lama.Id, lama.Nama, lama.Kota, latitude, longitude);

        await _proyek.UpdateAsync(baru, ct).ConfigureAwait(false);
        await _iklim.RefreshProfileAsync(proyekId, ct).ConfigureAwait(false);
    }

    public async Task AddComponentAsync(
        Guid proyekId, JenisKomponen jenis, double luasM2, CancellationToken ct = default)
    {
        if (await _proyek.GetByIdAsync(proyekId, ct).ConfigureAwait(false) is null)
        {
            throw new KeyNotFoundException($"Proyek {proyekId} tidak ditemukan.");
        }

        var komponen = new Komponen(Guid.NewGuid(), proyekId, jenis, luasM2);

        await _proyek.AddKomponenAsync(komponen, ct).ConfigureAwait(false);
    }
}