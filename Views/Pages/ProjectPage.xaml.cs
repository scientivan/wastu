using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wastu.Domain;
using Wastu.Models;
using Wastu.Services;
using JenisKomponen = Wastu.Models.JenisKomponen;

namespace Wastu.Views.Pages;

public partial class ProjectPage : PageBase
{
    public ProjectPage()
    {
        InitializeComponent();
        Map.Bind(LatBox, LonBox);
        LatBox.TextChanged += (_, _) => HitungPesisirOtomatis();
        LonBox.TextChanged += (_, _) => HitungPesisirOtomatis();
    }

    protected override void Refresh()
    {
        var p = S.Aktif;
        DataContext = new ProjectData(S);
        NameBox.Text = p.Nama;

        CityBox.SelectionChanged -= City_Changed;
        CityBox.Items.Clear();
        foreach (var city in Katalog.NamaKota.Append(p.Kota).Distinct())
            CityBox.Items.Add(city);
        CityBox.SelectedItem = p.Kota;
        CityBox.SelectionChanged += City_Changed;

        LatBox.Text = Format.Masukan(p.Latitude);
        LonBox.Text = Format.Masukan(p.Longitude);

        WallCheck.IsChecked = p.AdaKomponen(JenisKomponen.Dinding);
        RoofCheck.IsChecked = p.AdaKomponen(JenisKomponen.Atap);
        WallBox.IsEnabled = WallCheck.IsChecked == true;
        RoofBox.IsEnabled = RoofCheck.IsChecked == true;
        WallBox.Text = p.AdaKomponen(JenisKomponen.Dinding) ? Format.Masukan(p.LuasKomponen(JenisKomponen.Dinding)) : "";
        RoofBox.Text = p.AdaKomponen(JenisKomponen.Atap) ? Format.Masukan(p.LuasKomponen(JenisKomponen.Atap)) : "";

        CoastalOverrideCheck.IsChecked = p.ProfilIklim?.CatatanOverridePesisir is not null;
        CoastalBox.IsReadOnly = CoastalOverrideCheck.IsChecked != true;
        CoastalBox.Text = Format.Masukan(p.ProfilIklim?.JarakPesisirKm ?? S.Iklim.JarakPesisirKm);
    }

