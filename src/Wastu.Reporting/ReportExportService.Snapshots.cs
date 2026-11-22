using System.Text.Json;
using Wastu.Domain;
using Wastu.Domain.Emissions;
using Wastu.Domain.Services;

namespace Wastu.Reporting;

public sealed partial class ReportExportService
{
    private async Task<SnapshotLaporan> BuildSnapshotAsync(Proyek proyek,
        IReadOnlyList<Komponen> komponen, OpsiEkspor opsi, CancellationToken ct)
    {
        if (_evaluasiRepository is null)
            throw new InvalidOperationException("Repository hasil evaluasi diperlukan untuk ekspor.");
        var chosenComponents = komponen.Where(c => opsi.KomponenDipilih is null || opsi.KomponenDipilih.Contains(c.Id)).ToArray();
        if (chosenComponents.Length == 0) throw new InvalidOperationException("Pilih minimal satu komponen.");
        var all = new List<Evaluasi>();
        foreach (var c in chosenComponents)
        {
            var history = await _evaluasiRepository.ListByKomponenAsync(c.Id, ct).ConfigureAwait(false);
            // One complete version per component, never all historical batches mixed together.
            var latest = history.GroupBy(e => (e.BatchId, e.HashInput))
                .OrderByDescending(g => g.Max(e => e.DihitungPadaUtc)).FirstOrDefault()?.ToArray() ?? [];
            if (latest.Length == 0 || latest.Any(e => e.Input is null))
                throw new InvalidOperationException("Hasil belum memiliki snapshot masukan. Hitung evaluasi baru sebelum ekspor.");
            if (latest.Any(e => e.Input!.Komponen.LuasM2 != c.LuasM2
                || e.Input.Iklim.RevisiLokasi != proyek.RevisiLokasi
                || e.Input.Proyek is null || e.Input.Proyek.Latitude != proyek.Latitude
                || e.Input.Proyek.Longitude != proyek.Longitude))
                throw new InvalidOperationException("Input proyek berubah. Hitung evaluasi baru sebelum ekspor.");
            all.AddRange(latest.OrderByDescending(e => e.SkorKesesuaian)
                .ThenBy(e => e.EmisiAwalKg).ThenBy(e => e.BahanId));
        }
        var input = all[0].Input!;
        if (all.Any(e => e.Input!.Bobot != input.Bobot || e.Input.Konfigurasi != input.Konfigurasi
            || JsonSerializer.Serialize(e.Input.Iklim) != JsonSerializer.Serialize(input.Iklim)))
            throw new InvalidOperationException("Versi iklim, bobot atau model antarhasil berbeda. Hitung kembali komponen sebelum ekspor.");

        var selected = all.Where(e => opsi.BahanDipilih is null || opsi.BahanDipilih.Contains(e.BahanId)).ToArray();
        if (selected.Length == 0) throw new InvalidOperationException("Pilih minimal satu material.");
        var horizons = opsi.HorizonDipilih ?? [opsi.HorizonTahun];
        if (horizons.Count == 0 || horizons.Any(h => h is not (20 or 30 or 50)))
            throw new ArgumentException("Pilih horizon 20, 30 atau 50 tahun.", nameof(opsi));

        var materials = selected.Select(e => BahanLaporanMapper.Buat(e.Input!.Bahan, e,
            all.Where(x => x.KomponenId == e.KomponenId).ToList().FindIndex(x => x.Id == e.Id) + 1,
            input.Konfigurasi)).ToArray();
        var simulations = selected.SelectMany(e => horizons.Distinct().Select(h =>
        {
            var original = e.Input!;
            var s = PerhitunganSimulasi.Simulasikan(e.Id, e, original.Bahan.UsiaPakaiNominalTahun, h, original.Konfigurasi);
            return new SnapshotSimulasi { EvaluasiId = e.Id, BahanNama = original.Bahan.Nama,
                Jenis = original.Komponen.Jenis, HorizonTahun = h, UsiaPakaiNominal = original.Bahan.UsiaPakaiNominalTahun,
                UsiaPakaiEfektif = s.UsiaPakaiEfektifTahun, JumlahPenggantian = s.JumlahPenggantian,
                EmisiAwalKg = e.EmisiAwalKg, EmisiKumulatifKg = s.EmisiKumulatifKg,
                FaktorPenuaanA = s.FaktorPenuaanA, VersiModel = s.VersiModel ?? original.Konfigurasi.VersiModel };
        })).ToArray();
        var jobs = opsi.SertakanJadwal && _pekerjaanRepository is not null
            ? await _pekerjaanRepository.ListByProyekAsync(proyek.Id, ct).ConfigureAwait(false) : [];
        var climate = input.Iklim;
        var meta = climate.Metadata;
        return new SnapshotLaporan
        {
            ProyekId = proyek.Id, NamaProyek = proyek.Nama, Kota = proyek.Kota,
            Latitude = proyek.Latitude, Longitude = proyek.Longitude, ZonaWaktu = proyek.ZonaWaktu ?? meta?.ZonaWaktu ?? "Asia/Jakarta",
            Komponen = chosenComponents.Select(c => new SnapshotKomponen(c.Jenis, c.LuasM2)).ToArray(),
            Iklim = new SnapshotIklim { CurahHujanTahunanMm = climate.CurahHujanTahunanMm,
                KelembapanRataPct = climate.KelembapanRataPct, SuhuRataC = climate.SuhuRataC,
                JarakPesisirKm = climate.JarakPesisirKm, TingkatKorosi = climate.TingkatKorosi.ToString(),
                Provider = meta?.Penyedia ?? "Sumber tidak tercatat", Provenance = climate.Provenance.ToString(),
                DiambilPada = meta?.DiambilPadaUtc, Periode = meta?.PeriodeAwal is { } start && meta.PeriodeAkhir is { } end
                    ? $"{start:yyyy-MM-dd} s/d {end:yyyy-MM-dd}" : "Periode tidak tercatat" },
            BahanTerpilih = materials, SimulasiHorizon = simulations,
            Bobot = input.Bobot, VersiModel = input.Konfigurasi.VersiModel, SertakanJadwal = opsi.SertakanJadwal,
            JadwalTerkunci = jobs.Where(j => j.TanggalDikunci.HasValue).Select(j => new SnapshotPekerjaan
            {
                PekerjaanId = j.Id, Nama = j.Nama, Jenis = j.Jenis, TanggalMulai = j.TanggalMulai,
                TanggalSelesai = j.TanggalSelesai, TanggalDikunci = j.TanggalDikunci,
                StatusCuaca = j.KeputusanTerakhir?.Status.ToString() ?? "Belum terverifikasi",
                Keputusan = j.KeputusanTerakhir, AlasanCuaca = j.KeputusanTerakhir?.Catatan ?? ["Snapshot keputusan tidak tersedia"],
                SumberAmbang = j.OverrideAmbang is null ? "Asumsi generik jenis pekerjaan" : "Override pekerjaan",
                KonfirmasiRisiko = j.KeputusanTerakhir?.PersetujuanRisiko ?? false,
                KonfirmasiDataLama = j.KeputusanTerakhir?.KonfirmasiDataLama ?? false
            }).ToArray()
        };
    }

    public Task<Laporan> RegenerateAsync(Laporan report, string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(report.SnapshotJson))
            throw new InvalidOperationException("Laporan legacy tidak mempunyai snapshot untuk dibuat ulang.");
        var snapshot = JsonSerializer.Deserialize<SnapshotLaporan>(report.SnapshotJson)
            ?? throw new InvalidOperationException("Snapshot laporan tidak valid.");
        if (snapshot.Id != report.Id || snapshot.ProyekId != report.ProyekId
            || snapshot.HitungHash() != report.SnapshotHash)
            throw new InvalidOperationException("Identitas atau checksum snapshot laporan berbeda.");
        // A copy of the same report: preserve ID, original time, values and assumptions.
        return ExportSnapshotAsync(snapshot, path, ct);
    }
}
