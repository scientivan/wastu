using System.Globalization;
using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Markup;
using Wastu.Services;
using Wastu.Views;

namespace Wastu;

public partial class App : Application
{
    public static AppState State { get; set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Multicore JIT: profil startup direkam sekali lalu dipakai untuk mengompilasi paralel.
        Directory.CreateDirectory(StateStore.ProfileFolder);
        ProfileOptimization.SetProfileRoot(StateStore.ProfileFolder);
        ProfileOptimization.StartProfile("startup.jitprofile");

        CultureInfo.DefaultThreadCurrentCulture = Format.Id;
        CultureInfo.DefaultThreadCurrentUICulture = Format.Id;
        CultureInfo.CurrentCulture = Format.Id;
        CultureInfo.CurrentUICulture = Format.Id;
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(Format.Id.IetfLanguageTag)));

        // Berjalan paralel dengan parsing XAML jendela utama.
        var state = Task.Run(AppState.Muat);
        ImageCache.PreloadAsync();

        // MessageBox menjalankan message loop sendiri; tanpa penjaga ini exception yang berulang
        // (mis. saat layout/render) menumpuk dialog tanpa henti dan aplikasi tidak bisa ditutup.
        var showingError = false;
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            if (showingError) return;
            showingError = true;
            MessageBox.Show(MainWindow, "Terjadi kesalahan: " + args.Exception.Message, "Wastu",
                MessageBoxButton.OK, MessageBoxImage.Error);
            showingError = false;
        };

        base.OnStartup(e);
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
        Start(window, state);
    }

    // async void agar exception saat memuat diteruskan ke DispatcherUnhandledException, bukan hilang.
    private static async void Start(MainWindow window, Task<AppState> state) => await window.StartAsync(state);

    protected override void OnExit(ExitEventArgs e)
    {
        State.SimpanSekarang();
        base.OnExit(e);
    }
}
