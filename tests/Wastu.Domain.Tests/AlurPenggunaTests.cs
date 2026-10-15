using Wastu.Domain;
using Wastu.Domain.Catalog;
using Wastu.Domain.Climate;
using Wastu.Domain.Emissions;
using Wastu.Domain.Scoring;
using Wastu.Domain.Tests.Fakes;
using Wastu.InMemory;

namespace Wastu.Domain.Tests;

/// <summary>
/// Menjalankan alur pengguna dari proyek baru sampai melihat peringkat bahan dan
/// emisi Alive, memakai adapter in-memory.
/// </summary>
/// <remarks>
/// Test ini ada karena: kontrak port bisa saja benar satu per satu tapi tidak
/// dapat dirangkai. Yang diperiksa di sini bukan setiap perhitungan, melainkan
/// bahwa semuanya benar-benar saling terhubung di atas port yang sama.
/// Kriteria selesai PRD §15 MVP adalah pengguna membuat proyek baru, memasukkan
/// lokasi, dan melihat peringkat bahan dengan skor dan kgCO2e untuk satu
/// komponen; test ini menjalankan persis criteria itu.
/// </remarks>
public class AlurPenggunaTests
{
    [Fact]
    public async Task PenggunaMembuatProyekMelihatPeringkatDanMenghitungEmisi()
    {
        // --- Adapter tunggal untuk seluruh port, seperti satu basis data sungguhan.
        MaterialRepositoryInMemory material = KatalogDemo.BuatRepository();
        ProyekRepositoryInMemory proyek = new();
        EvaluasiRepositoryInMemory evaluasi = new();
        HistoricalClimateClientStub klien = new();

        var katalog = new MaterialService(material);
        var iklim = new ClimateAnalysisService(proyek, klien);
        var proyekService = new Wastu.Domain.Projects.ProjectService(proyek, iklim);
        var skor = new SuitabilityScoringService(material, proyek);
        var simulasi = new EmissionSimulationService(evaluasi, material);

        // --- F1: pengguna membuat proyek baru dan memasukkan lokasi.
        Proyek rumah = await proyekService.CreateProjectAsync(
            "Rumah Tapkan Pesisir", "Bandar Lampung", -5.45, 105.26);

        await proyekService.AddComponentAsync(rumah.Id, JenisKomponen.Dinding, luasM2: 120);
        IReadOnlyList<Komponen> komponen = await proyek.ListKomponenAsync(rumah.Id);
        Komponen dinding = Assert.Single(komponen);

        // --- F3: profil iklim diambil sekali lalu dipakai ulang.
        ProfilIklim profil = await iklim.GetOrFetchProfileAsync(rumah.Id);
        Assert.Equal(2400, profil.CurahHujanTahunanMm);

        // --- F2: katalog bahan untuk komponen dinding.
        IReadOnlyList<Bahan> katalogDinding = await katalog.ListByKindAsync(JenisKomponen.Dinding);
        Assert.Equal(15, katalogDinding.Count);

        // --- F4: peringkat bahan dengan skor dan kgCO2e berdampingan.
        IReadOnlyList<Evaluasi> peringkat = await skor.RankAsync(dinding.Id);
        Assert.Equal(15, peringkat.Count);

        foreach (Evaluasi item in peringkat.Take(3))
        {
            Assert.InRange(item.SkorKesesuaian, 0, 100);
            Assert.True(item.EmisiAwalKg > 0);
            Assert.False(string.IsNullOrWhiteSpace(item.Alasan));
        }

        // Peringkat benar-benar menurun, bukan hanya berisi angka yang masuk akal.
        for (int i = 1; i < peringkat.Count; i++)
        {
            Assert.True(peringkat[i - 1].SkorKesesuaian >= peringkat[i].SkorKesesuaian);
        }

        // --- F5: simulasi emisi untuk bahan peringkat teratas.
        await evaluasi.SimpanAsync(peringkat);
        SimulasiEmisi hasil = await simulasi.SimulateAsync(peringkat[0].Id, horizonTahun: 30);

        Assert.Equal(30, hasil.HorizonTahun);
        Assert.True(hasil.UsiaPakaiEfektifTahun > 0);
        Assert.True(hasil.JumlahPenggantian >= 0);
        Assert.Equal(
            peringkat[0].EmisiAwalKg * (1 + hasil.JumlahPenggantian),
            hasil.EmisiKumulatifKg,
            6);

        // Hasil simulasi benar-benar tersimpan, bukan sekadar dikembalikan.
        Assert.NotNull(evaluasi.AmbilSimulasi(peringkat[0].Id));
    }

    [Fact]
    public async Task PerubahanLokasiMengubahPeringkatBahan()
    {
        // PRD §F1: mengubah lokasi memicu pengambilan ulang profil iklim. Kalau ini
        // tidak bekerja, pengguna akan melihat peringkat untuk lokasi lama.
        MaterialRepositoryInMemory material = KatalogDemo.BuatRepository();
        ProyekRepositoryInMemory proyek = new();
        HistoricalClimateClientStub klien = new();

        var iklim = new ClimateAnalysisService(proyek, klien);
        var proyekService = new Wastu.Domain.Projects.ProjectService(proyek, iklim);
        var skor = new SuitabilityScoringService(material, proyek);

        Proyek rumah = await proyekService.CreateProjectAsync("Rumah", "Bandar Lampung", -5.45, 105.26);
        await proyekService.AddComponentAsync(rumah.Id, JenisKomponen.Dinding, 120);
        Komponen dinding = (await proyek.ListKomponenAsync(rumah.Id)).Single();

        // Klimat Bandar Lampung: pesisir, lembap, panas.
        await iklim.GetOrFetchProfileAsync(rumah.Id);
        IReadOnlyList<Evaluasi> diPesisir = await skor.RankAsync(dinding.Id);

        // Pindahkan ke inland yang lebih dingin dan kering; klien mengembalikan profil berbeda.
        klien.Hasil = new ProfilIklim(900, 58, 21.0, 150.0, TingkatKorosi.Rendah);
        await proyekService.UpdateLocationAsync(rumah.Id, -7.25, 112.75);
        IReadOnlyList<Evaluasi> diInland = await skor.RankAsync(dinding.Id);

        Assert.Equal(2, klien.JumlahPanggilan);
        Assert.Equal(15, diPesisir.Count);
        Assert.Equal(15, diInland.Count);

        // Peringkat harus benar-benar berbeda: peluangnya nol kalau tidak ada
        //iklim yang terpengaruh.
        Assert.NotEqual(
            diPesisir.Select(e => e.BahanId).ToArray(),
            diInland.Select(e => e.BahanId).ToArray());
    }
}