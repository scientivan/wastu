using Wastu.Data.Entities;
using Wastu.Domain;

namespace Wastu.Data;

internal static class KatalogSeed
{
    // Nilai sama dengan database/03_seed_supabase.sql dan Wastu.InMemory/KatalogDemo.
    private const string Ice =
        "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)";

    private const string Epd =
        "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)";

    internal static IReadOnlyList<BahanEntity> Bahan { get; } =
    [
        Dinding("000000000001", "Beton bertulang (in-situ)", 1.40, 4, 5, 50, 120, Ice),
        Dinding("000000000002", "Beton ringan (hebel autoclave)", 0.12, 5, 3, 30, 60, Ice),
        Dinding("000000000003", "Beton polibeton (hebel)", 0.10, 4, 3, 25, 55, Ice),
        Dinding("000000000004", "Bata merah + plester aci", 0.70, 3, 4, 30, 65, Ice),
        Dinding("000000000005", "Bata merah tanpa plester", 0.70, 2, 4, 20, 35, Ice),
        Dinding("000000000006", "Bata beton (concrete block)", 0.60, 4, 4, 40, 55, Ice),
        Dinding("000000000007", "Bata silikat berpori", 0.11, 5, 3, 30, 62, Epd),
        Dinding("000000000008", "Bata ringan honeycomb (AHCC)", 0.09, 4, 3, 30, 50, Epd),
        Dinding("000000000009", "Panel gipsum (dry wall)", 0.25, 1, 1, 20, 28, Ice),
        Dinding("000000000010", "Papan kayu + pelindung", 0.15, 1, 1, 15, 32, Ice),
        Dinding("000000000011", "Panel ACP (aluminium composite)", 0.20, 5, 5, 25, 190, Epd),
        Dinding("000000000012", "Panel EPS + plester", 0.04, 4, 4, 25, 70, Epd),
        Dinding("000000000013", "Semen fiber reinforced board", 0.25, 5, 5, 30, 75, Epd),
        Dinding("000000000014", "Batu andesit (dinding batu alam)", 3, 5, 5, 60, 45, Ice),
        Dinding("000000000015", "Beton geopolimer", 0.45, 4, 4, 45, 90, Epd),
        Atap("000000000016", "Dak beton bertulang", 1.40, 5, 5, 40, 150, Ice),
        Atap("000000000017", "Genting beton", 0.40, 5, 5, 30, 95, Ice),
        Atap("000000000018", "Genting keramik", 0.30, 5, 5, 35, 110, Ice),
        Atap("000000000019", "Genting tanah liat", 0.60, 4, 4, 30, 85, Ice),
        Atap("000000000020", "Genteng aspal (shingle)", 0.08, 3, 2, 20, 80, Epd),
        Atap("000000000021", "Baja galvanis (trapezium)", 0.05, 3, 2, 15, 90, Ice),
        Atap("000000000022", "Baja AZ / galvalum", 0.05, 4, 4, 20, 105, Epd),
        Atap("000000000023", "Seng (zink)", 0.11, 2, 1, 12, 130, Ice),
        Atap("000000000024", "Constantra / fiber cement", 0.07, 4, 3, 25, 100, Epd),
        Atap("000000000025", "Panel sandwich (inti EPS)", 0.04, 4, 4, 25, 95, Epd),
    ];

    private static BahanEntity Dinding(
        string idAkhir, string nama, double konduktivitas, int lembap, int korosi,
        double usia, double emisi, string sumber)
        => Buat(idAkhir, nama, JenisKomponen.Dinding, konduktivitas, lembap, korosi, usia, emisi, sumber);

    private static BahanEntity Atap(
        string idAkhir, string nama, double konduktivitas, int lembap, int korosi,
        double usia, double emisi, string sumber)
        => Buat(idAkhir, nama, JenisKomponen.Atap, konduktivitas, lembap, korosi, usia, emisi, sumber);

    private static BahanEntity Buat(
        string idAkhir, string nama, JenisKomponen jenis, double konduktivitas, int lembap,
        int korosi, double usia, double emisi, string sumber)
        => new()
        {
            Id = new Guid($"d0000000-0000-4000-8000-{idAkhir}"),
            Nama = nama,
            Jenis = jenis,
            KonduktivitasTermal = konduktivitas,
            KetahananLembap = lembap,
            KetahananKorosi = korosi,
            UsiaPakaiNominalTahun = usia,
            FaktorEmisi = emisi,
            SumberData = sumber
        };
}
