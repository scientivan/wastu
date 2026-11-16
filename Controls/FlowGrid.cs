using System.Windows;
using System.Windows.Controls;
using Wastu.Services;

namespace Wastu.Controls;

// Padanan CSS grid "repeat(n, minmax(0,1fr))" dengan gap. Tinggi baris = isi tertinggi
// pada baris itu; Stretch=false meniru align-items:start.
public sealed class FlowGrid : Panel
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns), typeof(int), typeof(FlowGrid), new FrameworkPropertyMetadata(2, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty CompactColumnsProperty = DependencyProperty.Register(
        nameof(CompactColumns), typeof(int), typeof(FlowGrid), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty ColumnGapProperty = DependencyProperty.Register(
        nameof(ColumnGap), typeof(double), typeof(FlowGrid), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty RowGapProperty = DependencyProperty.Register(
        nameof(RowGap), typeof(double), typeof(FlowGrid), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch), typeof(bool), typeof(FlowGrid), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    // Jumlah kolom ketika lebar jendela ≤ 1200 px (breakpoint referensi); 0 = tidak berubah.
    public int CompactColumns { get => (int)GetValue(CompactColumnsProperty); set => SetValue(CompactColumnsProperty, value); }
    public double ColumnGap { get => (double)GetValue(ColumnGapProperty); set => SetValue(ColumnGapProperty, value); }
    public double RowGap { get => (double)GetValue(RowGapProperty); set => SetValue(RowGapProperty, value); }
    public bool Stretch { get => (bool)GetValue(StretchProperty); set => SetValue(StretchProperty, value); }

    private double[] _rows = [];
    private double _columnWidth;

    public FlowGrid()
    {
        Loaded += (_, _) => Viewport.Changed += OnViewport;
        Unloaded += (_, _) => Viewport.Changed -= OnViewport;
    }

    private void OnViewport()
    {
        if (CompactColumns > 0) InvalidateMeasure();
    }

    private int EffectiveColumns => Math.Max(1, CompactColumns > 0 && Viewport.Width <= 1200 ? CompactColumns : Columns);

    protected override Size MeasureOverride(Size available)
    {
        var columns = EffectiveColumns;
        var visible = 0;
        foreach (UIElement child in InternalChildren)
            if (child.Visibility != Visibility.Collapsed) visible++;

        var rowCount = (visible + columns - 1) / columns;
        if (_rows.Length != rowCount) _rows = new double[rowCount];
        else Array.Clear(_rows);

        _columnWidth = double.IsInfinity(available.Width)
            ? double.PositiveInfinity
            : ChildWidth((available.Width - ColumnGap * (columns - 1)) / columns);

        var i = 0;
        var widest = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            child.Measure(new Size(_columnWidth, double.PositiveInfinity));
            var row = i++ / columns;
            _rows[row] = Math.Max(_rows[row], child.DesiredSize.Height);
            widest = Math.Max(widest, child.DesiredSize.Width);
        }

        if (double.IsInfinity(_columnWidth)) _columnWidth = widest;
        var height = 0.0;
        foreach (var r in _rows) height += r;
        height += RowGap * Math.Max(0, rowCount - 1);
        var width = double.IsInfinity(available.Width) ? widest * columns + ColumnGap * (columns - 1) : available.Width;
        return new Size(width, height);
    }

    // Chrome memakai lebar kolom pecahan; pembulatan layout WPF bisa melebarkannya hingga 0,5 px
    // sehingga teks terbungkus berbeda. Lebar isi dibulatkan ke bawah, posisi kolom tetap presisi.
    private double ChildWidth(double exact) => Math.Max(0, UseLayoutRounding ? Math.Floor(exact) : exact);

    protected override Size ArrangeOverride(Size final)
    {
        var columns = EffectiveColumns;
        var columnWidth = Math.Max(0, (final.Width - ColumnGap * (columns - 1)) / columns);
        var childWidth = ChildWidth(columnWidth);
        var i = 0;
        var y = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed) continue;
            var column = i % columns;
            var row = i / columns;
            if (column == 0 && row > 0) y += _rows[row - 1] + RowGap;
            var height = Stretch ? _rows[row] : child.DesiredSize.Height;
            child.Arrange(new Rect(column * (columnWidth + ColumnGap), y, childWidth, height));
            i++;
        }
        return final;
    }
}
