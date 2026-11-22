using System.Text.Json;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Services;
using Wastu.Domain.Weather;

namespace Wastu.Data;

internal static class Pemetaan
{
    internal static Bahan KeDomain(BahanEntity e) => new(
        e.Id,
        e.Nama,
        e.Jenis,
        e.KonduktivitasTermal,
        e.KetahananLembap,
        e.KetahananKorosi,
        e.UsiaPakaiNominalTahun,
        e.FaktorEmisi,
        e.SumberData,
        e.SusunanKetebalan,
        e.VersiData);

    internal static Proyek KeDomain(ProyekEntity e) => new(
        e.Id, e.Nama, e.Kota, e.Latitude, e.Longitude, e.RevisiLokasi, e.ZonaWaktu);

    internal static Komponen KeDomain(KomponenEntity e) => new(
        e.Id, e.BangunanId, e.Jenis, e.LuasM2);

    internal static ProfilIklim KeDomain(ProfilIklimEntity e)
    {
        var provenance = Enum.TryParse<ProvenanceIklim>(e.Provenance, true, out var p)
            ? p
            : ProvenanceIklim.LegacyUnknown;

        var statusPengambilan = Enum.TryParse<StatusPengambilanIklim>(e.StatusPengambilan, true, out var sp)
            ? sp
            : StatusPengambilanIklim.Tersedia;

        var metadata = new MetadataIklim(
            PeriodeAwal: e.PeriodeAwal,
            PeriodeAkhir: e.PeriodeAkhir,
            Penyedia: e.Penyedia ?? "Open-Meteo",
            Model: e.ModelIklim,
            DiambilPadaUtc: e.DiambilPada,
            ZonaWaktu: e.ZonaWaktu ?? "Asia/Jakarta",
            MetodePesisir: e.MetodePesisir ?? "Haversine Proximity",
            Provenance: provenance,
            StatusPengambilan: statusPengambilan);

        return new ProfilIklim(
            e.CurahHujanTahunanMm,
            e.KelembapanRataPct,
            e.SuhuRataC,
            e.JarakPesisirKm,
            e.TingkatKorosi,
            metadata: metadata,
            revisiLokasi: e.RevisiLokasi,
            provenance: provenance)
        {
            DiambilPada = e.DiambilPada
        };
    }

    internal static void SalinKe(ProfilIklim profil, ProfilIklimEntity target)
    {
        target.CurahHujanTahunanMm = profil.CurahHujanTahunanMm;
        target.KelembapanRataPct = profil.KelembapanRataPct;
        target.SuhuRataC = profil.SuhuRataC;
        target.JarakPesisirKm = profil.JarakPesisirKm;
        target.TingkatKorosi = profil.TingkatKorosi;
        target.DiambilPada = profil.DiambilPada ?? profil.Metadata?.DiambilPadaUtc ?? DateTimeOffset.UtcNow;
        target.RevisiLokasi = profil.RevisiLokasi;
        target.Provenance = (profil.Metadata?.Provenance ?? profil.Provenance).ToString();
        target.PeriodeAwal = profil.Metadata?.PeriodeAwal;
        target.PeriodeAkhir = profil.Metadata?.PeriodeAkhir;
        target.Penyedia = profil.Metadata?.Penyedia;
        target.ModelIklim = profil.Metadata?.Model;
        target.ZonaWaktu = profil.Metadata?.ZonaWaktu;
        target.MetodePesisir = profil.Metadata?.MetodePesisir;
        target.StatusPengambilan = (profil.Metadata?.StatusPengambilan ?? StatusPengambilanIklim.Tersedia).ToString();
    }

    internal static Evaluasi KeDomain(EvaluasiEntity e) => new(
        e.Id,
        e.BahanId,
        e.KomponenId,
        e.SkorKesesuaian,
        e.EmisiAwalKg,
        new SubSkorKesesuaian(e.SkorTermal, e.SkorLembap, e.SkorKorosi, e.SkorUsia),
        e.Alasan,
        e.BatchId,
        e.VersiModel,
        e.HashInput,
        e.DihitungPadaUtc ?? e.DihitungPada);

    internal static EvaluasiEntity KeEntity(Evaluasi e) => new()
    {
        Id = e.Id,
        KomponenId = e.KomponenId,
        BahanId = e.BahanId,
        SkorKesesuaian = e.SkorKesesuaian,
        EmisiAwalKg = e.EmisiAwalKg,
        Alasan = e.Alasan,
        SkorTermal = e.SubSkor.Termal,
        SkorLembap = e.SubSkor.Lembap,
        SkorKorosi = e.SubSkor.Korosi,
        SkorUsia = e.SubSkor.Usia,
        DihitungPada = e.DihitungPadaUtc ?? DateTimeOffset.UtcNow,
        BatchId = e.BatchId,
        VersiModel = e.VersiModel ?? "v2.0",
        HashInput = e.HashInput,
        DihitungPadaUtc = e.DihitungPadaUtc ?? DateTimeOffset.UtcNow
    };

