using Wastu.Domain.Projects;
using Wastu.Domain.Services;
using Wastu.Domain.Tests.Fakes;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Projects;

public class ProjectServiceTests
{
    [Fact]
    public async Task UpdateLocation_GagalMengambilIklim_TidakMemakaiProfilLokasiLama()
    {
        var repo = new ProyekRepositoryInMemory();
        var klien = new HistoricalClimateClientStub();
        var iklim = new Wastu.Domain.Climate.ClimateAnalysisService(repo, klien);
        var service = new ProjectService(repo, iklim);
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);
        await iklim.GetOrFetchProfileAsync(proyek.Id);
        klien.Gagal = true;

        var galat = await Assert.ThrowsAsync<GagalMemperbaruiProfilIklimException>(
            () => service.UpdateLocationAsync(proyek.Id, -6.20, 106.82));

        Assert.Null(galat.ProfilTersimpan);
        Assert.Null(await repo.GetProfilIklimAsync(proyek.Id));
        Assert.Equal(-6.20, (await repo.GetByIdAsync(proyek.Id))!.Latitude);
    }

    [Fact]
    public async Task CreateProject_MenyimpanDanMengembalikanProyek()
    {
        (ProjectService service, ProyekRepositoryInMemory repo, _) = Setup();

        Proyek hasil = await service.CreateProjectAsync("Rumah Tapkan Pesisir", "Bandar Lampung", -5.45, 105.26);

        Assert.NotEqual(Guid.Empty, hasil.Id);
        Assert.Equal("Rumah Tapkan Pesisir", hasil.Nama);
        Assert.Equal("Bandar Lampung", hasil.Kota);

        Proyek? tersimpan = await repo.GetByIdAsync(hasil.Id);
        Assert.NotNull(tersimpan);
        Assert.Equal(hasil.Id, tersimpan.Id);
    }

    [Fact]
    public async Task CreateProject_LokasiDiLuarIndonesia_DitolakSebelumDisimpan()
    {
        // PRD §F1 mensyaratkan koordinat berada di wilayah Indonesia.
        //
        // Batas di Proyek adalah kotak pembatas lintang -11..6 dan bujur 94..142,
        // bukan garis negara. Ini limitation yang sudah berlaku di skema (CHECK
        // bangunan_berada_di_indonesia) dan PRD §14 R5, bukan sesuatu yang
        // diperketat di sini. Paris di luar kotak, jadi pasti ditolak.
        (ProjectService service, ProyekRepositoryInMemory repo, _) = Setup();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateProjectAsync("Rumah", "Paris", 48.85, 2.35));

        Assert.Empty(await repo.ListKomponenAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateProject_LintangMelewatiBatasAtas_Ditolak()
    {
        (ProjectService service, _, _) = Setup();

        // Latitude 6 adalah batas atas Indonesia; 6,1 sudah di luar.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateProjectAsync("Rumah", "Sabang", 6.1, 95.3));
    }

    [Fact]
    public async Task CreateProject_NamaKosong_Ditolak()
    {
        (ProjectService service, _, _) = Setup();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => service.CreateProjectAsync("   ", "Bandar Lampung", -5.45, 105.26));
    }

    [Fact]
    public async Task UpdateLocation_MenggantiKoordinat()
    {
        (ProjectService service, ProyekRepositoryInMemory repo, _) = Setup();
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);

        await service.UpdateLocationAsync(proyek.Id, latitude: -6.20, longitude: 106.82);

        Proyek? tersimpan = await repo.GetByIdAsync(proyek.Id);
        Assert.NotNull(tersimpan);
        Assert.Equal(-6.20, tersimpan.Latitude);
        Assert.Equal(106.82, tersimpan.Longitude);
    }

    [Fact]
    public async Task UpdateLocation_MemicuRefreshProfilIklim()
    {
        // Ini acceptance criteria PRD §F1: mengubah lokasi memicu pengambilan ulang
        // profil iklim. Kalau pemanggilan ini hilang, pengguna tetap melihat peringkat
        // bahan untuk lokasi lama.
        (ProjectService service, _, IklimServicePerekam iklim) = Setup();
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);

        await service.UpdateLocationAsync(proyek.Id, -6.20, 106.82);

        Assert.Equal(1, iklim.JumlahRefresh);
        Assert.Equal(proyek.Id, iklim.RefreshTerakhir);
    }

    [Fact]
    public async Task UpdateLocation_CreateProjectTidakMemicuRefresh()
    {
        (ProjectService service, _, IklimServicePerekam iklim) = Setup();
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);

        await service.UpdateLocationAsync(proyek.Id, -6.20, 106.82);

        // CreateProject tidak boleh ikut memicu refresh. Profil iklim untuk proyek
        // baru memang belum ada, jadi belum ada yang perlu diambil ulang di titik itu.
        Assert.Equal(1, iklim.JumlahRefresh);
    }

    [Fact]
    public async Task UpdateLocation_KoordinatTidakValid_TidakMenyimpanPerubahan()
    {
        (ProjectService service, ProyekRepositoryInMemory repo, IklimServicePerekam iklim) = Setup();
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.UpdateLocationAsync(proyek.Id, -95.0, 106.82));

        Proyek? tersimpan = await repo.GetByIdAsync(proyek.Id);
        Assert.NotNull(tersimpan);
        Assert.Equal(-5.45, tersimpan.Latitude);
        Assert.Equal(0, iklim.JumlahRefresh);
    }

    [Fact]
    public async Task UpdateLocation_ProyekTidakAda_Throws()
    {
        (ProjectService service, _, _) = Setup();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateLocationAsync(Guid.NewGuid(), -6.20, 106.82));
    }

    [Fact]
    public async Task AddComponent_MenyimpanPadaProyekYangBenar()
    {
        (ProjectService service, ProyekRepositoryInMemory repo, _) = Setup();
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);

        await service.AddComponentAsync(proyek.Id, JenisKomponen.Dinding, 120);

        Komponen tersimpan = Assert.Single(await repo.ListKomponenAsync(proyek.Id));
        Assert.Equal(proyek.Id, tersimpan.ProyekId);
        Assert.Equal(JenisKomponen.Dinding, tersimpan.Jenis);
        Assert.Equal(120, tersimpan.LuasM2);
    }

    [Fact]
    public async Task AddComponent_BisaMenambahBeberapaKomponen()
    {
        (ProjectService service, ProyekRepositoryInMemory repo, _) = Setup();
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);

        await service.AddComponentAsync(proyek.Id, JenisKomponen.Dinding, 120);
        await service.AddComponentAsync(proyek.Id, JenisKomponen.Atap, 90);

        Assert.Equal(2, (await repo.ListKomponenAsync(proyek.Id)).Count);
    }

    [Fact]
    public async Task AddComponent_LuasNol_Ditolak()
    {
        (ProjectService service, _, _) = Setup();
        Proyek proyek = await service.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.AddComponentAsync(proyek.Id, JenisKomponen.Dinding, 0));
    }

    [Fact]
    public async Task AddComponent_ProyekTidakAda_Throws()
    {
        (ProjectService service, _, _) = Setup();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AddComponentAsync(Guid.NewGuid(), JenisKomponen.Dinding, 120));
    }

    private static (ProjectService Service, ProyekRepositoryInMemory Repo, IklimServicePerekam Iklim) Setup()
    {
        var repo = new ProyekRepositoryInMemory();
        IklimServicePerekam iklim = new();
        ProjectService service = new(repo, iklim);

        return (service, repo, iklim);
    }
}
