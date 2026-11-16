using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wastu.Controls;

// TextBlock dengan model tinggi baris Chromium:
//  - line-height normal = ascent + descent yang masing-masing dibulatkan ke piksel perangkat;
//  - line-height eksplisit membagi sisa ruang (leading) sama rata di atas dan bawah.
// WPF menaruh baseline secara proporsional (LineHeight × ascent/(ascent+descent)). Selisihnya
// konstan per baris, jadi dikoreksi dengan translasi render yang dibulatkan ke piksel (tanpa
// biaya layout dan tanpa membuat teks buram).
public sealed class CssText : TextBlock
{
    // Metrik hhea Inter (em).
    public const double Ascent = 0.96875, Descent = 0.2412109375;

    // line-height CSS sebagai rasio terhadap font-size elemen ini; NaN = normal.
    public static readonly DependencyProperty LeadingProperty = DependencyProperty.Register(
        nameof(Leading), typeof(double), typeof(CssText), new FrameworkPropertyMetadata(double.NaN, Changed));

    // Ukuran font blok induk saat teks ini berupa inline (<small> di dalam <div>): kotak baris
    // mengikuti strut induk yang lebih besar.
    public static readonly DependencyProperty StrutProperty = DependencyProperty.Register(
        nameof(Strut), typeof(double), typeof(CssText), new FrameworkPropertyMetadata(double.NaN, Changed));

    static CssText() => FontSizeProperty.OverrideMetadata(typeof(CssText), new FrameworkPropertyMetadata(Changed));

    public CssText()
    {
        LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        Apply();
    }

    public double Leading { get => (double)GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }
    public double Strut { get => (double)GetValue(StrutProperty); set => SetValue(StrutProperty, value); }

    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((CssText)d).Apply();

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Apply();
    }

    private void Apply()
    {
        var scale = VisualTreeHelper.GetDpi(this).DpiScaleY;
        var size = double.IsNaN(Strut) ? FontSize : Math.Max(Strut, FontSize);
        var (line, baseline) = Metrics(size, double.IsNaN(Leading) ? double.NaN : Leading * FontSize, scale);
        LineHeight = line;
        var shift = Math.Round((baseline - line * Ascent / (Ascent + Descent)) * scale) / scale;
        if (RenderTransform is TranslateTransform current && current.Y == shift) return;
        var transform = new TranslateTransform(0, shift);
        transform.Freeze();
        RenderTransform = transform;
    }

    // (tinggi baris, posisi baseline dari atas) menurut Chromium.
    public static (double Line, double Baseline) Metrics(double size, double explicitLine, double scale)
    {
        var ascent = Math.Round(Ascent * size * scale) / scale;
        var descent = Math.Round(Descent * size * scale) / scale;
        if (double.IsNaN(explicitLine)) return (ascent + descent, ascent);
        return (explicitLine, (explicitLine - ascent - descent) / 2 + ascent);
    }
}
