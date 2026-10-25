using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Wastu.Controls;

// Teks satu gaya dengan letter-spacing ala CSS, digambar langsung sebagai GlyphRun.
// WPF tidak punya letter-spacing, sedangkan judul referensi memakai tracking negatif.
// Mendukung baris baru (\n), pembungkusan per kata, dan satu sufiks bergaya berbeda
// yang sejajar baseline (mis. "86" + " / 100").
public sealed class TrackedText : FrameworkElement
{
    private const FrameworkPropertyMetadataOptions Layout =
        FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender;

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(TrackedText), new FrameworkPropertyMetadata("", Layout, Reset));

    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
        typeof(TrackedText), new FrameworkPropertyMetadata(14.0, Layout | FrameworkPropertyMetadataOptions.Inherits, Reset));

    public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
        typeof(TrackedText), new FrameworkPropertyMetadata(FontWeights.Normal, Layout | FrameworkPropertyMetadataOptions.Inherits, Reset));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(TrackedText), new FrameworkPropertyMetadata(Brushes.Black,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty TrackingProperty = DependencyProperty.Register(
        nameof(Tracking), typeof(double), typeof(TrackedText), new FrameworkPropertyMetadata(0.0, Layout, Reset));

    public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(
        nameof(LineHeight), typeof(double), typeof(TrackedText), new FrameworkPropertyMetadata(double.NaN, Layout, Reset));

    public static readonly DependencyProperty WrapProperty = DependencyProperty.Register(
        nameof(Wrap), typeof(bool), typeof(TrackedText), new FrameworkPropertyMetadata(false, Layout, Reset));

    public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
        nameof(TextAlignment), typeof(TextAlignment), typeof(TrackedText), new FrameworkPropertyMetadata(TextAlignment.Left, Layout, Reset));

    public static readonly DependencyProperty SuffixProperty = DependencyProperty.Register(
        nameof(Suffix), typeof(string), typeof(TrackedText), new FrameworkPropertyMetadata("", Layout, Reset));

    public static readonly DependencyProperty SuffixFontSizeProperty = DependencyProperty.Register(
        nameof(SuffixFontSize), typeof(double), typeof(TrackedText), new FrameworkPropertyMetadata(12.0, Layout, Reset));

    public static readonly DependencyProperty SuffixFontWeightProperty = DependencyProperty.Register(
        nameof(SuffixFontWeight), typeof(FontWeight), typeof(TrackedText), new FrameworkPropertyMetadata(FontWeights.Normal, Layout, Reset));

    public static readonly DependencyProperty SuffixForegroundProperty = DependencyProperty.Register(
        nameof(SuffixForeground), typeof(Brush), typeof(TrackedText), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SuffixTrackingProperty = DependencyProperty.Register(
        nameof(SuffixTracking), typeof(double), typeof(TrackedText), new FrameworkPropertyMetadata(0.0, Layout, Reset));

    public static readonly DependencyProperty SuffixGapProperty = DependencyProperty.Register(
        nameof(SuffixGap), typeof(double), typeof(TrackedText), new FrameworkPropertyMetadata(0.0, Layout, Reset));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public double Tracking { get => (double)GetValue(TrackingProperty); set => SetValue(TrackingProperty, value); }
    public double LineHeight { get => (double)GetValue(LineHeightProperty); set => SetValue(LineHeightProperty, value); }
    public bool Wrap { get => (bool)GetValue(WrapProperty); set => SetValue(WrapProperty, value); }
    public TextAlignment TextAlignment { get => (TextAlignment)GetValue(TextAlignmentProperty); set => SetValue(TextAlignmentProperty, value); }
    public string Suffix { get => (string)GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
    public double SuffixFontSize { get => (double)GetValue(SuffixFontSizeProperty); set => SetValue(SuffixFontSizeProperty, value); }
    public FontWeight SuffixFontWeight { get => (FontWeight)GetValue(SuffixFontWeightProperty); set => SetValue(SuffixFontWeightProperty, value); }
    public Brush? SuffixForeground { get => (Brush?)GetValue(SuffixForegroundProperty); set => SetValue(SuffixForegroundProperty, value); }
    public double SuffixTracking { get => (double)GetValue(SuffixTrackingProperty); set => SetValue(SuffixTrackingProperty, value); }
    public double SuffixGap { get => (double)GetValue(SuffixGapProperty); set => SetValue(SuffixGapProperty, value); }

    private readonly List<string> _lines = [];
    private double[] _lineWidths = [];
    private double _suffixWidth;
    private double _lineHeight, _baseline;
    private List<(GlyphRun Run, bool Suffix)>? _runs;
    private double _runsWidth = double.NaN;

    private static void Reset(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TrackedText)d)._runs = null;

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        _runs = null;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size available)
    {
        // Tinggi baris dan baseline mengikuti Chromium (lihat CssText), baseline diselaraskan ke piksel.
        var scale = VisualTreeHelper.GetDpi(this).DpiScaleY;
        var (line, baseline) = CssText.Metrics(FontSize, LineHeight, scale);
        _lineHeight = line;
        _baseline = Math.Round(baseline * scale) / scale;

        _lines.Clear();
        foreach (var paragraph in Text.Split('\n'))
        {
            if (!Wrap || double.IsInfinity(available.Width) || LineWidth(paragraph) <= available.Width)
            {
                _lines.Add(paragraph);
                continue;
            }
            var current = "";
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length == 0 || LineWidth(candidate) <= available.Width) current = candidate;
                else { _lines.Add(current); current = word; }
            }
            _lines.Add(current);
        }

        _lineWidths = new double[_lines.Count];
        var widest = 0.0;
        for (var i = 0; i < _lines.Count; i++)
            widest = Math.Max(widest, _lineWidths[i] = LineWidth(_lines[i]));

        _suffixWidth = Suffix.Length > 0
            ? SuffixGap + Measure(Suffix, GlyphFonts.Inter(SuffixFontWeight), SuffixFontSize, SuffixTracking)
            : 0;
        widest = Math.Max(widest, _lineWidths[^1] + _suffixWidth);
        _runs = null;
        return new Size(widest, _lineHeight * _lines.Count);
    }

    private double LineWidth(string text) => Measure(text, GlyphFonts.Inter(FontWeight), FontSize, Tracking);

    private static double Measure(string text, GlyphTypeface primary, double size, double tracking)
    {
        var width = 0.0;
        foreach (var c in text)
        {
            var (face, glyph) = GlyphFonts.Resolve(primary, c);
            width += face.AdvanceWidths[glyph] * size + tracking;
        }
        return width;
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_lines.Count == 0) return;
        if (_runs is null || _runsWidth != ActualWidth) BuildRuns();
        var suffixBrush = SuffixForeground ?? Foreground;
        foreach (var (run, suffix) in _runs!)
            dc.DrawGlyphRun(suffix ? suffixBrush : Foreground, run);
    }

    private void BuildRuns()
    {
        var dpi = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        _runs = [];
        _runsWidth = ActualWidth;
        var primary = GlyphFonts.Inter(FontWeight);
        for (var i = 0; i < _lines.Count; i++)
        {
            var lineWidth = _lineWidths[i] + (i == _lines.Count - 1 ? _suffixWidth : 0);
            var x = TextAlignment switch
            {
                TextAlignment.Center => (ActualWidth - lineWidth) / 2,
                TextAlignment.Right => ActualWidth - lineWidth,
                _ => 0
            };
            var y = i * _lineHeight + _baseline;
            x = AddRuns(_lines[i], primary, FontSize, Tracking, x, y, dpi, false);
            if (i == _lines.Count - 1 && Suffix.Length > 0)
                AddRuns(Suffix, GlyphFonts.Inter(SuffixFontWeight), SuffixFontSize, SuffixTracking, x + SuffixGap, y, dpi, true);
        }
    }

    private double AddRuns(string text, GlyphTypeface primary, double size, double tracking, double x, double y, float dpi, bool suffix)
    {
        var start = 0;
        while (start < text.Length)
        {
            var face = GlyphFonts.Resolve(primary, text[start]).Face;
            var end = start;
            var glyphs = new List<ushort>();
            var advances = new List<double>();
            while (end < text.Length)
            {
                var (f, g) = GlyphFonts.Resolve(primary, text[end]);
                if (f != face) break;
                glyphs.Add(g);
                advances.Add(f.AdvanceWidths[g] * size + tracking);
                end++;
            }
            var origin = new Point(x, y);
            _runs!.Add((new GlyphRun(face, 0, false, size, dpi, glyphs, origin, advances,
                null, null, null, null, null, null), suffix));
            foreach (var a in advances) x += a;
            start = end;
        }
        return x;
    }
}

