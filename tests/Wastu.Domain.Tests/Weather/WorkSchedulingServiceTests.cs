using Wastu.Domain;
using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Weather;

public class WorkSchedulingServiceTests
{
    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;
        public TestTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
        public void Advance(TimeSpan span) => _utcNow += span;
    }

    private sealed class StubForecastClient : IDailyForecastClient
    {
        public Func<double, double, int, CancellationToken, Task<HasilPrakiraan>>? Handler { get; set; }

        public Task<IReadOnlyList<DailyForecast>> GetForecastAsync(double lat, double lon, int days, CancellationToken ct = default)
            => throw new NotImplementedException();

        public async Task<HasilPrakiraan> GetForecastWithMetadataAsync(double lat, double lon, int days, CancellationToken ct = default)
        {
            if (Handler is not null)
            {
                return await Handler(lat, lon, days, ct);
            }
            return new HasilPrakiraan([], "Asia/Jakarta", DateTimeOffset.UtcNow, "Open-Meteo", 1);
        }
    }

    [Fact]
    public async Task T22_PindahProyekDanApiMati_CacheTidakTertukarDanFallbackDataLama()
    {
        // Skenario T22: Proyek A dan B, pindah A -> B -> A, API mati.
        // Cache A dan B tidak boleh tertukar / hilang. Cache lama A diberi label DataLama.
        var waktuAwalUtc = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(waktuAwalUtc);

        var proyekRepo = new ProyekRepositoryInMemory();
        var pekerjaanRepo = new PekerjaanRepositoryInMemory();
        var cacheRepo = new PrakiraanCacheRepositoryInMemory();
        var ambangRepo = new AmbangPekerjaanRepositoryInMemory(pekerjaanRepo);
        var forecastClient = new StubForecastClient();

        var service = new WorkSchedulingService(
            pekerjaanRepo, proyekRepo, cacheRepo, forecastClient, ambangRepo, clock);

        var tgl1 = new DateOnly(2026, 11, 20);
        var tgl2 = new DateOnly(2026, 11, 21);

        // Proyek A di Jakarta
        var idA = Guid.NewGuid();
        var proyekA = new Proyek(idA, "Proyek A", "Jakarta", -6.200, 106.800, revisiLokasi: 1, zonaWaktu: "Asia/Jakarta");
        await proyekRepo.AddAsync(proyekA);

        var jobAId = Guid.NewGuid();
        var jobA = new Pekerjaan(jobAId, idA, "Pengecoran A", JenisPekerjaan.Pengecoran, tgl1, tgl2);
        await pekerjaanRepo.AddAsync(jobA);

        // Proyek B di Surabaya
        var idB = Guid.NewGuid();
        var proyekB = new Proyek(idB, "Proyek B", "Surabaya", -7.250, 112.750, revisiLokasi: 1, zonaWaktu: "Asia/Jakarta");
        await proyekRepo.AddAsync(proyekB);

        var jobBId = Guid.NewGuid();
        var jobB = new Pekerjaan(jobBId, idB, "Pengecoran B", JenisPekerjaan.Pengecoran, tgl1, tgl2);
        await pekerjaanRepo.AddAsync(jobB);

        // Simpan cache awal untuk A (probabilitas hujan 20%) dan B (probabilitas hujan 80%)
        var cuacaA = new DailyForecast(tgl1, 20, 0, 24, 32, 70);
        var cacheA = new HasilPrakiraan([cuacaA], "Asia/Jakarta", waktuAwalUtc, "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(proyekA.Latitude, proyekA.Longitude, cacheA);

        var cuacaB = new DailyForecast(tgl1, 80, 15, 24, 31, 85);
        var cacheB = new HasilPrakiraan([cuacaB], "Asia/Jakarta", waktuAwalUtc, "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(proyekB.Latitude, proyekB.Longitude, cacheB);

        // Evaluasi A: harus membaca cache A (20% -> Layak)
        var evalA1 = await service.EvaluateDaysDetailedAsync(jobAId);
        Assert.Equal(StatusKelayakanCuaca.Layak, evalA1[0].Status);
        Assert.False(evalA1[0].DataLama);

        // Evaluasi B: harus membaca cache B (80% -> TidakLayak), cache A tidak tertukar ke B!
        var evalB1 = await service.EvaluateDaysDetailedAsync(jobBId);
        Assert.Equal(StatusKelayakanCuaca.TidakLayak, evalB1[0].Status);
        Assert.False(evalB1[0].DataLama);

        // Majukan waktu 7 jam (cache A dan B kini stale >= 6 jam)
        clock.Advance(TimeSpan.FromHours(7));

        // API mati: melempar exception saat refresh
        forecastClient.Handler = (_, _, _, _) => throw new HttpRequestException("Jaringan luring / API mati.");

        // Kembali evaluasi A: API gagal, harus fallback ke cache lama A milik Proyek A (bukan milik B!), berlabel DataLama = true
        var evalA2 = await service.EvaluateDaysDetailedAsync(jobAId);
        Assert.Equal(StatusKelayakanCuaca.Layak, evalA2[0].Status);
        Assert.True(evalA2[0].DataLama);
        Assert.NotNull(evalA2[0].UmurCache);
        Assert.True(evalA2[0].UmurCache >= TimeSpan.FromHours(7));

        // Evaluasi B: fallback ke cache lama B (80% TidakLayak)
        var evalB2 = await service.EvaluateDaysDetailedAsync(jobBId);
        Assert.Equal(StatusKelayakanCuaca.TidakLayak, evalB2[0].Status);
        Assert.True(evalB2[0].DataLama);
    }

    [Fact]
    public async Task T23_PenguncianTanggalMengikutiZonaProyek_MenolakTanggalLampau()
    {
        // Skenario T23: Host UTC/WIB, proyek di Jayapura (WIT = UTC+9).
        // Waktu UTC: 2026-11-20 16:00:00 -> Di Jayapura sudah tanggal 2026-11-21 01:00:00.
        // Tanggal 2026-11-20 adalah tanggal lampau pada zona proyek dan wajib ditolak!
        var nowUtc = new DateTimeOffset(2026, 11, 20, 16, 0, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(nowUtc);

        var proyekRepo = new ProyekRepositoryInMemory();
        var pekerjaanRepo = new PekerjaanRepositoryInMemory();
        var cacheRepo = new PrakiraanCacheRepositoryInMemory();
        var ambangRepo = new AmbangPekerjaanRepositoryInMemory(pekerjaanRepo);
        var forecastClient = new StubForecastClient();

        var service = new WorkSchedulingService(
            pekerjaanRepo, proyekRepo, cacheRepo, forecastClient, ambangRepo, clock);

        var proyekId = Guid.NewGuid();
        var proyek = new Proyek(proyekId, "Proyek Jayapura", "Jayapura", -2.53, 140.71, revisiLokasi: 1, zonaWaktu: "Asia/Jayapura");
        await proyekRepo.AddAsync(proyek);

        var tglKemarinDiWit = new DateOnly(2026, 11, 20);
        var tglHariIniDiWit = new DateOnly(2026, 11, 21);

        var jobId = Guid.NewGuid();
        var job = new Pekerjaan(jobId, proyekId, "Pengecoran", JenisPekerjaan.Pengecoran, tglKemarinDiWit, tglHariIniDiWit.AddDays(5));
        await pekerjaanRepo.AddAsync(job);

        // Sediakan cuaca valid untuk kedua tanggal
        var hari1 = new DailyForecast(tglKemarinDiWit, 20, 0, 24, 31, 70);
        var hari2 = new DailyForecast(tglHariIniDiWit, 20, 0, 24, 31, 70);
        var cache = new HasilPrakiraan([hari1, hari2], "Asia/Jayapura", nowUtc, "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(proyek.Latitude, proyek.Longitude, cache);

        // Mengunci tanggal 2026-11-20 (kemarin di WIT) harus ditolak dengan exception
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LockDateAsync(jobId, tglKemarinDiWit, new KonfirmasiPenguncian()));
        Assert.Contains("tanggal lampau", ex.Message);

        // Mengunci tanggal 2026-11-21 (hari ini di WIT) berhasil
        await service.LockDateAsync(jobId, tglHariIniDiWit, new KonfirmasiPenguncian());

        var jobTerkunci = await pekerjaanRepo.GetByIdAsync(jobId);
        Assert.Equal(StatusPekerjaan.Terkunci, jobTerkunci?.Status);
        Assert.Equal(tglHariIniDiWit, jobTerkunci?.TanggalDikunci);
    }

    [Fact]
    public async Task T29_RentangPekerjaan30Hari_SeluruhHariDinilai_HariTanpaDataTolakPenguncian()
    {
        // Skenario T29: rentang pekerjaan 30 hari (hari 1–30), provider hanya mempunyai 14 hari.
        // Seluruh 30 hari dievaluasi; hari 15–30 berstatus DataTidakTersedia.
        // Penguncian hari 20 (tanpa cuaca) ditolak; draft tetap tersimpan.
        var nowUtc = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(nowUtc);

        var proyekRepo = new ProyekRepositoryInMemory();
        var pekerjaanRepo = new PekerjaanRepositoryInMemory();
        var cacheRepo = new PrakiraanCacheRepositoryInMemory();
        var ambangRepo = new AmbangPekerjaanRepositoryInMemory(pekerjaanRepo);
        var forecastClient = new StubForecastClient();

        var service = new WorkSchedulingService(
            pekerjaanRepo, proyekRepo, cacheRepo, forecastClient, ambangRepo, clock);

        var tglAwal = new DateOnly(2026, 11, 20);
        var tglAkhir = tglAwal.AddDays(29); // 30 hari total

        var proyekId = Guid.NewGuid();
        var proyek = new Proyek(proyekId, "Proyek 30 Hari", "Jakarta", -6.20, 106.81, revisiLokasi: 1, zonaWaktu: "Asia/Jakarta");
        await proyekRepo.AddAsync(proyek);

        var jobId = Guid.NewGuid();
        var job = new Pekerjaan(jobId, proyekId, "Pengecoran Struktur Besar", JenisPekerjaan.Pengecoran, tglAwal, tglAkhir);
        await pekerjaanRepo.AddAsync(job);

        // Provider hanya menyediakan 14 hari prakiraan (hari 1 s/d 14)
        var daftar14Hari = Enumerable.Range(0, 14).Select(i =>
            new DailyForecast(tglAwal.AddDays(i), 20, 0, 24, 32, 65)).ToList();
        var cache = new HasilPrakiraan(daftar14Hari, "Asia/Jakarta", nowUtc, "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(proyek.Latitude, proyek.Longitude, cache);

        // Evaluasi seluruh 30 hari
        var hasilEvaluasi = await service.EvaluateDaysDetailedAsync(jobId);
        Assert.Equal(30, hasilEvaluasi.Count);

        // 14 hari pertama: Layak
        for (int i = 0; i < 14; i++)
        {
            Assert.Equal(StatusKelayakanCuaca.Layak, hasilEvaluasi[i].Status);
        }

        // Hari ke 15 s/d 30: DataTidakTersedia dengan alasan jelas
        for (int i = 14; i < 30; i++)
        {
            Assert.Equal(StatusKelayakanCuaca.DataTidakTersedia, hasilEvaluasi[i].Status);
            Assert.Contains("di luar rentang 14 hari", hasilEvaluasi[i].Alasan[0]);
        }

        // Penguncian final pada hari ke-20 (di luar rentang prakiraan cuaca) ditolak!
        var tglDiLuar = tglAwal.AddDays(19);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LockDateAsync(jobId, tglDiLuar, new KonfirmasiPenguncian()));
        Assert.Contains("tidak tersedia", ex.Message);

        // Pekerjaan tetap tersimpan sebagai Draft di repository (draft stays saved!)
        var jobDraft = await pekerjaanRepo.GetByIdAsync(jobId);
        Assert.NotNull(jobDraft);
        Assert.Equal(StatusPekerjaan.Draft, jobDraft!.Status);
        Assert.Null(jobDraft.TanggalDikunci);
    }

    [Fact]
    public async Task T30_PenguncianHariTidakLayakDanDataLama_MemerlukanKonfirmasiEksplisit()
    {
        // Skenario T30:
        // - Mengunci hari TidakLayak tanpa SetujuiRisiko ditolak; dengan SetujuiRisiko terkunci beserta snapshot.
        // - Mengunci data lama (>= 6 jam) tanpa SetujuiDataLama ditolak; dengan SetujuiDataLama berhasil.
        var nowUtc = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(nowUtc);

        var proyekRepo = new ProyekRepositoryInMemory();
        var pekerjaanRepo = new PekerjaanRepositoryInMemory();
        var cacheRepo = new PrakiraanCacheRepositoryInMemory();
        var ambangRepo = new AmbangPekerjaanRepositoryInMemory(pekerjaanRepo);
        var forecastClient = new StubForecastClient();

        var service = new WorkSchedulingService(
            pekerjaanRepo, proyekRepo, cacheRepo, forecastClient, ambangRepo, clock);

        var tgl1 = new DateOnly(2026, 11, 20);
        var tgl2 = new DateOnly(2026, 11, 21);

        var proyekId = Guid.NewGuid();
        var proyek = new Proyek(proyekId, "Proyek T30", "Bandung", -6.91, 107.61, revisiLokasi: 1, zonaWaktu: "Asia/Jakarta");
        await proyekRepo.AddAsync(proyek);

        var jobId = Guid.NewGuid();
        var job = new Pekerjaan(jobId, proyekId, "Pengecatan Fasad", JenisPekerjaan.Pengecatan, tgl1, tgl2);
        await pekerjaanRepo.AddAsync(job);

        // Hari 1: TidakLayak (RH 92% > 85%)
        var cuacaHujan = new DailyForecast(tgl1, 30, 0, 22, 29, 92);
        var cache = new HasilPrakiraan([cuacaHujan], "Asia/Jakarta", nowUtc, "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(proyek.Latitude, proyek.Longitude, cache);

        // 1. Kunci hari TidakLayak tanpa persetujuan risiko -> DITOLAK
        var exRisiko = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LockDateAsync(jobId, tgl1, new KonfirmasiPenguncian(SetujuiRisiko: false)));
        Assert.Contains("persetujuan risiko", exRisiko.Message);

        var jobBelumKunci = await pekerjaanRepo.GetByIdAsync(jobId);
        Assert.Equal(StatusPekerjaan.Draft, jobBelumKunci?.Status);

        // 2. Kunci hari TidakLayak dengan persetujuan risiko -> BERHASIL
        var konfirmasi = new KonfirmasiPenguncian(SetujuiRisiko: true, Catatan: "Telah disetujui mandor pelaksana.");
        await service.LockDateAsync(jobId, tgl1, konfirmasi);

        var jobKunci = await pekerjaanRepo.GetByIdAsync(jobId);
        Assert.Equal(StatusPekerjaan.Terkunci, jobKunci?.Status);
        Assert.Equal(tgl1, jobKunci?.TanggalDikunci);
        Assert.NotNull(jobKunci?.KeputusanTerakhir);
        Assert.True(jobKunci!.KeputusanTerakhir!.PersetujuanRisiko);
        Assert.Equal(StatusKelayakanCuaca.TidakLayak, jobKunci.KeputusanTerakhir.Status);
        Assert.Contains("92%", jobKunci.KeputusanTerakhir.Catatan[0]);

        // 3. Uji penguncian data lama (>= 6 jam)
        clock.Advance(TimeSpan.FromHours(8));
        forecastClient.Handler = (_, _, _, _) => throw new HttpRequestException("Luring");

        var jobId2 = Guid.NewGuid();
        var job2 = new Pekerjaan(jobId2, proyekId, "Pengecatan 2", JenisPekerjaan.Pengecatan, tgl1, tgl2);
        await pekerjaanRepo.AddAsync(job2);

        // Data cache berumur 8 jam -> tanpa SetujuiDataLama ditolak
        var exDataLama = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LockDateAsync(jobId2, tgl1, new KonfirmasiPenguncian(SetujuiRisiko: true, SetujuiDataLama: false)));
        Assert.Contains("data lama", exDataLama.Message);

        // Dengan SetujuiDataLama = true berhasil
        await service.LockDateAsync(jobId2, tgl1, new KonfirmasiPenguncian(SetujuiRisiko: true, SetujuiDataLama: true));
        var job2Kunci = await pekerjaanRepo.GetByIdAsync(jobId2);
        Assert.Equal(StatusPekerjaan.Terkunci, job2Kunci?.Status);
        Assert.True(job2Kunci!.KeputusanTerakhir!.KonfirmasiDataLama);
    }

    [Fact]
    public async Task T31_PrakiraanMemburukAtauLokasiAmbangBerubah_MenandaiPerluDitinjauTanpaHapusTanggal()
    {
        // Skenario T31:
        // Kunci hari lalu:
        // a) Ubah lokasi proyek (revisiLokasi naik), ATAU
        // b) Override ambang diperketat, ATAU
        // c) Prakiraan cuaca baru memburuk (dari Layak menjadi TidakLayak).
        // Hasil: Status menjadi PerluDitinjau tanpa memindahkan TanggalDikunci atau merusak KeputusanTerakhir!
        var nowUtc = new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(nowUtc);

        var proyekRepo = new ProyekRepositoryInMemory();
        var pekerjaanRepo = new PekerjaanRepositoryInMemory();
        var cacheRepo = new PrakiraanCacheRepositoryInMemory();
        var ambangRepo = new AmbangPekerjaanRepositoryInMemory(pekerjaanRepo);
        var forecastClient = new StubForecastClient();

        var service = new WorkSchedulingService(
            pekerjaanRepo, proyekRepo, cacheRepo, forecastClient, ambangRepo, clock);

        var tglKunci = new DateOnly(2026, 11, 22);

        var proyekId = Guid.NewGuid();
        var proyek = new Proyek(proyekId, "Proyek T31", "Semarang", -7.00, 110.42, revisiLokasi: 1, zonaWaktu: "Asia/Jakarta");
        await proyekRepo.AddAsync(proyek);

        var jobId = Guid.NewGuid();
        var job = new Pekerjaan(jobId, proyekId, "Pengecoran Plat", JenisPekerjaan.Pengecoran, tglKunci.AddDays(-1), tglKunci.AddDays(2));
        await pekerjaanRepo.AddAsync(job);

        // Awalnya hari kunci berstatus Layak (p = 25%, hujan = 0, suhu = 32°C)
        var cuacaAwal = new DailyForecast(tglKunci, 25, 0, 24, 32, 65);
        var cacheAwal = new HasilPrakiraan([cuacaAwal], "Asia/Jakarta", nowUtc, "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(proyek.Latitude, proyek.Longitude, cacheAwal);

        await service.LockDateAsync(jobId, tglKunci, new KonfirmasiPenguncian());

        var jobTerkunci = await pekerjaanRepo.GetByIdAsync(jobId);
        Assert.Equal(StatusPekerjaan.Terkunci, jobTerkunci?.Status);
        Assert.Equal(tglKunci, jobTerkunci?.TanggalDikunci);
        Assert.Equal(StatusKelayakanCuaca.Layak, jobTerkunci?.KeputusanTerakhir?.Status);

        // Kasus 1: Prakiraan baru memburuk (hari kunci menjadi TidakLayak: hujan 80%, curah 15 mm)
        var cuacaBuruk = new DailyForecast(tglKunci, 80, 15, 24, 30, 85);
        var cacheBaru = new HasilPrakiraan([cuacaBuruk], "Asia/Jakarta", nowUtc.AddHours(1), "Open-Meteo", 1);
        await cacheRepo.SimpanCacheAsync(proyek.Latitude, proyek.Longitude, cacheBaru);

        var ditinjau1 = await service.TinjauUlangPekerjaanAsync(jobId);

        Assert.NotNull(ditinjau1);
        Assert.Equal(StatusPekerjaan.PerluDitinjau, ditinjau1!.Status);
        // Tanggal dikunci dan snapshot awal TIDAK boleh terhapus / berpindah otomatis!
        Assert.Equal(tglKunci, ditinjau1.TanggalDikunci);
        Assert.Equal(StatusKelayakanCuaca.Layak, ditinjau1.KeputusanTerakhir?.Status);

        // Kasus 2: Lokasi proyek berpindah (revisiLokasi bertambah)
        var proyekPindah = new Proyek(proyekId, "Proyek T31 Pindah", "Yogyakarta", -7.79, 110.37, revisiLokasi: 2, zonaWaktu: "Asia/Jakarta");
        await proyekRepo.UpdateAsync(proyekPindah);

        // Kembalikan status ke Terkunci untuk menguji pemicu lokasi
        await pekerjaanRepo.UpdateAsync(ditinjau1 with { Status = StatusPekerjaan.Terkunci });

        var ditinjau2 = await service.TinjauUlangPekerjaanAsync(jobId);
        Assert.Equal(StatusPekerjaan.PerluDitinjau, ditinjau2?.Status);
        Assert.Equal(tglKunci, ditinjau2?.TanggalDikunci);
    }
}
