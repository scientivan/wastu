using Wastu.Domain.Weather;

namespace Wastu.Services;

internal static class WorkflowMapper
{
    internal static Guid LegacyJobId(Guid project, int index, PekerjaanDto job)
    {
        if (job.Id is { } id && id != Guid.Empty) return id;
        var text = $"{project:D}|{index}|{job.Nama}|{job.Jenis}|{job.Mulai:yyyy-MM-dd}|{job.Selesai:yyyy-MM-dd}";
        return new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)).AsSpan(0, 16));
    }

    public static Domain.Proyek KeDomain(Models.Bangunan p) => new(p.StableId, p.Nama, p.Kota,
        p.Latitude, p.Longitude, revisiLokasi: p.RevisiLokasi, zonaWaktu: p.ZonaWaktu);

    public static Pekerjaan KeDomain(Models.Pekerjaan j, Guid proyekId) => new(j.Id, proyekId, j.Nama,
        (JenisPekerjaan)j.Jenis, DateOnly.FromDateTime(j.TanggalMulaiRencana),
        DateOnly.FromDateTime(j.TanggalSelesaiRencana),
        j.Status == "Perlu ditinjau" ? StatusPekerjaan.PerluDitinjau
            : j.TanggalDikunci.HasValue ? StatusPekerjaan.Terkunci : StatusPekerjaan.Draft,
        j.TanggalDikunci is { } t ? DateOnly.FromDateTime(t) : null,
        overrideAmbang: j.OverrideAmbang,
        konfirmasi: new(j.PersetujuanRisiko, j.KonfirmasiDataLama, j.CatatanKeputusan),
        revisiLokasiSaatKunci: j.RevisiLokasiSaatKunci, keputusanTerakhir: j.Keputusan);

    public static Models.Pekerjaan DariDomain(Pekerjaan j) => new()
    {
        Id = j.Id, Nama = j.Nama, Jenis = (Models.JenisPekerjaan)j.Jenis,
        TanggalMulaiRencana = j.TanggalMulai.ToDateTime(TimeOnly.MinValue),
        TanggalSelesaiRencana = j.TanggalSelesai.ToDateTime(TimeOnly.MinValue),
        TanggalDikunci = j.TanggalDikunci?.ToDateTime(TimeOnly.MinValue),
        Status = j.Status == StatusPekerjaan.PerluDitinjau ? "Perlu ditinjau" : j.Status.ToString(),
        OverrideAmbang = j.OverrideAmbang, Keputusan = j.KeputusanTerakhir,
        RevisiLokasiSaatKunci = j.RevisiLokasiSaatKunci,
        PersetujuanRisiko = j.KeputusanTerakhir?.PersetujuanRisiko ?? j.Konfirmasi?.SetujuiRisiko ?? false,
        KonfirmasiDataLama = j.KeputusanTerakhir?.KonfirmasiDataLama ?? j.Konfirmasi?.SetujuiDataLama ?? false,
        CatatanKeputusan = j.KeputusanTerakhir is { } k ? string.Join("; ", k.Catatan) : j.Konfirmasi?.Catatan
    };

    public static PekerjaanDto KeDto(Pekerjaan j) => KeDto(DariDomain(j));
    public static PekerjaanDto KeDto(Models.Pekerjaan j) => new()
    {
        Id = j.Id, Nama = j.Nama, Jenis = j.Jenis,
        Mulai = DateOnly.FromDateTime(j.TanggalMulaiRencana),
        Selesai = DateOnly.FromDateTime(j.TanggalSelesaiRencana),
        Dikunci = j.TanggalDikunci is { } t ? DateOnly.FromDateTime(t) : null,
        Status = j.Status, OverrideAmbang = j.OverrideAmbang, Keputusan = j.Keputusan,
        RevisiLokasiSaatKunci = j.RevisiLokasiSaatKunci
    };

    public static Models.Pekerjaan DariDto(PekerjaanDto j) => DariDomain(new Pekerjaan(
        j.Id ?? Guid.NewGuid(), Guid.Empty, j.Nama, (JenisPekerjaan)j.Jenis, j.Mulai, j.Selesai,
        j.Status == "Perlu ditinjau" ? StatusPekerjaan.PerluDitinjau
            : j.Dikunci.HasValue ? StatusPekerjaan.Terkunci : StatusPekerjaan.Draft,
        j.Dikunci, overrideAmbang: j.OverrideAmbang, revisiLokasiSaatKunci: j.RevisiLokasiSaatKunci,
        keputusanTerakhir: j.Keputusan));

    public static Models.Laporan DariDomain(Domain.Services.Laporan l) => new()
    {
        Id = l.Id, ProyekId = l.ProyekId, PathBerkas = l.PathBerkas, DibuatPada = l.DibuatPada,
        TanggalDibuat = l.DibuatPada.LocalDateTime, ChecksumSha256 = l.ChecksumSha256,
        SnapshotHash = l.SnapshotHash, UkuranByte = l.UkuranByte, Status = l.Status, SnapshotJson = l.SnapshotJson
    };

    public static Domain.Services.Laporan KeDomain(Models.Laporan l) => new(l.Id, l.ProyekId,
        l.PathBerkas, l.DibuatPada, l.ChecksumSha256, l.SnapshotHash, l.UkuranByte, l.Status, l.SnapshotJson);
}
