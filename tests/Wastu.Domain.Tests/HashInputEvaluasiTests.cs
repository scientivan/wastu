namespace Wastu.Domain.Tests;

public class HashInputEvaluasiTests
{
    private readonly ProfilIklim _iklim = new(2500, 80, 28, 10, TingkatKorosi.Sedang, revisiLokasi: 1);
    private readonly KriteriaBobot _bobot = KriteriaBobot.Default;
    private readonly KonfigurasiModel _model = KonfigurasiModel.Bawaan;

    [Fact]
    public void Hitung_Deterministik_MenghasilkanHashSamaPadaInputIdentik()
    {
        string hash1 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");
        string hash2 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 hex string length
        Assert.True(hash1.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void Hitung_SensitifTerhadapLuas()
    {
        string hash1 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");
        string hash2 = HashInputEvaluasi.Hitung(100.1, _iklim, _bobot, _model, "v1");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hitung_SensitifTerhadapIklim()
    {
        var iklimLain = new ProfilIklim(2600, 80, 28, 10, TingkatKorosi.Sedang, revisiLokasi: 1);
        string hash1 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");
        string hash2 = HashInputEvaluasi.Hitung(100.0, iklimLain, _bobot, _model, "v1");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hitung_SensitifTerhadapRevisiLokasiIklim()
    {
        var iklimRevisi2 = new ProfilIklim(2500, 80, 28, 10, TingkatKorosi.Sedang, revisiLokasi: 2);
        string hash1 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");
        string hash2 = HashInputEvaluasi.Hitung(100.0, iklimRevisi2, _bobot, _model, "v1");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hitung_SensitifTerhadapBobot()
    {
        var bobotLain = KriteriaBobot.DariPersentase(40, 30, 20, 10);
        string hash1 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");
        string hash2 = HashInputEvaluasi.Hitung(100.0, _iklim, bobotLain, _model, "v1");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hitung_SensitifTerhadapKonfigurasiModel()
    {
        var modelLain = new KonfigurasiModel(faktorPenuaanA: 0.8);
        string hash1 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");
        string hash2 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, modelLain, "v1");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hitung_SensitifTerhadapIdentitasKatalog()
    {
        string hash1 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v1");
        string hash2 = HashInputEvaluasi.Hitung(100.0, _iklim, _bobot, _model, "v2");

        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-50.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Hitung_LuasTidakValid_MelemparPengecualian(double luas)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HashInputEvaluasi.Hitung(luas, _iklim, _bobot, _model, "v1"));
    }
}
