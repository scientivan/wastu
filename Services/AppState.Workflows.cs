using Wastu.Domain;
using Wastu.Domain.Weather;

namespace Wastu.Services;

public sealed partial class AppState
{
    internal string PendingReportDirectory => System.IO.Path.Combine(StateStore.ProfileFolder, "pending_reports",
        _toko is null ? "local" : System.IO.Path.GetFileNameWithoutExtension(_toko.SnapshotPath));
    private DesktopWorkflowService? _workflows;
    public DesktopWorkflowService Workflows => _workflows ??= new(this, _backend);

    internal async Task<Domain.Evaluasi> EvaluasiTersimpanAsync(Models.Evaluasi evaluasi, CancellationToken ct)
    {
        var domain = evaluasi.HasilDomain ?? throw new InvalidOperationException("Evaluasi belum tersedia.");
        var cache = _peringkat.Values.FirstOrDefault(c => c.Hasil.Contains(evaluasi));
        if (cache?.Penyimpanan is { } saving)
        {
            var results = await saving.WaitAsync(ct);
            domain = results.Single(e => e.BahanId == evaluasi.Bahan.DomainId);
            evaluasi.HasilDomain = domain;
        }
        return domain;
    }

    public bool PrakiraanDataLama => Aktif?.PrakiraanCache is not { } cache
        || !KebijakanCachePrakiraan.ApakahSegar(cache.DiambilPadaUtc, DateTimeOffset.UtcNow);

    internal void PulihkanPrakiraanAktif()
    {
        if (Aktif?.PrakiraanCache is not { } cache || cache.RevisiLokasi != Aktif.RevisiLokasi) return;
        Aktif.ZonaWaktu = cache.ZonaWaktu;
        Prakiraan = cache.Hari.Select(d => new Models.PrakiraanCuaca
        {
            Tanggal = d.Tanggal.ToDateTime(TimeOnly.MinValue),
            ProbabilitasHujan = d.ProbabilitasHujanPct,
            CurahHujan = d.CurahHujanMm, SuhuMin = d.SuhuMinC,
            SuhuMaks = d.SuhuMaksC, Kelembapan = d.KelembapanPct,
            DataLengkap = d.DataLengkap, CatatanKualitas = d.CatatanKualitas
        }).ToList();
        PrakiraanProyekId = Aktif.Id;
        PrakiraanDiperbarui = cache.DiambilPadaUtc.ToString("dd/MM/yyyy HH:mm 'UTC'");
    }

    public string? AturFaktorPenuaan(string input)
    {
        TolakTulis();
        if (!double.TryParse(input.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            || !double.IsFinite(value) || value <= 0 || value > 1)
            return "Faktor penuaan (a) harus berupa angka dalam rentang 0 < a ≤ 1.";
        Aktif.FaktorPenuaanA = value;
        Ubah();
        return null;
    }
}

public sealed record SimulationHistoryEntry(string InputHash, Guid BahanId,
    double UsiaNominal, SimulasiEmisi Hasil, double EmisiAwal = 0);
