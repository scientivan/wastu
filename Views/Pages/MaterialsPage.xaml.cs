using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class MaterialsPage : PageBase
{
    private List<MaterialCard> _cards = [];
    private bool _refreshing;

    public MaterialsPage() => InitializeComponent();

    public override void Refresh()
    {
        _refreshing = true;
        WallTab.IsChecked = S.Jenis == JenisKomponen.Dinding;
        RoofTab.IsChecked = S.Jenis == JenisKomponen.Atap;
        SearchBox.Text = S.Cari;
        _refreshing = false;

        var area = S.Luas;
        _cards = S.Peringkat().Select(e => new MaterialCard(Katalog.Dari(e.Bahan), e, area)).ToList();
        ApplyFilter();
    }

    // Pencarian hanya menyaring kartu yang sudah dibuat; gambar dan evaluasi tidak dihitung ulang.
    private void ApplyFilter()
    {
        var query = S.Cari;
        var shown = query.Length == 0
            ? _cards
            : _cards.FindAll(c => c.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        Cards.ItemsSource = shown;
        CountText.Text = shown.Count + " bahan";
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_refreshing) return;
        S.Cari = SearchBox.Text;
        ApplyFilter();
    }

    private void Kind_Click(object sender, RoutedEventArgs e)
    {
        S.AturJenis(Enum.Parse<JenisKomponen>(TagOf(sender)));
        Shell.Render();
    }

    private void Pick_Click(object sender, RoutedEventArgs e)
    {
        S.PilihUntukBanding(TagOf(sender));
        Shell.Go("compare");
    }
}

public sealed class MaterialCard : ViewData
{
    public MaterialCard(ItemKatalog item, Evaluasi e, double area)
    {
        Code = item.Kode;
        Image = ImageCache.Cover(item.Gambar);
        Kind = item.Jenis == JenisKomponen.Atap ? "MATERIAL ATAP" : "MATERIAL DINDING";
        Name = item.Bahan.Nama;
        Tag = TagData.Kesesuaian(e.SkorKesesuaian);
        Reason = e.Alasan.Split('.')[0] + ".";
        Score = Format.Angka(e.SkorKesesuaian) + "/100";
        ScoreBrush = e.SkorKesesuaian < 55 ? Palette.Red : Palette.Score;
        Emission = Format.Angka(e.EmisiAwal);
        EmissionLabel = $"kgCO₂e untuk {Format.Angka(area)} m²";
    }

    public string Code { get; }
    public Brush Image { get; }
    public string Kind { get; }
    public string Name { get; }
    public TagData Tag { get; }
    public string Reason { get; }
    public string Score { get; }
    public Brush ScoreBrush { get; }
    public string Emission { get; }
    public string EmissionLabel { get; }
}