    internal static void SalinKe(Evaluasi sumber, EvaluasiEntity target)
    {
        target.KomponenId = sumber.KomponenId;
        target.BahanId = sumber.BahanId;
        target.SkorKesesuaian = sumber.SkorKesesuaian;
        target.EmisiAwalKg = sumber.EmisiAwalKg;
        target.Alasan = sumber.Alasan;
        target.SkorTermal = sumber.SubSkor.Termal;
        target.SkorLembap = sumber.SubSkor.Lembap;
        target.SkorKorosi = sumber.SubSkor.Korosi;
        target.SkorUsia = sumber.SubSkor.Usia;
        target.BatchId = sumber.BatchId;
        target.VersiModel = sumber.VersiModel ?? target.VersiModel;
        target.HashInput = sumber.HashInput ?? target.HashInput;
        target.DihitungPadaUtc = sumber.DihitungPadaUtc ?? target.DihitungPadaUtc;
    }

    internal static SimulasiEmisi KeDomain(SimulasiEmisiEntity e) => new(
        e.EvaluasiId,
        e.HorizonTahun,
        e.UsiaPakaiEfektifTahun,
        e.JumlahPenggantian,
        e.EmisiKumulatifKg,
        e.VersiModel,
        e.FaktorPenuaanA,
        e.DihitungPadaUtc);

    internal static SimulasiEmisiEntity KeEntity(SimulasiEmisi s) => new()
    {
        Id = Guid.NewGuid(),
        EvaluasiId = s.EvaluasiId,
        HorizonTahun = s.HorizonTahun,
        UsiaPakaiEfektifTahun = s.UsiaPakaiEfektifTahun,
        JumlahPenggantian = s.JumlahPenggantian,
        EmisiKumulatifKg = s.EmisiKumulatifKg,
        VersiModel = s.VersiModel,
        FaktorPenuaanA = s.FaktorPenuaanA,
        DihitungPadaUtc = s.DihitungPadaUtc ?? DateTimeOffset.UtcNow
    };

    internal static SumberPropertiBahan KeDomain(SumberPropertiBahanEntity e)
    {
        var status = Enum.TryParse<StatusVerifikasiProperti>(e.Status, true, out var st)
            ? st
            : StatusVerifikasiProperti.Perkiraan;

        return new SumberPropertiBahan(
            e.BahanId,
            e.Properti,
            e.Referensi,
            e.Versi,
            e.Tautan,
            status,
            e.CatatanKonversi);
    }

    internal static SumberPropertiBahanEntity KeEntity(SumberPropertiBahan s) => new()
    {
        Id = Guid.NewGuid(),
        BahanId = s.BahanId,
        Properti = s.Properti,
        Referensi = s.Referensi,
        Versi = s.Versi,
        Tautan = s.Tautan,
        Status = s.Status.ToString().ToLowerInvariant(),
        CatatanKonversi = s.CatatanKonversi
    };

    internal static BatchEvaluasi KeDomain(BatchEvaluasiEntity e) => new(
        e.Id,
        e.ProyekId,
        e.KomponenId,
        e.RevisiLokasi,
        e.HashInput,
        e.VersiModel,
        new KriteriaBobot(e.BobotTermal, e.BobotLembap, e.BobotKorosi, e.BobotUsia),
        e.LuasM2,
        e.DihitungPadaUtc);

    internal static BatchEvaluasiEntity KeEntity(BatchEvaluasi b) => new()
    {
        Id = b.Id,
        ProyekId = b.ProyekId,
        KomponenId = b.KomponenId,
        RevisiLokasi = b.RevisiLokasi,
        HashInput = b.HashInput,
        VersiModel = b.VersiModel,
        BobotTermal = b.Bobot.Termal,
        BobotLembap = b.Bobot.Lembap,
        BobotKorosi = b.Bobot.Korosi,
        BobotUsia = b.Bobot.Usia,
        LuasM2 = b.LuasM2,
        DihitungPadaUtc = b.DihitungPadaUtc
    };

