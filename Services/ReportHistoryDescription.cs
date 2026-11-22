using System.Text.Json;
using Wastu.Reporting;

namespace Wastu.Services;

public sealed record ReportHistoryDescription(string Timestamp, string Selection, bool CanRegenerate)
{
    public static ReportHistoryDescription Create(Models.Laporan report, string projectZone)
    {
        SnapshotLaporan? snapshot = null;
        try { snapshot = ReportExportService.ReadOriginalSnapshot(WorkflowMapper.KeDomain(report)); }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { }
        string timestamp;
        try { timestamp = Format.WaktuProyek(report.DibuatPada, snapshot?.ZonaWaktu ?? projectZone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        { timestamp = report.DibuatPada.UtcDateTime.ToString("yyyy-MM-dd HH:mm") + " UTC"; }
        var selection = snapshot is null ? "Pilihan laporan lama tidak tercatat sebagai snapshot."
            : string.Join(", ", snapshot.Komponen.Select(k => k.Jenis).Distinct()) + " · "
                + snapshot.BahanTerpilih.Count + " material · "
                + string.Join("/", snapshot.SimulasiHorizon.Select(s => s.HorizonTahun).Distinct().Order()) + " tahun";
        return new(timestamp, selection, snapshot is not null);
    }
}
