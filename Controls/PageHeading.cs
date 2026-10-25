using System.Windows;
using System.Windows.Controls;

namespace Wastu.Controls;

// .heading: judul halaman, deskripsi, dan tindakan opsional di kanan (Content).
public sealed class PageHeading : ContentControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(PageHeading), new FrameworkPropertyMetadata(""));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(PageHeading), new FrameworkPropertyMetadata(""));

    static PageHeading() =>
        FocusableProperty.OverrideMetadata(typeof(PageHeading), new FrameworkPropertyMetadata(false));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
}
