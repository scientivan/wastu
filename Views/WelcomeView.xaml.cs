using System.Windows;
using System.Windows.Controls;
using Wastu.Services;

namespace Wastu.Views;

public partial class WelcomeView : UserControl
{
    private readonly MainWindow _window;

    public WelcomeView(MainWindow window)
    {
        _window = window;
        InitializeComponent();
        Architecture.Source = ImageCache.Welcome;
        Loaded += (_, _) => { Viewport.Changed += ApplyViewport; ApplyViewport(); };
        Unloaded += (_, _) => Viewport.Changed -= ApplyViewport;
    }

    private void ApplyViewport()
    {
        var wide = Viewport.Width >= 1800;
        var size = Viewport.Clamp(36, 3.8, 64);
        Headline.FontSize = size;
        Headline.LineHeight = size * 1.14;
        Headline.Tracking = size * -0.055;
        Copy.Margin = new Thickness(Viewport.Clamp(30, 4.6, 76), wide ? 55 : 38, Viewport.Clamp(30, 4.6, 76), wide ? 55 : 38);
        Intro.Margin = wide ? new Thickness(0, 80, 0, 90) : new Thickness(0, 45, 0, 65);
        Media.Padding = new Thickness(Viewport.Clamp(12, 2, 36));
    }

    private void Create_Click(object sender, RoutedEventArgs e) => _window.ShowCreate();
}
