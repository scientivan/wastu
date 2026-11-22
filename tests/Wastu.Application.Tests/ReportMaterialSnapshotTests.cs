using Wastu.Domain;
using Wastu.Services;
using Xunit;
using UiEvaluasi = Wastu.Models.Evaluasi;
using UiIklim = Wastu.Models.ProfilIklim;

namespace Wastu.Application.Tests;

public sealed class ReportMaterialSnapshotTests
{
    [Fact]
    public void MaterialSnapshot_PreservesActualSubscoresIdentityRankAndAllPropertyProvenance()
    {
        var materialId = Guid.NewGuid();
        var componentId = Guid.NewGuid();
        SumberPropertiBahan[] sources =
        [
            new(materialId, "konduktivitas_termal", "Rujukan termal", "2021", "https://example.org/thermal", StatusVerifikasiProperti.Terverifikasi, "Konversi termal asli"),
            new(materialId, "ketahanan_lembap", "Rujukan lembap", "v2", null, StatusVerifikasiProperti.Perkiraan, "Indeks lembap"),
            new(materialId, "ketahanan_korosi", "Rujukan korosi", "v3", null, StatusVerifikasiProperti.Manual, "Indeks korosi"),
            new(materialId, "usia_pakai_nominal", "Rujukan usia", "2020", null, StatusVerifikasiProperti.Perkiraan),
            new(materialId, "faktor_emisi", "Rujukan emisi", "v7", "https://example.org/emission", StatusVerifikasiProperti.Perkiraan, "Konversi massa ke m² dengan tebal 150 mm")
        ];
        var item = ItemKatalog.DariDomain(new Bahan(materialId, "Panel atap uji", JenisKomponen.Atap,
            0.2, 2, 4, 30, 51.25, "Sumber ringkas legacy", "Panel tebal 150 mm", 7, sources));
        var result = new Evaluasi(Guid.NewGuid(), materialId, componentId, 74, 5125,
            new SubSkorKesesuaian(81, 62, 73, 84), "Alasan per bahan yang tersimpan");
        var evaluation = new UiEvaluasi(item.Bahan, result);

        var snapshot = SnapshotBahanMapper.Buat(item, evaluation, componentId, peringkat: 10);

        Assert.Equal(materialId, snapshot.BahanId);
        Assert.Equal(componentId, snapshot.KomponenId);
        Assert.Equal(JenisKomponen.Atap, snapshot.Jenis);
        Assert.Equal(10, snapshot.Peringkat);
        Assert.Equal(item.Bahan.Nama, snapshot.Nama);
        Assert.Equal(74, snapshot.SkorKesesuaian);
        Assert.Equal(5125, snapshot.EmisiAwalKg);
        Assert.Equal(result.Alasan, snapshot.Alasan);
        Assert.Equal(81, snapshot.SubSkorTermal);
        Assert.Equal(62, snapshot.SubSkorLembap);
        Assert.Equal(73, snapshot.SubSkorKorosi);
        Assert.Equal(84, snapshot.SubSkorUsia);
        Assert.Equal(sources.Length, snapshot.SumberProperti.Count);
        foreach (var source in sources)
        {
            var mapped = Assert.Single(snapshot.SumberProperti, s => s.Properti == source.Properti);
            Assert.Contains(source.Referensi, mapped.Sumber);
            Assert.Equal(source.Versi, mapped.Tahun);
            Assert.Contains(source.Status.ToString(), mapped.Sumber);
            if (source.Versi is not null) Assert.Contains(source.Versi, mapped.Sumber);
            if (source.Tautan is not null) Assert.Contains(source.Tautan, mapped.Sumber);
            if (source.CatatanKonversi is not null) Assert.Contains(source.CatatanKonversi, mapped.Sumber);
        }
        Assert.Equal("0.2 W/(m·K)", snapshot.SumberProperti.Single(s => s.Properti == "konduktivitas_termal").Nilai);
        Assert.Equal("51.25 kgCO₂e/m²", snapshot.SumberProperti.Single(s => s.Properti == "faktor_emisi").Nilai);
    }

    [Fact]
    public void DirectScoring_RetainsComputedSubscoresAndCustomWeightsForReport()
    {
        var item = ItemKatalog.DariDomain(new Bahan(Guid.NewGuid(), "Dinding uji", JenisKomponen.Dinding,
            0.2, 2, 4, 30, 51.25, "Referensi"));
        var evaluation = new UiEvaluasi(item.Bahan);
        var climate = new UiIklim { CurahHujanTahunan = 3000, KelembapanRata = 100, SuhuRata = 26, JarakPesisirKm = 50 };

        var score = evaluation.HitungSkor(climate, 20, 40, new KriteriaBobot(0.1, 0.2, 0.3, 0.4));
        var snapshot = SnapshotBahanMapper.Buat(item, evaluation, Guid.NewGuid(), 1);

        Assert.Equal(68, score, 10);
        Assert.Equal(100, snapshot.SubSkorTermal);
        Assert.Equal(40, snapshot.SubSkorLembap);
        Assert.Equal(100, snapshot.SubSkorKorosi);
        Assert.Equal(50, snapshot.SubSkorUsia);
    }

    [Theory]
    [InlineData(54.999, "Tidak disarankan")]
    [InlineData(55, "Perlu pertimbangan")]
    [InlineData(74.999, "Perlu pertimbangan")]
    [InlineData(75, "Cocok")]
    public void MaterialLabels_UseFullPrecisionDomainThresholds(double score, string label)
    {
        var item = ItemKatalog.DariDomain(new Bahan(Guid.NewGuid(), "Bahan uji", JenisKomponen.Dinding,
            0.2, 2, 4, 30, 50, "Referensi legacy"));
        var componentId = Guid.NewGuid();
        var evaluation = new UiEvaluasi(item.Bahan, new Evaluasi(Guid.NewGuid(), item.DataDomain.Id,
            componentId, score, 1000, new SubSkorKesesuaian(80, 80, 80, 80), "Alasan"));

        Assert.Equal(label, SnapshotBahanMapper.Buat(item, evaluation, componentId, 1).LabelKesesuaian);
    }

    [Fact]
    public void NoPropertyProvenance_UsesHonestLegacySourceFallback()
    {
        var item = ItemKatalog.DariDomain(new Bahan(Guid.NewGuid(), "Bahan lama", JenisKomponen.Dinding,
            0.2, 2, 4, 30, 50, "Referensi legacy"));

        var snapshot = SnapshotBahanMapper.Buat(item, new UiEvaluasi(item.Bahan), Guid.NewGuid(), 1);

        var source = Assert.Single(snapshot.SumberProperti);
        Assert.Contains("Referensi legacy", source.Sumber);
        Assert.Contains("Status verifikasi properti tidak tercatat", source.Sumber);
        Assert.Null(source.Tahun);
    }
}
