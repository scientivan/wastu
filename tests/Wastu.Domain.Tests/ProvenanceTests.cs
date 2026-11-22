namespace Wastu.Domain.Tests;

public class ProvenanceTests
{
    [Fact]
    public void ProfilIklim_KonstruktorLama_MemilikiProvenanceLegacyUnknown()
    {
        var profil = new ProfilIklim(2000, 75, 27, 10, TingkatKorosi.Sedang);

        Assert.Equal(ProvenanceIklim.LegacyUnknown, profil.Provenance);
        Assert.Equal(1, profil.RevisiLokasi);
        Assert.Null(profil.Metadata);
    }

    [Fact]
    public void ProfilIklim_DenganMetadata_MenyimpanMetadataDanProvenance()
    {
        var metadata = new MetadataIklim(
            PeriodeAwal: new DateOnly(2020, 1, 1),
            PeriodeAkhir: new DateOnly(2024, 1, 1),
            Penyedia: "Open-Meteo",
            Model: "ERA5",
            DiambilPadaUtc: DateTimeOffset.UtcNow,
            ZonaWaktu: "Asia/Jakarta",
            MetodePesisir: "Haversine Proximity",
            Provenance: ProvenanceIklim.ApiHistoris,
            StatusPengambilan: StatusPengambilanIklim.Tersedia);

        var profil = new ProfilIklim(2500, 80, 28, 5, TingkatKorosi.Tinggi, metadata, revisiLokasi: 3);

        Assert.Equal(ProvenanceIklim.ApiHistoris, profil.Provenance);
        Assert.Equal(3, profil.RevisiLokasi);
        Assert.NotNull(profil.Metadata);
        Assert.Equal("Open-Meteo", profil.Metadata.Penyedia);
    }

    [Fact]
    public void ProfilIklim_NilaiNonFinite_MelemparPengecualian()
    {
        Assert.Throws<ArgumentException>(() => new ProfilIklim(double.NaN, 75, 27, 10, TingkatKorosi.Sedang));
        Assert.Throws<ArgumentException>(() => new ProfilIklim(2000, 75, 27, double.NaN, TingkatKorosi.Sedang));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProfilIklim(2000, double.NaN, 27, 10, TingkatKorosi.Sedang));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProfilIklim(2000, 75, double.NaN, 10, TingkatKorosi.Sedang));
    }

    [Fact]
    public void Bahan_KonstruktorLama_MemilikiVersiDataSatuDanSusunanNull()
    {
        var bahan = new Bahan(
            Guid.NewGuid(), "Bata Merah", JenisKomponen.Dinding,
            0.8, 3, 3, 30, 25.0, "SNI 15-2094-2000");

        Assert.Equal(1, bahan.VersiData);
        Assert.Null(bahan.SusunanKetebalan);
        Assert.Null(bahan.SumberProperti);
    }

    [Fact]
    public void Bahan_DenganSumberPropertiDanKetebalan_TersimpanLengkap()
    {
        var id = Guid.NewGuid();
        var sumberList = new List<SumberPropertiBahan>
        {
            new(id, "KonduktivitasTermal", "SNI 03-6389-2000", "2000", "https://bsn.go.id", StatusVerifikasiProperti.Terverifikasi, "Tabel A.1")
        };

        var bahan = new Bahan(
            id, "Bata Ringan AAC", JenisKomponen.Dinding,
            0.15, 4, 3, 40, 18.0, "Katalog Hebel",
            susunanKetebalan: "10 cm plesteran 2 sisi",
            versiData: 2,
            sumberProperti: sumberList);

        Assert.Equal("10 cm plesteran 2 sisi", bahan.SusunanKetebalan);
        Assert.Equal(2, bahan.VersiData);
        var sumber = Assert.Single(bahan.SumberProperti!);
        Assert.Equal(StatusVerifikasiProperti.Terverifikasi, sumber.Status);
        Assert.Equal("KonduktivitasTermal", sumber.Properti);
    }

    [Fact]
    public void Bahan_NilaiNonFinite_MelemparPengecualian()
    {
        Assert.Throws<ArgumentException>(() =>
            new Bahan(Guid.NewGuid(), "Bata", JenisKomponen.Dinding, double.NaN, 3, 3, 30, 25.0, "Sumber"));
        Assert.Throws<ArgumentException>(() =>
            new Bahan(Guid.NewGuid(), "Bata", JenisKomponen.Dinding, 0.5, 3, 3, double.NaN, 25.0, "Sumber"));
        Assert.Throws<ArgumentException>(() =>
            new Bahan(Guid.NewGuid(), "Bata", JenisKomponen.Dinding, 0.5, 3, 3, 30, double.NaN, "Sumber"));
    }
}
