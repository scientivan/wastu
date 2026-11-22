using System.Globalization;
using Wastu.Domain;

namespace Wastu.Reporting;

public static class BahanLaporanMapper
{
    public static SnapshotBahan Buat(Bahan bahan, Evaluasi evaluasi, int peringkat, KonfigurasiModel? model = null)
    {
        var sources = (bahan.SumberProperti ?? []).Select(s =>
        {
            var details = new List<string> { s.Referensi, $"Status: {s.Status}" };
            if (!string.IsNullOrWhiteSpace(s.Versi)) details.Add($"Versi: {s.Versi}");
            if (!string.IsNullOrWhiteSpace(s.Tautan)) details.Add($"Tautan: {s.Tautan}");
            if (!string.IsNullOrWhiteSpace(s.CatatanKonversi)) details.Add($"Konversi: {s.CatatanKonversi}");
            return new SnapshotSumberProperti(s.Properti, Nilai(bahan, s.Properti), string.Join("; ", details), s.Versi);
        }).ToArray();
        if (sources.Length == 0)
            sources = [new("Faktor Emisi", Nilai(bahan, "faktor_emisi"),
                $"{bahan.SumberData}; Status verifikasi properti tidak tercatat")];
        return new SnapshotBahan
        {
            BahanId = bahan.Id, KomponenId = evaluasi.KomponenId, Jenis = bahan.Jenis, Peringkat = peringkat,
            Nama = bahan.Nama, SkorKesesuaian = evaluasi.SkorKesesuaian,
            SusunanKetebalan = bahan.SusunanKetebalan,
            LabelKesesuaian = (model ?? KonfigurasiModel.Bawaan).LabelUntuk(evaluasi.SkorKesesuaian) switch
            {
                LabelKesesuaian.TidakDisarankan => "Tidak disarankan",
                LabelKesesuaian.PerluPertimbangan => "Perlu pertimbangan", _ => "Cocok"
            },
            EmisiAwalKg = evaluasi.EmisiAwalKg, Alasan = evaluasi.Alasan,
            SubSkorTermal = evaluasi.SubSkor.Termal, SubSkorLembap = evaluasi.SubSkor.Lembap,
            SubSkorKorosi = evaluasi.SubSkor.Korosi, SubSkorUsia = evaluasi.SubSkor.Usia, SumberProperti = sources
        };
    }

    private static string Nilai(Bahan bahan, string properti)
    {
        static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        return properti switch
        {
            "konduktivitas_termal" => $"{N(bahan.KonduktivitasTermal)} W/(m·K)",
            "ketahanan_lembap" => $"{bahan.KetahananLembap}/5",
            "ketahanan_korosi" => $"{bahan.KetahananKorosi}/5",
            "usia_pakai_nominal" or "usia_pakai_nominal_tahun" or "usia_pakai_tahun" => $"{N(bahan.UsiaPakaiNominalTahun)} tahun",
            "faktor_emisi" => $"{N(bahan.FaktorEmisi)} kgCO₂e/m²", _ => "Nilai properti tidak tercatat"
        };
    }
}
