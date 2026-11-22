using Wastu.Domain.Abstractions;
using Wastu.Domain.Climate;
using Wastu.Domain.Projects;
using Wastu.Domain.Services;
using Wastu.Domain.Tests.Fakes;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Climate;

/// <summary>
/// Rangkaian pengujian terpadu untuk Paket 2a dan Paket 3 (T01 - T10):
/// Komponen fleksibel, validasi koordinat terpadu, daur hidup iklim otomatis,
/// penjaga respons basi / pembatalan, provenance metadata, dan kalkulator pesisir.
/// </summary>
public class ProjectClimateLifecycleTests
{
    // =========================================================================
    // T01: Flexible components (Dinding-only, Atap-only, and Both)
    // =========================================================================
    [Fact]
    public async Task T01_CreateAndUpdateProject_FlexibleComponents_Success()
    {
        var repo = new ProyekRepositoryInMemory();
        var client = new HistoricalClimateClientStub();
        var climateService = new ClimateAnalysisService(repo, client);
        var projectService = new ProjectService(repo, climateService);

        // 1. Dinding only
        var pDinding = await projectService.CreateProjectAsync(
            "Proyek Dinding Saja", "Bandar Lampung", -5.45, 105.26,
            [new InputKomponen(JenisKomponen.Dinding, 120.0)]);
        var kompDinding = await repo.ListKomponenAsync(pDinding.Id);
        Assert.Single(kompDinding);
        Assert.Equal(JenisKomponen.Dinding, kompDinding[0].Jenis);
        Assert.Equal(120.0, kompDinding[0].LuasM2);

        // 2. Atap only
        var pAtap = await projectService.CreateProjectAsync(
            "Proyek Atap Saja", "Bandar Lampung", -5.45, 105.26,
            [new InputKomponen(JenisKomponen.Atap, 85.5)]);
        var kompAtap = await repo.ListKomponenAsync(pAtap.Id);
        Assert.Single(kompAtap);
        Assert.Equal(JenisKomponen.Atap, kompAtap[0].Jenis);
        Assert.Equal(85.5, kompAtap[0].LuasM2);

        // 3. Both components
        var pBoth = await projectService.CreateProjectAsync(
            "Proyek Lengkap", "Bandar Lampung", -5.45, 105.26,
            [new InputKomponen(JenisKomponen.Dinding, 150.0), new InputKomponen(JenisKomponen.Atap, 100.0)]);
        var kompBoth = await repo.ListKomponenAsync(pBoth.Id);
        Assert.Equal(2, kompBoth.Count);
        Assert.Contains(kompBoth, k => k.Jenis == JenisKomponen.Dinding && k.LuasM2 == 150.0);
        Assert.Contains(kompBoth, k => k.Jenis == JenisKomponen.Atap && k.LuasM2 == 100.0);

        // 4. Update from Both to Dinding-only
        await projectService.UpdateProjectAsync(
            pBoth.Id, "Proyek Dinding Saja Revisi", "Bandar Lampung", -5.45, 105.26,
            [new InputKomponen(JenisKomponen.Dinding, 140.0)]);
        var kompUpdated = await repo.ListKomponenAsync(pBoth.Id);
        Assert.Single(kompUpdated);
        Assert.Equal(JenisKomponen.Dinding, kompUpdated[0].Jenis);
        Assert.Equal(140.0, kompUpdated[0].LuasM2);
    }

