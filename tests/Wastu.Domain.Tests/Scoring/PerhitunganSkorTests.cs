using Wastu.Domain.Scoring;
using Wastu.Domain.Tests.Fixtures;

namespace Wastu.Domain.Tests.Scoring;

public class PerhitunganSkorTests
{
    // Toleransi 6 desimal. Perhitungan floating point tidak pernah eksak, dan
    // selisih di bawah angka ini tidak berarti apa-apa untuk skor yang ditampilkan.
    private const int Presisi = 6;

    // ---------------------------------------------------------------- sub-skor termal

    [Fact]
    public void SkorTermal_BahanDiDalamPita_MendapatSeratus()
    {
        // Beton ringan 0,12 W/mK berada di dalam pita pada suhu 27,5 C (pita 0,10-0,525).
        double skor = PerhitunganSkor.SkorTermal(
            DataContoh.BetonRingan(), DataContoh.IklimPesisirBandarLampung());

        Assert.Equal(100, skor, Presisi);
    }

    [Fact]
    public void SkorTermal_KonduktivitasSangatRendah_TetapSeratus()
    {
        // Panel EPS 0,04 W/mK lebih baik dari batas bawah pita, bukan kegagalan.
        double skor = PerhitunganSkor.SkorTermal(
            BahanDengan(0.04, 4, 4, 25, 70), DataContoh.IklimPesisirBandarLampung());

        Assert.Equal(100, skor, Presisi);
    }

    [Fact]
    public void SkorTermal_BetonBertulang_TurunKarenaKonduktivitasTinggi()
    {
        // Batas atas pita pada 27,5 C adalah 0,525 W/mK, jadi 1,40 menghasilkan 37,5.
        double skor = PerhitunganSkor.SkorTermal(
            DataContoh.BetonBertulang(), DataContoh.IklimPesisirBandarLampung());

        Assert.Equal(37.5, skor, Presisi);
    }

    [Fact]
    public void SkorTermal_LokasiLebihPanas_BahanYangSamaMendapatSkorLebihRendah()
    {
        // Ini arah yang PRD §11.1 minta: konduktivitas yang terlalu tinggi pada suhu
        // rata-rata tinggi harus menurunkan skor.
        double sejuk = PerhitunganSkor.SkorTermal(DataContoh.BetonBertulang(), DataContoh.DenganSuhu(20));
        double panas = PerhitunganSkor.SkorTermal(DataContoh.BetonBertulang(), DataContoh.DenganSuhu(35));

        Assert.True(sejuk > panas, $"Di 20 C skor {sejuk} seharusnya lebih tinggi daripada di 35 C ({panas}).");
        Assert.Equal(42.857143, sejuk, Presisi);
        Assert.Equal(21.428571, panas, Presisi);
    }

    [Fact]
    public void SkorTermal_SuhuSangatTinggi_TetapDiBatasAtasPitaTerendah()
    {
        // Di atas 32 C batas atas pita berhenti turun dan tidak boleh naik lagi.
        double skor = PerhitunganSkor.SkorTermal(DataContoh.BetonBertulang(), DataContoh.DenganSuhu(50));

        Assert.Equal(21.428571, skor, Presisi);
    }

    [Theory]
    [InlineData(0.20)]
    [InlineData(0.45)]
    [InlineData(0.70)]
    [InlineData(1.00)]
    [InlineData(3.00)]
    public void SkorTermal_KonduktivitasBerbagaiNilai_TetapDalamRentang(double konduktivitas)
    {
        double skor = PerhitunganSkor.SkorTermal(
            BahanDengan(konduktivitas, 3, 3, 30, 50), DataContoh.DenganSuhu(30));

        Assert.InRange(skor, 0, 100);
    }

    // --------------------------------------------------------------- sub-skor lembap

    [Fact]
    public void SkorLembap_PadaIklimPesisirBandarLampung_DemandMencapaiLima()
    {
        // Beban = 2400/3000 + 82/100 = 1,62 sehingga dijepit ke 1 dan demand = 5.
        // Skornya = ketahanan / 5 * 100.
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung();

        Assert.Equal(80, PerhitunganSkor.SkorLembap(DataContoh.BetonBertulang(), iklim), Presisi);
        Assert.Equal(100, PerhitunganSkor.SkorLembap(DataContoh.BetonRingan(), iklim), Presisi);
        Assert.Equal(20, PerhitunganSkor.SkorLembap(DataContoh.PanelGipsum(), iklim), Presisi);
    }

    [Fact]
    public void SkorLembap_KetahananMaksimumSelaluMemenuhiDemand()
    {
        // Ketahanan 5 adalah indeks tertinggi, jadi tidak pernah di bawah demand berapa pun
        // dan tidak pernah dirugikan cuaca mana pun.
        double skor = PerhitunganSkor.SkorLembap(
            DataContoh.BetonRingan(), DataContoh.IklimDinginInland());

        Assert.Equal(100, skor, Presisi);
    }

