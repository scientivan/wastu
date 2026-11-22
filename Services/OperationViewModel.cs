using System.ComponentModel;

namespace Wastu.Services;

/// <summary>Observable command state shared by desktop workflows, independent of WPF.</summary>
public class OperationViewModel : INotifyPropertyChanged
{
    private CancellationTokenSource? _cancellation;
    public bool IsBusy { get; private set; }
    public string? Error { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Cancel() => _cancellation?.Cancel();

    public async Task<bool> RunAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy) return false;
        IsBusy = true;
        Error = null;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        Changed();
        try { await action(cancellation.Token); return true; }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        { Error = "Operasi dibatalkan."; return false; }
        catch (Exception)
        { Error = "Operasi belum berhasil. Periksa koneksi atau penyimpanan, lalu coba lagi."; return false; }
        finally { _cancellation = null; IsBusy = false; Changed(); }
    }

    private void Changed()
    {
        PropertyChanged?.Invoke(this, new(nameof(IsBusy)));
        PropertyChanged?.Invoke(this, new(nameof(Error)));
    }
}

public sealed class SimulationViewModel : OperationViewModel
{
    public IReadOnlyList<SimulationHistoryEntry> Results { get; private set; } = [];
    public Task<bool> LoadAsync(DesktopWorkflowService service) => RunAsync(async ct =>
    {
        var result = await service.SimulateAsync(ct);
        ct.ThrowIfCancellationRequested();
        Results = result;
    });
}

public sealed class ScheduleViewModel : OperationViewModel
{
    public static Domain.Weather.AmbangPekerjaan? ParseOverride(Models.JenisPekerjaan kind,
        string rainProbability, string rain, string humidity, string minTemperature, string maxTemperature)
    {
        static double? Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!Format.CobaBaca(text, out double value)) throw new FormatException("Isi angka ambang yang valid.");
            return value;
        }
        var defaults = Domain.Weather.AmbangLayakKerja.Untuk((Domain.Weather.JenisPekerjaan)kind);
        var result = new Domain.Weather.AmbangPekerjaan(Parse(rainProbability), Parse(rain), Parse(humidity),
            Parse(minTemperature), Parse(maxTemperature), defaults.TingkatSuhu);
        return result == defaults ? null : result;
    }

    public Task<bool> RefreshAsync(DesktopWorkflowService service, bool force = false)
        => RunAsync(ct => service.RefreshForecastAsync(force, ct));
}

public sealed class ReportViewModel : OperationViewModel
{
    public async Task<Domain.Services.Laporan?> ExportAsync(DesktopWorkflowService service,
        Reporting.SnapshotLaporan snapshot, string path)
    {
        Domain.Services.Laporan? report = null;
        await RunAsync(async ct => report = await service.ExportAsync(snapshot, path, ct));
        return report;
    }
}
