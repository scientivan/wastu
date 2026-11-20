using System.Windows;
using System.Windows.Media;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class ProjectPage : PageBase
{
    public ProjectPage()
    {
        InitializeComponent();
        Map.Bind(LatBox, LonBox);
    }

    protected override void Refresh()
    {
        var p = S.Aktif;
        DataContext = new ProjectData(S);
        NameBox.Text = p.Nama;
        CityBox.Items.Clear();
        foreach (var city in Katalog.NamaKota.Append(p.Kota).Distinct())
            CityBox.Items.Add(city);
        CityBox.SelectedItem = p.Kota;
        LatBox.Text = Format.Masukan(p.Latitude);
        LonBox.Text = Format.Masukan(p.Longitude);
        WallBox.Text = Format.Masukan(p.LuasKomponen(JenisKomponen.Dinding));
        RoofBox.Text = Format.Masukan(p.LuasKomponen(JenisKomponen.Atap));
        CoastalBox.Text = Format.Masukan(S.Iklim.JarakPesisirKm);
        CoastalBox.IsReadOnly = S.PostgreSql;
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
        // Validasi bawaan form (required, type=number) sebelum handler submit referensi.
        if (new[] { NameBox, LatBox, LonBox, WallBox, RoofBox, CoastalBox }.FirstOrDefault(f => f.Text.Length == 0) is { } empty)
        {
            empty.Focus();
            Shell.Toast("Harap isi bidang ini.");
            return;
        }
        if (!Format.CobaBaca(LatBox.Text, out var lat) | !Format.CobaBaca(LonBox.Text, out var lon) |
            !Format.CobaBaca(WallBox.Text, out var wall) | !Format.CobaBaca(RoofBox.Text, out var roof) |
            !Format.CobaBaca(CoastalBox.Text, out var coastal))
        {
            Shell.Toast("Masukkan angka.");
            return;
        }

        if (lat < -11 || lat > 6 || lon < 94 || lon > 142) { Shell.Toast("Koordinat di luar bounding box wilayah Indonesia."); return; }
        if (NameBox.Text.Trim().Length == 0) { Shell.Toast("Masukkan nama proyek."); return; }
        if (coastal < 0) { Shell.Toast("Jarak pesisir tidak boleh negatif."); return; }
        if (wall <= 0 || roof <= 0) { Shell.Toast("Luas bidang harus lebih dari nol."); return; }

        var city = (string)CityBox.SelectedItem;
        await Shell.RunProjectChangeAsync(async () =>
        {
            var pesan = await S.UbahProyekAsync(NameBox.Text.Trim(), city, lat, lon, wall, roof, coastal);
            Shell.Render();
            Shell.Toast(pesan is null ? "Proyek disimpan." : $"Proyek disimpan. {pesan}");
        });
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(Window.GetWindow(this)!, "Hapus proyek aktif beserta jadwal terkait?", "Wastu",
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
        Coastal = Format.Angka(climate.JarakPesisirKm) + " km";
        Caption = climate.Sumber + ". Diperbarui " + climate.DiperbaruiPada;
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
    public string Caption { get; }
}

public sealed class ProjectPill(int id, string label, bool active) : ViewData
{
    public int Id { get; } = id;
    public string Label { get; } = label;
    public Brush Background { get; } = active ? Palette.PillActiveBg : Palette.White;
    public Brush Border { get; } = active ? Palette.PillActiveBorder : Palette.Line;
}