public static class GlyphFonts
{
    private static readonly int[] Weights = [400, 500, 550, 600, 650, 700, 750];
    private static readonly GlyphTypeface?[] Cache = new GlyphTypeface?[Weights.Length];
    private static readonly Dictionary<int, GlyphTypeface> Fallback = [];

    public static GlyphTypeface Inter(FontWeight weight)
    {
        var w = weight.ToOpenTypeWeight();
        var best = 0;
        for (var i = 1; i < Weights.Length; i++)
            if (Math.Abs(Weights[i] - w) < Math.Abs(Weights[best] - w)) best = i;
        return Cache[best] ??= new GlyphTypeface(new Uri($"pack://application:,,,/Assets/Fonts/Inter-{Weights[best]}.ttf"));
    }

    // Glyph yang tidak ada di subset Inter (mis. ₂) jatuh ke Segoe UI, seperti di browser.
    public static (GlyphTypeface Face, ushort Glyph) Resolve(GlyphTypeface primary, char c)
    {
        if (primary.CharacterToGlyphMap.TryGetValue(c, out var glyph)) return (primary, glyph);
        var w = primary.Weight.ToOpenTypeWeight();
        if (!Fallback.TryGetValue(w, out var face))
        {
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, primary.Weight, FontStretches.Normal)
                .TryGetGlyphTypeface(out face);
            Fallback[w] = face;
        }
        return face.CharacterToGlyphMap.TryGetValue(c, out glyph) ? (face, glyph) : (primary, (ushort)0);
    }
}
