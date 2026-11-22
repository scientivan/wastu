using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wastu.Data.Entities;
using Wastu.Data.Repositories;
using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;
using Wastu.Domain.Weather;

namespace Wastu.Data.Tests;

public class PostgreSqlIntegrationTests
{
    private static string TemukanRootRepositori()
    {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "database", "02_schema.sql")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new FileNotFoundException("Root repositori wastu tidak ditemukan dari " + AppContext.BaseDirectory);
    }

    [PerluPostgreSql]
    public async Task Skenario1_FreshInstallParity_MigrateDanSqlScriptBerhasilSertaKatalogCocok()
    {
        // 1A. Instalasi skema via MigrateAsync() pada skema terisolasi pertama
        await using var dbEf = await PostgreSqlTestDatabase.CreateAsync();
        await using (var db = dbEf.Factory.CreateDbContext())
        {
            await db.Database.MigrateAsync();

            int jumlahBahan = await db.Bahan.CountAsync();
            int dinding = await db.Bahan.CountAsync(b => b.Jenis == JenisKomponen.Dinding);
            int atap = await db.Bahan.CountAsync(b => b.Jenis == JenisKomponen.Atap);

            Assert.Equal(25, jumlahBahan);
            Assert.Equal(15, dinding);
            Assert.Equal(10, atap);

            var modelAktif = await db.KonfigurasiModel.FirstOrDefaultAsync(k => k.Aktif);
            Assert.NotNull(modelAktif);
            Assert.Equal("v2.0", modelAktif!.VersiModel);
        }

        // 1B. Instalasi skema via database/02_schema.sql + 03_seed_supabase.sql pada skema terisolasi kedua
        await using var dbSql = await PostgreSqlTestDatabase.CreateAsync();
        string root = TemukanRootRepositori();
        string schemaSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "02_schema.sql"));
        string seedSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "03_seed_supabase.sql"));

        await dbSql.ExecuteSqlScriptAsync(schemaSql);
        await dbSql.ExecuteSqlScriptAsync(seedSql);

        await using (var db = dbSql.Factory.CreateDbContext())
        {
            int jumlahBahanSql = await db.Bahan.CountAsync();
            int dindingSql = await db.Bahan.CountAsync(b => b.Jenis == JenisKomponen.Dinding);
            int atapSql = await db.Bahan.CountAsync(b => b.Jenis == JenisKomponen.Atap);

            Assert.Equal(25, jumlahBahanSql);
            Assert.Equal(15, dindingSql);
            Assert.Equal(10, atapSql);

            // Verifikasi iklim benih demo memiliki provenance = 'Contoh' (PRD §6.3, T37)
            var profilDemo = await db.ProfilIklim.FirstOrDefaultAsync();
            Assert.NotNull(profilDemo);
            Assert.Equal("Contoh", profilDemo!.Provenance);
            Assert.NotEqual("ApiHistoris", profilDemo.Provenance);

            // Verifikasi simulasi emisi benih demo terisi
            int jumlahSimulasi = await db.SimulasiEmisi.CountAsync();
            Assert.True(jumlahSimulasi >= 2);
        }
    }

    [PerluPostgreSql]
    public async Task Skenario2_UpgradeDariBaselineInitialCreate_IdempotenDanDataLegacyUtuh_T37()
    {
        await using var dbTest = await PostgreSqlTestDatabase.CreateAsync();

        Guid legacyProyekId = Guid.NewGuid();
        Guid legacyKomponenId = Guid.NewGuid();
        Guid legacyEvaluasiId = Guid.NewGuid();
        Guid legacyBahanId = new("d0000000-0000-4000-8000-000000000001"); // Beton bertulang

        // Langkah 1: Migrasikan hanya sampai InitialCreate
        await using (var db = dbTest.Factory.CreateDbContext())
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261109011548_InitialCreate");

            // Seed data legacy di skema baseline via raw SQL agar sesuai dengan kolom InitialCreate
            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO bangunan (id, nama, kota, latitude, longitude, dibuat_pada)
                  VALUES ({0}, 'Proyek Warisan Legacy', 'Bandar Lampung', -5.45, 105.26, now());",
                legacyProyekId);

            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO komponen (id, bangunan_id, jenis, luas_m2)
                  VALUES ({0}, {1}, 'dinding', 100.0);",
                legacyKomponenId, legacyProyekId);

            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO profil_iklim (bangunan_id, curah_hujan_tahunan_mm, kelembapan_rata_pct, suhu_rata_c, jarak_pesisir_km, tingkat_korosi, diambil_pada)
                  VALUES ({0}, 2400, 82, 27.5, 3.0, 'tinggi', now());",
                legacyProyekId);

            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO evaluasi (id, komponen_id, bahan_id, skor_kesesuaian, emisi_awal_kg, alasan, skor_termal, skor_lembap, skor_korosi, skor_usia, dihitung_pada)
                  VALUES ({0}, {1}, {2}, 72, 12000, 'Alasan evaluasi lama', 100, 50, 88, 50, now());",
                legacyEvaluasiId, legacyKomponenId, legacyBahanId);

            await db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO simulasi_emisi (evaluasi_id, horizon_tahun, usia_pakai_efektif_tahun, jumlah_penggantian, emisi_kumulatif_kg)
                  VALUES ({0}, 30, 43.0, 0, 12000);",
                legacyEvaluasiId);
        }

        // Langkah 2: Migrasikan maju ke AddV2PersistenceHistory
        await using (var db = dbTest.Factory.CreateDbContext())
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync();
        }

        // Langkah 3: Verifikasi data legacy tetap utuh dan provenance di-backfill ke 'LegacyUnknown'
        await using (var db = dbTest.Factory.CreateDbContext())
        {
            var proyek = await db.Bangunan.FirstOrDefaultAsync(b => b.Id == legacyProyekId);
            Assert.NotNull(proyek);
            Assert.Equal("Proyek Warisan Legacy", proyek!.Nama);
            Assert.Equal(1, proyek.RevisiLokasi);

            var iklim = await db.ProfilIklim.FirstOrDefaultAsync(i => i.BangunanId == legacyProyekId);
            Assert.NotNull(iklim);
            Assert.Equal("LegacyUnknown", iklim!.Provenance);
            Assert.NotEqual("ApiHistoris", iklim.Provenance);
            Assert.Equal(1, iklim.RevisiLokasi);

            var evaluasi = await db.Evaluasi.FirstOrDefaultAsync(e => e.Id == legacyEvaluasiId);
            Assert.NotNull(evaluasi);
            Assert.Equal("v2.0", evaluasi!.VersiModel);

            var simulasi = await db.SimulasiEmisi.FirstOrDefaultAsync(s => s.EvaluasiId == legacyEvaluasiId);
            Assert.NotNull(simulasi);
            Assert.NotEqual(Guid.Empty, simulasi!.Id);
            Assert.Equal(30, simulasi.HorizonTahun);
            Assert.Equal("v2.0", simulasi.VersiModel);
            Assert.Equal(12000, simulasi.EmisiKumulatifKg);

            // Langkah 4: Pastikan re-running migrasi bersifat idempoten tanpa galat (T37)
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync();
        }
    }

    [PerluPostgreSql]
    public async Task Skenario3_MultiHorizonSimulasiPersistence_T20()
    {
        await using var dbTest = await PostgreSqlTestDatabase.CreateAsync();

        // Terapkan skema penuh
        await using (var db = dbTest.Factory.CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        var proyekRepo = new ProyekRepositoryEf(dbTest.Factory);
        var evaluasiRepo = new EvaluasiRepositoryEf(dbTest.Factory);

        var proyekId = Guid.NewGuid();
        var komponenId = Guid.NewGuid();
        var evaluasiId = Guid.NewGuid();
        var bahanId = new Guid("d0000000-0000-4000-8000-000000000001");

        var proyek = new Proyek(proyekId, "Gedung Uji Horizon", "Jakarta", -6.20, 106.82);
        var komponen = new Komponen(komponenId, proyekId, JenisKomponen.Dinding, 150.0);
        await proyekRepo.AddAsync(proyek, komponen: [komponen]);

        var evaluasi = new Evaluasi(
            evaluasiId,
            bahanId,
            komponenId,
            80.0,
            18000.0,
            new SubSkorKesesuaian(85, 80, 75, 80),
            "Layak uji multi horizon");
        await evaluasiRepo.SimpanAsync([evaluasi]);

        // Simpan 3 horizon sekaligus (20, 30, 50)
        var sim20 = new SimulasiEmisi(evaluasiId, 20, 25.0, 0, 18000.0, "v2.0", 0.5);
        var sim30 = new SimulasiEmisi(evaluasiId, 30, 25.0, 1, 36000.0, "v2.0", 0.5);
        var sim50 = new SimulasiEmisi(evaluasiId, 50, 25.0, 2, 54000.0, "v2.0", 0.5);

        await evaluasiRepo.SimpanAsync(sim20);
        await evaluasiRepo.SimpanAsync(sim30);
        await evaluasiRepo.SimpanAsync(sim50);

        // Upsert horizon 20 dengan nilai emisi yang direvisi
        var sim20Revisi = new SimulasiEmisi(evaluasiId, 20, 25.0, 0, 19500.0, "v2.0", 0.5);
        await evaluasiRepo.SimpanAsync(sim20Revisi);

        // Muat ulang dari DbContext baru yang bersih
        await using (var dbFresh = dbTest.Factory.CreateDbContext())
        {
            var daftarSimulasi = await dbFresh.SimulasiEmisi.AsNoTracking()
                .Where(s => s.EvaluasiId == evaluasiId)
                .OrderBy(s => s.HorizonTahun)
                .ToListAsync();

            Assert.Equal(3, daftarSimulasi.Count);

            var h20 = daftarSimulasi.Single(s => s.HorizonTahun == 20);
            var h30 = daftarSimulasi.Single(s => s.HorizonTahun == 30);
            var h50 = daftarSimulasi.Single(s => s.HorizonTahun == 50);

            Assert.Equal(19500.0, h20.EmisiKumulatifKg);
            Assert.Equal(36000.0, h30.EmisiKumulatifKg);
            Assert.Equal(54000.0, h50.EmisiKumulatifKg);
        }
    }

    [PerluPostgreSql]
    public async Task Skenario4_BatchEvaluasiDeduplication()
    {
        await using var dbTest = await PostgreSqlTestDatabase.CreateAsync();

        await using (var db = dbTest.Factory.CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        var proyekRepo = new ProyekRepositoryEf(dbTest.Factory);
        var batchRepo = new BatchEvaluasiRepositoryEf(dbTest.Factory);

        var proyekId = Guid.NewGuid();
        var komponenId = Guid.NewGuid();
        var batchId1 = Guid.NewGuid();

        var proyek = new Proyek(proyekId, "Proyek Batch", "Surabaya", -7.25, 112.75);
        var komponen = new Komponen(komponenId, proyekId, JenisKomponen.Dinding, 200.0);
        await proyekRepo.AddAsync(proyek, komponen: [komponen]);

        string hashKanonik = "hash-kanonik-dedup-xyz789";

        var batch1 = new BatchEvaluasi(
            batchId1,
            proyekId,
            komponenId,
            revisiLokasi: 1,
            hashInput: hashKanonik,
            versiModel: "v2.0",
            bobot: KriteriaBobot.Default,
            luasM2: 200.0,
            dihitungPadaUtc: DateTimeOffset.UtcNow);

        var evaluasi1 = new Evaluasi(
            Guid.NewGuid(),
            new Guid("d0000000-0000-4000-8000-000000000001"),
            komponenId,
            78.0,
            24000.0,
            new SubSkorKesesuaian(80, 75, 80, 75),
            "Batch evaluasi pertama");

        await batchRepo.SimpanBatchAsync(batch1, [evaluasi1]);

        // Batch kedua dengan hash input sama (evaluasi ulang identik)
        var batchId2 = Guid.NewGuid();
        var batch2 = new BatchEvaluasi(
            batchId2,
            proyekId,
            komponenId,
            revisiLokasi: 1,
            hashInput: hashKanonik,
            versiModel: "v2.0",
            bobot: KriteriaBobot.Default,
            luasM2: 200.0,
            dihitungPadaUtc: DateTimeOffset.UtcNow.AddMinutes(10));

        var evaluasiUlang = evaluasi1 with { Id = Guid.NewGuid(), Alasan = "Percobaan ulang" };
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            batchRepo.SimpanBatchAsync(batch2 with { Id = Guid.NewGuid() },
                [evaluasiUlang with { Id = Guid.NewGuid() }])));

        // Verifikasi tidak ada duplikasi baris batch evaluasi untuk hash dan komponen yang sama
        await using (var dbFresh = dbTest.Factory.CreateDbContext())
        {
            var daftarBatch = await dbFresh.BatchEvaluasi.AsNoTracking()
                .Where(b => b.KomponenId == komponenId)
                .ToListAsync();

            Assert.Single(daftarBatch);
            Assert.Equal(batchId1, daftarBatch[0].Id);
            Assert.Equal(hashKanonik, daftarBatch[0].HashInput);
            Assert.Equal(batch1.DihitungPadaUtc.ToUnixTimeMilliseconds(),
                daftarBatch[0].DihitungPadaUtc.ToUnixTimeMilliseconds());
            var evaluasiTersimpan = await dbFresh.Evaluasi.AsNoTracking()
                .Where(e => e.KomponenId == komponenId).ToListAsync();
            Assert.Single(evaluasiTersimpan);
            Assert.Equal(evaluasi1.Id, evaluasiTersimpan[0].Id);
            Assert.Equal(evaluasi1.Alasan, evaluasiTersimpan[0].Alasan);
        }
    }

    [PerluPostgreSql]
    public async Task Skenario5_PekerjaanKeputusanJadwalDanPruneCache_T40_SertaLokasiBerubah_T31()
    {
        await using var dbTest = await PostgreSqlTestDatabase.CreateAsync();

        await using (var db = dbTest.Factory.CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        var proyekRepo = new ProyekRepositoryEf(dbTest.Factory);
        var pekerjaanRepo = new PekerjaanRepositoryEf(dbTest.Factory);
        var cacheRepo = new PrakiraanCacheRepositoryEf(dbTest.Factory);

        var proyekId = Guid.NewGuid();
        double latAwal = -5.45;
        double lonAwal = 105.26;

        var proyek = new Proyek(proyekId, "Proyek Penjadwalan", "Bandar Lampung", latAwal, lonAwal);
        await proyekRepo.AddAsync(proyek);

        var profil = new ProfilIklim(2400, 82, 27.5, 3.0, TingkatKorosi.Tinggi);
        await proyekRepo.SimpanProfilIklimAsync(proyekId, profil);

        // 1. Simpan prakiraan cuaca: satu tua (>30 hari) dan satu segar
        var tglMulai = new DateOnly(2026, 12, 1);
        var tglSelesai = new DateOnly(2026, 12, 10);
        var tglKunci = new DateOnly(2026, 12, 5);

        var hariTua = new DailyForecast(tglMulai, 50, 10.0, 24, 33, 85);
        var cacheTua = new HasilPrakiraan([hariTua], "Asia/Jakarta", DateTimeOffset.UtcNow.AddDays(-40), "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(latAwal, lonAwal, cacheTua);

        var hariSegar = new DailyForecast(tglSelesai, 20, 0.0, 25, 32, 70);
        var cacheSegar = new HasilPrakiraan([hariSegar], "Asia/Jakarta", DateTimeOffset.UtcNow.AddHours(-2), "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(latAwal, lonAwal, cacheSegar);

        // 2. Buat pekerjaan dan kunci tanggal
        var pekerjaanId = Guid.NewGuid();
        var pekerjaan = new Pekerjaan(
            pekerjaanId,
            proyekId,
            "Pengecoran Balok Struktur",
            JenisPekerjaan.Pengecoran,
            tglMulai,
            tglSelesai,
            StatusPekerjaan.Terkunci,
            tglKunci,
            konfirmasi: new KonfirmasiPenguncian(SetujuiRisiko: true, SetujuiDataLama: false, Catatan: "Disetujui mandor"));

        await pekerjaanRepo.AddAsync(pekerjaan);

        // 3. Bersihkan cache lama (> 30 hari)
        await cacheRepo.PruneExpiredAsync(DateTimeOffset.UtcNow.AddDays(-30));

        // 4. Verifikasi: cache lama terhapus, cache segar tetap ada
        await using (var dbFresh = dbTest.Factory.CreateDbContext())
        {
            var cacheRows = await dbFresh.PrakiraanCache.AsNoTracking().ToListAsync();
            Assert.Single(cacheRows);
            Assert.Equal(tglSelesai, cacheRows[0].TanggalCuaca);

            // 5. Verifikasi: snapshot keputusan jadwal TIDAK terhapus oleh pruning cache (T40)!
            var keputusanRows = await dbFresh.KeputusanJadwal.AsNoTracking()
                .Where(k => k.PekerjaanId == pekerjaanId)
                .ToListAsync();

            Assert.NotEmpty(keputusanRows);
            Assert.True(keputusanRows[0].PersetujuanRisiko);
        }

        // 6. Verifikasi T31 data layer: perubahan koordinat proyek mengubah status pekerjaan terkunci menjadi PerluDitinjau
        var proyekPindah = new Proyek(proyekId, "Proyek Pindah Lokasi", "Jakarta", -6.20, 106.82, revisiLokasi: 2);
        await proyekRepo.UpdateAsync(proyekPindah);

        var pekerjaanSetelahPindah = await pekerjaanRepo.GetByIdAsync(pekerjaanId);
        Assert.NotNull(pekerjaanSetelahPindah);
        Assert.Equal(StatusPekerjaan.PerluDitinjau, pekerjaanSetelahPindah!.Status);
        Assert.Equal(tglKunci, pekerjaanSetelahPindah.TanggalDikunci);
    }

    [PerluPostgreSql]
    public async Task Skenario6_ReportHistoryDanModelConfigPersistence()
    {
        await using var dbTest = await PostgreSqlTestDatabase.CreateAsync();

        await using (var db = dbTest.Factory.CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        var proyekRepo = new ProyekRepositoryEf(dbTest.Factory);
        var configRepo = new KonfigurasiModelRepositoryEf(dbTest.Factory);
        var laporanRepo = new LaporanRepositoryEf(dbTest.Factory);

        var proyekId = Guid.NewGuid();
        var proyek = new Proyek(proyekId, "Proyek Laporan", "Semarang", -7.00, 110.42);
        await proyekRepo.AddAsync(proyek);

        // Simpan konfigurasi model baru
        var customConfig = new KonfigurasiModel(
            versiModel: "v2.5-penelitian",
            bobotLembapHujanAlpha: 0.65,
            acuanCurahHujanMm: 2500.0,
            acuanJarakPesisirKm: 45.0,
            pitaTermalBatasBawah: 0.15,
            pitaTermalBatasAtasSejuk: 0.55,
            pitaTermalBatasAtasPanas: 0.28,
            suhuSejukC: 25.5,
            suhuPanasC: 32.5,
            faktorPenuaanA: 0.48,
            ambangTidakDisarankan: 52.0,
            ambangCocok: 78.0);

        await configRepo.SimpanAsync(customConfig);

        var aktif = await configRepo.GetActiveAsync();
        Assert.Equal("v2.5-penelitian", aktif.VersiModel);
        Assert.Equal(2500.0, aktif.AcuanCurahHujanMm);

        // Simpan metadata laporan PDF
        var laporanId = Guid.NewGuid();
        var laporan = new Laporan(
            laporanId,
            proyekId,
            "/home/user/Wastu_Rekomendasi_Semarang.pdf",
            DateTimeOffset.UtcNow,
            checksumSha256: "sha256-hex-sample-1234567890abcdef",
            snapshotHash: "snapshot-hash-v2.5",
            ukuranByte: 2048000,
            status: "Tersimpan", snapshotJson: "{\"NamaProyek\":\"Proyek Laporan\",\"VersiModel\":\"v2.5-penelitian\"}");

        await laporanRepo.SimpanAsync(laporan);

        // Reload dari DbContext baru
        await using (var dbFresh = dbTest.Factory.CreateDbContext())
        {
            var laporanDb = await dbFresh.Laporan.AsNoTracking().FirstOrDefaultAsync(l => l.Id == laporanId);
            Assert.NotNull(laporanDb);
            Assert.Equal("/home/user/Wastu_Rekomendasi_Semarang.pdf", laporanDb!.PathBerkas);
            Assert.Equal("sha256-hex-sample-1234567890abcdef", laporanDb.ChecksumSha256);
            Assert.Equal("snapshot-hash-v2.5", laporanDb.HashSnapshot);
            Assert.Equal(2048000, laporanDb.UkuranByte);
            Assert.Equal(laporan.SnapshotJson, laporanDb.OpsiJson);
        }
        var restoredReport = await laporanRepo.GetByIdAsync(laporanId);
        Assert.Equal(laporan.SnapshotJson, restoredReport!.SnapshotJson);
        await laporanRepo.SimpanAsync(restoredReport with { PathBerkas = "salinan.pdf" });
        Assert.Equal(laporan.SnapshotJson, (await laporanRepo.GetByIdAsync(laporanId))!.SnapshotJson);
    }

    [PerluPostgreSql]
    public async Task Skenario7_PemeriksaSkemaDatabase_MelaporkanPendingDanUpToDate()
    {
        await using var dbTest = await PostgreSqlTestDatabase.CreateAsync();

        var pemeriksa = new PemeriksaSkemaDatabase(dbTest.Factory);

        // 1. Pada DB baseline (hanya dimigrasi ke InitialCreate)
        await using (var db = dbTest.Factory.CreateDbContext())
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261109011548_InitialCreate");
        }

        var hasilBaseline = await pemeriksa.PeriksaAsync();
        Assert.True(hasilBaseline.DatabaseDapatDiakses);
        Assert.True(hasilBaseline.AdaMigrasiTertunda);
        Assert.False(hasilBaseline.ApakahKompatibel);
        Assert.Contains(hasilBaseline.MigrasiTertunda, m => m.Contains("AddV2PersistenceHistory"));

        // 2. Terapkan seluruh migrasi
        await using (var db = dbTest.Factory.CreateDbContext())
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync();
        }

        var hasilLengkap = await pemeriksa.PeriksaAsync();
        Assert.True(hasilLengkap.DatabaseDapatDiakses);
        Assert.False(hasilLengkap.AdaMigrasiTertunda);
        Assert.True(hasilLengkap.ApakahKompatibel);
        Assert.Empty(hasilLengkap.MigrasiTertunda);
        Assert.Contains(hasilLengkap.MigrasiDiterapkan, m => m.Contains("AddV2PersistenceHistory"));
    }
}
