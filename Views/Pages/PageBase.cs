using System.Windows;
using System.Windows.Controls;
using Wastu.Services;

namespace Wastu.Views.Pages;

public abstract class PageBase : UserControl
{
    public ShellView Shell { get; set; } = null!;

    private int _revision = -1;
    private (bool, bool) _tier;

    protected static AppState S => App.State;

    // Kunjungan ulang tanpa perubahan state (dan breakpoint yang sama) memakai pohon visual yang ada.
    public void Show(bool force)
    {
        var tier = (Viewport.Width <= 1200, Viewport.Width >= 1700);
        if (!force && _revision == S.Revisi && tier == _tier)
        {
            OnRevisit();
            return;
        }
        _revision = S.Revisi;
        _tier = tier;
        Refresh();
    }

    // Membangun ulang data tampilan dari state lalu memasangnya sebagai DataContext.
    protected abstract void Refresh();

    protected virtual void OnRevisit() { }

    protected void Go_Click(object sender, RoutedEventArgs e) => Shell.Go(TagOf(sender));

    // Padanan atribut data-* pada tombol referensi.
    protected static string TagOf(object sender) => (string)((FrameworkElement)sender).Tag;
}
