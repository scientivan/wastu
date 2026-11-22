namespace Wastu.Domain.Tests;

public class KriteriaBobotTests
{
    [Fact]
    public void Default_BerpolaEquiponderalSesuaiPRD()
    {
        // PRD §11.1 menyarankan bobot awal 0,25 masing-masing.
        Assert.Equal(0.25, KriteriaBobot.Default.Termal);
        Assert.Equal(0.25, KriteriaBobot.Default.Lembap);
        Assert.Equal(0.25, KriteriaBobot.Default.Korosi);
        Assert.Equal(0.25, KriteriaBobot.Default.Usia);
        Assert.Equal(1, KriteriaBobot.Default.Jumlah, 9);
    }

    [Fact]
    public void JumlahBobotHarusSatu_Diterima()
    {
        KriteriaBobot bobot = new(0.5, 0.2, 0.2, 0.1);

        Assert.Equal(1, bobot.Jumlah, 9);
    }

    [Fact]
    public void JumlahBobotTidakSatu_Ditolak()
    {
        // PRD §11.1 mensyaratkan jumlah bobot tepat 1. Menerimanya akan membuat skor
        // akhir keluar dari rentang 0-100 yang dijanjikan ke pengguna.
        ArgumentOutOfRangeException galat =
            Assert.Throws<ArgumentOutOfRangeException>(() => new KriteriaBobot(0.5, 0.5, 0.5, 0.5));

        Assert.Contains("harus sama dengan 1", galat.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BobotNegatif_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KriteriaBobot(1.5, -0.5, 0, 0));
    }

    [Fact]
    public void BobotNolDiSatuKriteria_Diterima()
    {
        // Membebaskan satu kriteria harus diperbolehkan: PRD §F4 minta pengguna bisa
        // menurunkan bobot, dan nol adalah batas bawah yang wajar.
        KriteriaBobot bobot = new(1, 0, 0, 0);

        Assert.Equal(1, bobot.Jumlah, 9);
    }

    [Fact]
    public void BobotNaN_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KriteriaBobot(double.NaN, 0.5, 0.25, 0.25));
    }

    [Fact]
    public void WithBypass_NaNDanNegatif_Ditolak()
    {
        var dasar = KriteriaBobot.Default;
        Assert.Throws<ArgumentOutOfRangeException>(() => dasar with { Termal = double.NaN });
        Assert.Throws<ArgumentOutOfRangeException>(() => dasar with { Lembap = -0.1 });
    }

    [Fact]
    public void WithBypass_JumlahTidakSatu_PastikanValidMelempar()
    {
        var diubah = KriteriaBobot.Default with { Termal = 0.5 };
        // Termal = 0.5, Lembap = 0.25, Korosi = 0.25, Usia = 0.25 -> total = 1.25
        Assert.Throws<ArgumentOutOfRangeException>(() => diubah.PastikanValid());
    }

    [Fact]
    public void DariPersentase_Valid_TepatSatu()
    {
        var bobot = KriteriaBobot.DariPersentase(40, 30, 20, 10);
        Assert.Equal(0.40, bobot.Termal, 9);
        Assert.Equal(0.30, bobot.Lembap, 9);
        Assert.Equal(0.20, bobot.Korosi, 9);
        Assert.Equal(0.10, bobot.Usia, 9);
        Assert.Equal(1.0, bobot.Jumlah, 9);
    }

    [Theory]
    [InlineData(99.0)]
    [InlineData(100.1)]
    [InlineData(50.0)]
    public void DariPersentase_JumlahBukan100_Ditolak(double total)
    {
        double diff = total - 100.0;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            KriteriaBobot.DariPersentase(25 + diff, 25, 25, 25));
    }

    [Fact]
    public void DariPersentase_NaNDanNegatif_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(double.NaN, 50, 25, 25));
        Assert.Throws<ArgumentOutOfRangeException>(() => KriteriaBobot.DariPersentase(-10, 50, 30, 30));
    }
}