    private void City_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CityBox.SelectedItem is string cityName && Katalog.Kota.TryGetValue(cityName, out var kota))
        {
            LatBox.Text = Format.Masukan(kota.Lintang);
            LonBox.Text = Format.Masukan(kota.Bujur);
            HitungPesisirOtomatis();
        }
    }

    private void ComponentCheck_Click(object sender, RoutedEventArgs e)
    {
        WallBox.IsEnabled = WallCheck.IsChecked == true;
        RoofBox.IsEnabled = RoofCheck.IsChecked == true;
    }

    private void CoastalOverride_Click(object sender, RoutedEventArgs e)
    {
        bool isOverride = CoastalOverrideCheck.IsChecked == true;
        CoastalBox.IsReadOnly = !isOverride;
        if (!isOverride)
        {
            HitungPesisirOtomatis();
        }
    }

    private void HitungPesisirOtomatis()
    {
        if (CoastalOverrideCheck.IsChecked == true) return;
        if (Format.CobaBaca(LatBox.Text, out var lat) && Format.CobaBaca(LonBox.Text, out var lon))
        {
            var dist = JarakPesisir.CobaHitungKm(lat, lon);
            if (dist.HasValue)
            {
                CoastalBox.Text = Format.Masukan(Math.Round(dist.Value, 1));
            }
        }
    }

    private void New_Click(object sender, RoutedEventArgs e) => Shell.NewProject();

    private void Project_Click(object sender, RoutedEventArgs e)
    {
        S.PilihProyek((int)((FrameworkElement)sender).Tag);
        Shell.Render();
    }

    private async void Gps_Click(object sender, RoutedEventArgs e)
    {
        GpsButton.IsEnabled = false;
        GpsLabel.Text = "Mendeteksi lokasi…";
        try
        {
            var posisi = await Lokasi.DeteksiAsync();
            Map.Place(posisi.Lintang, posisi.Bujur, 17);
            HitungPesisirOtomatis();
            Shell.Toast($"Lokasi terdeteksi (akurasi ±{Format.Angka(Math.Round(posisi.AkurasiMeter))} m). Simpan proyek untuk menerapkan.");
        }
        catch (InvalidOperationException ex)
        {
            Shell.Toast(ex.Message);
        }
        finally
        {
            GpsButton.IsEnabled = true;
            GpsLabel.Text = "Gunakan lokasi saya";
        }
    }

    private async void Climate_Click(object sender, RoutedEventArgs e) => await Shell.RefreshClimateAsync();

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (NameBox.Text.Trim().Length == 0)
        {
            NameBox.Focus();
            Shell.Toast("Masukkan nama proyek.");
            return;
        }

        if (!Format.CobaBaca(LatBox.Text, out var lat) || !Format.CobaBaca(LonBox.Text, out var lon))
        {
            LatBox.Focus();
            Shell.Toast("Masukkan angka koordinat.");
            return;
        }

        if (!ValidasiKoordinat.CobaValidasi(lat, lon, out var pesanKoordinat))
        {
            LatBox.Focus();
            Shell.Toast(pesanKoordinat!);
            return;
        }

        if (WallCheck.IsChecked != true && RoofCheck.IsChecked != true)
        {
            Shell.Toast("Pilih minimal satu komponen (dinding atau atap).");
            return;
        }

        var daftarKomponen = new List<(JenisKomponen Jenis, double Luas)>();
        if (WallCheck.IsChecked == true)
        {
            if (!Format.CobaBaca(WallBox.Text, out var wall) || wall <= 0)
            {
                WallBox.Focus();
                Shell.Toast("Luas dinding harus lebih dari nol.");
                return;
            }
            daftarKomponen.Add((JenisKomponen.Dinding, wall));
        }

        if (RoofCheck.IsChecked == true)
        {
            if (!Format.CobaBaca(RoofBox.Text, out var roof) || roof <= 0)
            {
                RoofBox.Focus();
                Shell.Toast("Luas atap harus lebih dari nol.");
                return;
            }
            daftarKomponen.Add((JenisKomponen.Atap, roof));
        }

        double? pesisirManual = null;
        if (CoastalOverrideCheck.IsChecked == true)
        {
            if (!Format.CobaBaca(CoastalBox.Text, out var cVal) || cVal < 0)
            {
                CoastalBox.Focus();
                Shell.Toast("Jarak pesisir tidak boleh negatif.");
                return;
            }
            pesisirManual = cVal;
        }

        var city = (string)CityBox.SelectedItem ?? S.Aktif.Kota;
        await Shell.RunProjectChangeAsync(async () =>
        {
            var pesan = await S.UbahProyekAsync(NameBox.Text.Trim(), city, lat, lon, daftarKomponen, pesisirManual);
            Shell.Render();
            Shell.Toast(pesan is null ? "Proyek disimpan." : $"Proyek disimpan. {pesan}");
        });
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(Window.GetWindow(this)!, "Hapus proyek aktif beserta jadwal terkait??", "Wastu",
                MessageBoxButton.OKCancel, MessageBoxImage.None) != MessageBoxResult.OK)
            return;
        await Shell.RunProjectChangeAsync(async () =>
        {
            await S.HapusProyekAktifAsync();
            if (S.AdaProyek)
                Shell.Render();
            else
            {
                S.Ubah();
                Shell.ProjectsEmptied();
            }
        });
    }
}

