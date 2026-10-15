using Wastu.Domain.Catalog;
using Wastu.Domain.Tests.Fixtures;
using Wastu.InMemory;

namespace Wastu.Domain.Tests.Catalog;

public class MaterialServiceTests
{
    [Fact]
    public async Task ListByKind_MemisahkanDindingDanAtap()
    {
        MaterialService service = Service();

        Assert.Equal(15, (await service.ListByKindAsync(JenisKomponen.Dinding)).Count);
        Assert.Equal(10, (await service.ListByKindAsync(JenisKomponen.Atap)).Count);
    }

    [Fact]
    public async Task ListByKind_SemuaBahanPunyaJenisYangDiminta()
    {
        MaterialService service = Service();

        foreach (Bahan bahan in await service.ListByKindAsync(JenisKomponen.Dinding))
        {
            Assert.Equal(JenisKomponen.Dinding, bahan.Jenis);
        }
    }

    [Fact]
    public async Task ListByKind_SumberDataSelaluIkutDibawa()
    {
        // PRD §F2: sumber data setiap nilai wajib ditampilkan agar transparan, bukan
        // disembunyikan sebagai angka pasti (§11.4).
        MaterialService service = Service();

        foreach (Bahan bahan in await service.ListByKindAsync(JenisKomponen.Atap))
        {
            Assert.False(string.IsNullOrWhiteSpace(bahan.SumberData));
        }
    }

    [Fact]
    public async Task ListByKind_UrutanStabilAntarPemanggilan()
    {
        // Adapter in-memory memakai ConcurrentDictionary yang urutannya tidak dijamin;
        // katalog yang tampil ke pengguna tidak boleh ikut berganti-ganti urutan.
        MaterialService service = Service();

        IReadOnlyList<Bahan> pertama = await service.ListByKindAsync(JenisKomponen.Dinding);
        IReadOnlyList<Bahan> kedua = await service.ListByKindAsync(JenisKomponen.Dinding);

        Assert.Equal(
            pertama.Select(b => b.Id),
            kedua.Select(b => b.Id));
    }

    [Fact]
    public async Task GetById_MengembalikanBahanYangDiminta()
    {
        MaterialService service = Service();

        Bahan bahan = await service.GetByIdAsync(DataContoh.IdBetonBertulang);

        Assert.Equal("Beton bertulang (in-situ)", bahan.Nama);
        Assert.Equal(120, bahan.FaktorEmisi);
    }

    [Fact]
    public async Task GetById_BahanTidakAda_ThrowsDenganPesanYangJelas()
    {
        // Kontrak mengembalikan tipe non-nullable, jadi tidak boleh null yang lolos.
        MaterialService service = Service();

        KeyNotFoundException galat = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(Guid.NewGuid()));

        Assert.Contains("tidak ada di katalog", galat.Message, StringComparison.Ordinal);
    }

    private static MaterialService Service()
        => new(KatalogDemo.BuatRepository());
}