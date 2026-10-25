using System.Windows.Controls;
using System.Windows.Media;
using Wastu.Controls;
using Wastu.Models;
using Wastu.Services;

namespace Wastu.Views.Pages;

public partial class SimulationPage : PageBase
{
    private bool _refreshing;

    public SimulationPage() => InitializeComponent();

    protected override void Refresh()
    {
        _refreshing = true;
        KindBox.SelectedIndex = S.Jenis == JenisKomponen.Dinding ? 0 : 1;
        HorizonBox.SelectedIndex = S.Horizon switch { 20 => 0, 50 => 2, _ => 1 };
        _refreshing = false;

        var simulated = S.Terpilih().ConvertAll(item => (Item: item, Eval: S.Simulasikan(item)));
        // plot(): emisi kumulatif tiap tahun 0..horizon
        Chart.SetData(simulated.ConvertAll(x =>
        {
            var sim = x.Eval.SimulasiEmisi!;
            var curve = new double[S.Horizon + 1];
            for (var year = 0; year <= S.Horizon; year++)
                curve[year] = x.Eval.EmisiAwal * (1 + sim.PenggantianHingga(year));
            return curve;
        }), S.Horizon);
        DataContext = new SimulationData(S, simulated);
    }

    private void Kind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || KindBox.SelectedItem is not ComboBoxItem { Tag: string kind }) return;
        S.AturJenis(Enum.Parse<JenisKomponen>(kind));
        Shell.Render();
    }

    private void Horizon_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || HorizonBox.SelectedItem is not ComboBoxItem { Tag: string horizon }) return;
        S.Horizon = int.Parse(horizon);
        Shell.Render();
    }
}

public sealed class SimulationData(AppState s, List<(ItemKatalog Item, Evaluasi Eval)> simulated) : ViewData
{
    public List<LegendItem> Legend { get; } = simulated.Select((x, i) =>
        new LegendItem(x.Item.Bahan.Nama, EmissionChart.SeriesBrushes[Math.Min(i, 1)])).ToList();

    public List<SimStat> Stats { get; } = simulated.ConvertAll(x => new SimStat(x.Item.Bahan.Nama, x.Eval.SimulasiEmisi!));

    public string AreaChip { get; } = Format.Angka(s.Luas) + " m² (dari profil proyek)";
}

public sealed class LegendItem(string name, Brush brush) : ViewData
{
    public string Name { get; } = name;
    public Brush Brush { get; } = brush;
}

public sealed class SimStat(string name, SimulasiEmisi sim) : ViewData
{
    public string Name { get; } = name;
    public string Total { get; } = Format.Angka(sim.EmisiKumulatif) + " ";
    public string Detail { get; } = $"{sim.JumlahPenggantian} penggantian usia efektif {Format.Angka(sim.UsiaPakaiEfektif)} tahun";
}
