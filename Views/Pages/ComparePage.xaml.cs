using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class ComparePage : PageBase
{
    private bool _refreshing;

    public ComparePage() => InitializeComponent();

    protected override void Refresh()
    {
        var ranked = S.Peringkat();
        var selected = S.Terpilih();
        _refreshing = true;
        WallTab.IsChecked = S.Jenis == JenisKomponen.Dinding;
        RoofTab.IsChecked = S.Jenis == JenisKomponen.Atap;
        Fill(SelectA, ranked, selected.ElementAtOrDefault(0));
        Fill(SelectB, ranked, selected.ElementAtOrDefault(1));
        _refreshing = false;
        DataContext = new CompareData(S, ranked, selected);
    }

    private static void Fill(ComboBox box, Evaluasi[] ranked, ItemKatalog? selected)
    {
        box.Items.Clear();
        foreach (var e in ranked)
        {
            var item = Katalog.Dari(e.Bahan);
            var option = new ComboBoxItem { Content = item.Bahan.Nama, Tag = item.Kode };
            box.Items.Add(option);
            if (item == selected) box.SelectedItem = option;
        }
        // <select> tanpa opsi terpilih menampilkan opsi pertama.
        if (box.SelectedIndex < 0 && box.Items.Count > 0) box.SelectedIndex = 0;
    }

    private void Kind_Click(object sender, RoutedEventArgs e)
    {
        S.AturJenis(Enum.Parse<JenisKomponen>(TagOf(sender)));
        Shell.Render();
    }

    private void Select_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || ((ComboBox)sender).SelectedItem is not ComboBoxItem { Tag: string code }) return;
        var index = int.Parse(TagOf(sender));
        var selected = S.Dipilih.ToList();
        while (selected.Count <= index) selected.Add("");
        selected[index] = code;
        S.Dipilih = selected.ToArray();
        Shell.Render();
    }
}

public sealed class CompareData : ViewData
{
    public CompareData(AppState s, Evaluasi[] ranked, List<ItemKatalog> selected)
    {
        Cards = selected.ConvertAll(item => new CompareCard(item, s.Nilai(item)));
        AreaLabel = "Luas " + Format.Angka(s.Luas) + " m²";
        Rows = ranked.Select((e, i) => new RankRow(Katalog.Dari(e.Bahan), e, i, ranked.Length)).ToList();
    }

    public List<CompareCard> Cards { get; }
    public string AreaLabel { get; }
    public List<RankRow> Rows { get; }
}

public sealed class CompareCard(ItemKatalog item, Evaluasi e) : ViewData
{
    public Brush Image { get; } = ImageCache.Cover(item.Gambar);
    public string Name { get; } = item.Bahan.Nama;
    public TagData Tag { get; } = TagData.Kesesuaian(e.SkorKesesuaian);
    public string Score { get; } = Format.Angka(e.SkorKesesuaian);
    public string Emission { get; } = Format.Angka(e.EmisiAwal) + " ";
    public string Reason { get; } = e.Alasan;
    public string Factor { get; } = $"Faktor emisi {item.Bahan.FaktorEmisi.ToString(System.Globalization.CultureInfo.InvariantCulture)} kgCO₂e/m²";
}

public sealed class RankRow(ItemKatalog item, Evaluasi e, int index, int count) : ViewData
{
    public Brush Thumb { get; } = ImageCache.Thumb(item.Gambar);
    public string Name { get; } = $"{index + 1}. {item.Bahan.Nama}";
    public string Score { get; } = Format.Angka(e.SkorKesesuaian);
    public Brush ScoreBrush { get; } = e.SkorKesesuaian < 55 ? Palette.Red : Palette.Score;
    public double BarWidth { get; } = 65 * e.SkorKesesuaian / 100;
    public string Emission { get; } = Format.Angka(e.EmisiAwal) + " kgCO₂e";
    public TagData Tag { get; } = TagData.Kesesuaian(e.SkorKesesuaian);
    public Thickness RowBorder { get; } = new(0, 0, 0, index == count - 1 ? 0 : 1);
}