    // --------------------------------------------------------------- sub-skor korosi

    [Fact]
    public void SkorKorosi_DiPesisir_KetahananLimaMemenuhiDemand()
    {
        // Demand di 3 km = 1 + 4 * (1 - 3/50) = 4,76, sehingga 5/4,76 = 105 persen
        // dan harus dijepit ke 100.
        double skor = PerhitunganSkor.SkorKorosi(
            DataContoh.BetonBertulang(), DataContoh.IklimPesisirBandarLampung());

        Assert.Equal(100, skor, Presisi);
    }

    [Fact]
    public void SkorKorosi_DiPesisir_KetahananRendahTurun()
    {
        double skor = PerhitunganSkor.SkorKorosi(
            DataContoh.PanelGipsum(), DataContoh.IklimPesisirBandarLampung());

        Assert.Equal(21.008403, skor, Presisi);
    }

    [Fact]
    public void SkorKorosi_JauhDariLaut_TidakLagiMembedakanBahan()
    {
        // Sesuai arah PRD §11.1: semakin jauh dari pesisir, semakin tidak dominan
        // bobot ketahanan korosi.
        ProfilIklim inland = DataContoh.IklimDinginInland();

        Assert.Equal(100, PerhitunganSkor.SkorKorosi(DataContoh.PanelGipsum(), inland), Presisi);
        Assert.Equal(100, PerhitunganSkor.SkorKorosi(DataContoh.BetonBertulang(), inland), Presisi);
    }

    [Fact]
    public void SkorKorosi_MendekatiPesisir_SkorTurun()
    {
        double dari40km = PerhitunganSkor.SkorKorosi(
            DataContoh.PanelGipsum(), new ProfilIklim(2400, 82, 27.5, 40, TingkatKorosi.Sedang));
        double dari3km = PerhitunganSkor.SkorKorosi(
            DataContoh.PanelGipsum(), DataContoh.IklimPesisirBandarLampung());

        Assert.Equal(55.555556, dari40km, Presisi);
        Assert.True(dari3km < dari40km);
    }

    // ---------------------------------------------------------------- sub-skor usia

    [Fact]
    public void SkorUsia_DinormalkanTerhadapAcuan()
    {
        Assert.Equal(83.333333, PerhitunganSkor.SkorUsia(DataContoh.BetonBertulang()), Presisi);
        Assert.Equal(58.333333, PerhitunganSkor.SkorUsia(DataContoh.GentingKeramik()), Presisi);
        Assert.Equal(50, PerhitunganSkor.SkorUsia(DataContoh.BetonRingan()), Presisi);
    }

