using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;
using Wastu.Domain.Weather;
using Wastu.InMemory;

namespace Wastu.Data.Tests;

public class InMemoryRepositoriesTests
{
    [Fact]
    public async Task SumberBahanRepositoryInMemory_MenyimpanDanMengambilSumberDenganBenar()
    {
        var repo = new SumberBahanRepositoryInMemory();
        var bahanId = Guid.NewGuid();

        var sumber1 = new SumberPropertiBahan(bahanId, "konduktivitas_termal", "SNI 03-6389-2000", "2000", null, StatusVerifikasiProperti.Terverifikasi);
        var sumber2 = new SumberPropertiBahan(bahanId, "faktor_emisi", "ICE Database v3", "v3.0", "https://circularecology.com", StatusVerifikasiProperti.Perkiraan);

        await repo.SimpanAsync(bahanId, [sumber1, sumber2]);

        var hasil = await repo.ListByBahanIdAsync(bahanId);
        Assert.Equal(2, hasil.Count);
        Assert.Contains(hasil, s => s.Properti == "konduktivitas_termal" && s.Status == StatusVerifikasiProperti.Terverifikasi);
        Assert.Contains(hasil, s => s.Properti == "faktor_emisi" && s.Status == StatusVerifikasiProperti.Perkiraan);
    }

    [Fact]
    public async Task BatchEvaluasiRepositoryInMemory_MenyimpanDanMencegahDuplikasiBatch()
    {
        var evaluasiRepo = new EvaluasiRepositoryInMemory();
        var repo = new BatchEvaluasiRepositoryInMemory(evaluasiRepo);

        var proyekId = Guid.NewGuid();
        var komponenId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var batch1 = new BatchEvaluasi(
            batchId,
            proyekId,
            komponenId,
            revisiLokasi: 1,
            hashInput: "hash-kanonik-abc123",
            versiModel: "v2.0",
            bobot: KriteriaBobot.Default,
            luasM2: 120,
            dihitungPadaUtc: DateTimeOffset.UtcNow);

        var evaluasi1 = new Evaluasi(
            Guid.NewGuid(),
            Guid.NewGuid(),
            komponenId,
            85.0,
            1200.0,
            new SubSkorKesesuaian(90, 80, 85, 85),
            "Sangat cocok");

        await repo.SimpanBatchAsync(batch1, [evaluasi1]);

        var tersimpan = await repo.GetBatchAsync(batchId);
        Assert.NotNull(tersimpan);
        Assert.Equal("hash-kanonik-abc123", tersimpan!.HashInput);

        // Batch kedua dengan hash dan komponen yang sama (idempotensi / deduplikasi)
        var batch2 = new BatchEvaluasi(
            Guid.NewGuid(),
            proyekId,
            komponenId,
            revisiLokasi: 1,
            hashInput: "hash-kanonik-abc123",
            versiModel: "v2.0",
            bobot: KriteriaBobot.Default,
            luasM2: 120,
            dihitungPadaUtc: DateTimeOffset.UtcNow.AddMinutes(5));

        await repo.SimpanBatchAsync(batch2, [evaluasi1]);

        var daftarBatch = await repo.ListBatchesByProyekAsync(proyekId);
        Assert.Single(daftarBatch);

        var latest = await repo.GetLatestBatchByKomponenAsync(komponenId);
        Assert.NotNull(latest);
        Assert.Equal(batchId, latest!.Id);
    }

    [Fact]
    public async Task EvaluasiRepositoryInMemory_MultiHorizonSimulasiTidakSalingMenimpa_T20()
    {
        var repo = new EvaluasiRepositoryInMemory();
        var evaluasiId = Guid.NewGuid();

        var sim20 = new SimulasiEmisi(evaluasiId, 20, 25.0, 0, 5000.0, "v2.0", 0.5);
        var sim30 = new SimulasiEmisi(evaluasiId, 30, 25.0, 1, 10000.0, "v2.0", 0.5);
        var sim50 = new SimulasiEmisi(evaluasiId, 50, 25.0, 2, 15000.0, "v2.0", 0.5);

        await repo.SimpanAsync(sim20);
        await repo.SimpanAsync(sim30);
        await repo.SimpanAsync(sim50);

        // Upsert horizon 20 dengan nilai emisi direvisi
        var sim20Revisi = new SimulasiEmisi(evaluasiId, 20, 25.0, 0, 5200.0, "v2.0", 0.5);
        await repo.SimpanAsync(sim20Revisi);

        var dibaca20 = repo.AmbilSimulasi(evaluasiId, 20, "v2.0");
        var dibaca30 = repo.AmbilSimulasi(evaluasiId, 30, "v2.0");
        var dibaca50 = repo.AmbilSimulasi(evaluasiId, 50, "v2.0");

        Assert.NotNull(dibaca20);
        Assert.NotNull(dibaca30);
        Assert.NotNull(dibaca50);

        Assert.Equal(5200.0, dibaca20!.EmisiKumulatifKg);
        Assert.Equal(10000.0, dibaca30!.EmisiKumulatifKg);
        Assert.Equal(15000.0, dibaca50!.EmisiKumulatifKg);

        var semua = repo.ListSimulasiByEvaluasi(evaluasiId);
        Assert.Equal(3, semua.Count);
    }

