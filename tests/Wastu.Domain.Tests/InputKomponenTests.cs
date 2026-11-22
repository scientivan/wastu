namespace Wastu.Domain.Tests;

public class InputKomponenTests
{
    [Fact]
    public void InputKomponen_Valid_TersimpanSesuai()
    {
        var input = new InputKomponen(JenisKomponen.Dinding, 120.5);

        Assert.Equal(JenisKomponen.Dinding, input.Jenis);
        Assert.Equal(120.5, input.LuasM2);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-10.0)]
    public void InputKomponen_LuasNolAtauNegatif_Ditolak(double luas)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputKomponen(JenisKomponen.Atap, luas));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InputKomponen_LuasNonFinite_Ditolak(double luas)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputKomponen(JenisKomponen.Dinding, luas));
    }

    [Fact]
    public void InputKomponen_EnumTidakDikenal_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputKomponen((JenisKomponen)999, 50.0));
    }

    [Fact]
    public void Komponen_EnumTidakDikenal_Ditolak()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Komponen(Guid.NewGuid(), Guid.NewGuid(), (JenisKomponen)999, 50.0));
    }
}
