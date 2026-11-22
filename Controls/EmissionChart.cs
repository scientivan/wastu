using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Wastu.Services;

namespace Wastu.Controls;

// Grafik emisi kumulatif (step chart). Digambar dalam koordinat viewBox 720×280 lalu diskalakan seragam,
// sama seperti <svg class="chart"> pada referensi (max-height 340 px) (PRD §F5-04, T19).
public sealed class EmissionChart : FrameworkElement
{
    private const double W = 720, H = 280, Left = 54, Right = 18, Top = 20, Bottom = 38;

    public static readonly Brush[] SeriesBrushes = [Freeze(new SolidColorBrush(Color.FromRgb(0x35, 0x7d, 0x4b))), Freeze(new SolidColorBrush(Color.FromRgb(0xe6, 0xa9, 0x4c)))];
    private static readonly Pen GridPen = Freeze(new Pen(Freeze(new SolidColorBrush(Color.FromRgb(0xe4, 0xe9, 0xe1))), 1));
    private static readonly Brush AxisBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x81, 0x92, 0x89)));
    private static readonly Pen[] SeriesPens = SeriesBrushes.Select(b => Freeze(new Pen(b, 3) { LineJoin = PenLineJoin.Round })).ToArray();
    private static readonly Typeface AxisFace = new(GlyphFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    private IReadOnlyList<IReadOnlyList<(double Year, double Value)>> _curves = [];
    private IReadOnlyList<IReadOnlyList<(double Year, double Value)>> _keyPoints = [];
    private int _horizon = 30;
    private DrawingGroup? _drawing;

    public void SetData(
        IReadOnlyList<IReadOnlyList<(double Year, double Value)>> stepCurves,
        int horizon,
        IReadOnlyList<IReadOnlyList<(double Year, double Value)>>? keyPoints = null)
    {
        _curves = stepCurves ?? [];
        _keyPoints = keyPoints ?? [];
        _horizon = horizon <= 0 ? 30 : horizon;
        _drawing = null;
        InvalidateVisual();
    }

    public void SetData(IReadOnlyList<double[]> curves, int horizon)
    {
        var adapted = new List<IReadOnlyList<(double Year, double Value)>>();
        foreach (var curve in curves)
        {
            var pts = new List<(double Year, double Value)>();
            if (curve.Length > 0)
            {
                pts.Add((0.0, curve[0]));
                for (int y = 1; y < curve.Length; y++)
                {
                    if (Math.Abs(curve[y] - curve[y - 1]) > 1e-6)
                    {
                        pts.Add((y, curve[y - 1]));
                    }
                    pts.Add((y, curve[y]));
                }
            }
            adapted.Add(pts);
        }
        SetData(adapted, horizon);
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
            {
                foreach (var pt in curve)
                {
                    ymax = Math.Max(ymax, pt.Value);
                }
            }
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
                if (curve.Count < 2) continue;

                var pen = SeriesPens[Math.Min(i, SeriesPens.Length - 1)];
                var brush = SeriesBrushes[Math.Min(i, SeriesBrushes.Length - 1)];

                var geometry = new StreamGeometry();
                using (var g = geometry.Open())
                {
                    g.BeginFigure(new Point(Px(curve[0].Year), Py(curve[0].Value)), false, false);
                    for (var j = 1; j < curve.Count; j++)
                    {
                        g.LineTo(new Point(Px(curve[j].Year), Py(curve[j].Value)), true, true);
                    }
                }
                geometry.Freeze();
                dc.DrawGeometry(null, pen, geometry);

                // Render key event markers (t=0, interior replacement steps, and endpoint t=H)
                if (i < _keyPoints.Count && _keyPoints[i].Count > 0)
                {
                    foreach (var kp in _keyPoints[i])
                    {
                        dc.DrawEllipse(brush, null, new Point(Px(kp.Year), Py(kp.Value)), 3.5, 3.5);
                    }
                }
                else
                {
                    dc.DrawEllipse(brush, null, new Point(Px(curve[0].Year), Py(curve[0].Value)), 3.5, 3.5);
                    for (int j = 1; j < curve.Count; j++)
                    {
                        if (Math.Abs(curve[j].Year - curve[j - 1].Year) < 1e-6 && curve[j].Value > curve[j - 1].Value)
                        {
                            dc.DrawEllipse(brush, null, new Point(Px(curve[j].Year), Py(curve[j].Value)), 3.5, 3.5);
                        }
                    }
                    dc.DrawEllipse(brush, null, new Point(Px(curve[^1].Year), Py(curve[^1].Value)), 3.5, 3.5);
                }
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