    internal static Pekerjaan KeDomain(PekerjaanEntity e)
    {
        AmbangPekerjaan? overrideAmbang = null;
        if (!string.IsNullOrWhiteSpace(e.OverrideAmbangJson))
        {
            try
            {
                overrideAmbang = JsonSerializer.Deserialize<AmbangPekerjaan>(e.OverrideAmbangJson);
            }
            catch
            {
                // fallback
            }
        }

        KonfirmasiPenguncian? konfirmasi = null;
        if (!string.IsNullOrWhiteSpace(e.KonfirmasiJson))
        {
            try
            {
                konfirmasi = JsonSerializer.Deserialize<KonfirmasiPenguncian>(e.KonfirmasiJson);
            }
            catch
            {
                // fallback
            }
        }

        return new Pekerjaan(
            e.Id,
            e.BangunanId,
            e.Nama,
            e.Jenis,
            e.TanggalMulai,
            e.TanggalSelesai,
            e.Status,
            e.TanggalDikunci,
            e.KomponenId,
            e.BahanId,
            overrideAmbang,
            konfirmasi,
            e.TerakhirDiperbaruiUtc);
    }

    internal static PekerjaanEntity KeEntity(Pekerjaan p) => new()
    {
        Id = p.Id,
        BangunanId = p.ProyekId,
        Nama = p.Nama,
        Jenis = p.Jenis,
        TanggalMulai = p.TanggalMulai,
        TanggalSelesai = p.TanggalSelesai,
        Status = p.Status,
        TanggalDikunci = p.TanggalDikunci,
        KomponenId = p.KomponenId,
        BahanId = p.BahanId,
        RevisiLokasiSaatKunci = null,
        OverrideAmbangJson = p.OverrideAmbang is not null ? JsonSerializer.Serialize(p.OverrideAmbang) : null,
        KonfirmasiJson = p.Konfirmasi is not null ? JsonSerializer.Serialize(p.Konfirmasi) : null,
        TerakhirDiperbaruiUtc = p.TerakhirDiperbaruiUtc
    };

    internal static AmbangPekerjaan KeDomain(AmbangPekerjaanEntity e) => new(
        e.ProbabilitasHujanMaksPct,
        e.CurahHujanMaksMm,
        e.KelembapanMaksPct,
        e.SuhuMinC,
        e.SuhuMaksC,
        e.TingkatSuhu);

    internal static KonfigurasiModel KeDomain(KonfigurasiModelEntity e) => new(
        e.VersiModel,
        e.BobotLembapHujanAlpha,
        e.AcuanCurahHujanMm,
        e.AcuanJarakPesisirKm,
        e.PitaTermalBatasBawah,
        e.PitaTermalBatasAtasSejuk,
        e.PitaTermalBatasAtasPanas,
        e.SuhuSejukC,
        e.SuhuPanasC,
        e.FaktorPenuaanA,
        e.AmbangTidakDisarankan,
        e.AmbangCocok);

    internal static KonfigurasiModelEntity KeEntity(KonfigurasiModel k, bool aktif = true) => new()
    {
        VersiModel = k.VersiModel,
        BobotLembapHujanAlpha = k.BobotLembapHujanAlpha,
        AcuanCurahHujanMm = k.AcuanCurahHujanMm,
        AcuanJarakPesisirKm = k.AcuanJarakPesisirKm,
        PitaTermalBatasBawah = k.PitaTermalBatasBawah,
        PitaTermalBatasAtasSejuk = k.PitaTermalBatasAtasSejuk,
        PitaTermalBatasAtasPanas = k.PitaTermalBatasAtasPanas,
        SuhuSejukC = k.SuhuSejukC,
        SuhuPanasC = k.SuhuPanasC,
        FaktorPenuaanA = k.FaktorPenuaanA,
        AmbangTidakDisarankan = k.AmbangTidakDisarankan,
        AmbangCocok = k.AmbangCocok,
        Aktif = aktif,
        DibuatPadaUtc = DateTimeOffset.UtcNow
    };

    internal static Laporan KeDomain(LaporanEntity e) => new(
        e.Id,
        e.BangunanId,
        e.PathBerkas,
        e.DibuatPadaUtc,
        e.ChecksumSha256,
        e.HashSnapshot,
        e.UkuranByte,
        e.StatusBerkas);

    internal static LaporanEntity KeEntity(Laporan l, string? opsiJson = null) => new()
    {
        Id = l.Id,
        BangunanId = l.ProyekId,
        PathBerkas = l.PathBerkas,
        DibuatPadaUtc = l.DibuatPada,
        HashSnapshot = l.SnapshotHash,
        StatusBerkas = l.Status,
        ChecksumSha256 = l.ChecksumSha256,
        UkuranByte = l.UkuranByte,
        OpsiJson = opsiJson
    };
}
