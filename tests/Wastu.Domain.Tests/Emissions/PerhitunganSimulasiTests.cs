using Wastu.Domain.Emissions;
using Wastu.Domain.Tests.Fixtures;

namespace Wastu.Domain.Tests.Emissions;

public class PerhitunganSimulasiTests
{
    private const int Presisi = 6;
    private const int Horizon = 30;

    // ------------------------------------------------------------- kasus uji wajib T18 & PRD-R02

    [Fact]
    public void KasusWajar_SkorSeratus_SatuPenggantian_DanEmisiKaliDua()
    {
        // PRD §11.3, PRD-R02, T18: Usia nominal 15 tahun, horizon 30 tahun, skor 100 -> L=15, replacements=1, total emisi x2.
        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(), EvaluasiDengan(100, 10), usiaPakaiNominal: 15, horizonTahun: Horizon);

        Assert.Equal(15, hasil.UsiaPakaiEfektifTahun, Presisi);
        Assert.Equal(1, hasil.JumlahPenggantian);
        Assert.Equal(20, hasil.EmisiKumulatifKg, Presisi);
        Assert.Equal("v2.0", hasil.VersiModel);
        Assert.Equal(0.5, hasil.FaktorPenuaanA);
    }

    [Fact]
    public void KasusWajar_SkorRendah_LebihBanyakPenggantian_EmisiBerlipat()
    {
        // PRD §11.3, PRD-R02, T18: Usia nominal 15 tahun, skor 20, horizon 30 tahun -> L=9, replacements=3 (tahun 9, 18, 27), total emisi x4.
        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(), EvaluasiDengan(20, 10), usiaPakaiNominal: 15, horizonTahun: Horizon);

        Assert.Equal(9, hasil.UsiaPakaiEfektifTahun, Presisi);
        Assert.Equal(3, hasil.JumlahPenggantian);
        Assert.Equal(40, hasil.EmisiKumulatifKg, Presisi);
    }

    [Fact]
    public void T18_KasusTabelPemeriksaanKewajaran()
    {
        // Tabel PRD §11.3 & PRD-R02:
        // Nominal 15, Skor 100 -> L=15. H=30 -> 1 penggantian (t=15), total x2.
        double l100 = PerhitunganSimulasi.UsiaPakaiEfektif(15, 100);
        Assert.Equal(15, l100, Presisi);
        Assert.Equal(1, PerhitunganSimulasi.JumlahPenggantian(30, l100));
        Assert.Equal(200, PerhitunganSimulasi.EmisiKumulatif(100, 1));

        // Nominal 15, Skor 20 -> L=9. H=30 -> 3 penggantian (t=9, 18, 27), total x4.
        double l20 = PerhitunganSimulasi.UsiaPakaiEfektif(15, 20);
        Assert.Equal(9, l20, Presisi);
        Assert.Equal(3, PerhitunganSimulasi.JumlahPenggantian(30, l20));
        Assert.Equal(400, PerhitunganSimulasi.EmisiKumulatif(100, 3));

        // Nominal 15, Skor 100, H=15 -> 0 penggantian, total x1.
        Assert.Equal(0, PerhitunganSimulasi.JumlahPenggantian(15, l100));
        Assert.Equal(100, PerhitunganSimulasi.EmisiKumulatif(100, 0));

        // Nominal 15, Skor 100, H=31 -> 2 penggantian (t=15, 30), total x3.
        Assert.Equal(2, PerhitunganSimulasi.JumlahPenggantian(31, l100));
        Assert.Equal(300, PerhitunganSimulasi.EmisiKumulatif(100, 2));
    }

    // ------------------------------------------------------------- kasus uji T19: Titik interior & batas akhir

    [Fact]
    public void T19_L15_Horizon30_Titik0_15_30_DanKonsistensiEndpoint()
    {
        // T19: L15, titik grafik 0, 15, 30, horizon 30:
        // Awal x1; kejadian interior 15 tampak x2; akhir 30 tetap x2.
        const double emisiAwal = 50.0;
        const double l = 15.0;
        const double h = 30.0;

        double at0 = PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 0, h, l);
        double at14 = PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 14.999, h, l);
        double at15 = PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 15.0, h, l);
        double at29 = PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 29.999, h, l);
        double at30 = PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 30.0, h, l);

        Assert.Equal(50.0, at0, Presisi);     // x1
        Assert.Equal(50.0, at14, Presisi);    // x1
        Assert.Equal(100.0, at15, Presisi);   // x2 right at year 15!
        Assert.Equal(100.0, at29, Presisi);   // x2
        Assert.Equal(100.0, at30, Presisi);   // x2 at endpoint (no jump at t=H)

        IReadOnlyList<double> kejadian = PerhitunganSimulasi.KejadianPenggantian(h, l);
        Assert.Single(kejadian);
        Assert.Equal(15.0, kejadian[0], Presisi);

        IReadOnlyList<TitikSimulasi> kurva = PerhitunganSimulasi.TitikKurvaStep(emisiAwal, h, l);
        // (0, 50) -> (15, 50) -> (15, 100) -> (30, 100)
        Assert.Equal(4, kurva.Count);
        Assert.Equal(new TitikSimulasi(0.0, 50.0), kurva[0]);
        Assert.Equal(new TitikSimulasi(15.0, 50.0), kurva[1]);
        Assert.Equal(new TitikSimulasi(15.0, 100.0), kurva[2]);
        Assert.Equal(new TitikSimulasi(30.0, 100.0), kurva[3]);
    }

    [Fact]
    public void T19_L9_Horizon30_Kejadian9_18_27_DanEndpointKali4()
    {
        // T19: L9, horizon 30: mempunyai kejadian 9, 18, 27 dan akhir x4.
        const double emisiAwal = 25.0;
        const double l = 9.0;
        const double h = 30.0;

        IReadOnlyList<double> kejadian = PerhitunganSimulasi.KejadianPenggantian(h, l);
        Assert.Equal(3, kejadian.Count);
        Assert.Equal(9.0, kejadian[0], Presisi);
        Assert.Equal(18.0, kejadian[1], Presisi);
        Assert.Equal(27.0, kejadian[2], Presisi);

        Assert.Equal(25.0, PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 0, h, l), Presisi);     // x1
        Assert.Equal(25.0, PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 8.99, h, l), Presisi);  // x1
        Assert.Equal(50.0, PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 9.0, h, l), Presisi);   // x2
        Assert.Equal(75.0, PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 18.0, h, l), Presisi);  // x3
        Assert.Equal(100.0, PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 27.0, h, l), Presisi); // x4
        Assert.Equal(100.0, PerhitunganSimulasi.EmisiKumulatifPadaTitik(emisiAwal, 30.0, h, l), Presisi); // x4 (endpoint)

        IReadOnlyList<TitikSimulasi> kurva = PerhitunganSimulasi.TitikKurvaStep(emisiAwal, h, l);
        Assert.Equal(8, kurva.Count);
        Assert.Equal(new TitikSimulasi(0.0, 25.0), kurva[0]);
        Assert.Equal(new TitikSimulasi(9.0, 25.0), kurva[1]);
        Assert.Equal(new TitikSimulasi(9.0, 50.0), kurva[2]);
        Assert.Equal(new TitikSimulasi(18.0, 50.0), kurva[3]);
        Assert.Equal(new TitikSimulasi(18.0, 75.0), kurva[4]);
        Assert.Equal(new TitikSimulasi(27.0, 75.0), kurva[5]);
        Assert.Equal(new TitikSimulasi(27.0, 100.0), kurva[6]);
        Assert.Equal(new TitikSimulasi(30.0, 100.0), kurva[7]);
    }

    // ------------------------------------------------------------- batas presisi numerik

    [Theory]
    [InlineData(29.999, 15.0, 1)] // H < 30 -> 1 replacement (at t=15)
    [InlineData(30.0, 15.0, 1)]   // H == 30 -> 1 replacement (strictly interior 0 < t < 30)
    [InlineData(30.001, 15.0, 2)] // H > 30 -> 2 replacements (at t=15 and t=30)
    [InlineData(14.999, 15.0, 0)] // H < 15 -> 0 replacements
    [InlineData(15.0, 15.0, 0)]   // H == 15 -> 0 replacements
    [InlineData(15.001, 15.0, 1)] // H > 15 -> 1 replacement (at t=15)
    public void BatasPresisi_JumlahPenggantianEksak(double horizon, double usiaEfektif, int ekspektasi)
    {
        int aktual = PerhitunganSimulasi.JumlahPenggantian(horizon, usiaEfektif);
        Assert.Equal(ekspektasi, aktual);
    }

    // ------------------------------------------------------------- konfigurasi faktor penuaan a

    [Theory]
    [InlineData(0.1, 0, 0.1)]
    [InlineData(0.1, 100, 1.0)]
    [InlineData(0.5, 0, 0.5)]
    [InlineData(0.5, 100, 1.0)]
    [InlineData(0.8, 50, 0.9)]
    [InlineData(1.0, 0, 1.0)]
    [InlineData(1.0, 50, 1.0)]
    [InlineData(1.0, 100, 1.0)]
    public void FaktorPenuaanA_Terkonfigurasi_SesuaiRumus(double a, double skor, double ekspektasi)
    {
        double aktual = PerhitunganSimulasi.FaktorPenyesuaian(skor, a);
        Assert.Equal(ekspektasi, aktual, Presisi);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.0001)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FaktorPenuaanA_DiLuarRentang_Ditolak(double invalidA)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.FaktorPenyesuaian(50, invalidA));
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.UsiaPakaiEfektif(15, 50, invalidA));
    }

    [Fact]
    public void Simulasikan_StampingVersiModelFaktorDanWaktu()
    {
        var evaluasi = EvaluasiDengan(80, 100);
        var waktuUtc = new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);
        var konfigurasi = new KonfigurasiModel(versiModel: "v2.0-custom", faktorPenuaanA: 0.7);

        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            evaluasi.Id, evaluasi, 20, 30, konfigurasi, waktuUtc);

        Assert.Equal("v2.0-custom", hasil.VersiModel);
        Assert.Equal(0.7, hasil.FaktorPenuaanA, Presisi);
        Assert.Equal(waktuUtc, hasil.DihitungPadaUtc);
    }

    // ------------------------------------------------------------- sifat monotonik

    [Fact]
    public void Monotonik_SkorTurun_EmisiKumulatifMenaikAtauSama()
    {
        double[] skor = [100, 90, 75, 50, 25, 0];

        for (int i = 1; i < skor.Length; i++)
        {
            SimulasiEmisi sebelumnya = PerhitunganSimulasi.Simulasikan(
                Guid.NewGuid(), EvaluasiDengan(skor[i - 1], 10), 15, Horizon);
            SimulasiEmisi sekarang = PerhitunganSimulasi.Simulasikan(
                Guid.NewGuid(), EvaluasiDengan(skor[i], 10), 15, Horizon);

            Assert.True(
                sebelumnya.EmisiKumulatifKg <= sekarang.EmisiKumulatifKg,
                $"Skor turun dari {skor[i - 1]} ke {skor[i]} tidak boleh menurunkan emisi kumulatif.");
        }
    }

    [Fact]
    public void Monotonik_HorizonMeningkat_JumlahPenggantianDanEmisiMenaikAtauSama()
    {
        double usiaEfektif = PerhitunganSimulasi.UsiaPakaiEfektif(15, 60); // 15 * 0.8 = 12
        int penggantianSebelumnya = 0;
        double emisiSebelumnya = 100.0;

        for (int h = 1; h <= 60; h++)
        {
            int penggantian = PerhitunganSimulasi.JumlahPenggantian(h, usiaEfektif);
            double emisi = PerhitunganSimulasi.EmisiKumulatif(100.0, penggantian);

            Assert.True(penggantian >= penggantianSebelumnya,
                $"Horizon {h} memiliki {penggantian} penggantian, kurang dari horizon sebelumnya {penggantianSebelumnya}.");
            Assert.True(emisi >= emisiSebelumnya,
                $"Horizon {h} memiliki emisi {emisi}, kurang dari horizon sebelumnya {emisiSebelumnya}.");

            penggantianSebelumnya = penggantian;
            emisiSebelumnya = emisi;
        }
    }

    // ---------------------------------------------------------- rekonstruksi seed

    [Fact]
    public void Seed_BetonBertulang_Horizon30_TanpaPenggantian()
    {
        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(),
            EvaluasiDengan(DataContoh.SkorSeedBetonBertulang, emisiAwal: 14400),
            DataContoh.BetonBertulang().UsiaPakaiNominalTahun,
            Horizon);

        Assert.Equal(43, hasil.UsiaPakaiEfektifTahun, Presisi);
        Assert.Equal(0, hasil.JumlahPenggantian);
        Assert.Equal(14400, hasil.EmisiKumulatifKg, Presisi);
    }

    [Fact]
    public void Seed_BajaGalvanis_Horizon30_TigaPenggantian()
    {
        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(),
            EvaluasiDengan(DataContoh.SkorSeedBajaGalvanis, emisiAwal: 8100),
            DataContoh.BajaGalvanis().UsiaPakaiNominalTahun,
            Horizon);

        Assert.Equal(9.375, hasil.UsiaPakaiEfektifTahun, Presisi);
        Assert.Equal(3, hasil.JumlahPenggantian);
        Assert.Equal(32400, hasil.EmisiKumulatifKg, Presisi);
    }

    [Fact]
    public void ArgumenInti_BahanBurukUntukIklim_EmitinyaJauhLebihBesar()
    {
        const double EmisiAwal = 8100;

        SimulasiEmisi baik = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(), EvaluasiDengan(Skor: 72, EmisiAwal), 50, Horizon);
        SimulasiEmisi buruk = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(), EvaluasiDengan(Skor: 25, EmisiAwal), 15, Horizon);

        Assert.True(buruk.EmisiKumulatifKg > baik.EmisiKumulatifKg);
        Assert.Equal(0, baik.JumlahPenggantian);
        Assert.Equal(3, buruk.JumlahPenggantian);
    }

    // ------------------------------------------------------------------ komponen rumus

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(20, 0.6)]
    [InlineData(72, 0.86)]
    [InlineData(100, 1.0)]
    public void FaktorPenyesuaian_SesuaiRumusPRD(double skor, double hoped)
    {
        Assert.Equal(hoped, PerhitunganSimulasi.FaktorPenyesuaian(skor), Presisi);
    }

    [Fact]
    public void FaktorPenyesuaian_TidakPernahDiBawahSeparuh()
    {
        Assert.Equal(0.5, PerhitunganSimulasi.FaktorPenyesuaian(0), Presisi);
    }

    [Fact]
    public void JumlahPenggantian_UsiaPakaiLebihPanjangDariHorizon_Nol()
    {
        Assert.Equal(0, PerhitunganSimulasi.JumlahPenggantian(Horizon, 50));
    }

    [Fact]
    public void JumlahPenggantian_HorizonTepatSatuSiklus_TidakDihitungPenggantianTambahan()
    {
        Assert.Equal(1, PerhitunganSimulasi.JumlahPenggantian(Horizon, 15));
    }

    [Fact]
    public void JumlahPenggantian_HorizonLebihPanjang_Meningkat()
    {
        int tigaPuluh = PerhitunganSimulasi.JumlahPenggantian(30, 10);
        int enamPuluh = PerhitunganSimulasi.JumlahPenggantian(60, 10);

        Assert.Equal(2, tigaPuluh);
        Assert.Equal(5, enamPuluh);
    }

    [Fact]
    public void EmisiKumulatif_SesuaiRumusPRD()
    {
        Assert.Equal(1000, PerhitunganSimulasi.EmisiKumulatif(1000, 0), Presisi);
        Assert.Equal(2000, PerhitunganSimulasi.EmisiKumulatif(1000, 1), Presisi);
        Assert.Equal(4000, PerhitunganSimulasi.EmisiKumulatif(1000, 3), Presisi);
    }

    // -------------------------------------------------------------------- validasi input

    [Theory]
    [InlineData(-1.0)]
    [InlineData(101.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void SkorDiLuarRentang_Ditolak(double invalidSkor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.FaktorPenyesuaian(invalidSkor));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UsiaPakaiNominalTidakValid_Ditolak(double invalidUsia)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.UsiaPakaiEfektif(invalidUsia, 50));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void HorizonTidakValid_Ditolak(int invalidH)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.JumlahPenggantian(invalidH, 10));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void HorizonDoubleTidakValid_Ditolak(double invalidH)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.JumlahPenggantian(invalidH, 10.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void UsiaEfektifTidakValid_Ditolak(double invalidL)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.JumlahPenggantian(30, invalidL));
    }

    [Fact]
    public void EmisiAwalNegatif_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.EmisiKumulatif(-1, 0));
    }

    [Fact]
    public void JumlahPenggantianNegatif_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.EmisiKumulatif(100, -1));
    }

    [Fact]
    public void EvaluasiNull_Ditolak()
    {
        Assert.Throws<ArgumentNullException>(
            () => PerhitunganSimulasi.Simulasikan(Guid.NewGuid(), null!, 15, Horizon));
    }

    [Theory]
    [InlineData(-1.0, 30.0)]
    [InlineData(31.0, 30.0)]
    [InlineData(double.NaN, 30.0)]
    public void WaktuTPadaTitikTidakValid_Ditolak(double waktuT, double horizon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PerhitunganSimulasi.EmisiKumulatifPadaTitik(100.0, waktuT, horizon, 15.0));
    }

    private static Evaluasi EvaluasiDengan(double Skor, double emisiAwal) => new(
        Id: Guid.NewGuid(),
        BahanId: DataContoh.IdBetonBertulang,
        KomponenId: DataContoh.KomponenDindingId,
        SkorKesesuaian: Skor,
        EmisiAwalKg: emisiAwal,
        SubSkor: new SubSkorKesesuaian(Skor, Skor, Skor, Skor),
        Alasan: "Evaluasi uji");
}