using System.Windows;
using System.Windows.Threading;
using Wastu.Services;

namespace Wastu.Views;

// Satu jendela untuk seluruh alur: splash → welcome/formulir → ruang kerja.
// Tampilan disimpan setelah dibuat sehingga perpindahan tidak membangun ulang pohon visual.
public partial class MainWindow : Window
{
    // Durasi minimum splash pada welcome.js.
    private static readonly TimeSpan SplashMinimum = TimeSpan.FromMilliseconds(600);

    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromMilliseconds(2800) };
    private WelcomeView? _welcome;
    private ShellView? _shell;

    public MainWindow()
    {
        InitializeComponent();
        var area = SystemParameters.WorkArea;
        Width = Math.Min(1440, area.Width * 0.92);
        Height = Math.Min(900, area.Height * 0.92);
        if (area.Width < 1180) WindowState = WindowState.Maximized;

        Root.SizeChanged += (_, e) => Viewport.Update(e.NewSize.Width);
        _toastTimer.Tick += (_, _) => HideToast();
    }

    public async Task StartAsync(Task<AppState> state)
    {
        var minimum = Task.Delay(SplashMinimum);
        App.State = await state;
        await ImageCache.PreloadAsync();

        // Tujuan dibangun di balik splash supaya tampil seketika saat splash ditutup.
        var hasProject = App.State.AdaProyek;
        if (hasProject) ShowShell("home");
        else ShowWelcome();
        await minimum;
        Splash.Dismiss(fade: !hasProject);
        _shell?.Prewarm();
    }

    public void ShowWelcome()
    {
        HideToast();
        _welcome ??= new WelcomeView(this);
        Host.Child = _welcome;
        Title = "Wastu";
    }

    public void ShowCreate()
    {
        HideToast();
        Host.Child = new CreateProjectView(this);
        Title = "Proyek baru | Wastu";
    }

    // Tutup formulir: kembali ke ruang kerja bila sudah ada proyek, selain itu ke welcome.
    public void LeaveCreate()
    {
        if (App.State.AdaProyek) ShowShell("home");
        else ShowWelcome();
    }

    public void ShowShell(string page)
    {
        var first = _shell is null;
        _shell ??= new ShellView(this);
        Host.Child = _shell;
        _shell.Go(page);
        if (first && Splash.Visibility != Visibility.Visible) _shell.Prewarm();
    }

    // Toast milik satu halaman; berpindah ke welcome/formulir (halaman HTML lain) menghapusnya.
    private void HideToast()
    {
        _toastTimer.Stop();
        ToastBox.Visibility = Visibility.Collapsed;
    }

    public void Toast(string message)
    {
        ToastText.Text = message;
        ToastBox.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }
}
