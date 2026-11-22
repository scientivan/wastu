using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wastu.Controls;
using Wastu.Domain.Emissions;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class SimulationPage : PageBase
{
    private bool _refreshing;
    private double _faktorPenuaanA = 0.5;

    public SimulationPage() => InitializeComponent();

    protected override void Refresh()
    {
        _refreshing = true;
        if (KindBox.Items.Count >= 2)
        {
            bool hasWall = S.AdaProyek && S.Aktif.AdaKomponen(JenisKomponen.Dinding);
            bool hasRoof = S.AdaProyek && S.Aktif.AdaKomponen(JenisKomponen.Atap);
            if (KindBox.Items[0] is ComboBoxItem itemWall)
            {
                itemWall.Visibility = hasWall ? Visibility.Visible : Visibility.Collapsed;
                itemWall.IsEnabled = hasWall;
            }
            if (KindBox.Items[1] is ComboBoxItem itemRoof)
            {
                itemRoof.Visibility = hasRoof ? Visibility.Visible : Visibility.Collapsed;
                itemRoof.IsEnabled = hasRoof;
            }
        }
        KindBox.SelectedIndex = S.Jenis == Models.JenisKomponen.Dinding ? 0 : 1;
        HorizonBox.SelectedIndex = S.Horizon switch { 20 => 0, 50 => 2, _ => 1 };
        FaktorABox.Text = _faktorPenuaanA.ToString("0.##", new CultureInfo("id-ID"));
        _refreshing = false;

        RenderSimulation();
    }

    private void RenderSimulation()
    {
        var terpilih = S.Terpilih();
        bool isEmpty = S.Luas <= 0 || terpilih.Count == 0;

        if (isEmpty)
        {
            EmptyPanel.Visibility = Visibility.Visible;
            if (ContentGrid != null) ContentGrid.Visibility = Visibility.Collapsed;

            string reason = S.Luas <= 0
                ? $"Komponen {S.Jenis} belum memiliki luas bidang yang valid pada profil proyek (luas saat ini = {Format.Angka(S.Luas)} m²). Atur luas bidang terlebih dahulu untuk mensimulasikan emisi."
                : $"Belum ada material yang dipilih untuk komponen {S.Jenis}. Silakan pilih material pembanding terlebih dahulu.";
            EmptyMessageText.Text = reason;

            DataContext = new SimulationData(S, [], _faktorPenuaanA, true, reason);
            return;
        }

        EmptyPanel.Visibility = Visibility.Collapsed;
        if (ContentGrid != null) ContentGrid.Visibility = Visibility.Visible;

        var simulated = new List<(ItemKatalog Item, Models.Evaluasi Eval)>();
        foreach (var item in terpilih)
        {
            var eval = S.Nilai(item);
            eval.ProyeksikanEmisi(S.Horizon, _faktorPenuaanA);
            simulated.Add((Item: item, Eval: eval));
        }

        // Susun titik-titik kurva step dan titik kejadian penggantian dari Domain PerhitunganSimulasi
        var seriesList = new List<IReadOnlyList<(double Year, double Value)>>();
        var eventsList = new List<IReadOnlyList<(double Year, double Value)>>();

        foreach (var x in simulated)
        {
            var sim = x.Eval.SimulasiEmisi!;
            var stepPoints = PerhitunganSimulasi.TitikKurvaStep(x.Eval.EmisiAwal, S.Horizon, sim.UsiaPakaiEfektif);
            seriesList.Add(stepPoints.Select(p => (Year: p.WaktuTahun, Value: p.EmisiKumulatifKg)).ToList());

            var replacementEvents = PerhitunganSimulasi.KejadianPenggantian(S.Horizon, sim.UsiaPakaiEfektif);
            var keyPoints = new List<(double Year, double Value)>
            {
                (0.0, x.Eval.EmisiAwal)
            };
            foreach (var t in replacementEvents)
            {
                keyPoints.Add((t, PerhitunganSimulasi.EmisiKumulatifPadaTitik(x.Eval.EmisiAwal, t, S.Horizon, sim.UsiaPakaiEfektif)));
            }
            keyPoints.Add((S.Horizon, PerhitunganSimulasi.EmisiKumulatifPadaTitik(x.Eval.EmisiAwal, S.Horizon, S.Horizon, sim.UsiaPakaiEfektif)));
            eventsList.Add(keyPoints);
        }

        Chart.SetData(seriesList, S.Horizon, eventsList);
        DataContext = new SimulationData(S, simulated, _faktorPenuaanA, false, string.Empty);
    }

    private void Kind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || KindBox.SelectedItem is not ComboBoxItem { Tag: string kind }) return;
        S.AturJenis(Enum.Parse<Models.JenisKomponen>(kind));
        Shell.Render();
    }

    private void Horizon_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || HorizonBox.SelectedItem is not ComboBoxItem { Tag: string horizon }) return;
        S.Horizon = int.Parse(horizon);
        Shell.Render();
    }

    private void ApplyFaktorA()
    {
        string input = (FaktorABox.Text ?? string.Empty).Trim().Replace(',', '.');

        if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            || !double.IsFinite(parsed)
            || parsed <= 0
            || parsed > 1)
        {
            TxtFaktorAError.Text = "Faktor penuaan (a) harus berupa angka finite dalam rentang 0 < a ≤ 1 (misal 0,5). Nilai sebelumnya tetap dipertahankan.";
            TxtFaktorAError.Visibility = Visibility.Visible;
            return;
        }

        TxtFaktorAError.Text = string.Empty;
        TxtFaktorAError.Visibility = Visibility.Collapsed;
        _faktorPenuaanA = parsed;
        FaktorABox.Text = parsed.ToString("0.##", new CultureInfo("id-ID"));
        RenderSimulation();
    }

    private void ResetA_Click(object sender, RoutedEventArgs e)
    {
        _faktorPenuaanA = 0.5;
        FaktorABox.Text = "0,5";
        TxtFaktorAError.Text = string.Empty;
        TxtFaktorAError.Visibility = Visibility.Collapsed;
        RenderSimulation();
    }

    private void FaktorA_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_refreshing) return;
        ApplyFaktorA();
    }

    private void FaktorA_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyFaktorA();
            e.Handled = true;
        }
    }
}

