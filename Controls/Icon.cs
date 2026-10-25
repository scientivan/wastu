using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Wastu.Controls;

// Ikon garis 24×24 (gaya SVG referensi) yang mewarisi Foreground induknya.
public sealed class Icon : FrameworkElement
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(Geometry), typeof(Icon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(
        typeof(Icon), new FrameworkPropertyMetadata(Brushes.Black,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits, ResetPen));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(Icon), new FrameworkPropertyMetadata(1.75, FrameworkPropertyMetadataOptions.AffectsRender, ResetPen));

    public Geometry? Data { get => (Geometry?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
    public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

    private Pen? _pen;
    private ScaleTransform? _scale;

    private static void ResetPen(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Icon)d)._pen = null;

    protected override Size MeasureOverride(Size availableSize) => new(0, 0);

    protected override void OnRender(DrawingContext dc)
    {
        if (Data is null || ActualWidth <= 0) return;
        if (_pen is null)
        {
            _pen = new Pen(Foreground, StrokeThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            _pen.Freeze();
        }
        var s = ActualWidth / 24;
        if (_scale is null || _scale.ScaleX != s)
        {
            _scale = new ScaleTransform(s, s);
            _scale.Freeze();
        }
        dc.PushTransform(_scale);
        dc.DrawGeometry(null, _pen, Data);
        dc.Pop();
    }
}
