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

    protected override void Refresh()
    {
        _refreshing = true;
        bool hasWall = S.AdaProyek && S.Aktif.AdaKomponen(JenisKomponen.Dinding);
        bool hasRoof = S.AdaProyek && S.Aktif.AdaKomponen(JenisKomponen.Atap);
        WallTab.IsEnabled = hasWall;
        WallTab.Visibility = hasWall ? Visibility.Visible : Visibility.Collapsed;
        RoofTab.IsEnabled = hasRoof;
        RoofTab.Visibility = hasRoof ? Visibility.Visible : Visibility.Collapsed;

        WallTab.IsChecked = S.Jenis == JenisKomponen.Dinding;
        RoofTab.IsChecked = S.Jenis == JenisKomponen.Atap;
        SearchBox.Text = S.Cari;
        WeightModeBadge.Text = S.BobotAktif != Domain.KriteriaBobot.Default ? "Bobot kustom aktif" : "Bobot sama (25%)";
        _refreshing = false;

        var area = S.Luas;
        _cards = [];
        try
        {
            var evaluasiList = S.Peringkat();
            foreach (var e in evaluasiList)
            {
                try
                {
                    if (e?.Bahan is null) continue;
                    var item = S.Item(e.Bahan);
                    if (item is null) continue;
                    _cards.Add(new MaterialCard(item, e, area));
                }
                catch
                {
                    // Isolasi baris bahan tidak valid secara anggun tanpa menjatuhkan seluruh katalog
                }
            }
        }
        catch
        {
            // Jika kalkulasi evaluasi gagal, tampilkan kartu bahan dari katalog secara aman
            try
            {
                _cards = S.KatalogAktif
                    .Where(item => item.Jenis == S.Jenis)
                    .Select(item => new MaterialCard(item, null, area))
                    .ToList();
            }
            catch
            {
                // Diamkan fallback terakhir
            }
        }

        ApplyFilter();
    }

    // go() mengosongkan pencarian setiap pindah halaman.
    protected override void OnRevisit()
    {
        if (SearchBox.Text == S.Cari) return;
        _refreshing = true;
        SearchBox.Text = S.Cari;
        _refreshing = false;
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

    private void Detail_Click(object sender, RoutedEventArgs e)
    {
        var code = TagOf(sender);
        var card = _cards.Find(c => c.Code == code);
        if (card is null) return;

        TampilkanDetail(card.Item);
    }

    private void TampilkanDetail(ItemKatalog item)
    {
        DetailName.Text = item.Bahan.Nama;
        DetailKindBadge.Text = item.Jenis == JenisKomponen.Atap ? "ATAP" : "DINDING";
        DetailAssembly.Text = string.IsNullOrWhiteSpace(item.Bahan.SusunanKetebalan)
            ? "Susunan standar material tunggal"
            : item.Bahan.SusunanKetebalan;

        DetailThermal.Text = $"{Format.Angka(item.Bahan.KonduktivitasTermal)} W/(m·K)";
        DetailMoisture.Text = $"{item.Bahan.KetahananLembap} / 5";
        DetailCorrosion.Text = $"{item.Bahan.KetahananKorosi} / 5";
        DetailLifespan.Text = $"{Format.Angka(item.Bahan.UsiaPakaiTahun)} tahun";
        DetailEmission.Text = $"{Format.Angka(item.Bahan.FaktorEmisi)} kgCO₂e/m²";

        var sumberList = (item.Bahan.SumberProperti ?? [])
            .Select(s => new SumberItemViewData(s))
            .ToList();

        DetailSources.ItemsSource = sumberList;
        DetailSourcesEmpty.Visibility = sumberList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        DetailModal.Visibility = Visibility.Visible;
    }

    private void CloseDetail_Click(object sender, RoutedEventArgs e)
    {
        DetailModal.Visibility = Visibility.Collapsed;
    }

    private async void RefreshCatalog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshBtn.IsEnabled = false;
            RefreshBtn.Content = "Menyegarkan...";

            await S.SegarkanKatalogAsync();
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Gagal menyegarkan katalog: " + ex.Message, "Segarkan Katalog", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            RefreshBtn.IsEnabled = true;
            RefreshBtn.Content = "Segarkan Katalog";
        }
    }
}

public sealed class MaterialCard : ViewData
{
    public MaterialCard(ItemKatalog item, Evaluasi? e, double area)
    {
        Item = item;
        Code = item.Kode;
        Image = ImageCache.Cover(item.Gambar);
        Kind = item.Jenis == JenisKomponen.Atap ? "MATERIAL ATAP" : "MATERIAL DINDING";
        Name = item.Bahan.Nama;
        Tag = e is not null ? TagData.Kesesuaian(e.SkorKesesuaian) : new TagData("Katalog", Palette.TagBg, Palette.TagFg);
        Reason = e is not null
            ? (e.Alasan.Split(';')[0].TrimEnd('.') + ".")
            : "Bahan siap dievaluasi untuk proyek Anda.";
        Source = item.Bahan.DataSource;
        Score = e is not null ? (Format.Angka(e.SkorKesesuaian) + "/100") : "-";
        ScoreBrush = (e is not null && e.SkorKesesuaian < 55) ? Palette.Red : Palette.Score;
        Emission = e is not null ? Format.Angka(e.EmisiAwal) : Format.Angka(item.Bahan.FaktorEmisi * Math.Max(0, area));
        EmissionLabel = $"kgCO₂e untuk {Format.Angka(area)} m²";
    }

    public ItemKatalog Item { get; }
    public string Code { get; }
    public Brush Image { get; }
    public string Kind { get; }
    public string Name { get; }
    public TagData Tag { get; }
    public string Reason { get; }
    public string Source { get; }
    public string Score { get; }
    public Brush ScoreBrush { get; }
    public string Emission { get; }
    public string EmissionLabel { get; }
}

public sealed class SumberItemViewData
{
    public SumberItemViewData(Domain.SumberPropertiBahan s)
    {
        Properti = FormatNamaProperti(s.Properti);
        Status = s.Status == Domain.StatusVerifikasiProperti.Terverifikasi ? "Terverifikasi" : "Perkiraan";
        StatusBrush = s.Status == Domain.StatusVerifikasiProperti.Terverifikasi ? Palette.Score : Palette.TagGoldFg;
        Referensi = s.Referensi + (string.IsNullOrWhiteSpace(s.Versi) ? "" : $" (Versi {s.Versi})");
        Tautan = s.Tautan;
        TautanVisibility = string.IsNullOrWhiteSpace(s.Tautan) ? Visibility.Collapsed : Visibility.Visible;
        Catatan = s.CatatanKonversi ?? "Tidak ada catatan konversi tambahan.";
    }

    public string Properti { get; }
    public string Status { get; }
    public Brush StatusBrush { get; }
    public string Referensi { get; }
    public string? Tautan { get; }
    public Visibility TautanVisibility { get; }
    public string Catatan { get; }

    private static string FormatNamaProperti(string raw) => raw switch
    {
        "konduktivitas_termal" => "Konduktivitas Termal",
        "ketahanan_lembap" => "Ketahanan Lembap",
        "ketahanan_korosi" => "Ketahanan Korosi",
        "usia_pakai_nominal" => "Usia Pakai Nominal",
        "faktor_emisi" => "Faktor Emisi Embodied",
        _ => raw
    };
}
