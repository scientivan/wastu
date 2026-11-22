using Wastu.Domain;
using Wastu.Domain.Scoring;
using Wastu.Domain.Tests.Fixtures;
using Wastu.Domain.Weather;

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
    public void SkorTermal_LambdaSatuPadaSuhuDuaPuluhEnamDanTigaPuluhDua()
    {
        // T12: batas atas pita 0,60 pada 26 C dan 0,30 pada 32 C. Lambda 1,0
        // menghasilkan 100 * batasAtas / lambda.
        Bahan bahan = BahanDengan(1.0, 3, 3, 30, 50);

        Assert.Equal(60, PerhitunganSkor.SkorTermal(bahan, Iklim(suhu: 26)), Presisi);
        Assert.Equal(30, PerhitunganSkor.SkorTermal(bahan, Iklim(suhu: 32)), Presisi);
    }

    [Fact]
    public void SkorTermal_InterpolasiLinearDiAntaraSuhuSejukDanPanas()
    {
        // Pada 29 C batas atas = 0,60 - (3/6) * 0,30 = 0,45.
        Bahan bahan = BahanDengan(0.90, 3, 3, 30, 50);

        Assert.Equal(50, PerhitunganSkor.SkorTermal(bahan, Iklim(suhu: 29)), Presisi);
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
    public void SkorLembap_T11_HujanTujuhRatusDanTigaRibuTidakJenuhDini()
    {
        // T11: RH 80%, ketahanan 3. Rumus komposit terbobot menghasilkan dua nilai
        // berbeda, bukan sama-sama 100 seperti rumus penjumlahan lama.
        Bahan bahan = BahanDengan(0.30, 3, 3, 30, 50);

        double hujanRendah = PerhitunganSkor.SkorLembap(bahan, Iklim(hujan: 700, rh: 80));
        double hujanTinggi = PerhitunganSkor.SkorLembap(bahan, Iklim(hujan: 3000, rh: 80));

        Assert.Equal(97.8260869565, hujanRendah, 1e-6);
        Assert.Equal(65.2173913043, hujanTinggi, 1e-6);
        Assert.True(hujanRendah > hujanTinggi);
    }

    [Fact]
    public void SkorLembap_T11_SelisihSkorAkhirSekitarDelapanKomaSatuLima()
    {
        // Dengan bobot lembap 0,25 dan sub-skor lain identik, selisih skor akhir
        // = 0,25 * (97,826087 - 65,217391) = 8,152174.
        var subRendah = new SubSkorKesesuaian(Termal: 80, Lembap: 97.8260869565, Korosi: 80, Usia: 80);
        var subTinggi = new SubSkorKesesuaian(Termal: 80, Lembap: 65.2173913043, Korosi: 80, Usia: 80);

        double skorRendah = PerhitunganSkor.SkorKesesuaian(subRendah, KriteriaBobot.Default);
        double skorTinggi = PerhitunganSkor.SkorKesesuaian(subTinggi, KriteriaBobot.Default);

        Assert.Equal(8.152173913, skorRendah - skorTinggi, 1e-6);
    }

    [Fact]
    public void SkorLembap_MengikutiRumusKompositKonfigurasi()
    {
        // Hitung manual: r = 2400/3000 = 0,8; h = 0,82; beban = 0,5*0,8 + 0,5*0,82 = 0,81;
        // kebutuhan = 1 + 4*0,81 = 4,24.
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung();

        Assert.Equal(94.339623, PerhitunganSkor.SkorLembap(DataContoh.BetonBertulang(), iklim), Presisi);
        Assert.Equal(100, PerhitunganSkor.SkorLembap(DataContoh.BetonRingan(), iklim), Presisi);
        Assert.Equal(23.584906, PerhitunganSkor.SkorLembap(DataContoh.PanelGipsum(), iklim), Presisi);
    }

    [Fact]
    public void SkorLembap_KetahananLebihTinggi_TidakPernahMemberiSkorLebihRendah()
    {
        // PRD-R04: pada beban yang sama, ketahanan lebih tinggi tidak boleh lebih rendah.
        ProfilIklim iklim = Iklim(hujan: 1800, rh: 85);
        double rendah = PerhitunganSkor.SkorLembap(BahanDengan(0.3, 2, 3, 30, 50), iklim);
        double tinggi = PerhitunganSkor.SkorLembap(BahanDengan(0.3, 4, 3, 30, 50), iklim);

        Assert.True(tinggi >= rendah, $"{tinggi} seharusnya >= {rendah}.");
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

    [Fact]
    public void SkorLembap_KonfigurasiKustom_MengubahHasilSesuaiAlpha()
    {
        // alpha = 0 membuat beban hanya ditentukan kelembapan, bukan hujan.
        Bahan bahan = BahanDengan(0.30, 3, 3, 30, 50);
        var konfigurasi = new KonfigurasiModel(bobotLembapHujanAlpha: 0.0);

        double skorHanyaRh = PerhitunganSkor.SkorLembap(bahan, Iklim(hujan: 3000, rh: 80), konfigurasi);

        // h = 0,8; beban = 0,8; kebutuhan = 4,2; 300/4,2 = 71,428571.
        Assert.Equal(71.428571, skorHanyaRh, Presisi);
    }

    [Fact]
    public void SkorLembap_KonfigurasiAcuanNolHasilWith_Ditolak()
    {
        // Parameter model juga bisa dilewati validasinya lewat `with`.
        KonfigurasiModel konfigurasi = KonfigurasiModel.Bawaan with { AcuanCurahHujanMm = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSkor.SkorLembap(
            BahanDengan(0.30, 3, 3, 30, 50), Iklim(), konfigurasi));
    }

    [Fact]
    public void SkorKorosi_KonfigurasiAcuanJarakNolHasilWith_Ditolak()
    {
        KonfigurasiModel konfigurasi = KonfigurasiModel.Bawaan with { AcuanJarakPesisirKm = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSkor.SkorKorosi(
            BahanDengan(0.30, 3, 3, 30, 50), Iklim(), konfigurasi));
    }

    // --------------------------------------------------------------- sub-skor korosi

    [Fact]
    public void SkorKorosi_T13_KetahananDuaPadaNolDanLimaPuluhKilometer()
    {
        // T13: jarak 0 km -> kebutuhan 5 -> 100*2/5 = 40.
        // Jarak >= 50 km -> kebutuhan 1 -> 200 dijepit 100.
        Bahan bahan = BahanDengan(0.30, 3, 2, 30, 50);

        Assert.Equal(40, PerhitunganSkor.SkorKorosi(bahan, Iklim(jarak: 0)), Presisi);
        Assert.Equal(100, PerhitunganSkor.SkorKorosi(bahan, Iklim(jarak: 50)), Presisi);
        Assert.Equal(100, PerhitunganSkor.SkorKorosi(bahan, Iklim(jarak: 120)), Presisi);
    }

    [Fact]
    public void SkorKorosi_TidakMembacaLabelTingkatKorosiTekstual()
    {
        // T13: rumus hanya memakai jarak kontinu. Label enum tidak boleh mengubah skor.
        Bahan bahan = BahanDengan(0.30, 3, 2, 30, 50);

        double rendah = PerhitunganSkor.SkorKorosi(bahan, Iklim(jarak: 3, korosi: TingkatKorosi.Rendah));
        double tinggi = PerhitunganSkor.SkorKorosi(bahan, Iklim(jarak: 3, korosi: TingkatKorosi.Tinggi));

        Assert.Equal(rendah, tinggi, Presisi);
    }

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
    public void SkorUsia_DinormalkanTerhadapRentangKategori()
    {
        Assert.Equal(100, PerhitunganSkor.SkorUsia(DataContoh.BetonBertulang(), 20, 50), Presisi);
        Assert.Equal(50, PerhitunganSkor.SkorUsia(DataContoh.GentingKeramik(), 20, 50), Presisi);
        Assert.Equal(33.333333, PerhitunganSkor.SkorUsia(DataContoh.BetonRingan(), 20, 50), Presisi);
    }

    [Fact]
    public void SkorUsia_T14_RentangDuaPuluhTigaPuluhEmpatPuluh()
    {
        // T14: min 20, maks 40 -> 20 menjadi 0, 30 menjadi 50, 40 menjadi 100.
        Assert.Equal(0, PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 20, 50), 20, 40), Presisi);
        Assert.Equal(50, PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 30, 50), 20, 40), Presisi);
        Assert.Equal(100, PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 40, 50), 20, 40), Presisi);
    }

    [Fact]
    public void SkorUsia_JikaUsiaSemuaBahanSama_MengembalikanSeratus()
    {
        double skor = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 120, 40), 120, 120);

        Assert.Equal(100, skor, Presisi);
    }

    [Fact]
    public void SkorUsia_MonotonDenganUsiaPakai()
    {
        double skor15 = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 15, 40), 15, 45);
        double skor30 = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 30, 40), 15, 45);
        double skor45 = PerhitunganSkor.SkorUsia(BahanDengan(0.30, 3, 3, 45, 40), 15, 45);

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
    public void EmisiAwal_T15_LuasDuaKaliLipat_MenggandakanEmisi()
    {
        Bahan bahan = DataContoh.BetonBertulang();
        var kecil = new Komponen(Guid.NewGuid(), DataContoh.ProyekId, JenisKomponen.Dinding, 120);
        var besar = new Komponen(Guid.NewGuid(), DataContoh.ProyekId, JenisKomponen.Dinding, 240);

        double emisiKecil = PerhitunganSkor.EmisiAwal(bahan, kecil);
        double emisiBesar = PerhitunganSkor.EmisiAwal(bahan, besar);

        Assert.Equal(emisiKecil * 2, emisiBesar, Presisi);
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
        double beratUsia = PerhitunganSkor.SkorKesesuaian(sub, new KriteriaBobot(0.10, 0.10, 0.10, 0.70));

        Assert.Equal(52.5, seimbang, Presisi);

        // 0,10*20 + 0,10*50 + 0,10*50 + 0,70*90 = 2 + 5 + 5 + 63 = 75
        Assert.Equal(75, beratUsia, Presisi);
    }

    [Fact]
    public void SkorKesesuaian_SelaluDalamRentangNolSampaiSeratus()
    {
        SubSkorKesesuaian nol = new(Termal: 0, Lembap: 0, Korosi: 0, Usia: 0);

        Assert.InRange(PerhitunganSkor.SkorKesesuaian(nol, KriteriaBobot.Default), 0, 100);
    }

    [Fact]
    public void SkorKesesuaian_T15_SubSkorTidakFinite_Ditolak()
    {
        var sub = new SubSkorKesesuaian(double.NaN, 50, 50, 50);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PerhitunganSkor.SkorKesesuaian(sub, KriteriaBobot.Default));
    }

    [Fact]
    public void SkorKesesuaian_T15_BobotTidakValidHasilWith_Ditolak()
    {
        // Record dengan init setter bisa melewati validasi jumlah lewat `with`.
        KriteriaBobot bobot = KriteriaBobot.Default with { Termal = 0.50 };
        var sub = new SubSkorKesesuaian(80, 80, 80, 80);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => PerhitunganSkor.SkorKesesuaian(sub, bobot));
    }

    [Fact]
    public void HitungSubSkor_MengembalikanKeempatAspekSekaligus()
    {
        SubSkorKesesuaian sub = PerhitunganSkor.HitungSubSkor(
            DataContoh.BetonBertulang(), DataContoh.IklimPesisirBandarLampung(), 20, 50);

        Assert.Equal(37.5, sub.Termal, Presisi);
        Assert.Equal(94.339623, sub.Lembap, Presisi);
        Assert.Equal(100, sub.Korosi, Presisi);
        Assert.Equal(100, sub.Usia, Presisi);
    }

    // ------------------------------------------------------------------ alasan tekstual

    [Fact]
    public void SusunAlasan_MenyebutKekuatanDanKelemahan_DalamBahasaSehariHari()
    {
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung();
        Bahan bahan = DataContoh.BetonBertulang();
        SubSkorKesesuaian sub = PerhitunganSkor.HitungSubSkor(bahan, iklim, 20, 50);

        string alasan = PerhitunganSkor.SusunAlasan(bahan, iklim, sub);

        // Termal 37,5 lemah dan harus dijelaskan tanpa istilah teknis mentah.
        Assert.Contains("mudah menghantarkan panas", alasan, StringComparison.Ordinal);
        // Aspek yang memadai tetap mendapat kalimat positif, bukan hanya daftar kelemahan.
        Assert.Contains("memadai", alasan, StringComparison.Ordinal);
        Assert.Contains("jarak ke pantai sebagai proksi risiko udara bergaram", alasan, StringComparison.Ordinal);
        Assert.EndsWith(".", alasan, StringComparison.Ordinal);
    }

    [Fact]
    public void SusunAlasan_SemuaAdequat_MengembalikanKalimatPositif()
    {
        SubSkorKesesuaian sub = new(Termal: 100, Lembap: 100, Korosi: 100, Usia: 100);

        string alasan = PerhitunganSkor.SusunAlasan(
            DataContoh.BetonRingan(), DataContoh.IklimPesisirBandarLampung(), sub);

        Assert.Contains("tidak mudah menghantarkan panas", alasan, StringComparison.Ordinal);
        Assert.Contains("memadai", alasan, StringComparison.Ordinal);
        Assert.Contains("tergolong panjang", alasan, StringComparison.Ordinal);
        Assert.DoesNotContain("kurang sesuai", alasan, StringComparison.Ordinal);
    }

    [Fact]
    public void SusunAlasan_BanyakAspekLemah_MenyebutSemuanya()
    {
        SubSkorKesesuaian sub = new(Termal: 0, Lembap: 0, Korosi: 0, Usia: 0);

        string alasan = PerhitunganSkor.SusunAlasan(
            DataContoh.PanelGipsum(), DataContoh.IklimPesisirBandarLampung(), sub);

        // UI tidak boleh pernah menampilkan alasan kosong.
        Assert.False(string.IsNullOrWhiteSpace(alasan));
        Assert.Contains("mudah menghantarkan panas", alasan, StringComparison.Ordinal);
        Assert.Contains("kurang sesuai", alasan, StringComparison.Ordinal);
        Assert.Contains("Ketahanan korosi", alasan, StringComparison.Ordinal);
        Assert.Contains("relatif pendek", alasan, StringComparison.Ordinal);
    }

    [Fact]
    public void SusunAlasan_SetiapBahanMendapatPenjelasan()
    {
        ProfilIklim iklim = DataContoh.IklimPesisirBandarLampung();
        Bahan[] katalog =
        [
            DataContoh.BetonBertulang(),
            DataContoh.BetonRingan(),
            DataContoh.PanelGipsum(),
            DataContoh.GentingKeramik(),
            DataContoh.BajaGalvanis()
        ];

        foreach (Bahan bahan in katalog)
        {
            SubSkorKesesuaian sub = PerhitunganSkor.HitungSubSkor(bahan, iklim, 15, 50);
            string alasan = PerhitunganSkor.SusunAlasan(bahan, iklim, sub);

            Assert.False(string.IsNullOrWhiteSpace(alasan));
            Assert.True(alasan.Length > 30, $"Alasan untuk {bahan.Nama} terlalu pendek.");
        }
    }

    // --------------------------------------------------------------- label terpusat

    [Theory]
    [InlineData(54.999, LabelKesesuaian.TidakDisarankan)]
    [InlineData(55.0, LabelKesesuaian.PerluPertimbangan)]
    [InlineData(74.999, LabelKesesuaian.PerluPertimbangan)]
    [InlineData(75.0, LabelKesesuaian.Cocok)]
    public void T17_LabelTerpusat_MemakaiBatasLimaPuluhLimaDanTujuhPuluhLima(
        double skor,
        LabelKesesuaian diharapkan)
    {
        Assert.Equal(diharapkan, KonfigurasiModel.Bawaan.LabelUntuk(skor));
    }

    [Fact]
    public void T17_AmbangLayakKerja_MemakaiNilaiTerpusatLimaPuluhLima()
    {
        Assert.Equal(55, AmbangLayakKerja.SkorMinimumDisarankan);
        Assert.Equal(KonfigurasiModel.Bawaan.AmbangTidakDisarankan, AmbangLayakKerja.SkorMinimumDisarankan);
    }

    private static ProfilIklim Iklim(
        double hujan = 700,
        double rh = 80,
        double suhu = 27.5,
        double jarak = 3,
        TingkatKorosi korosi = TingkatKorosi.Sedang) =>
        new(hujan, rh, suhu, jarak, korosi);

    private static Bahan BahanDengan(
        double konduktivitas,
        int ketahananLembap,
        int ketahananKorosi,
        double usia,
        double emisi,
        string nama = "Bahan uji") => new(
        Guid.NewGuid(),
        nama,
        JenisKomponen.Dinding,
        konduktivitas,
        ketahananLembap,
        ketahananKorosi,
        usia,
        emisi,
        "Data uji");
}
