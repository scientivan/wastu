using Wastu.Reporting;
using Evaluasi = Wastu.Models.Evaluasi;

namespace Wastu.Services;

internal static class SnapshotBahanMapper
{
    public static SnapshotBahan Buat(ItemKatalog item, Evaluasi evaluasi, Guid komponenId, int peringkat)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(evaluasi);
        var result = evaluasi.HasilDomain ?? new Domain.Evaluasi(Guid.Empty, item.DataDomain.Id, komponenId,
            evaluasi.SkorKesesuaian, evaluasi.EmisiAwal, evaluasi.SubSkor, evaluasi.Alasan);
        return BahanLaporanMapper.Buat(item.DataDomain, result with { KomponenId = komponenId }, peringkat);
    }
}
