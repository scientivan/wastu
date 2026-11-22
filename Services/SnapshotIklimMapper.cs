using System.Globalization;
using Wastu.Domain;
using Wastu.Models;
using Wastu.Reporting;
using ProfilIklim = Wastu.Models.ProfilIklim;

namespace Wastu.Services;

internal static class SnapshotIklimMapper
{
    public static SnapshotIklim Buat(ProfilIklim climate, bool offline)
    {
        ArgumentNullException.ThrowIfNull(climate);
        var notes = new List<string>();
        if (offline) notes.Add("Laporan menggunakan profil iklim tersimpan saat luring.");
        if (!string.IsNullOrWhiteSpace(climate.Sumber)
            && !string.Equals(climate.Sumber, climate.Provider, StringComparison.Ordinal))
            notes.Add($"Sumber profil: {climate.Sumber.Trim()}.");

        var originNote = climate.Provenance switch
        {
            ProvenanceIklim.EstimasiBawaan => "Profil iklim merupakan estimasi bawaan; bukan data pengamatan API historis.",
            ProvenanceIklim.Contoh => "Profil iklim menggunakan data contoh atau sintetis.",
            ProvenanceIklim.LegacyUnknown => "Asal-usul profil iklim lama belum terverifikasi.",
            ProvenanceIklim.ManualOverride => "Profil iklim memuat masukan atau penyesuaian manual.",
            _ => null
        };
        if (originNote is not null) notes.Add(originNote);

        var cacheNote = climate.KualitasCache switch
        {
            KualitasCacheIklim.TidakCocokLokasi => "Peringatan: profil/cache iklim tidak cocok dengan lokasi proyek; perlu pengambilan ulang.",
            KualitasCacheIklim.DataLama => "Peringatan: cache iklim berupa data lama.",
            KualitasCacheIklim.TidakTersedia => "Cache iklim tidak tersedia.",
            _ => null
        };
        if (cacheNote is not null) notes.Add(cacheNote);

        var fetchNote = climate.StatusPengambilan switch
        {
            StatusPengambilanIklim.GagalMemperbarui => "Pembaruan iklim terakhir gagal; laporan memakai profil yang masih tersedia.",
            StatusPengambilanIklim.Mengambil => "Pembaruan iklim masih berlangsung; laporan memakai profil yang sudah tersedia.",
            StatusPengambilanIklim.BelumTersedia => "Pengambilan iklim belum tersedia.",
            _ => null
        };
        if (fetchNote is not null) notes.Add(fetchNote);

        var coastalNotes = new List<string>
        {
            "Jarak ke garis pantai adalah proksi risiko korosi atmosferik akibat udara bergaram, bukan pengukuran salinitas langsung."
        };
        if (!climate.PesisirTersedia)
            coastalNotes.Add("Jarak pesisir tidak tersedia; angka yang ditampilkan belum terverifikasi.");
        if (!string.IsNullOrWhiteSpace(climate.CatatanOverridePesisir))
            coastalNotes.Add($"Override jarak pesisir manual: {climate.CatatanOverridePesisir.Trim()}.");
        if (!string.IsNullOrWhiteSpace(climate.VersiAlgoritmePesisir))
            coastalNotes.Add($"Metode/versi pesisir: {climate.VersiAlgoritmePesisir.Trim()}.");

        return new SnapshotIklim
        {
            CurahHujanTahunanMm = climate.CurahHujanTahunan,
            KelembapanRataPct = climate.KelembapanRata,
            SuhuRataC = climate.SuhuRata,
            JarakPesisirKm = climate.JarakPesisirKm,
            TingkatKorosi = !string.IsNullOrWhiteSpace(climate.TingkatKorosi)
                ? climate.TingkatKorosi : climate.KeDomain().TingkatKorosi.ToString(),
            Provider = !string.IsNullOrWhiteSpace(climate.Provider) ? climate.Provider.Trim()
                : !string.IsNullOrWhiteSpace(climate.Sumber) ? climate.Sumber.Trim()
                : climate.Provenance == ProvenanceIklim.EstimasiBawaan ? "Estimasi bawaan"
                : climate.Provenance == ProvenanceIklim.ManualOverride ? "Input manual"
                : "Sumber tidak tercatat",
            Periode = Periode(climate.PeriodeMulai, climate.PeriodeSelesai),
            Provenance = climate.Provenance.ToString(),
            DiambilPada = climate.DiambilPadaUtc,
            IsOffline = offline,
            CatatanOffline = notes.Count > 0 ? string.Join(" ", notes) : null,
            PesisirDisclaimer = string.Join(" ", coastalNotes)
        };
    }

    private static string Periode(DateOnly? mulai, DateOnly? selesai)
    {
        string Tanggal(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return (mulai, selesai) switch
        {
            ({ } awal, { } akhir) => $"{Tanggal(awal)} s.d. {Tanggal(akhir)}",
            ({ } awal, null) => $"Mulai {Tanggal(awal)}; akhir periode tidak tercatat",
            (null, { } akhir) => $"Awal periode tidak tercatat; sampai {Tanggal(akhir)}",
            _ => "Periode tidak tercatat"
        };
    }
}
