using System.Globalization;
using Wastu.Domain;
using Wastu.Reporting;
using Evaluasi = Wastu.Models.Evaluasi;

namespace Wastu.Services;

internal static class SnapshotBahanMapper
{
    public static SnapshotBahan Buat(ItemKatalog item, Evaluasi evaluasi, Guid komponenId, int peringkat)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(evaluasi);
        var bahan = item.DataDomain;
        var sumber = bahan.SumberProperti ?? item.Bahan.SumberProperti;
        var sources = sumber.Select(s =>
        {
            var details = new List<string> { s.Referensi, $"Status: {s.Status}" };
            if (!string.IsNullOrWhiteSpace(s.Versi)) details.Add($"Versi: {s.Versi}");
            if (!string.IsNullOrWhiteSpace(s.Tautan)) details.Add($"Tautan: {s.Tautan}");
            if (!string.IsNullOrWhiteSpace(s.CatatanKonversi)) details.Add($"Konversi: {s.CatatanKonversi}");
            return new SnapshotSumberProperti(s.Properti, Nilai(bahan, s.Properti), string.Join("; ", details), s.Versi);
        }).ToArray();

        if (sources.Length == 0)
            sources = [new SnapshotSumberProperti("Faktor Emisi", Nilai(bahan, "faktor_emisi"),
                $"{bahan.SumberData}; Status verifikasi properti tidak tercatat")];

        return new SnapshotBahan
        {
            BahanId = bahan.Id,
            KomponenId = komponenId,
            Jenis = bahan.Jenis,
            Peringkat = peringkat,
            Nama = evaluasi.Bahan.Nama,
            SkorKesesuaian = evaluasi.SkorKesesuaian,
            LabelKesesuaian = KonfigurasiModel.Bawaan.LabelUntuk(evaluasi.SkorKesesuaian) switch
            {
                LabelKesesuaian.TidakDisarankan => "Tidak disarankan",
                LabelKesesuaian.PerluPertimbangan => "Perlu pertimbangan",
                _ => "Cocok"
            },
            EmisiAwalKg = evaluasi.EmisiAwal,
            Alasan = evaluasi.Alasan,
            SubSkorTermal = evaluasi.SubSkor.Termal,
            SubSkorLembap = evaluasi.SubSkor.Lembap,
            SubSkorKorosi = evaluasi.SubSkor.Korosi,
            SubSkorUsia = evaluasi.SubSkor.Usia,
            SumberProperti = sources
        };
    }

    private static string Nilai(Bahan bahan, string properti)
    {
        static string Angka(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        return properti switch
        {
            "konduktivitas_termal" => $"{Angka(bahan.KonduktivitasTermal)} W/(m·K)",
            "ketahanan_lembap" => $"{bahan.KetahananLembap}/5",
            "ketahanan_korosi" => $"{bahan.KetahananKorosi}/5",
            "usia_pakai_nominal" or "usia_pakai_nominal_tahun" or "usia_pakai_tahun" => $"{Angka(bahan.UsiaPakaiNominalTahun)} tahun",
            "faktor_emisi" => $"{Angka(bahan.FaktorEmisi)} kgCO₂e/m²",
            _ => "Nilai properti tidak tercatat"
        };
    }
}
