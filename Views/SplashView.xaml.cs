using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Wastu.Views;

public partial class SplashView : UserControl
{
    private readonly AnimationClock? _loading;

    public SplashView()
    {
        InitializeComponent();
        if (!SystemParameters.ClientAreaAnimation)
        {
            // prefers-reduced-motion: indikator penuh tanpa animasi.
            Bar.Width = 150;
            BarShift.X = 0;
            return;
        }

        // @keyframes loading { translateX(-100%) → translateX(330%) } 1.1s ease-in-out infinite
        var animation = new DoubleAnimation(-67.5, 67.5 * 3.3, TimeSpan.FromMilliseconds(1100))
        {
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = CubicBezierEase.CssEaseInOut
        };
        animation.Freeze();
        _loading = animation.CreateClock();
        BarShift.ApplyAnimationClock(TranslateTransform.XProperty, _loading);
    }

    public void Dismiss(bool fade)
    {
        IsHitTestVisible = false;
        if (!fade || !SystemParameters.ClientAreaAnimation)
        {
            Hide();
            return;
        }
        // .splash { transition: opacity .22s ease }
        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220)) { EasingFunction = CubicBezierEase.CssEase };
        animation.Completed += (_, _) => Hide();
        BeginAnimation(OpacityProperty, animation);
    }

    private void Hide()
    {
        _loading?.Controller?.Stop();
        Visibility = Visibility.Collapsed;
    }
}

// Fungsi waktu cubic-bezier CSS.
public sealed class CubicBezierEase(double x1, double y1, double x2, double y2) : IEasingFunction
{
    public static readonly CubicBezierEase CssEase = new(.25, .1, .25, 1);
    public static readonly CubicBezierEase CssEaseInOut = new(.42, 0, .58, 1);

    public double Ease(double t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        // Cari parameter kurva untuk x = t (Newton-Raphson), lalu hitung y.
        var u = t;
        for (var i = 0; i < 8; i++)
        {
            var x = Bezier(u, x1, x2) - t;
            var dx = Derivative(u, x1, x2);
            if (Math.Abs(x) < 1e-6 || Math.Abs(dx) < 1e-6) break;
            u -= x / dx;
        }
        return Bezier(u, y1, y2);
    }

    private static double Bezier(double u, double p1, double p2) =>
        3 * (1 - u) * (1 - u) * u * p1 + 3 * (1 - u) * u * u * p2 + u * u * u;

    private static double Derivative(double u, double p1, double p2) =>
        3 * (1 - u) * (1 - u) * p1 + 6 * (1 - u) * u * (p2 - p1) + 3 * u * u * (1 - p2);
}
