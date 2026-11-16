using Wastu.Domain.Climate;
using Wastu.Domain.Services;
using Wastu.Domain.Tests.Fakes;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Climate;

public class ClimateAnalysisServiceTests
{
    private static readonly Proyek Indo = new(
        Guid.NewGuid(), "Rumah Tapkan Pesisir", "Bandar Lampung", -5.45, 105.26);

    // ------------------------------------------------------- GetOrFetch: cache permanen

    [Fact]
    public async Task GetOrFetch_BelumAda_MengambilDariKlienDanMenyimpan()
    {
        (ClimateAnalysisService service, ProyekRepositoryInMemory repo, HistoricalClimateClientStub klien) = Setup();

        ProfilIklim hasil = await service.GetOrFetchProfileAsync(Indo.Id);

        Assert.Equal(1, klien.JumlahPanggilan);
        Assert.Equal(2400, hasil.CurahHujanTahunanMm);

        ProfilIklim? tersimpan = await repo.GetProfilIklimAsync(Indo.Id);
        Assert.NotNull(tersimpan);
        Assert.Equal(2400, tersimpan.CurahHujanTahunanMm);
    }

    [Fact]
    public async Task GetOrFetch_MengirimKoordinatProyekKeKlien()
    {
        (ClimateAnalysisService service, _, HistoricalClimateClientStub klien) = Setup();

        await service.GetOrFetchProfileAsync(Indo.Id);

        Assert.Equal(Indo.Latitude, klien.LatitudeTerakhir);
        Assert.Equal(Indo.Longitude, klien.LongitudeTerakhir);
    }

    [Fact]
    public async Task GetOrFetch_SudahAda_TidakMemanggilKlienLagi()
    {
        // PRD §8.4: profil iklim disimpan permanen setelah diambil sekali. Memanggil
        // ulang API di sini akan melanggar jaminan pengguna bahwa perbandingan tetap
        // bisa dilihat tanpa koneksi.
        (ClimateAnalysisService service, ProyekRepositoryInMemory repo, HistoricalClimateClientStub klien) = Setup();

        await service.GetOrFetchProfileAsync(Indo.Id);
        await service.GetOrFetchProfileAsync(Indo.Id);
        await service.GetOrFetchProfileAsync(Indo.Id);

        Assert.Equal(1, klien.JumlahPanggilan);
    }

    [Fact]
    public async Task GetOrFetch_PunyaUmurCacheTanpaBatas_EnamJamTidakRelevan()
    {
        // PRD §8.4 hanya memberi masa berlaku enam jam untuk prakiraan harian §F6.
        // Profil iklim tidak punya TTL, jadi layanan ini tidak boleh  punya
        // jalur yang membatalkan data lama karena waktu.
        (ClimateAnalysisService service, _, HistoricalClimateClientStub klien) = Setup();

        await service.GetOrFetchProfileAsync(Indo.Id);

        Assert.Equal(1, klien.JumlahPanggilan);
    }

    // ------------------------------------------------------------ Refresh: ambil ulang

    [Fact]
    public async Task Refresh_SelaluMemanggilKlien()
    {
        (ClimateAnalysisService service, _, HistoricalClimateClientStub klien) = Setup();

        await service.GetOrFetchProfileAsync(Indo.Id);
        await service.RefreshProfileAsync(Indo.Id);

        Assert.Equal(2, klien.JumlahPanggilan);
    }

    [Fact]
    public async Task Refresh_MenimpaProfilTersimpan()
    {
        (ClimateAnalysisService service, ProyekRepositoryInMemory repo, HistoricalClimateClientStub klien) = Setup();
        await service.GetOrFetchProfileAsync(Indo.Id);

        klien.Hasil = new ProfilIklim(1500, 60, 24.0, 80.0, TingkatKorosi.Rendah);
        ProfilIklim hasil = await service.RefreshProfileAsync(Indo.Id);

        Assert.Equal(1500, hasil.CurahHujanTahunanMm);
        Assert.Equal(1500, (await repo.GetProfilIklimAsync(Indo.Id))!.CurahHujanTahunanMm);
    }

