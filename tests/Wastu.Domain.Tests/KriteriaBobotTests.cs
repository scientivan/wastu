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
}