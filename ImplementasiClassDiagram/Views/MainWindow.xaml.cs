using System.Globalization;
using System.Windows;
using ImplementasiClassDiagram.Models;

namespace ImplementasiClassDiagram.Views;

public partial class MainWindow : Window
{
    private Bangunan? _bangunan;

    public MainWindow()
    {
        InitializeComponent();
        JenisKomponenComboBox.ItemsSource = Enum.GetValues<JenisKomponen>();
        JenisKomponenComboBox.SelectedIndex = 0;
    }

    private void SimpanBangunan_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NamaBangunanTextBox.Text)
            || string.IsNullOrWhiteSpace(KotaTextBox.Text))
        {
            MessageBox.Show(
                "Nama bangunan dan kota wajib diisi.",
                "Validasi",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _bangunan = new Bangunan
        {
            Nama = NamaBangunanTextBox.Text.Trim(),
            Kota = KotaTextBox.Text.Trim()
        };

        KomponenListBox.Items.Clear();
        StatusTextBlock.Text = $"Bangunan '{_bangunan.Nama}' berhasil disimpan.";
    }

    private void TambahKomponen_Click(object sender, RoutedEventArgs e)
    {
        if (_bangunan is null)
        {
            MessageBox.Show(
                "Simpan data bangunan terlebih dahulu.",
                "Informasi",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (!double.TryParse(
                LuasTextBox.Text,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var luas)
            || luas <= 0)
        {
            MessageBox.Show(
                "Luas komponen harus berupa angka lebih dari nol.",
                "Validasi",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var komponen = new Komponen
        {
            Jenis = (JenisKomponen)JenisKomponenComboBox.SelectedItem,
            Luas = luas
        };

        _bangunan.TambahKomponen(komponen);
        KomponenListBox.Items.Add($"{komponen.Jenis} — {komponen.Luas:N2} m²");
        StatusTextBlock.Text = $"Total komponen: {_bangunan.Komponen.Count}";
    }
}