    // =========================================================================
    // T02: Rejection of invalid components and blank metadata
    // =========================================================================
    [Fact]
    public async Task T02_RejectInvalidComponentsAndBlankMetadata()
    {
        var repo = new ProyekRepositoryInMemory();
        var client = new HistoricalClimateClientStub();
        var climateService = new ClimateAnalysisService(repo, client);
        var projectService = new ProjectService(repo, climateService);

        // Empty component list
        await Assert.ThrowsAsync<ArgumentException>(() =>
            projectService.CreateProjectAsync("Proyek", "Jakarta", -6.20, 106.82, Array.Empty<InputKomponen>()));

        // Duplicate component kind
        await Assert.ThrowsAsync<ArgumentException>(() =>
            projectService.CreateProjectAsync("Proyek", "Jakarta", -6.20, 106.82,
                [new InputKomponen(JenisKomponen.Dinding, 100), new InputKomponen(JenisKomponen.Dinding, 50)]));

        // Area <= 0
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputKomponen(JenisKomponen.Dinding, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputKomponen(JenisKomponen.Dinding, -5.0));

        // Non-finite area
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputKomponen(JenisKomponen.Dinding, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputKomponen(JenisKomponen.Dinding, double.PositiveInfinity));

        // Blank name or city
        await Assert.ThrowsAsync<ArgumentException>(() =>
            projectService.CreateProjectAsync("  ", "Jakarta", -6.20, 106.82, [new InputKomponen(JenisKomponen.Dinding, 100)]));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            projectService.CreateProjectAsync("Proyek", "   ", -6.20, 106.82, [new InputKomponen(JenisKomponen.Dinding, 100)]));
    }

    // =========================================================================
    // T03: Unified coordinate validation boundaries (-11..6, 95..141)
    // =========================================================================
    [Theory]
    [InlineData(-11.0, 95.0, true)]
    [InlineData(6.0, 141.0, true)]
    [InlineData(-5.45, 105.26, true)]
    [InlineData(-11.001, 100.0, false)]
    [InlineData(6.001, 100.0, false)]
    [InlineData(0.0, 94.999, false)]
    [InlineData(0.0, 141.001, false)]
    public void T03_ValidasiKoordinat_Boundaries(double lat, double lon, bool expectedValid)
    {
        bool isValid = ValidasiKoordinat.CobaValidasi(lat, lon, out var pesan);
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.Null(pesan);
            ValidasiKoordinat.Validasi(lat, lon);
        }
        else
        {
            Assert.NotNull(pesan);
            Assert.Throws<ArgumentOutOfRangeException>(() => ValidasiKoordinat.Validasi(lat, lon));
        }
    }

    [Fact]
    public void T03_ValidasiKoordinat_NonFiniteCoordinates_Rejected()
    {
        Assert.False(ValidasiKoordinat.CobaValidasi(double.NaN, 105.0, out var err1));
        Assert.Contains("finite", err1);

        Assert.False(ValidasiKoordinat.CobaValidasi(-5.0, double.PositiveInfinity, out var err2));
        Assert.Contains("finite", err2);

        Assert.Throws<ArgumentException>(() => ValidasiKoordinat.Validasi(double.NaN, 100.0));
        Assert.Throws<ArgumentException>(() => ValidasiKoordinat.Validasi(-5.0, double.NegativeInfinity));
    }

    // =========================================================================
    // T04: Metadata/area change does NOT bump revision or fetch climate; coord change does
    // =========================================================================
    [Fact]
    public async Task T04_UpdateNonCoordinates_NoRevisionBumpAndNoClimateCall()
    {
        var repo = new ProyekRepositoryInMemory();
        var client = new HistoricalClimateClientStub();
        var climateService = new ClimateAnalysisService(repo, client);
        var projectService = new ProjectService(repo, climateService);

        var proyek = await projectService.CreateProjectAsync(
            "Awal", "Bandar Lampung", -5.45, 105.26, [new InputKomponen(JenisKomponen.Dinding, 100)]);
        Assert.Equal(1, proyek.RevisiLokasi);

        // Populate initial climate profile
        await climateService.GetOrFetchProfileAsync(proyek.Id);
        Assert.Equal(1, client.JumlahPanggilan);

        // Update name, city, components, and areas (same coordinates)
        await projectService.UpdateProjectAsync(
            proyek.Id, "Nama Baru", "Kota Baru", -5.45, 105.26,
            [new InputKomponen(JenisKomponen.Dinding, 150), new InputKomponen(JenisKomponen.Atap, 80)]);

        var terkini = await repo.GetByIdAsync(proyek.Id);
        Assert.NotNull(terkini);
        Assert.Equal(1, terkini.RevisiLokasi); // Revisi NOT bumped
        Assert.Equal(1, client.JumlahPanggilan); // No additional HTTP call

        // Now update coordinates: revision MUST bump and climate refresh triggered
        await projectService.UpdateProjectAsync(
            proyek.Id, "Nama Baru", "Jakarta", -6.20, 106.82,
            [new InputKomponen(JenisKomponen.Dinding, 150)]);

        var setelahPindah = await repo.GetByIdAsync(proyek.Id);
        Assert.NotNull(setelahPindah);
        Assert.Equal(2, setelahPindah.RevisiLokasi); // Revisi bumped to 2
        Assert.Equal(2, client.JumlahPanggilan); // HTTP called again
    }

    // =========================================================================
    // T05: Climate fetch produces structured MetadataIklim; cache has 0 extra HTTP
    // =========================================================================
    [Fact]
    public async Task T05_StructuredMetadataIklim_AndSubsequentCachedRead()
    {
        var repo = new ProyekRepositoryInMemory();
        var client = new HistoricalClimateClientStub();
        var climateService = new ClimateAnalysisService(repo, client);
        var projectService = new ProjectService(repo, climateService);

        var proyek = await projectService.CreateProjectAsync(
            "Proyek Uji", "Bandar Lampung", -5.45, 105.26, [new InputKomponen(JenisKomponen.Dinding, 100)]);

        var profil = await climateService.GetOrFetchProfileAsync(proyek.Id);

        Assert.Equal(1, client.JumlahPanggilan);
        Assert.NotNull(profil.Metadata);
        Assert.Equal(ProvenanceIklim.ApiHistoris, profil.Metadata.Provenance);
        Assert.Equal(proyek.RevisiLokasi, profil.Metadata.RevisiLokasi);
        Assert.Equal(KualitasCacheIklim.CocokLokasi, profil.Metadata.KualitasCache);
        Assert.Equal(JarakPesisirCalculator.VersiAlgoritme, profil.Metadata.VersiAlgoritmePesisir);
        Assert.Contains("Open-Meteo", profil.Metadata.Penyedia);
        Assert.True(profil.Metadata.DiambilPadaUtc <= DateTimeOffset.UtcNow);

        // Subsequent read: must be from cache without additional HTTP
        var profil2 = await climateService.GetOrFetchProfileAsync(proyek.Id);
        Assert.Equal(1, client.JumlahPanggilan);
        Assert.Equal(profil.CurahHujanTahunanMm, profil2.CurahHujanTahunanMm);
    }

    // =========================================================================
    // T06: When climate API fails after project creation, project remains persisted
    // =========================================================================
    [Fact]
    public async Task T06_ClimateFailureOnProjectCreation_ProjectRemainsSaved()
    {
        var repo = new ProyekRepositoryInMemory();
        var client = new HistoricalClimateClientStub { Gagal = true };
        var climateService = new ClimateAnalysisService(repo, client);
        var projectService = new ProjectService(repo, climateService);

        // 1. Calling CreateWithClimateAsync surfaces warning and keeps project saved
        var (proyek, profil, peringatan) = await projectService.CreateWithClimateAsync(
            "Proyek Gagal Iklim", "Bandar Lampung", -5.45, 105.26,
            [new InputKomponen(JenisKomponen.Dinding, 100)]);

        Assert.NotNull(proyek);
        Assert.Null(profil);
        Assert.NotNull(peringatan);

        // Project MUST exist in repository!
        var saved = await repo.GetByIdAsync(proyek.Id);
        Assert.NotNull(saved);
        Assert.Equal("Proyek Gagal Iklim", saved.Nama);
        Assert.Equal(-5.45, saved.Latitude);

        // 2. Refresh on failed climate throws GagalMemperbaruiProfilIklimException
        var ex = await Assert.ThrowsAsync<GagalMemperbaruiProfilIklimException>(() =>
            climateService.RefreshProfileAsync(proyek.Id));
        Assert.NotNull(ex);
    }

    // =========================================================================
    // T07: When coordinates change and climate refresh fails, old cache marked TidakCocokLokasi
    // =========================================================================
    [Fact]
    public async Task T07_CoordinateChange_ClimateRefreshFails_MarksTidakCocokLokasi()
    {
        var repo = new ProyekRepositoryInMemory();
        var client = new HistoricalClimateClientStub();
        var climateService = new ClimateAnalysisService(repo, client);

        // Proyek di Bandar Lampung dengan revisi 1 dan profil iklim awal
        var proyek = new Proyek(Guid.NewGuid(), "Proyek Pindah", "Bandar Lampung", -5.45, 105.26, revisiLokasi: 1);
        await repo.AddAsync(proyek);
        var awal = await climateService.GetOrFetchProfileAsync(proyek.Id);
        Assert.Equal(1, awal.Metadata?.RevisiLokasi);

        // Koordinat proyek diperbarui ke Jakarta dengan revisi 2
        var proyekBaru = new Proyek(proyek.Id, "Proyek Pindah", "Jakarta", -6.20, 106.82, revisiLokasi: 2);
        await repo.UpdateAsync(proyekBaru);
        // Pastikan cache iklim lama tersimpan untuk menguji penanganan data lokasi sebelumnya
        await repo.SimpanProfilIklimAsync(proyek.Id, awal);

        // Klien gagal saat mengambil iklim Jakarta
        client.Gagal = true;

        var ex = await Assert.ThrowsAsync<GagalMemperbaruiProfilIklimException>(() =>
            climateService.RefreshProfileAsync(proyek.Id));

        // Exception message must explicitly warn about mismatch with previous location
        Assert.Contains("Data tersimpan berasal dari lokasi sebelumnya", ex.Message);
        Assert.NotNull(ex.ProfilTersimpan);
        Assert.Equal(KualitasCacheIklim.TidakCocokLokasi, ex.ProfilTersimpan.Metadata?.KualitasCache);
    }

    // =========================================================================
    // T08: When refresh fails on SAME coordinates, existing valid profile remains intact
    // =========================================================================
    [Fact]
    public async Task T08_SameCoordinatesRefreshFails_ExistingProfileRemainsIntact()
    {
        var repo = new ProyekRepositoryInMemory();
        var client = new HistoricalClimateClientStub();
        var climateService = new ClimateAnalysisService(repo, client);
        var projectService = new ProjectService(repo, climateService);

        var proyek = await projectService.CreateProjectAsync(
            "Proyek Tetap", "Bandar Lampung", -5.45, 105.26, [new InputKomponen(JenisKomponen.Dinding, 100)]);
        var awal = await climateService.GetOrFetchProfileAsync(proyek.Id);

        // Set failure and refresh on same location
        client.Gagal = true;
        var ex = await Assert.ThrowsAsync<GagalMemperbaruiProfilIklimException>(() =>
            climateService.RefreshProfileAsync(proyek.Id));

        Assert.NotNull(ex.ProfilTersimpan);
        Assert.Equal(awal.CurahHujanTahunanMm, ex.ProfilTersimpan.CurahHujanTahunanMm);
        Assert.Equal(ProvenanceIklim.ApiHistoris, ex.ProfilTersimpan.Metadata?.Provenance);
        Assert.Equal(KualitasCacheIklim.CocokLokasi, ex.ProfilTersimpan.Metadata?.KualitasCache);
    }

    // =========================================================================
    // T09: Stale-response & cancellation guard
    // =========================================================================
    private sealed class DelayedClimateClient : IHistoricalClimateClient
    {
        public TaskCompletionSource<ProfilIklim> Tcs { get; } = new();

        public Task<ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
        {
            ct.Register(() => Tcs.TrySetCanceled(ct));
            return Tcs.Task;
        }
    }

    [Fact]
    public async Task T09_StaleResponseGuard_LateResponseNeverOverwritesNewerRevision()
    {
        var repo = new ProyekRepositoryInMemory();
        var delayedClient = new DelayedClimateClient();
        var climateService = new ClimateAnalysisService(repo, delayedClient);

        var proyek = new Proyek(Guid.NewGuid(), "Proyek Balap", "Bandar Lampung", -5.45, 105.26, revisiLokasi: 1);
        await repo.AddAsync(proyek);

        // Start slow fetch for revision 1
        var fetchTask = climateService.RefreshProfileAsync(proyek.Id);

        // Meanwhile, update project coordinates to revision 2
        var proyekRev2 = new Proyek(proyek.Id, "Proyek Balap", "Jakarta", -6.20, 106.82, revisiLokasi: 2);
        await repo.UpdateAsync(proyekRev2);

        // Now let slow fetch for rev-1 complete
        var profilRev1 = new ProfilIklim(2000, 80, 27.0, 5.0, TingkatKorosi.Tinggi);
        delayedClient.Tcs.SetResult(profilRev1);

        // Climate service must detect the revision mismatch and throw OperationCanceledException!
        await Assert.ThrowsAsync<OperationCanceledException>(() => fetchTask);

        // Ensure repo does NOT have the stale rev-1 profile!
        var storedClimate = await repo.GetProfilIklimAsync(proyek.Id);
        Assert.Null(storedClimate);
    }

    // =========================================================================
    // T10: JarakPesisirCalculator pure unit tests
    // =========================================================================
    [Fact]
    public void T10_JarakPesisirCalculator_PointToSegmentGeometry()
    {
        // 1. Exact point on segment -> distance 0
        double distOnSegment = JarakPesisirCalculator.HitungJarakTitikKeSegmenKm(
            -6.0, 106.0, -6.0, 105.0, -6.0, 107.0);
        Assert.Equal(0.0, distOnSegment, 4);

        // 2. Zero-length segment (A == B) -> point-to-point distance
        double distZeroSegment = JarakPesisirCalculator.HitungJarakTitikKeSegmenKm(
            -6.0, 106.0, -6.0, 106.0, -6.0, 106.0);
        Assert.Equal(0.0, distZeroSegment, 4);

        // 3. Perpendicular projection is closer than endpoints
        // Segment along latitude -6 from lon 105 to 107. Point at (-5.9, 106.0).
        double distPerp = JarakPesisirCalculator.HitungJarakTitikKeSegmenKm(
            -5.9, 106.0, -6.0, 105.0, -6.0, 107.0);
        double distEndA = JarakPesisirCalculator.HitungJarakTitikKeSegmenKm(
            -5.9, 106.0, -6.0, 105.0, -6.0, 105.0);
        Assert.True(distPerp < distEndA);
        Assert.True(distPerp > 10.0 && distPerp < 12.0); // ~11.1 km for 0.1 deg lat
    }

    [Theory]
    [InlineData(0.0, TingkatKorosi.Tinggi)]
    [InlineData(5.0, TingkatKorosi.Tinggi)]
    [InlineData(9.999, TingkatKorosi.Tinggi)]
    [InlineData(10.0, TingkatKorosi.Sedang)]
    [InlineData(20.0, TingkatKorosi.Sedang)]
    [InlineData(34.999, TingkatKorosi.Sedang)]
    [InlineData(35.0, TingkatKorosi.Rendah)]
    [InlineData(100.0, TingkatKorosi.Rendah)]
    public void T10_JarakPesisirCalculator_CorrosionClassification(double jarakKm, TingkatKorosi expected)
    {
        Assert.Equal(expected, JarakPesisirCalculator.KlasifikasiKorosi(jarakKm));
    }

    [Fact]
    public void T10_JarakPesisirCalculator_MissingOrEmptyCoastline_ReturnsNull()
    {
        // Null polylines -> null
        Assert.Null(JarakPesisirCalculator.HitungJarakMinimumKm(-6.2, 106.8, null!));

        // Empty polylines -> null
        Assert.Null(JarakPesisirCalculator.HitungJarakMinimumKm(-6.2, 106.8, []));

        // Polylines with single point (0 segments) -> null
        IReadOnlyList<IReadOnlyList<(double Lat, double Lon)>> singlePointPolyline =
        [
            [(-6.0, 106.0)]
        ];
        Assert.Null(JarakPesisirCalculator.HitungJarakMinimumKm(-6.2, 106.8, singlePointPolyline));
    }
}
