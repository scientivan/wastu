namespace Wastu.Domain;

/// <summary>Masukan asli satu hasil; tidak dibaca ulang dari katalog ketika meninjau riwayat.</summary>
public sealed record InputEvaluasi(
    Komponen Komponen,
    Bahan Bahan,
    ProfilIklim Iklim,
    KriteriaBobot Bobot,
    KonfigurasiModel Konfigurasi,
    double UsiaKategoriMinimum,
    double UsiaKategoriMaksimum,
    Proyek? Proyek = null,
    IReadOnlyList<Guid>? BahanKategori = null,
    int SchemaVersion = 1);
