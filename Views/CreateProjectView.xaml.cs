using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wastu.Controls;
using Wastu.Services;

namespace Wastu.Views;

// create.html: pembuatan proyek tiga langkah (identitas, lokasi, luas bidang).
public partial class CreateProjectView : UserControl
{
    private static readonly string[] StepNames = ["Identitas", "Lokasi", "Luas bidang"];

    private readonly MainWindow _window;
    private readonly StackPanel[] _sections;
    private readonly TrackedText[] _titles;
    private readonly (Border Circle, TextBlock Number, TextBlock Label)[] _steps = new (Border, TextBlock, TextBlock)[3];
    private readonly Popup _bubble = new() { Placement = PlacementMode.Bottom, StaysOpen = true, AllowsTransparency = false };
    private readonly CssText _bubbleText = new() { FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(9, 0, 0, 0) };
    private readonly DispatcherTimer _bubbleTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private int _step;

    public CreateProjectView(MainWindow window)
    {
        _window = window;
        InitializeComponent();
        _sections = [Step0, Step1, Step2];
        _titles = [Title0, Title1, Title2];
        Illustration.Source = ImageCache.Planning;
        BuildSteps();
        BuildBubble();

        foreach (var name in Katalog.NamaKota) CityBox.Items.Add(name);
        CityBox.Items.Add(Katalog.LokasiLain);
        CityBox.SelectedIndex = 0;

        AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => HideBubble()));
        Loaded += (_, _) => { Viewport.Changed += ApplyViewport; ApplyViewport(); };
        Unloaded += (_, _) => { Viewport.Changed -= ApplyViewport; HideBubble(); };
        SizeChanged += (_, _) => ApplyViewport();
        ShowStep(0);
    }

    private void BuildSteps()
    {
        for (var i = 0; i < 3; i++)
        {
            var item = new Grid { Height = 78 };
            if (i < 2)
                item.Children.Add(new Border
                {
                    Width = 1, Height = 29, Background = (Brush)FindResource("StepLine"),
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(17, 42, 0, 0)
                });
            var number = new CssText { Text = (i + 1).ToString(), FontSize = 15, FontWeight = FontWeight.FromOpenTypeWeight(550), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var circle = new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(18), BorderThickness = new Thickness(1), Child = number, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
            var label = new CssText { Text = StepNames[i], FontSize = 15, Margin = new Thickness(51, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var row = new Grid { Height = 36, VerticalAlignment = VerticalAlignment.Top };
            row.Children.Add(circle);
            row.Children.Add(label);
            item.Children.Add(row);
            Steps.Children.Add(item);
            _steps[i] = (circle, number, label);
        }
    }

    // Padanan gelembung validasi bawaan browser (reportValidity).
    private void BuildBubble()
    {
        var mark = new Border
        {
            Width = 20, Height = 20, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromRgb(0xF2, 0x99, 0x1D)),
            Child = new CssText { Text = "!", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(mark);
        content.Children.Add(_bubbleText);
        var bubble = new Border
        {
            Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xB9, 0xC2, 0xB8)), BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 8, 14, 8), Child = content
        };
        TextElement.SetFontFamily(bubble, (FontFamily)FindResource("Inter"));
        TextElement.SetForeground(bubble, (Brush)FindResource("Ink"));
        _bubble.Child = bubble;
        _bubbleTimer.Tick += (_, _) => HideBubble();
    }

    private void ApplyViewport()
    {
        var wide = Viewport.Width >= 1700;
        SidebarColumn.Width = new GridLength(wide ? 320 : 280);
        var size = Viewport.Clamp(28, 2.5, 39);
        foreach (var title in _titles)
        {
            title.FontSize = size;
            title.LineHeight = size * 1.2;
        }
        var pad = Viewport.Clamp(32, 6, 96);
        Body.Margin = new Thickness(pad, wide ? 70 : 50, pad, 50);
        Footer.Padding = new Thickness(pad, 20, pad, 20);
        if (ActualHeight > 0)
        {
            Workspace.MinHeight = Math.Max(0, ActualHeight - 84);
            Illustration.MaxHeight = Math.Clamp(ActualHeight - 500, 96, 240);
        }
        LayoutBody();
    }

    private void Wizard_SizeChanged(object sender, SizeChangedEventArgs e) => LayoutBody();

    // .wizard-body { width: 100%; max-width: 1100px; padding: 50px clamp(32px, 6vw, 96px) }
    private void LayoutBody()
    {
        var available = Math.Min(1100, Wizard.ActualWidth);
        Body.Width = Math.Max(0, available - Body.Margin.Left - Body.Margin.Right);
    }

    private void ShowStep(int next)
    {
        HideBubble();
        _step = next;
        for (var i = 0; i < 3; i++)
        {
            _sections[i].Visibility = i == _step ? Visibility.Visible : Visibility.Collapsed;
            var (circle, number, label) = _steps[i];
            var current = i == _step;
            var complete = i < _step;
            circle.Background = (Brush)FindResource(current ? "Green" : complete ? "StepDoneBg" : "PaperEntry");
            circle.BorderBrush = (Brush)FindResource(current ? "Green" : complete ? "StepDoneBorder" : "StepBorder");
            number.Foreground = (Brush)FindResource(current ? "White" : complete ? "Green" : "StepInk");
            label.Foreground = (Brush)FindResource(current ? "Green" : "StepInk");
            label.FontWeight = current ? FontWeights.Bold : FontWeights.Medium;
        }
        StepCount.Text = $"LANGKAH {_step + 1} DARI 3";
        BackButton.Content = _step > 0 ? "Kembali" : "Batal";
        NextButton.Content = _step == 2 ? "Simpan proyek" : "Lanjutkan";
        ErrorText.Visibility = Visibility.Collapsed;
        SummaryName.Text = NameBox.Text;
        SummaryCity.Text = IsCustomCity ? CustomCityBox.Text : (string)CityBox.SelectedItem;
        Scroller.ScrollToTop();
    }

    private bool IsCustomCity => (string)CityBox.SelectedItem == Katalog.LokasiLain;

    private void City_Changed(object sender, SelectionChangedEventArgs e)
    {
        CustomCityGroup.Visibility = IsCustomCity ? Visibility.Visible : Visibility.Collapsed;
        if (Katalog.Kota.TryGetValue((string)CityBox.SelectedItem, out var kota))
        {
            LatBox.Text = Format.Masukan(kota.Lintang);
            LonBox.Text = Format.Masukan(kota.Bujur);
            CoastalBox.Text = Format.Masukan(kota.JarakPesisir);
        }
        else
        {
            LatBox.Text = LonBox.Text = CoastalBox.Text = "";
        }
    }

    private void Number_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        foreach (var c in e.Text)
            if (!char.IsDigit(c) && c is not (',' or '.' or '-' or '+' or 'e' or 'E'))
            {
                e.Handled = true;
                return;
            }
    }

    private bool ValidStep()
    {
        var fields = _step switch
        {
            0 => new (Control, Func<string?>)[] { (NameBox, () => NameBox.Text.Trim().Length == 0 ? "Masukkan nama proyek." : null) },
            1 => [
                (CustomCityBox, () => IsCustomCity && CustomCityBox.Text.Trim().Length == 0 ? "Masukkan kota atau kabupaten." : null),
                (LatBox, () => Range(LatBox.Text, -11, 6)),
                (LonBox, () => Range(LonBox.Text, 95, 141)),
                (CoastalBox, () => Range(CoastalBox.Text, 0, null))
            ],
            _ => [
                (WallBox, () => Range(WallBox.Text, 0.1, null)),
                (RoofBox, () => Range(RoofBox.Text, 0.1, null))
            ]
        };
        foreach (var (field, check) in fields)
        {
            if (check() is not { } message) continue;
            field.Focus();
            ShowBubble(field, message);
            return false;
        }
        return true;
    }

    private static string? Range(string text, double min, double? max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Harap isi bidang ini.";
        if (!Format.CobaBaca(text, out var v)) return "Masukkan angka.";
        if (v < min) return $"Nilai harus lebih besar dari atau sama dengan {Format.Masukan(min)}.";
        if (max is { } m && v > m) return $"Nilai harus lebih kecil dari atau sama dengan {Format.Masukan(m)}.";
        return null;
    }

    private void ShowBubble(Control target, string message)
    {
        _bubbleText.Text = message;
        _bubble.PlacementTarget = target;
        _bubble.HorizontalOffset = 12;
        _bubble.VerticalOffset = 4;
        _bubble.IsOpen = true;
        _bubbleTimer.Stop();
        _bubbleTimer.Start();
    }

    private void HideBubble()
    {
        _bubbleTimer.Stop();
        _bubble.IsOpen = false;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_step > 0) ShowStep(_step - 1);
        else _window.LeaveCreate();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => _window.LeaveCreate();

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidStep()) return;
        if (_step < 2)
        {
            ShowStep(_step + 1);
            return;
        }

        var name = NameBox.Text.Trim();
        var city = IsCustomCity ? CustomCityBox.Text.Trim() : (string)CityBox.SelectedItem;
        if (name.Length == 0 || city.Length == 0 ||
            !Format.CobaBaca(LatBox.Text, out var lat) || !Format.CobaBaca(LonBox.Text, out var lon) ||
            !Format.CobaBaca(WallBox.Text, out var wall) || !Format.CobaBaca(RoofBox.Text, out var roof) ||
            !Format.CobaBaca(CoastalBox.Text, out var coastal) ||
            lat < -11 || lat > 6 || lon < 95 || lon > 141 || wall <= 0 || roof <= 0 || coastal < 0)
        {
            ErrorText.Text = "Periksa kembali data proyek sebelum menyimpan.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        App.State.TambahProyek(name, city, lat, lon, wall, roof, coastal);
        App.State.Ubah();
        _window.LeaveCreate();
    }
}
