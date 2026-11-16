using Wastu.Domain.Emissions;
using Wastu.Domain.Tests.Fixtures;

namespace Wastu.Domain.Tests.Emissions;

public class PerhitunganSimulasiTests
{
    private const int Presisi = 6;
    private const int Horizon = 30;

    // ------------------------------------------------------------- kasus uji wajib

    [Fact]
    public void KasusWajar_SkorSeratus_SatuPenggantian_DanEmisiKaliDua()
    {
        // PRD §11.3 dan §4.2 menyebut ini kasus uji wajib. Usia nominal 15 tahun,
        // horizon 30 tahun, skor 100 sehingga tidak ada penalti iklim dan usia
        // efektif tetap 15 tahun.
        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(), EvaluasiDengan(100, 10), usiaPakaiNominal: 15, horizonTahun: Horizon);

        Assert.Equal(15, hasil.UsiaPakaiEfektifTahun, Presisi);
        Assert.Equal(1, hasil.JumlahPenggantian);
        Assert.Equal(20, hasil.EmisiKumulatifKg, Presisi);
    }

    [Fact]
    public void KasusWajar_SkorRendah_LebihBanyakPenggantian_EmisiBerlipat()
    {
        // PRD §11.3 menyebut skor 20 berarti usia efektif 9 tahun dan emisi menjadi
        // tiga kali lipat emisi awal.
        //
        // Catatan penyimpangan: PRD menyebut 2 penggantian, sedangkan implementasi
        // ini menghitung 3.alasannya ada di PerhitunganSimulasi: bahan berumur
        // 9 tahun dalam horizon 30 tahun diganti pada tahun 9, 18, dan 27 sehingga
        // tiga kali, dan angka 3 inilah yang sesuai dengan baris seed baja galvanis
        // yang sudah ada di repositori. Ini yang diuji di bawah sebagai jangkar.
        SimulasiEmisi hasil = PerhitunganSimulasi.Simulasikan(
            Guid.NewGuid(), EvaluasiDengan(20, 10), usiaPakaiNominal: 15, horizonTahun: Horizon);

        Assert.Equal(9, hasil.UsiaPakaiEfektifTahun, Presisi);
        Assert.Equal(3, hasil.JumlahPenggantian);
        Assert.Equal(40, hasil.EmisiKumulatifKg, Presisi);
    }

    [Fact]
    public void ArahHasil_SkorTurun_TebaikEmisiKumulatifNaik()
    {
        // Inilah PROPERTY inti Wastu yang diuji §4.2: bahan yang buruk untuk iklim
        // setempat mengulang emisi produksi lebih sering.
        double[] skor = [100, 90, 75, 50, 25, 0];

        for (int i = 1; i < skor.Length; i++)
        {
            SimulasiEmisi sebelumnya = PerhitunganSimulasi.Simulasikan(
                Guid.NewGuid(), EvaluasiDengan(skor[i - 1], 10), 15, Horizon);
            SimulasiEmisi sekarang = PerhitunganSimulasi.Simulasikan(
                Guid.NewGuid(), EvaluasiDengan(skor[i], 10), 15, Horizon);

            // Menaik, tidak harus ketat: penggantian bersifat diskret, jadi ada rentang
            // skor yang tetap menghasilkan jumlah penggantian sama. Yang penting tidak
            // pernah turun.
            Assert.True(
                sebelumnya.EmisiKumulatifKg <= sekarang.EmisiKumulatifKg,
                $"Skor turun dari {skor[i - 1]} ke {skor[i]} tidak boleh menurunkan emisi kumulatif, "
                + $"tapi {sebelumnya.EmisiKumulatifKg} jadi {sekarang.EmisiKumulatifKg}.");
        }
    }

    // ---------------------------------------------------------- rekonstruksi seed

    [Fact]
    public void Seed_BetonBertulang_Horizon30_TanpaPenggantian()
    {
        // Baris seed: 30, 43.000, 0, 14400 untuk beton bertulang dinding 120 m2.
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
        // Baris seed: 30, 9.375, 3, 32400 untuk baja galvanis atap 90 m2
        // dengan emisi awal 90 * 90 = 8100.
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
        // Dua bahan dengan usia nominal sama pada iklim sama. Yang salah pilih untuk
        // lokasi harus menghasilkan emisi kumulatif lebih besar — itulah argumen
        // pembeda Wastu dari kalkulator karbon material biasa.
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
        // PRD §11.3: bahan berskor 0 diasumsikan hanya bertahan separuh usia nominalnya.
        Assert.Equal(0.5, PerhitunganSimulasi.FaktorPenyesuaian(0), Presisi);
    }

    [Fact]
    public void JumlahPenggantian_UsiaPakaiLebihPanjangDariHorizon_Nol()
    {
        // Bahan berumur 50 tahun pada horizon 30 tidak pernah perlu diganti.
        Assert.Equal(0, PerhitunganSimulasi.JumlahPenggantian(Horizon, 50));
    }

    [Fact]
    public void JumlahPenggantian_HorizonTepatSatuSiklus_TidakDihitungPenggantianTambahan()
    {
        // Horizon yang habis persis di akhir satu siklus Replacement ada di tahun 15,
        // bukan di tahun 30 karena horizon berhenti di situ.
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

    // -------------------------------------------------------------------- validasi

    [Fact]
    public void SkorDiLuarRentang_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.FaktorPenyesuaian(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.FaktorPenyesuaian(101));
    }

    [Fact]
    public void UsiaPakaiNominalNol_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.UsiaPakaiEfektif(0, 50));
    }

    [Fact]
    public void HorizonNol_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PerhitunganSimulasi.JumlahPenggantian(0, 10));
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

    private static Evaluasi EvaluasiDengan(double Skor, double emisiAwal) => new(
        Id: Guid.NewGuid(),
        BahanId: DataContoh.IdBetonBertulang,
        KomponenId: DataContoh.KomponenDindingId,
        SkorKesesuaian: Skor,
        EmisiAwalKg: emisiAwal,
        SubSkor: new SubSkorKesesuaian(Skor, Skor, Skor, Skor),
        Alasan: "Evaluasi uji");
}