    [Fact]
    public async Task PekerjaanDanPrakiraanCache_PruneCacheTidakMenghapusKeputusanHistoris_T40()
    {
        var pekerjaanRepo = new PekerjaanRepositoryInMemory();
        var cacheRepo = new PrakiraanCacheRepositoryInMemory();

        var proyekId = Guid.NewGuid();
        var pekerjaanId = Guid.NewGuid();
        var tglMulai = new DateOnly(2026, 12, 1);
        var tglSelesai = new DateOnly(2026, 12, 7);
        var tglKunci = new DateOnly(2026, 12, 3);

        var pekerjaan = new Pekerjaan(
            pekerjaanId,
            proyekId,
            "Pengecoran Plat Lantai 2",
            JenisPekerjaan.Pengecoran,
            tglMulai,
            tglSelesai,
            StatusPekerjaan.Terkunci,
            tglKunci,
            konfirmasi: new KonfirmasiPenguncian(SetujuiRisiko: true, SetujuiDataLama: false, Catatan: "Mitigasi terpal disiapkan"),
            terakhirDiperbaruiUtc: DateTimeOffset.UtcNow);

        await pekerjaanRepo.AddAsync(pekerjaan);

        // Simpan prakiraan cache: 1 hari lampau (>30 hari) dan 1 hari segar
        double lat = -6.20;
        double lon = 106.81;

        var hariLampau = new DailyForecast(tglMulai, 20, 1.0, 24, 32, 75);
        var hasilLampau = new HasilPrakiraan([hariLampau], "Asia/Jakarta", DateTimeOffset.UtcNow.AddDays(-35), "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(lat, lon, hasilLampau);

        var hariSegar = new DailyForecast(tglSelesai, 10, 0.0, 25, 33, 70);
        var hasilSegar = new HasilPrakiraan([hariSegar], "Asia/Jakarta", DateTimeOffset.UtcNow.AddHours(-1), "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(lat, lon, hasilSegar);

        // Pruning cache lebih tua dari 30 hari
        var batasKedaluwarsa = DateTimeOffset.UtcNow.AddDays(-30);
        await cacheRepo.PruneExpiredAsync(batasKedaluwarsa);

        // Cache diperiksa
        var cacheAktif = await cacheRepo.GetCacheAsync(lat, lon, 1);
        Assert.NotNull(cacheAktif);
        Assert.Single(cacheAktif!.Hari);
        Assert.Equal(tglSelesai, cacheAktif.Hari[0].Tanggal);

        // Snapshot keputusan historis jadwal pada pekerjaan tetap utuh
        var tersimpanJob = await pekerjaanRepo.GetByIdAsync(pekerjaanId);
        Assert.NotNull(tersimpanJob);
        Assert.Equal(StatusPekerjaan.Terkunci, tersimpanJob!.Status);
        Assert.Equal(tglKunci, tersimpanJob.TanggalDikunci);

        var riwayatKeputusan = pekerjaanRepo.AmbilKeputusanHistoris();
        Assert.Single(riwayatKeputusan);
        Assert.True(riwayatKeputusan[0].Terkunci);
        Assert.True(riwayatKeputusan[0].Konfirmasi?.SetujuiRisiko);
    }

    [Fact]
    public async Task ProyekRepositoryInMemory_PerubahanKoordinatMenandaiPekerjaanPerluDitinjau_T31()
    {
        var pekerjaanRepo = new PekerjaanRepositoryInMemory();
        var proyekRepo = new ProyekRepositoryInMemory(pekerjaanRepo);

        var proyekId = Guid.NewGuid();
        var proyekAwal = new Proyek(proyekId, "Rumah Uji", "Bandar Lampung", -5.45, 105.26);
        await proyekRepo.AddAsync(proyekAwal);

        var profil = new ProfilIklim(2400, 82, 27.5, 3.0, TingkatKorosi.Tinggi);
        await proyekRepo.SimpanProfilIklimAsync(proyekId, profil);

        var pekerjaanId = Guid.NewGuid();
        var tglKunci = new DateOnly(2026, 12, 5);
        var pekerjaan = new Pekerjaan(
            pekerjaanId,
            proyekId,
            "Pengecatan Dinding Luar",
            JenisPekerjaan.Pengecatan,
            new DateOnly(2026, 12, 1),
            new DateOnly(2026, 12, 10),
            StatusPekerjaan.Terkunci,
            tglKunci);

        await pekerjaanRepo.AddAsync(pekerjaan);

        // Ubah lokasi proyek ke Jakarta
        var proyekPindah = new Proyek(proyekId, "Rumah Uji Pindah", "Jakarta", -6.20, 106.82, revisiLokasi: 2);
        await proyekRepo.UpdateAsync(proyekPindah);

        // Profil iklim lama harus terhapus
        var iklimSetelahPindah = await proyekRepo.GetProfilIklimAsync(proyekId);
        Assert.Null(iklimSetelahPindah);

        // Pekerjaan terkunci harus otomatis ditandai PerluDitinjau tanpa menghapus TanggalDikunci
        var jobSetelahPindah = await pekerjaanRepo.GetByIdAsync(pekerjaanId);
        Assert.NotNull(jobSetelahPindah);
        Assert.Equal(StatusPekerjaan.PerluDitinjau, jobSetelahPindah!.Status);
        Assert.Equal(tglKunci, jobSetelahPindah.TanggalDikunci);
    }

    [Fact]
    public async Task AmbangPekerjaanRepositoryInMemory_MendukungHierarkiOverrideDanDefault()
    {
        var repo = new AmbangPekerjaanRepositoryInMemory();

        // Default untuk jenis pekerjaan belum ada override
        var defPengecoran = await repo.GetByJenisAsync(JenisPekerjaan.Pengecoran);
        Assert.NotNull(defPengecoran);
        Assert.Equal(60, defPengecoran!.ProbabilitasHujanMaksPct);
        Assert.Equal(5, defPengecoran.CurahHujanMaksMm);
        Assert.Equal(35, defPengecoran.SuhuMaksC);
        Assert.Equal(TingkatPelanggaran.Peringatan, defPengecoran.TingkatSuhu);

        var defPengecatan = await repo.GetByJenisAsync(JenisPekerjaan.Pengecatan);
        Assert.NotNull(defPengecatan);
        Assert.Equal(40, defPengecatan!.ProbabilitasHujanMaksPct);
        Assert.Equal(85, defPengecatan.KelembapanMaksPct);
        Assert.Equal(TingkatPelanggaran.Keras, defPengecatan.TingkatSuhu);

        // Simpan override spesifik untuk satu pekerjaan
        var jobId = Guid.NewGuid();
        var overrideAmbang = new AmbangPekerjaan(
            probabilitasHujanMaksPct: 30,
            curahHujanMaksMm: 2,
            kelembapanMaksPct: 70,
            suhuMinC: 15,
            suhuMaksC: 30,
            tingkatSuhu: TingkatPelanggaran.Keras);

        await repo.SimpanOverrideAsync(jobId, overrideAmbang);

        var terbaca = await repo.GetByPekerjaanIdAsync(jobId);
        Assert.NotNull(terbaca);
        Assert.Equal(30, terbaca!.ProbabilitasHujanMaksPct);
        Assert.Equal(2, terbaca.CurahHujanMaksMm);
        Assert.Equal(70, terbaca.KelembapanMaksPct);
    }

    [Fact]
    public async Task KonfigurasiModelRepositoryInMemory_MenyimpanDanMengaktifkanModelBerversi()
    {
        var repo = new KonfigurasiModelRepositoryInMemory();

        var aktifAwal = await repo.GetActiveAsync();
        Assert.Equal("v2.0", aktifAwal.VersiModel);
        Assert.Equal(3000.0, aktifAwal.AcuanCurahHujanMm);

        var modelBaru = new KonfigurasiModel(
            versiModel: "v2.1-custom",
            bobotLembapHujanAlpha: 0.6,
            acuanCurahHujanMm: 2800.0,
            acuanJarakPesisirKm: 40.0,
            pitaTermalBatasBawah: 0.12,
            pitaTermalBatasAtasSejuk: 0.58,
            pitaTermalBatasAtasPanas: 0.32,
            suhuSejukC: 25.0,
            suhuPanasC: 33.0,
            faktorPenuaanA: 0.45,
            ambangTidakDisarankan: 50.0,
            ambangCocok: 80.0);

        await repo.SimpanAsync(modelBaru);

        var aktifBaru = await repo.GetActiveAsync();
        Assert.Equal("v2.1-custom", aktifBaru.VersiModel);
        Assert.Equal(2800.0, aktifBaru.AcuanCurahHujanMm);

        var modelLama = await repo.GetByVersiAsync("v2.0");
        Assert.NotNull(modelLama);
        Assert.Equal("v2.0", modelLama!.VersiModel);
    }

    [Fact]
    public async Task LaporanRepositoryInMemory_MenyimpanDanMenghapusMetadataLaporan()
    {
        var repo = new LaporanRepositoryInMemory();
        var proyekId = Guid.NewGuid();
        var laporanId = Guid.NewGuid();

        var laporan = new Laporan(
            laporanId,
            proyekId,
            "/home/user/dokumen/Laporan_Wastu.pdf",
            DateTimeOffset.UtcNow,
            checksumSha256: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            snapshotHash: "snap-12345",
            ukuranByte: 1048576,
            status: "Tersimpan");

        await repo.SimpanAsync(laporan);

        var hasil = await repo.GetByIdAsync(laporanId);
        Assert.NotNull(hasil);
        Assert.Equal(proyekId, hasil!.ProyekId);
        Assert.Equal("snap-12345", hasil.SnapshotHash);

        var daftar = await repo.ListByProyekAsync(proyekId);
        Assert.Single(daftar);

        await repo.DeleteAsync(laporanId);
        Assert.Null(await repo.GetByIdAsync(laporanId));
        Assert.Empty(await repo.ListByProyekAsync(proyekId));
    }
}
