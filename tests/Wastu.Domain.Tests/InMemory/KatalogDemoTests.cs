using Wastu.Domain;
using Wastu.Domain.Tests.Fixtures;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.InMemory;

public class KatalogDemoTests
{
    [Fact]
    public void JumlahBahanMemenuhiSyaratPRD()
    {
        // PRD §4.1 mensyaratkan minimal 15 bahan dinding dan 10 bahan atap.
        Assert.Equal(15, KatalogDemo.Dinding.Count);
        Assert.Equal(10, KatalogDemo.Atap.Count);
        Assert.Equal(25, KatalogDemo.Semua.Count);
    }

    [Fact]
    public void SemuaAdalahGabunganDindingDanAtap()
    {
        // Uji ini menangkap bug urutan inisialisasi statis: bila Semua dideklarasikan
        // sebelum Dinding dan Atap, prosesnya akan melihat array kosong dan test ini
        // gagal dengan jumlah 0.
        Assert.Equal(KatalogDemo.Dinding.Count + KatalogDemo.Atap.Count, KatalogDemo.Semua.Count);
    }

    [Fact]
    public void TiapBahanPunyaSumberData()
    {
        // PRD §1 dan §11.4 menjadikan sumber data sebagai komitmen transparansi,
        // bukan metadata opsional. Bahan tanpa sumber tidak boleh lolos.
        foreach (Bahan bahan in KatalogDemo.Semua)
        {
            Assert.False(string.IsNullOrWhiteSpace(bahan.SumberData), $"{bahan.Nama} tidak punya sumber data.");
        }
    }

    [Fact]
    public void IdBahanUnik()
    {
        // Tabel bahan punya bahan_nama_unik UNIQUE (nama, jenis) dan id primary key.
        Guid[] id = KatalogDemo.Semua.Select(b => b.Id).ToArray();
        Assert.Equal(id.Length, id.Distinct().Count());

        (string, JenisKomponen)[] kunci = KatalogDemo.Semua
            .Select(b => (b.Nama, b.Jenis))
            .ToArray();

        Assert.Equal(kunci.Length, kunci.Distinct().Count());
    }

    [Fact]
    public void SetiapBahanKonstruktorTervalidasi_SaatDibangun()
    {
        // Membangun KatalogDemo sudah menjalankan validasi konstruktor Bahan untuk
        // semua 25 entri. Test ini hanya memastikan prosesnya benar-benar terjadi.
        Assert.All(KatalogDemo.Semua, bahan =>
        {
            Assert.True(bahan.KonduktivitasTermal > 0);
            Assert.InRange(bahan.KetahananLembap, 1, 5);
            Assert.InRange(bahan.KetahananKorosi, 1, 5);
            Assert.True(bahan.UsiaPakaiNominalTahun > 0);
            Assert.True(bahan.FaktorEmisi >= 0);
        });
    }

    [Fact]
    public async Task RepositoryYangDibuatSudahBerisiKatalog()
    {
        MaterialRepositoryInMemory repository = KatalogDemo.BuatRepository();

        Assert.Equal(15, (await repository.ListByKindAsync(JenisKomponen.Dinding)).Count);
        Assert.Equal(10, (await repository.ListByKindAsync(JenisKomponen.Atap)).Count);
    }

    [Fact]
    public void NilaiBahanSesuaiSeed()
    {
        // Jangkar regresi ke database/03_seed_supabase.sql untuk beberapa nilai yang
        // dipakai dokumen dan demo, supaya katalog C# tidak menyimpang diam-diam.
        Bahan beton = Cari(DataContoh.IdBetonBertulang);
        Assert.Equal("Beton bertulang (in-situ)", beton.Nama);
        Assert.Equal(1.40, beton.KonduktivitasTermal, 2);
        Assert.Equal(4, beton.KetahananLembap);
        Assert.Equal(5, beton.KetahananKorosi);
        Assert.Equal(50, beton.UsiaPakaiNominalTahun);
        Assert.Equal(120, beton.FaktorEmisi);

        Bahan baja = Cari(DataContoh.IdBajaGalvanis);
        Assert.Equal("Baja galvanis (trapezium)", baja.Nama);
        Assert.Equal(15, baja.UsiaPakaiNominalTahun);
        Assert.Equal(90, baja.FaktorEmisi);
    }

    private static Bahan Cari(Guid id)
        => KatalogDemo.Semua.Single(b => b.Id == id);
}