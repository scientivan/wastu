using Wastu.Domain.Abstractions;
using Wastu.Domain.Services;

namespace Wastu.Domain.Scoring;

/// <summary>
/// Menyusun peringkat bahan untuk satu komponen beserta skor kesesuaian dan emisi awalnya
/// (PRD §F4, §11.1, §11.2).
/// </summary>
/// <remarks>
/// Seluruh perhitungan ada di <see cref="PerhitunganSkor"/>; kelas ini hanya
/// mengorkestrasi: mengambil data, menghitung, mengurutkan, dan menyimpan.
/// Pemisahan inilah yang membuat aturan domain bisa diuji tanpa database.
/// </remarks>
public sealed class SuitabilityScoringService : ISuitabilityScoringService
{
    private readonly IMaterialRepository _material;
    private readonly IProyekRepository _proyek;

    public SuitabilityScoringService(IMaterialRepository material, IProyekRepository proyek)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(proyek);

        _material = material;
        _proyek = proyek;
    }

    public async Task<IReadOnlyList<Evaluasi>> RankAsync(
        Guid komponenId,
        KriteriaBobot? bobot = null,
        CancellationToken ct = default)
    {
        Komponen komponen = await _proyek.GetKomponenAsync(komponenId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Komponen {komponenId} tidak ditemukan.");

        ProfilIklim iklim = await _proyek.GetProfilIklimAsync(komponen.ProyekId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Proyek {komponen.ProyekId} belum memiliki profil iklim. Jalankan analisis iklim lebih dulu (PRD §F3).");

        IReadOnlyList<Bahan> bahan = await _material.ListByKindAsync(komponen.Jenis, ct).ConfigureAwait(false);

        KriteriaBobot bobotEfektif = bobot ?? KriteriaBobot.Default;

        var hasil = new List<Evaluasi>(bahan.Count);

        foreach (Bahan item in bahan)
        {
            SubSkorKesesuaian subSkor = PerhitunganSkor.HitungSubSkor(item, iklim);

            hasil.Add(new Evaluasi(
                BahanId: item.Id,
                KomponenId: komponen.Id,
                SkorKesesuaian: PerhitunganSkor.SkorKesesuaian(subSkor, bobotEfektif),
                EmisiAwalKg: PerhitunganSkor.EmisiAwal(item, komponen),
                SubSkor: subSkor,
                Alasan: PerhitunganSkor.SusunAlasan(item, iklim, subSkor)));
        }

        // Peringkat dari skor tertinggi; kunci kedua memakai emisi awal agar bahan dengan
        // skor sama tidak bergantian urutannya tiap kali pemanggilan diulang.
        hasil.Sort((a, b) =>
        {
            int urutSkor = b.SkorKesesuaian.CompareTo(a.SkorKesesuaian);
            return urutSkor != 0 ? urutSkor : a.EmisiAwalKg.CompareTo(b.EmisiAwalKg);
        });

        return hasil;
    }
}