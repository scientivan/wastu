using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Wastu.Services;

namespace Wastu.Controls;

// Grafik emisi kumulatif. Digambar dalam koordinat viewBox 720×280 lalu diskalakan seragam,
// sama seperti <svg class="chart"> pada referensi (max-height 340 px).
public sealed class EmissionChart : FrameworkElement
{
    private const double W = 720, H = 280, Left = 54, Right = 18, Top = 20, Bottom = 38;

    public static readonly Brush[] SeriesBrushes = [Freeze(new SolidColorBrush(Color.FromRgb(0x35, 0x7d, 0x4b))), Freeze(new SolidColorBrush(Color.FromRgb(0xe6, 0xa9, 0x4c)))];
    private static readonly Pen GridPen = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromRgb(0xe4, 0xe9, 0xe1))), 1));
    private static readonly Brush AxisBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x81, 0x92, 0x89)));
    private static readonly Pen[] SeriesPens = SeriesBrushes.Select(b => Freeze(new Pen(b, 3) { LineJoin = PenLineJoin.Round })).ToArray();
    private static readonly Typeface AxisFace = new(GlyphFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    private IReadOnlyList<double[]> _curves = [];
    private int _horizon = 30;
    private DrawingGroup? _drawing;

    public void SetData(IReadOnlyList<double[]> curves, int horizon)
    {
        _curves = curves;
        _horizon = horizon;
        _drawing = null;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size available)
    {
        var width = double.IsInfinity(available.Width) ? W : available.Width;
        return new Size(width, Math.Min(width * H / W, 340));
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) => _drawing = null;

    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        _drawing ??= Build(VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var scale = Math.Min(ActualWidth / W, ActualHeight / H);
        dc.PushTransform(new TranslateTransform((ActualWidth - W * scale) / 2, (ActualHeight - H * scale) / 2));
        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.DrawDrawing(_drawing);
        dc.Pop();
        dc.Pop();
    }

    private DrawingGroup Build(double pixelsPerDip)
    {
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            var ymax = 1.0;
            foreach (var curve in _curves)
                foreach (var v in curve) ymax = Math.Max(ymax, v);
            ymax *= 1.14;
            double Px(double year) => Left + year / _horizon * (W - Left - Right);
            double Py(double value) => H - Bottom - value / ymax * (H - Top - Bottom);

            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, W, H));
            foreach (var z in new[] { 0, .25, .5, .75, 1 })
            {
                var y = Py(ymax * z);
                dc.DrawLine(GridPen, new Point(Left, y), new Point(W - Right, y));
                var label = Text(Format.Angka(ymax * z / 1000) + "t", pixelsPerDip);
                dc.DrawText(label, new Point(Left - 8 - label.WidthIncludingTrailingWhitespace, y + 4 - label.Baseline));
            }
            foreach (var z in new[] { 0, .25, .5, .75, 1 })
            {
                var label = Text(Math.Round(_horizon * z, MidpointRounding.AwayFromZero) + " th", pixelsPerDip);
                dc.DrawText(label, new Point(Px(_horizon * z) - label.WidthIncludingTrailingWhitespace / 2, H - 12 - label.Baseline));
            }
            for (var i = 0; i < _curves.Count; i++)
            {
                var curve = _curves[i];
                if (curve.Length < 2) continue;
                var geometry = new StreamGeometry();
                using (var g = geometry.Open())
                {
                    g.BeginFigure(new Point(Px(0), Py(curve[0])), false, false);
                    for (var year = 1; year < curve.Length; year++)
                        g.LineTo(new Point(Px(year), Py(curve[year])), true, true);
                }
                geometry.Freeze();
                dc.DrawGeometry(null, SeriesPens[Math.Min(i, 1)], geometry);
            }
        }
        group.Freeze();
        return group;
    }

    private static FormattedText Text(string text, double pixelsPerDip) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, AxisFace, 10, AxisBrush, pixelsPerDip);

    private static FontFamily GlyphFamily() =>
        new(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Inter");

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
