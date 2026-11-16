using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace Wastu.Views;

// Data tampilan bersifat immutable dan dibuat ulang setiap render. Event kosong ini
// membuat WPF tidak memakai PropertyDescriptor.AddValueChanged (yang menahan referensi).
public abstract class ViewData : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
}

public sealed class TagData(string text, Brush bg, Brush fg) : ViewData
{
    public string Text { get; } = text;
    public Brush Bg { get; } = bg;
    public Brush Fg { get; } = fg;

    // tag(score) pada referensi
    public static TagData Kesesuaian(double skor) => skor < 55
        ? new("Tidak disarankan", Palette.TagRedBg, Palette.TagRedFg)
        : skor < 75
            ? new("Perlu pertimbangan", Palette.TagGoldBg, Palette.TagGoldFg)
            : new("Cocok", Palette.TagBg, Palette.TagFg);
}

public static class Palette
{
    private static Brush Get(string key) => (Brush)Application.Current.Resources[key];
    private static Brush Hex(uint rgb)
    {
        var brush = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        brush.Freeze();
        return brush;
    }

    public static readonly Brush TagBg = Get("TagBg"), TagFg = Get("TagFg");
    public static readonly Brush TagGoldBg = Get("TagGoldBg"), TagGoldFg = Get("TagGoldFg");
    public static readonly Brush TagPaleBg = Get("TagPaleBg"), TagPaleFg = Get("TagPaleFg");
    public static readonly Brush TagRedBg = Hex(0xFAEAE4), TagRedFg = Hex(0xA34F42);
    public static readonly Brush Score = Get("Score"), Red = Get("Red"), Ink = Get("Ink"), Muted = Get("Muted");
    public static readonly Brush Line = Get("Line"), White = Get("White"), Green = Get("Green");
    public static readonly Brush PillActiveBg = Hex(0xEDF5E4), PillActiveBorder = Hex(0x90AE7C);
    public static readonly Brush DayGoodBg = Hex(0xF2F6EC), DayGoodBorder = Hex(0xCFDDC6);
    public static readonly Brush DayBadBg = Hex(0xFCF1EC), DayBadBorder = Hex(0xE8CFC3);
    public static readonly Brush DayWarnBg = Hex(0xFFF6DF), DayWarnBorder = Hex(0xE8D6A5);
}