    [Fact]
    public void SkorUsia_DiAtasAcuan_TidakMelebihiSeratus()
    {
        // Tidak ada bahan seed yang melewati 60 tahun, tetapi admin boleh menambah bahan
        // baru lewat basis data tanpa mengubah kode (PRD §F2).
        double skor = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 120, 40));

        Assert.Equal(100, skor, Presisi);
    }

    [Fact]
    public void SkorUsia_MonotonDenganUsiaPakai()
    {
        double skor15 = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 15, 40));
        double skor30 = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 30, 40));
        double skor45 = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 45, 40));

        Assert.True(skor15 < skor30, $"15 tahun ({skor15}) harus di bawah 30 tahun ({skor30}).");
        Assert.True(skor30 < skor45, $"30 tahun ({skor30}) harus di bawah 45 tahun ({skor45}).");
    }

    // ------------------------------------------------------- komposisi dan emisi awal

    [Fact]
    public void EmisiAwal_SesuaiRumusPRD11_2()
    {
        // PRD §11.2: emisi_awal = faktor_emisi dikali luas_area.
        // Jangkar regresi ke seed: beton bertulang 120 kgCO2e/m2 pada dinding 120 m2 = 14400.
        Assert.Equal(
            14400,
            PerhitunganSkor.EmisiAwal(DataContoh.BetonBertulang(), DataContoh.Dinding()),
            Presisi);

        // Genting keramik 110 kgCO2e/m2 pada atap 90 m2 = 9900.
        Assert.Equal(
            9900,
            PerhitunganSkor.EmisiAwal(DataContoh.GentingKeramik(), DataContoh.Atap()),
            Presisi);
    }

    [Fact]
    public void EmisiAwal_BahanDenganFaktorTinggi_MenghasilkanAngkaLebihBesar()
    {
        double beton = PerhitunganSkor.EmisiAwal(DataContoh.BetonBertulang(), DataContoh.Dinding());
        double gipsum = PerhitunganSkor.EmisiAwal(DataContoh.PanelGipsum(), DataContoh.Dinding());

        // 28 kgCO2e/m2 pada 120 m2 = 3360.
        Assert.Equal(3360, gipsum, Presisi);
        Assert.True(beton > gipsum);
    }

    [Fact]
    public void SkorKesesuaian_BobotDefault_BerhasilDihitung()
    {
        SubSkorKesesuaian sub = new(Termal: 37.5, Lembap: 80, Korosi: 100, Usia: 83.333333);

        double skor = PerhitunganSkor.SkorKesesuaian(sub, KriteriaBobot.Default);

        // 0,25 * (37,5 + 80 + 100 + 83,333333) = 75,208333
        Assert.Equal(75.208333, skor, Presisi);
    }

    [Fact]
    public void SkorKesesuaian_MengubahBobot_MenggeserSkor()
    {
        // Sub-skor sama persis, hanya bobot yang berubah. Ini yang PRD §F4 janjikan:
        // pengguna mengubah bobot dan melihat peringkat berubah.
        SubSkorKesesuaian sub = new(Termal: 20, Lembap: 50, Korosi: 50, Usia: 90);

        double seimbang = PerhitunganSkor.SkorKesesuaian(sub, new KriteriaBobot(0.25, 0.25, 0.25, 0.25));
        double BeratUsia = PerhitunganSkor.SkorKesesuaian(sub, new KriteriaBobot(0.10, 0.10, 0.10, 0.70));

        Assert.Equal(52.5, seimbang, Presisi);

        // 0,10*20 + 0,10*50 + 0,10*50 + 0,70*90 = 2 + 5 + 5 + 63 = 75
        Assert.Equal(75, BeratUsia, Presisi);
    }

    [Fact]
    public void SkorKesesuaian_SelaluDalamRentangNolSampaiSeratus()
    {
        SubSkorKesesuaian nol = new(Termal: 0, Lembap: 0, Korosi: 0, Usia: 0);

        Assert.InRange(PerhitunganSkor.SkorKesesuaian(nol, KriteriaBobot.Default), 0, 100);
    }

    [Fact]
    public void HitungSubSkor_MengembalikanKeempatAspekSekaligus()
    {
        SubSkorKesesuaian sub = PerhitunganSkor.HitungSubSkor(
            DataContoh.BetonBertulang(), DataContoh.IklimPesisirBandarLampung());

        Assert.Equal(37.5, sub.Termal, Presisi);
        Assert.Equal(80, sub.Lembap, Presisi);
        Assert.Equal(100, sub.Korosi, Presisi);
        Assert.Equal(83.333333, sub.Usia, Presisi);
    }

    // ------------------------------------------------------------------ alasan tekstual

    [Fact]
    public void SusunAlasan_HanyaMenyebutSubSkorYangLemah()
    {
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung();
        Bahan bahan = DataContoh.BetonBertulang();
        SubSkorKesesuaian sub = PerhitunganSkor.HitungSubSkor(bahan, iklim);

        string alasan = PerhitunganSkor.SusunAlasan(bahan, iklim, sub);

        // Termal 37,5 dan usia 83,3; satu-satunya di bawah ambang 60 adalah termal.
        Assert.Contains("Konduktivitas termal", alasan, StringComparison.Ordinal);
        Assert.DoesNotContain("Ketahanan korosi", alasan, StringComparison.Ordinal);
        Assert.EndsWith(".", alasan, StringComparison.Ordinal);
    }

    [Fact]
    public void SusunAlasan_SemuaAdequat_MengembalikanKalimatPositif()
    {
        SubSkorKesesuaian sub = new(Termal: 100, Lembap: 100, Korosi: 100, Usia: 100);

        string alasan = PerhitunganSkor.SusunAlasan(
            DataContoh.BetonBertulang(), DataContoh.IklimPesisirBandarLampung(), sub);

        Assert.Contains("sudah sesuai", alasan, StringComparison.Ordinal);
        Assert.DoesNotContain(";", alasan, StringComparison.Ordinal);
    }

    [Fact]
    public void SusunAlasan_BanyakAspekLemah_MenyebutSemuanya()
    {
        SubSkorKesesuaian sub = new(Termal: 0, Lembap: 0, Korosi: 0, Usia: 0);

        string alasan = PerhitunganSkor.SusunAlasan(
            DataContoh.PanelGipsum(), DataContoh.IklimPesisirBandarLampung(), sub);

        // UI tidak boleh pernah menampilkan alasan kosong.
        Assert.False(string.IsNullOrWhiteSpace(alasan));
        Assert.Contains(";", alasan, StringComparison.Ordinal);
    }

    private static Bahan BahanDengan(double konduktivitas, int ketahananLembap, int ketahananKorosi, double usia, double emisi) => new(
        Guid.NewGuid(),
        "Bahan uji",
        JenisKomponen.Dinding,
        konduktivitas,
        ketahananLembap,
        ketahananKorosi,
        usia,
        emisi,
        "Data uji");
}