public sealed class SimulationData(
    AppState s,
    List<(ItemKatalog Item, Models.Evaluasi Eval)> simulated,
    double faktorPenuaanA,
    bool isEmpty,
    string emptyReason) : ViewData
{
    public List<LegendItem> Legend { get; } = simulated.Select((x, i) =>
        new LegendItem(x.Item.Bahan.Nama, EmissionChart.SeriesBrushes[Math.Min(i, EmissionChart.SeriesBrushes.Length - 1)])).ToList();

    public List<SimStat> Stats { get; } = simulated.ConvertAll(x =>
    {
        var sim = x.Eval.SimulasiEmisi!;
        return new SimStat(
            x.Item.Bahan.Nama,
            x.Item.Bahan.UsiaPakaiTahun,
            sim.UsiaPakaiEfektif,
            sim.JumlahPenggantian,
            x.Eval.EmisiAwal,
            sim.EmisiKumulatif);
    });

    public string AreaChip { get; } = Format.Angka(s.Luas) + " m² (dari profil proyek)";
    public double FaktorPenuaanA { get; } = faktorPenuaanA;
    public bool IsEmpty { get; } = isEmpty;
    public string EmptyReason { get; } = emptyReason;
}

public sealed class LegendItem(string name, Brush brush) : ViewData
{
    public string Name { get; } = name;
    public Brush Brush { get; } = brush;
}

public sealed class SimStat(
    string name,
    double usiaNominal,
    double usiaEfektif,
    int jumlahPenggantian,
    double emisiAwal,
    double emisiKumulatif) : ViewData
{
    public string Name { get; } = name;
    public string Total { get; } = Format.Angka(emisiKumulatif) + " ";
    public string Detail { get; } = $"{jumlahPenggantian} kali penggantian (usia efektif {Format.Angka(usiaEfektif)} tahun)";
    public string Breakdown { get; } = $"Usia nominal: {Format.Angka(usiaNominal)} tahun | Emisi awal: {Format.Angka(emisiAwal)} kgCO₂e";
}
