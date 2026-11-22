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
    private readonly SimulationViewModel _viewModel = new();
    private Task<bool>? _pending;
    private int _request;
    private double _faktorPenuaanA => S.Aktif.FaktorPenuaanA;

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
        FaktorABox.IsEnabled = !S.ModeBacaSaja;
        BtnResetA.IsEnabled = !S.ModeBacaSaja;
        FaktorABox.Text = _faktorPenuaanA.ToString("0.##", new CultureInfo("id-ID"));
        _refreshing = false;

        RenderSimulation();
    }

    private async void RenderSimulation()
    {
        int request = ++_request;
        var project = S.Aktif;
        _viewModel.Cancel();
        if (_pending is { IsCompleted: false }) await _pending;
        if (request != _request) return;
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

        _pending = _viewModel.LoadAsync(S.Workflows);
        bool success = await _pending;
        if (request != _request || !ReferenceEquals(project, S.Aktif)) return;
        if (!success)
        {
            EmptyPanel.Visibility = Visibility.Visible;
            if (ContentGrid is not null) ContentGrid.Visibility = Visibility.Collapsed;
            EmptyMessageText.Text = _viewModel.Error ?? "Simulasi belum tersedia.";
            return;
        }
        var simulated = _viewModel.Results;

        // Susun titik-titik kurva step dan titik kejadian penggantian dari Domain PerhitunganSimulasi
        var seriesList = new List<IReadOnlyList<(double Year, double Value)>>();
        var eventsList = new List<IReadOnlyList<(double Year, double Value)>>();

        foreach (var x in simulated)
        {
            var sim = x.Hasil;
            var stepPoints = PerhitunganSimulasi.TitikKurvaStep(x.EmisiAwal, S.Horizon, sim.UsiaPakaiEfektifTahun);
            seriesList.Add(stepPoints.Select(p => (Year: p.WaktuTahun, Value: p.EmisiKumulatifKg)).ToList());

            var replacementEvents = PerhitunganSimulasi.KejadianPenggantian(S.Horizon, sim.UsiaPakaiEfektifTahun);
            var keyPoints = new List<(double Year, double Value)>
            {
                (0.0, x.EmisiAwal)
            };
            foreach (var t in replacementEvents)
            {
                keyPoints.Add((t, PerhitunganSimulasi.EmisiKumulatifPadaTitik(x.EmisiAwal, t, S.Horizon, sim.UsiaPakaiEfektifTahun)));
            }
            keyPoints.Add((S.Horizon, PerhitunganSimulasi.EmisiKumulatifPadaTitik(x.EmisiAwal, S.Horizon, S.Horizon, sim.UsiaPakaiEfektifTahun)));
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
        var error = S.AturFaktorPenuaan(FaktorABox.Text ?? "");
        TxtFaktorAError.Text = error ?? "";
        TxtFaktorAError.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
        if (error is not null) return;
        FaktorABox.Text = _faktorPenuaanA.ToString("0.##", new CultureInfo("id-ID"));
        RenderSimulation();
    }

    private void ResetA_Click(object sender, RoutedEventArgs e)
    {
        if (S.ModeBacaSaja) return;
        S.AturFaktorPenuaan("0,5");
        FaktorABox.Text = "0,5";
        TxtFaktorAError.Text = string.Empty;
        TxtFaktorAError.Visibility = Visibility.Collapsed;
        RenderSimulation();
    }

    private void FaktorA_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_refreshing || S.ModeBacaSaja) return;
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
    IReadOnlyList<SimulationHistoryEntry> simulated,
    double faktorPenuaanA,
    bool isEmpty,
    string emptyReason) : ViewData
{
    public List<LegendItem> Legend { get; } = simulated.Select((x, i) =>
        new LegendItem(s.KatalogAktif.Single(k => k.DataDomain.Id == x.BahanId).Bahan.Nama, EmissionChart.SeriesBrushes[Math.Min(i, EmissionChart.SeriesBrushes.Length - 1)])).ToList();

    public List<SimStat> Stats { get; } = simulated.Select(x =>
    {
        var sim = x.Hasil;
        return new SimStat(
            s.KatalogAktif.Single(k => k.DataDomain.Id == x.BahanId).Bahan.Nama,
            x.UsiaNominal,
            sim.UsiaPakaiEfektifTahun,
            sim.JumlahPenggantian,
            x.EmisiAwal,
            sim.EmisiKumulatifKg);
    }).ToList();

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