public sealed class ProjectData : ViewData
{
    public ProjectData(AppState s)
    {
        var p = s.Aktif;
        var climate = s.Iklim;
        Projects = s.Proyek.ConvertAll(x => new ProjectPill(x.Id, x.Nama + " " + x.Kota, x.Id == p.Id));
        var fetched = climate.SudahDiperbarui;
        EmptyClimate = fetched ? Visibility.Collapsed : Visibility.Visible;
        HasClimate = fetched ? Visibility.Visible : Visibility.Collapsed;
        EmptyText = $"Data iklim {p.Kota} belum diperbarui.";
        ClimateTitle = "Iklim " + p.Kota;
        Rain = Format.Angka(climate.CurahHujanTahunan) + " mm";
        Humidity = Format.Angka(climate.KelembapanRata) + "%";
        Temperature = Format.Angka(climate.SuhuRata) + "°C";
        Coastal = climate.PesisirTersedia ? Format.Angka(climate.JarakPesisirKm) + " km" : "Tidak tersedia";

        CoastalCaption = climate.PesisirTersedia
            ? "Perkiraan jarak ke garis pantai; proksi paparan udara bergaram."
            : "Data pesisir tidak tersedia.";

        ProviderText = "Penyedia: " + (!string.IsNullOrWhiteSpace(climate.Provider) ? climate.Provider : climate.Sumber);
        PeriodText = climate.PeriodeMulai.HasValue && climate.PeriodeSelesai.HasValue
            ? $"Periode data: {climate.PeriodeMulai.Value:yyyy-MM-dd} s/d {climate.PeriodeSelesai.Value:yyyy-MM-dd}"
            : "Periode data: 10 tahun historis";
        TimestampText = climate.DiambilPadaUtc.HasValue
            ? $"Diambil: {Format.WaktuLokal(climate.DiambilPadaUtc.Value.LocalDateTime)} WIB ({climate.DiambilPadaUtc.Value:yyyy-MM-dd HH:mm} UTC)"
            : (climate.DiperbaruiPada is not null ? $"Diperbarui: {climate.DiperbaruiPada}" : "Belum diperbarui");

        if (climate.KualitasCache == KualitasCacheIklim.TidakCocokLokasi)
        {
            ProvenanceBadge = "Lokasi Lama (Perlu Pembaruan)";
            BadgeBg = Palette.TagRedBg;
            BadgeFg = Palette.TagRedFg;
            BadgeBorder = Palette.DayBadBorder;
        }
        else if (climate.StatusPengambilan == StatusPengambilanIklim.GagalMemperbarui)
        {
            ProvenanceBadge = "Gagal Memperbarui";
            BadgeBg = Palette.TagRedBg;
            BadgeFg = Palette.TagRedFg;
            BadgeBorder = Palette.DayBadBorder;
        }
        else
        {
            switch (climate.Provenance)
            {
                case ProvenanceIklim.ApiHistoris:
                    ProvenanceBadge = "API Historis";
                    BadgeBg = Palette.TagBg;
                    BadgeFg = Palette.TagFg;
                    BadgeBorder = Palette.DayGoodBorder;
                    break;
                case ProvenanceIklim.CacheHistoris:
                    ProvenanceBadge = "Cache Historis";
                    BadgeBg = Palette.TagBg;
                    BadgeFg = Palette.TagFg;
                    BadgeBorder = Palette.DayGoodBorder;
                    break;
                case ProvenanceIklim.EstimasiBawaan:
                    ProvenanceBadge = "Estimasi Bawaan";
                    BadgeBg = Palette.TagPaleBg;
                    BadgeFg = Palette.TagPaleFg;
                    BadgeBorder = Palette.Line;
                    break;
                case ProvenanceIklim.Contoh:
                    ProvenanceBadge = "Data Contoh";
                    BadgeBg = Palette.TagGoldBg;
                    BadgeFg = Palette.TagGoldFg;
                    BadgeBorder = Palette.DayWarnBorder;
                    break;
                case ProvenanceIklim.ManualOverride:
                    ProvenanceBadge = "Manual Override";
                    BadgeBg = Palette.TagGoldBg;
                    BadgeFg = Palette.TagGoldFg;
                    BadgeBorder = Palette.DayWarnBorder;
                    break;
                default:
                    ProvenanceBadge = "Legacy (Sumber Tidak Diketahui)";
                    BadgeBg = Palette.TagPaleBg;
                    BadgeFg = Palette.TagPaleFg;
                    BadgeBorder = Palette.Line;
                    break;
            }
        }
    }

    public List<ProjectPill> Projects { get; }
    public Visibility EmptyClimate { get; }
    public Visibility HasClimate { get; }
    public string EmptyText { get; }
    public string ClimateTitle { get; }
    public string Rain { get; }
    public string Humidity { get; }
    public string Temperature { get; }
    public string Coastal { get; }
    public string CoastalCaption { get; }
    public string ProviderText { get; }
    public string PeriodText { get; }
    public string TimestampText { get; }
    public string ProvenanceBadge { get; }
    public Brush BadgeBg { get; }
    public Brush BadgeFg { get; }
    public Brush BadgeBorder { get; }
}

public sealed class ProjectPill(int id, string label, bool active) : ViewData
{
    public int Id { get; } = id;
    public string Label { get; } = label;
    public Brush Background { get; } = active ? Palette.PillActiveBg : Palette.White;
    public Brush Border { get; } = active ? Palette.PillActiveBorder : Palette.Line;
}
