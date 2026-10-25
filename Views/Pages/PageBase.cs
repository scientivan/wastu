using System.Windows;
using System.Windows.Controls;
using Wastu.Services;

namespace Wastu.Views.Pages;

public abstract class PageBase : UserControl
{
    public ShellView Shell { get; set; } = null!;

    protected static AppState S => App.State;

    // Membangun ulang data tampilan dari state lalu memasangnya sebagai DataContext.
    public abstract void Refresh();

    protected void Go_Click(object sender, RoutedEventArgs e) => Shell.Go(TagOf(sender));

    // Padanan atribut data-* pada tombol referensi.
    protected static string TagOf(object sender) => (string)((FrameworkElement)sender).Tag;
}
