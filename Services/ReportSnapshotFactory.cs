using Wastu.Reporting;

namespace Wastu.Services;

public static class ReportSnapshotFactory
{
    public static async Task<SnapshotLaporan> BuildAsync(AppState state, Models.JenisKomponen jenis, int horizon,
        bool includeSchedule, IReadOnlySet<string> selectedCodes, CancellationToken ct = default)
    {
        // Capture all inputs and persistence tasks before the first await.
        var rank = state.Peringkat(jenis);
        var snapshot = Build(state, jenis, horizon, includeSchedule, selectedCodes);
        var originals = rank.Where(e => selectedCodes.Contains(state.Item(e.Bahan).Kode))
            .Select(e => (Id: e.HasilDomain!.Id, Pending: state.EvaluasiTersimpanAsync(e, ct))).ToArray();
        var persisted = await Task.WhenAll(originals.Select(e => e.Pending));
        var ids = originals.Zip(persisted).ToDictionary(e => e.First.Id, e => e.Second.Id);
        return snapshot with { SimulasiHorizon = snapshot.SimulasiHorizon
            .Select(s => s with { EvaluasiId = ids[s.EvaluasiId] }).ToArray() };
    }

    public static SnapshotLaporan Build(AppState state, Models.JenisKomponen jenis, int horizon,
        bool includeSchedule, IReadOnlySet<string> selectedCodes)
    {
        if (selectedCodes.Count == 0)
            throw new InvalidOperationException("Pilih minimal satu material untuk laporan.");
        var p = state.Aktif;
        var climate = state.Iklim;
        var rank = state.Peringkat(jenis);

        // Map components
        var compList = p.Komponen.Where(k => k.Jenis == jenis).Select(k => new SnapshotKomponen(
            k.Jenis == Models.JenisKomponen.Dinding ? Wastu.Domain.JenisKomponen.Dinding : Wastu.Domain.JenisKomponen.Atap,
            k.Luas)).ToList();

        var snapIklim = SnapshotIklimMapper.Buat(climate, state.ModeBacaSaja);

        // Selected materials (including any rank e.g. rank 10!)
        var selectedBahan = new List<SnapshotBahan>();
        for (int i = 0; i < rank.Length; i++)
        {
            var e = rank[i];
            var item = state.Item(e.Bahan);
            if (selectedCodes.Contains(item.Kode))
            {
                selectedBahan.Add(SnapshotBahanMapper.Buat(item, e,
                    p.Komponen.Single(k => k.Jenis == jenis).Id, i + 1));
            }
        }

        // Selected simulations
        var simList = new List<SnapshotSimulasi>();
        foreach (var mat in selectedBahan)
        {
            var evaluasi = rank.FirstOrDefault(r => r.Bahan.DomainId == mat.BahanId);
            if (evaluasi is not null)
            {
                var item = state.Item(evaluasi.Bahan);
                evaluasi.ProyeksikanEmisi(horizon, p.FaktorPenuaanA);
                var sim = evaluasi.SimulasiEmisi;
                if (sim is not null)
                {
                    simList.Add(new SnapshotSimulasi
                    {
                        EvaluasiId = evaluasi.HasilDomain!.Id,
                        BahanNama = mat.Nama,
                        Jenis = mat.Jenis,
                        HorizonTahun = horizon,
                        UsiaPakaiNominal = evaluasi.Bahan.UsiaPakaiTahun,
                        UsiaPakaiEfektif = sim.UsiaPakaiEfektif,
                        JumlahPenggantian = sim.JumlahPenggantian,
                        EmisiAwalKg = evaluasi.EmisiAwal,
                        EmisiKumulatifKg = sim.EmisiKumulatif,
                        FaktorPenuaanA = sim.FaktorPenuaanA,
                        VersiModel = "v2.0-a" + p.FaktorPenuaanA.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                    });
                }
            }
        }

        // Locked jobs
        var lockedJobs = new List<SnapshotPekerjaan>();
        if (includeSchedule)
        {
            foreach (var j in p.Pekerjaan.Where(w => w.TanggalDikunci.HasValue))
            {
                lockedJobs.Add(new SnapshotPekerjaan
                {
                    PekerjaanId = j.Id,
                    Nama = j.Nama,
                    Jenis = j.Jenis == Models.JenisPekerjaan.Pengecoran ? Wastu.Domain.Weather.JenisPekerjaan.Pengecoran : Wastu.Domain.Weather.JenisPekerjaan.Pengecatan,
                    TanggalMulai = DateOnly.FromDateTime(j.TanggalMulaiRencana),
                    TanggalSelesai = DateOnly.FromDateTime(j.TanggalSelesaiRencana),
                    TanggalDikunci = DateOnly.FromDateTime(j.TanggalDikunci!.Value),
                    StatusCuaca = j.Keputusan?.Status.ToString() ?? "Belum terverifikasi",
                    AlasanCuaca = j.Keputusan?.Catatan ?? ["Keputusan lama tidak memiliki snapshot cuaca."],
                    KonfirmasiRisiko = j.PersetujuanRisiko,
                    KonfirmasiDataLama = j.KonfirmasiDataLama,
                    Catatan = j.CatatanKeputusan,
                    Keputusan = j.Keputusan,
                    SumberAmbang = j.OverrideAmbang is null ? "Asumsi generik jenis pekerjaan" : "Override pekerjaan; spesifikasi produk belum dicantumkan"
                });
            }
        }

        return new SnapshotLaporan
        {
            ProyekId = p.StableId,
            NamaProyek = p.Nama,
            Kota = p.Kota,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            ZonaWaktu = p.ZonaWaktu,
            Komponen = compList,
            Iklim = snapIklim,
            BahanTerpilih = selectedBahan,
            SimulasiHorizon = simList,
            SertakanJadwal = includeSchedule,
            JadwalTerkunci = lockedJobs,
            DibuatPadaUtc = DateTimeOffset.UtcNow,
            DibuatPadaWib = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)),
            VersiModel = Domain.KonfigurasiModel.Bawaan.VersiModel,
            Bobot = state.BobotAktif
        };
    }
}
