using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class ComparePage : PageBase
{
    private bool _refreshing;
    private bool _syncingWeights;

    public ComparePage() => InitializeComponent();

    protected override void Refresh()
    {
        var ranked = S.Peringkat();
        var selected = S.Terpilih();
        _refreshing = true;
        bool hasWall = S.AdaProyek && S.Aktif.AdaKomponen(JenisKomponen.Dinding);
        bool hasRoof = S.AdaProyek && S.Aktif.AdaKomponen(JenisKomponen.Atap);
        WallTab.IsEnabled = hasWall;
        WallTab.Visibility = hasWall ? Visibility.Visible : Visibility.Collapsed;
        RoofTab.IsEnabled = hasRoof;
        RoofTab.Visibility = hasRoof ? Visibility.Visible : Visibility.Collapsed;

        WallTab.IsChecked = S.Jenis == JenisKomponen.Dinding;
        RoofTab.IsChecked = S.Jenis == JenisKomponen.Atap;
        Fill(SelectA, ranked, selected.ElementAtOrDefault(0));
        Fill(SelectB, ranked, selected.ElementAtOrDefault(1));

        SyncWeightControls();

        _refreshing = false;
        DataContext = new CompareData(S, ranked, selected);
    }

    private void SyncWeightControls()
    {
        if (S.Aktif is null) return;
        _syncingWeights = true;
        try
        {
            bool kustom = S.Aktif.GunakanBobotKustom;
            MvpWeightRadio.IsChecked = !kustom;
            V2WeightRadio.IsChecked = kustom;

            MvpWeightInfoPanel.Visibility = !kustom ? Visibility.Visible : Visibility.Collapsed;
            V2WeightControlPanel.Visibility = kustom ? Visibility.Visible : Visibility.Collapsed;

            var bobot = S.Aktif.Bobot;
            ThermalWeightBox.Text = (bobot.Termal * 100).ToString("G15", CultureInfo.InvariantCulture);
            MoistureWeightBox.Text = (bobot.Lembap * 100).ToString("G15", CultureInfo.InvariantCulture);
            CorrosionWeightBox.Text = (bobot.Korosi * 100).ToString("G15", CultureInfo.InvariantCulture);
            LifespanWeightBox.Text = (bobot.Usia * 100).ToString("G15", CultureInfo.InvariantCulture);

            WeightValidationMessage.Text = "";
            UpdateWeightSumIndicator();

            bool readOnly = S.ModeBacaSaja || S.ButuhPemulihan;
            MvpWeightRadio.IsEnabled = !readOnly;
            V2WeightRadio.IsEnabled = !readOnly;
            ThermalWeightBox.IsEnabled = !readOnly;
            MoistureWeightBox.IsEnabled = !readOnly;
            CorrosionWeightBox.IsEnabled = !readOnly;
            LifespanWeightBox.IsEnabled = !readOnly;
            ApplyWeightsBtn.IsEnabled = !readOnly;
            ResetWeightsBtn.IsEnabled = !readOnly;
            WeightReadOnlyBanner.Visibility = readOnly ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _syncingWeights = false;
        }
    }

    private void WeightMode_Click(object sender, RoutedEventArgs e)
    {
        if (_refreshing || _syncingWeights || S.ModeBacaSaja) return;
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            bool isCustom = tag == "v2";
            S.AturModeBobot(isCustom);
            Shell.Render();
        }
    }

    private void WeightInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_refreshing || _syncingWeights) return;
        UpdateWeightSumIndicator();
    }

    private void UpdateWeightSumIndicator()
    {
        if (TryParseWeights(out double t, out double l, out double k, out double u, out _))
        {
            double sum = t + l + k + u;
            if (Math.Abs(sum - 100.0) < 1e-9)
            {
                WeightTotalIndicator.Text = "Total: 100%";
                WeightTotalIndicator.Foreground = Palette.Green;
            }
            else
            {
                WeightTotalIndicator.Text = $"Total: {sum:0.#}%";
                WeightTotalIndicator.Foreground = Palette.Red;
            }
        }
        else
        {
            WeightTotalIndicator.Text = "Total: -";
            WeightTotalIndicator.Foreground = Palette.Red;
        }
    }

    private bool TryParseWeights(out double termal, out double lembap, out double korosi, out double usia, out string? error)
    {
        termal = lembap = korosi = usia = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(ThermalWeightBox.Text) ||
            string.IsNullOrWhiteSpace(MoistureWeightBox.Text) ||
            string.IsNullOrWhiteSpace(CorrosionWeightBox.Text) ||
            string.IsNullOrWhiteSpace(LifespanWeightBox.Text))
        {
            error = "Seluruh persentase bobot harus diisi.";
            return false;
        }

        static bool ParsePct(string text, out double val)
        {
            text = text.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out val) && double.IsFinite(val);
        }

        if (!ParsePct(ThermalWeightBox.Text, out termal) ||
            !ParsePct(MoistureWeightBox.Text, out lembap) ||
            !ParsePct(CorrosionWeightBox.Text, out korosi) ||
            !ParsePct(LifespanWeightBox.Text, out usia))
        {
            error = "Persentase bobot harus berupa angka valid non-negatif.";
            return false;
        }

        if (termal < 0 || lembap < 0 || korosi < 0 || usia < 0)
        {
            error = "Persentase bobot tidak boleh bernilai negatif.";
            return false;
        }

        return true;
    }

    private void ApplyWeights_Click(object sender, RoutedEventArgs e)
    {
        if (S.ModeBacaSaja) return;

        if (!TryParseWeights(out double t, out double l, out double k, out double u, out string? error))
        {
            WeightValidationMessage.Text = error ?? "Nilai bobot tidak valid.";
            return;
        }

        string? err = S.AturBobotPersentase(t, l, k, u);
        if (err is not null)
        {
            WeightValidationMessage.Text = err;
            return;
        }

        WeightValidationMessage.Text = "";
        Shell.Render();
    }

    private void ResetWeights_Click(object sender, RoutedEventArgs e)
    {
        if (S.ModeBacaSaja) return;
        S.ResetBobotAktif();
        Shell.Render();
    }

    private void Fill(ComboBox box, Evaluasi[] ranked, ItemKatalog? selected)
    {
        box.Items.Clear();
        foreach (var e in ranked)
        {
            var item = S.Item(e.Bahan);
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
        Description = s.Iklim.SudahDiperbarui
            ? "Tinjau kesesuaian iklim dan emisi awal secara berdampingan."
            : "Peringkat memakai estimasi iklim. Perbarui profil iklim di halaman proyek.";
        Cards = selected.ConvertAll(item => new CompareCard(item, s.Nilai(item)));
        AreaLabel = "Luas " + Format.Angka(s.Luas) + " m²";
        Rows = ranked.Select((e, i) => new RankRow(s.Item(e.Bahan), e, i, ranked.Length)).ToList();
    }

    public List<CompareCard> Cards { get; }
    public string Description { get; }
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
    public double BarWidth { get; } = 65 * Math.Clamp(e.SkorKesesuaian, 0, 100) / 100;
    public string Emission { get; } = Format.Angka(e.EmisiAwal) + " kgCO₂e";
    public TagData Tag { get; } = TagData.Kesesuaian(e.SkorKesesuaian);
    public string Reason { get; } = e.Alasan;
    public Thickness RowBorder { get; } = new(0, 0, 0, index == count - 1 ? 0 : 1);
}
