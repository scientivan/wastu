using Wastu.Data.Entities;
using Wastu.Domain;

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
        e.SumberData);

    internal static Proyek KeDomain(ProyekEntity e) => new(
        e.Id, e.Nama, e.Kota, e.Latitude, e.Longitude);

    internal static Komponen KeDomain(KomponenEntity e) => new(
        e.Id, e.BangunanId, e.Jenis, e.LuasM2);

    internal static ProfilIklim KeDomain(ProfilIklimEntity e) => new(
        e.CurahHujanTahunanMm,
        e.KelembapanRataPct,
        e.SuhuRataC,
        e.JarakPesisirKm,
        e.TingkatKorosi) { DiambilPada = e.DiambilPada };

    internal static Evaluasi KeDomain(EvaluasiEntity e) => new(
        e.Id,
        e.BahanId,
        e.KomponenId,
        e.SkorKesesuaian,
        e.EmisiAwalKg,
        new SubSkorKesesuaian(e.SkorTermal, e.SkorLembap, e.SkorKorosi, e.SkorUsia),
        e.Alasan);

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
        SkorUsia = e.SubSkor.Usia
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
    }

    internal static SimulasiEmisi KeDomain(SimulasiEmisiEntity e) => new(
        e.EvaluasiId,
        e.HorizonTahun,
        e.UsiaPakaiEfektifTahun,
        e.JumlahPenggantian,
        e.EmisiKumulatifKg);

    internal static SimulasiEmisiEntity KeEntity(SimulasiEmisi s) => new()
    {
        EvaluasiId = s.EvaluasiId,
        HorizonTahun = s.HorizonTahun,
        UsiaPakaiEfektifTahun = s.UsiaPakaiEfektifTahun,
        JumlahPenggantian = s.JumlahPenggantian,
        EmisiKumulatifKg = s.EmisiKumulatifKg
    };
}
