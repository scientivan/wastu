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

    public Task<IReadOnlyList<Proyek>> ListAsync(CancellationToken ct = default) => _proyek.ListAsync(ct);

    public Task DeleteAsync(Guid proyekId, CancellationToken ct = default) => _proyek.DeleteAsync(proyekId, ct);

    public async Task<Proyek> CreateProjectAsync(
        string nama, string kota, double latitude, double longitude, CancellationToken ct = default)
    {
        // Konstruktor Proyek menolak nama kosong, koordinat kosong, dan koordinat
        // di luar wilayah Indonesia. Semua itu dicek sebelum apa pun disimpan.
        var proyek = new Proyek(Guid.NewGuid(), nama, kota, latitude, longitude);

        await _proyek.AddAsync(proyek, ct).ConfigureAwait(false);

        return proyek;
    }

    public async Task<Proyek> CreateProjectAsync(string nama, string kota, double latitude, double longitude,
        double luasDinding, double luasAtap, CancellationToken ct = default)
    {
        var proyek = new Proyek(Guid.NewGuid(), nama, kota, latitude, longitude);
        await _proyek.AddAsync(proyek, ct, Bidang(proyek.Id, [], luasDinding, luasAtap)).ConfigureAwait(false);
        return proyek;
    }

    public async Task<Proyek> CreateProjectAsync(string nama, string kota, double latitude, double longitude,
        IReadOnlyList<InputKomponen> komponen, CancellationToken ct = default)
    {
        ValidasiKomponenInput(komponen);
        var proyek = new Proyek(Guid.NewGuid(), nama, kota, latitude, longitude);
        var daftarKomponen = komponen.Select(k => new Komponen(Guid.NewGuid(), proyek.Id, k.Jenis, k.LuasM2)).ToArray();
        await _proyek.AddAsync(proyek, ct, daftarKomponen).ConfigureAwait(false);
        return proyek;
    }

    public async Task<(Proyek Proyek, ProfilIklim? Profil, string? Peringatan)> CreateWithClimateAsync(
        string nama, string kota, double latitude, double longitude,
        IReadOnlyList<InputKomponen> komponen, CancellationToken ct = default)
    {
        var proyek = await CreateProjectAsync(nama, kota, latitude, longitude, komponen, ct).ConfigureAwait(false);
        try
        {
            var profil = await _iklim.GetOrFetchProfileAsync(proyek.Id, ct).ConfigureAwait(false);
            return (proyek, profil, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (proyek, null, ex.Message);
        }
    }

    public async Task<ProfilIklim?> UpdateProjectAsync(Guid proyekId, string nama, string kota, double latitude, double longitude,
        double luasDinding, double luasAtap, CancellationToken ct = default)
    {
        Proyek lama = await AmbilAsync(proyekId, ct).ConfigureAwait(false);
        int revisi = (lama.Latitude != latitude || lama.Longitude != longitude) ? lama.RevisiLokasi + 1 : lama.RevisiLokasi;
        var baru = new Proyek(proyekId, nama, kota, latitude, longitude, revisi, lama.ZonaWaktu);
        var komponen = await _proyek.ListKomponenAsync(proyekId, ct).ConfigureAwait(false);
        return await SimpanPerubahanAsync(lama, baru, Bidang(proyekId, komponen, luasDinding, luasAtap), ct)
            .ConfigureAwait(false);
    }

    public async Task<ProfilIklim?> UpdateProjectAsync(Guid proyekId, string nama, string kota, double latitude, double longitude,
        IReadOnlyList<InputKomponen> komponen, CancellationToken ct = default)
    {
        ValidasiKomponenInput(komponen);
        Proyek lama = await AmbilAsync(proyekId, ct).ConfigureAwait(false);
        int revisi = (lama.Latitude != latitude || lama.Longitude != longitude) ? lama.RevisiLokasi + 1 : lama.RevisiLokasi;
        var baru = new Proyek(proyekId, nama, kota, latitude, longitude, revisi, lama.ZonaWaktu);
        var komponenLama = await _proyek.ListKomponenAsync(proyekId, ct).ConfigureAwait(false);
        var daftarKomponen = komponen.Select(k =>
        {
            Guid id = komponenLama.FirstOrDefault(x => x.Jenis == k.Jenis)?.Id ?? Guid.NewGuid();
            return new Komponen(id, proyekId, k.Jenis, k.LuasM2);
        }).ToArray();
        return await SimpanPerubahanAsync(lama, baru, daftarKomponen, ct).ConfigureAwait(false);
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
        Proyek lama = await AmbilAsync(proyekId, ct).ConfigureAwait(false);

        // Sengaja membuat Proyek baru lewat konstruktor, bukan memakai ekspresi with.
        // Ekspresi with pada record menyalin field apa adanya dan TIDAK menjalankan
        // ulang validasi konstruktor, sehingga batas wilayah Indonesia akan lolos
        // tanpa diperiksa setiap kali lokasi diubah.
        int revisi = (lama.Latitude != latitude || lama.Longitude != longitude) ? lama.RevisiLokasi + 1 : lama.RevisiLokasi;
        Proyek baru = new(lama.Id, lama.Nama, lama.Kota, latitude, longitude, revisi, lama.ZonaWaktu);

        await SimpanPerubahanAsync(lama, baru, null, ct).ConfigureAwait(false);
    }

    private static void ValidasiKomponenInput(IReadOnlyList<InputKomponen> komponen)
    {
        ArgumentNullException.ThrowIfNull(komponen);
        if (komponen.Count == 0)
        {
            throw new ArgumentException("Proyek harus memiliki minimal satu komponen aktif.", nameof(komponen));
        }

        if (komponen.Select(k => k.Jenis).Distinct().Count() != komponen.Count)
        {
            throw new ArgumentException("Jenis komponen dalam proyek tidak boleh duplikat.", nameof(komponen));
        }

        foreach (var k in komponen)
        {
            if (k is null)
            {
                throw new ArgumentException("Komponen masukan tidak boleh null.", nameof(komponen));
            }
            if (!double.IsFinite(k.LuasM2) || k.LuasM2 <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(komponen), k.LuasM2, "Luas komponen harus bernilai finite dan lebih besar dari 0 m².");
            }
        }
    }

    private async Task<ProfilIklim?> SimpanPerubahanAsync(Proyek lama, Proyek baru, IReadOnlyList<Komponen>? komponen,
        CancellationToken ct)
    {
        await _proyek.UpdateAsync(baru, ct, komponen).ConfigureAwait(false);
        if (lama.Latitude == baru.Latitude && lama.Longitude == baru.Longitude)
            return null;

        try
        {
            return await _iklim.RefreshProfileAsync(baru.Id, ct).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not (OperationCanceledException or GagalMemperbaruiProfilIklimException))
        {
            throw new GagalMemperbaruiProfilIklimException("Profil iklim lokasi baru belum dapat disimpan.", null, e);
        }
    }

    private async Task<Proyek> AmbilAsync(Guid proyekId, CancellationToken ct) =>
        await _proyek.GetByIdAsync(proyekId, ct).ConfigureAwait(false)
        ?? throw new KeyNotFoundException($"Proyek {proyekId} tidak ditemukan.");

    private static Komponen[] Bidang(Guid proyekId, IReadOnlyList<Komponen> lama, double dinding, double atap) =>
    [
        new(lama.FirstOrDefault(k => k.Jenis == JenisKomponen.Dinding)?.Id ?? Guid.NewGuid(),
            proyekId, JenisKomponen.Dinding, dinding),
        new(lama.FirstOrDefault(k => k.Jenis == JenisKomponen.Atap)?.Id ?? Guid.NewGuid(),
            proyekId, JenisKomponen.Atap, atap)
    ];

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