    // ------------------------------------------------------------- kegagalan PRD §8.4

    [Fact]
    public async Task Refresh_Gagal_DanAdaProfilLama_ThrowsTapiTetapMembawaDataLama()
    {
        // PRD §8.4 meminta pesan eksplisit dan fallback ke cache terakhir. Galat
        // membawa keduanya supaya antarmuka bisa menampilkan keduanya.
        (ClimateAnalysisService service, _, HistoricalClimateClientStub klien) = Setup();
        await service.GetOrFetchProfileAsync(Indo.Id);
        klien.Gagal = true;

        GagalMemperbaruiProfilIklimException galat = await Assert.ThrowsAsync<GagalMemperbaruiProfilIklimException>(
            () => service.RefreshProfileAsync(Indo.Id));

        Assert.NotNull(galat.ProfilTersimpan);
        Assert.Equal(2400, galat.ProfilTersimpan.CurahHujanTahunanMm);
        Assert.Contains("belum termasuk perubahan terbaru", galat.Message, StringComparison.Ordinal);
        Assert.IsType<HttpRequestException>(galat.InnerException);
    }

    [Fact]
    public async Task Refresh_Gagal_DanAdaProfilLama_ProfilTersimpanTidakDitimpa()
    {
        (ClimateAnalysisService service, ProyekRepositoryInMemory repo, HistoricalClimateClientStub klien) = Setup();
        await service.GetOrFetchProfileAsync(Indo.Id);
        klien.Gagal = true;

        await Assert.ThrowsAsync<GagalMemperbaruiProfilIklimException>(
            () => service.RefreshProfileAsync(Indo.Id));

        Assert.Equal(2400, (await repo.GetProfilIklimAsync(Indo.Id))!.CurahHujanTahunanMm);
    }

    [Fact]
    public async Task Refresh_Gagal_DanBelumAdaProfilLama_ThrowsTanpaDataCadangan()
    {
        (ClimateAnalysisService service, _, HistoricalClimateClientStub klien) = Setup();
        klien.Gagal = true;

        GagalMemperbaruiProfilIklimException galat = await Assert.ThrowsAsync<GagalMemperbaruiProfilIklimException>(
            () => service.RefreshProfileAsync(Indo.Id));

        Assert.Null(galat.ProfilTersimpan);
        Assert.Contains("Periksa koneksi", galat.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_Dibatalkan_TidakDiubahMenjadiGalatFallback()
    {
        // CancellationException berarti pengguna membatalkan, bukan jaringan turun.
        // Mengubahnya menjadi fallback diam-diam akan menyembunyikan pembatalan.
        (ClimateAnalysisService service, _, HistoricalClimateClientStub klien) = Setup();
        await service.GetOrFetchProfileAsync(Indo.Id);

        CancellationTokenSource cts = new();
        await cts.CancelAsync();

        klien.Gagal = true;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RefreshProfileAsync(Indo.Id, cts.Token));
    }

    // -------------------------------------------------------------- proyek tidak ada

    [Fact]
    public async Task GetOrFetch_ProyekTidakAda_Throws()
    {
        (ClimateAnalysisService service, _, _) = Setup();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetOrFetchProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Refresh_ProyekTidakAda_Throws()
    {
        (ClimateAnalysisService service, _, _) = Setup();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RefreshProfileAsync(Guid.NewGuid()));
    }

    private static (ClimateAnalysisService Service, ProyekRepositoryInMemory Repo, HistoricalClimateClientStub Klien) Setup()
    {
        ProyekRepositoryInMemory repo = new();
        repo.AddAsync(Indo);

        HistoricalClimateClientStub klien = new();
        ClimateAnalysisService service = new(repo, klien);

        return (service, repo, klien);
    }
}