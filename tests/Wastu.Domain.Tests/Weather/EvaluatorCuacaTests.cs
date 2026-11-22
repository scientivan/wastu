using Wastu.Domain.Abstractions;
using Wastu.Domain.Weather;

namespace Wastu.Domain.Tests.Weather;

public class EvaluatorCuacaTests
{
    private static readonly DateOnly TanggalUji = new(2026, 11, 20);

    [Fact]
    public void T24_Pengecoran_BatasEksak_TidakMelanggar_Layak()
    {
        // PRD §12, T24: p=60%, hujan=5.0 mm, suhu=35.0°C tidak melanggar (Layak)
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 60.0,
            CurahHujanMm: 5.0,
            SuhuMinC: 25.0,
            SuhuMaksC: 35.0,
            KelembapanPct: 70.0);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecoran, AmbangLayakKerja.Pengecoran);

        Assert.Equal(StatusKelayakanCuaca.Layak, hasil.Status);
        Assert.Single(hasil.Alasan);
        Assert.Contains("Memenuhi", hasil.Alasan[0]);
    }

    [Fact]
    public void T24_Pengecoran_Probabilitas60Koma1_TidakLayak()
    {
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 60.1,
            CurahHujanMm: 5.0,
            SuhuMinC: 25.0,
            SuhuMaksC: 35.0,
            KelembapanPct: 70.0);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecoran, AmbangLayakKerja.Pengecoran);

        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasil.Status);
        Assert.Contains(hasil.Alasan, a => a.Contains("Peluang hujan 60.1%") && a.Contains("60%"));
    }

    [Fact]
    public void T24_Pengecoran_CurahHujan5Koma01_TidakLayak()
    {
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 50.0,
            CurahHujanMm: 5.01,
            SuhuMinC: 25.0,
            SuhuMaksC: 35.0,
            KelembapanPct: 70.0);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecoran, AmbangLayakKerja.Pengecoran);

        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasil.Status);
        Assert.Contains(hasil.Alasan, a => a.Contains("Curah hujan 5.01 mm") && a.Contains("5 mm"));
    }

    [Fact]
    public void T24_Pengecoran_SuhuSajaDiAtas35_Waspada()
    {
        // T24: suhu saja di atas menjadi Waspada, bukan Tidak layak
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 40.0,
            CurahHujanMm: 2.0,
            SuhuMinC: 26.0,
            SuhuMaksC: 35.1,
            KelembapanPct: 65.0);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecoran, AmbangLayakKerja.Pengecoran);

        Assert.Equal(StatusKelayakanCuaca.Waspada, hasil.Status);
        Assert.Contains(hasil.Alasan, a => a.Contains("Suhu maksimum 35.1°C") && a.Contains("mitigasi"));
    }

    [Fact]
    public void T24_Pengecoran_PelanggaranKerasDanSuhuTinggi_TidakLayakDenganSemuaAlasan()
    {
        // Hujan lebat dan suhu panas: status utama TidakLayak, tetapi mencantumkan semua alasan termasuk suhu
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 75.0,
            CurahHujanMm: 12.0,
            SuhuMinC: 26.0,
            SuhuMaksC: 36.5,
            KelembapanPct: 70.0);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecoran, AmbangLayakKerja.Pengecoran);

        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasil.Status);
        Assert.Equal(3, hasil.Alasan.Count);
        Assert.Contains(hasil.Alasan, a => a.Contains("Peluang hujan 75%"));
        Assert.Contains(hasil.Alasan, a => a.Contains("Curah hujan 12 mm"));
        Assert.Contains(hasil.Alasan, a => a.Contains("Suhu maksimum 36.5°C"));
    }

    [Fact]
    public void T25_Pengecatan_BatasEksak_TidakMelanggar_Layak()
    {
        // PRD §12, T25: RH=85%, p=40%, suhuMin=10°C, suhuMaks=35°C adalah Layak
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 40.0,
            CurahHujanMm: 0.0,
            SuhuMinC: 10.0,
            SuhuMaksC: 35.0,
            KelembapanPct: 85.0);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecatan, AmbangLayakKerja.Pengecatan);

        Assert.Equal(StatusKelayakanCuaca.Layak, hasil.Status);
    }

    [Fact]
    public void T25_Pengecatan_RHKolektif85Koma1_TidakLayak()
    {
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 30.0,
            CurahHujanMm: 0.0,
            SuhuMinC: 22.0,
            SuhuMaksC: 30.0,
            KelembapanPct: 85.1);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecatan, AmbangLayakKerja.Pengecatan);

        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasil.Status);
        Assert.Contains(hasil.Alasan, a => a.Contains("Kelembapan relatif maks harian 85.1%") && a.Contains("85%"));
    }

    [Fact]
    public void T25_Pengecatan_Probabilitas40Koma1_TidakLayak()
    {
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 40.1,
            CurahHujanMm: 0.0,
            SuhuMinC: 22.0,
            SuhuMaksC: 30.0,
            KelembapanPct: 70.0);

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecatan, AmbangLayakKerja.Pengecatan);

        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasil.Status);
        Assert.Contains(hasil.Alasan, a => a.Contains("Peluang hujan 40.1%") && a.Contains("40%"));
    }

    [Fact]
    public void T25_Pengecatan_SuhuDiLuarRentang10Sampai35_TidakLayak()
    {
        // 9.9°C melanggar batas bawah 10°C
        var dingin = new DailyForecast(TanggalUji, 20, 0, 9.9, 25, 60);
        var hasilDingin = EvaluatorCuaca.EvaluasiHari(TanggalUji, dingin, JenisPekerjaan.Pengecatan, AmbangLayakKerja.Pengecatan);
        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasilDingin.Status);
        Assert.Contains(hasilDingin.Alasan, a => a.Contains("Suhu minimum 9.9°C") && a.Contains("10°C"));

        // 35.1°C melanggar batas atas 35°C (pada pengecatan adalah keras!)
        var panas = new DailyForecast(TanggalUji, 20, 0, 24, 35.1, 60);
        var hasilPanas = EvaluatorCuaca.EvaluasiHari(TanggalUji, panas, JenisPekerjaan.Pengecatan, AmbangLayakKerja.Pengecatan);
        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasilPanas.Status);
        Assert.Contains(hasilPanas.Alasan, a => a.Contains("Suhu maksimum 35.1°C") && a.Contains("35°C"));
    }

    [Fact]
    public void T26_Pengecatan_StatistikRHMaksimumMenghasilkanTidakLayak()
    {
        // Jika memakai rata-rata 80%, pengecatan akan dianggap Layak (karena 80 <= 85).
        // Namun karena WP06 mewajibkan MAX hourly RH (90%), pengecatan menjadi TidakLayak!
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 20.0,
            CurahHujanMm: 0.0,
            SuhuMinC: 22.0,
            SuhuMaksC: 30.0,
            KelembapanPct: 90.0); // MAX dari jam lokal

        var hasil = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecatan, AmbangLayakKerja.Pengecatan);

        Assert.Equal(StatusKelayakanCuaca.TidakLayak, hasil.Status);
        Assert.Contains(hasil.Alasan, a => a.Contains("Kelembapan relatif maks harian 90%") && a.Contains("85%"));
    }

    [Fact]
    public void T27_DataCacatAtauHilang_MenghasilkanDataTidakTersediaDanBukanLayak()
    {
        // Data cacat: DataLengkap == false
        var cuacaCacat = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 0,
            CurahHujanMm: 0,
            SuhuMinC: 0,
            SuhuMaksC: 0,
            KelembapanPct: 0,
            DataLengkap: false,
            CatatanKualitas: "Probabilitas hujan bernilai null");

        var hasilCacat = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuacaCacat, JenisPekerjaan.Pengecoran, AmbangLayakKerja.Pengecoran);

        Assert.Equal(StatusKelayakanCuaca.DataTidakTersedia, hasilCacat.Status);
        Assert.Contains(hasilCacat.Alasan, a => a.Contains("Probabilitas hujan bernilai null"));

        // Data null (hari di luar rentang prakiraan)
        var hasilNull = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, null, JenisPekerjaan.Pengecoran, AmbangLayakKerja.Pengecoran);

        Assert.Equal(StatusKelayakanCuaca.DataTidakTersedia, hasilNull.Status);
        Assert.Contains(hasilNull.Alasan, a => a.Contains("di luar rentang 14 hari"));
    }

    [Fact]
    public void T28_OverrideAmbangPengecatan_PrioritasTertinggiMengubahKeputusan()
    {
        // Cuaca: probabilitas hujan 35%.
        // Default pengecatan: 40% (maka 35% Layak).
        // Override pekerjaan: 30% (maka 35% menjadi TidakLayak dan keputusan menyebut ambang 30%!).
        var cuaca = new DailyForecast(
            Tanggal: TanggalUji,
            ProbabilitasHujanPct: 35.0,
            CurahHujanMm: 0.0,
            SuhuMinC: 22.0,
            SuhuMaksC: 30.0,
            KelembapanPct: 75.0);

        var defaultAmbang = AmbangLayakKerja.Pengecatan;
        var evaluasiDefault = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecatan, defaultAmbang);
        Assert.Equal(StatusKelayakanCuaca.Layak, evaluasiDefault.Status);

        var overrideAmbang = defaultAmbang with { ProbabilitasHujanMaksPct = 30.0 };
        var evaluasiOverride = EvaluatorCuaca.EvaluasiHari(
            TanggalUji, cuaca, JenisPekerjaan.Pengecatan, overrideAmbang);

        Assert.Equal(StatusKelayakanCuaca.TidakLayak, evaluasiOverride.Status);
        Assert.Contains(evaluasiOverride.Alasan, a => a.Contains("Peluang hujan 35%") && a.Contains("30%"));
        Assert.Equal(30.0, evaluasiOverride.AmbangEfektif?.ProbabilitasHujanMaksPct);
    }
}
