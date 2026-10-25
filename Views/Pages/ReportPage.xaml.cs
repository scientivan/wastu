using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class ReportPage : PageBase
{
    public ReportPage() => InitializeComponent();

    public override void Refresh() => DataContext = new ReportData(S);

    // window.print() + @media print: hanya isi laporan, A4 dengan margin 14 mm.
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        const double margin = 14 / 25.4 * 96;
        var pageWidth = dialog.PrintableAreaWidth;
        var pageHeight = dialog.PrintableAreaHeight;
        var width = pageWidth - margin * 2;
        var height = pageHeight - margin * 2;

        var print = new ReportPage { DataContext = DataContext };
        print.Heading.Visibility = Visibility.Collapsed;
        print.Report.BorderThickness = new Thickness(0);
        print.Report.Padding = new Thickness(0);
        print.Measure(new Size(width, double.PositiveInfinity));
        print.Arrange(new Rect(0, 0, width, print.DesiredSize.Height));
        print.UpdateLayout();

        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);
        var total = print.ActualHeight;
        for (var top = 0.0; top < total; top += height)
        {
            var slice = Math.Min(height, total - top);
            var area = new Rectangle
            {
                Width = width,
                Height = slice,
                Fill = new VisualBrush(print)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    ViewboxUnits = BrushMappingMode.Absolute,
                    Viewbox = new Rect(0, top, width, slice)
                }
            };
            var page = new FixedPage { Width = pageWidth, Height = pageHeight, Background = Brushes.White };
            FixedPage.SetLeft(area, margin);
            FixedPage.SetTop(area, margin);
            page.Children.Add(area);
            document.Pages.Add(new PageContent { Child = page });
        }

        dialog.PrintDocument(document.DocumentPaginator, "Laporan Wastu - " + S.Aktif.Nama);
        S.Aktif.TambahLaporan(new Laporan { TanggalDibuat = DateTime.Now, Format = "pdf" });
    }
}

public sealed class ReportData : ViewData
{
    public ReportData(AppState s)
    {
        var p = s.Aktif;
        var climate = s.Iklim;
        Subtitle = Format.Tanggal(DateTime.Today) + " " + p.Nama;
        Location = $"{p.Kota} ({Format.Angka(p.Latitude)}, {Format.Angka(p.Longitude)})";
        Areas = $"Dinding {Format.Angka(p.LuasKomponen(JenisKomponen.Dinding))} m² Atap {Format.Angka(p.LuasKomponen(JenisKomponen.Atap))} m²";
        Climate = climate.SudahDiperbarui
            ? $"{Format.Angka(climate.CurahHujanTahunan)} mm hujan per tahun, {Format.Angka(climate.KelembapanRata)}% kelembapan"
            : "Profil iklim belum diperbarui.";
        ClimateDetail = $"Suhu {Format.Angka(climate.SuhuRata)}°C. Jarak pesisir {Format.Angka(climate.JarakPesisirKm)} km";
        HasClimate = climate.SudahDiperbarui ? Visibility.Visible : Visibility.Collapsed;

        RankingTitle = "Peringkat material " + (s.Jenis == JenisKomponen.Dinding ? "dinding" : "atap");
        var top = s.Peringkat().Take(5).ToArray();
        Rows = top.Select((e, i) => new ReportRow(e, i == top.Length - 1)).ToList();

        SimulationTitle = $"Simulasi {s.Horizon} tahun";
        Simulations = s.Terpilih().ConvertAll(item => new ReportSimulation(item.Bahan.Nama, s.Simulasikan(item).SimulasiEmisi!));

        Locked = p.Pekerjaan.Where(j => j.TanggalDikunci is not null)
            .Select(j => $"{j.Nama} pada {Format.Iso(j.TanggalDikunci!.Value)}").ToList();
        NoLocked = Locked.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public string Subtitle { get; }
    public string Location { get; }
    public string Areas { get; }
    public string Climate { get; }
    public string ClimateDetail { get; }
    public Visibility HasClimate { get; }
    public string RankingTitle { get; }
    public List<ReportRow> Rows { get; }
    public string SimulationTitle { get; }
    public List<ReportSimulation> Simulations { get; }
    public List<string> Locked { get; }
    public Visibility NoLocked { get; }
}

public sealed class ReportRow(Evaluasi e, bool last) : ViewData
{
    public string Name { get; } = e.Bahan.Nama;
    public string Score { get; } = Format.Angka(e.SkorKesesuaian) + "/100";
    public string Emission { get; } = Format.Angka(e.EmisiAwal);
    public string Reason { get; } = e.Alasan;
    public Thickness RowBorder { get; } = new(0, 0, 0, last ? 0 : 1);
}

public sealed class ReportSimulation(string name, SimulasiEmisi sim) : ViewData
{
    public string Name { get; } = name;
    public string Total { get; } = Format.Angka(sim.EmisiKumulatif) + " kgCO₂e";
    public string Detail { get; } = $"{sim.JumlahPenggantian} penggantian, {Format.Angka(sim.UsiaPakaiEfektif)} tahun usia efektif";
}
