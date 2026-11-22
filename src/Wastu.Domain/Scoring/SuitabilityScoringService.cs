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
    private readonly IBatchEvaluasiRepository? _batch;
    private readonly IEvaluasiRepository? _evaluasi;
    private readonly KonfigurasiModel _konfigurasi;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Membuat layanan penilaian. Parameter batch/evaluasi/konfigurasi/clock opsional
    /// agar konstruktor dua argumen yang sudah dipakai UI dan tes tetap bekerja.
    /// </summary>
    public SuitabilityScoringService(
        IMaterialRepository material,
        IProyekRepository proyek,
        IBatchEvaluasiRepository? batch = null,
        IEvaluasiRepository? evaluasi = null,
        KonfigurasiModel? konfigurasi = null,
        TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(proyek);

        _material = material;
        _proyek = proyek;
        _batch = batch;
        _evaluasi = evaluasi;
        _konfigurasi = konfigurasi ?? KonfigurasiModel.Bawaan;
        PerhitunganSkor.PastikanKonfigurasiValid(_konfigurasi);
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<Evaluasi>> RankAsync(
        Guid komponenId,
        KriteriaBobot? bobot = null,
        CancellationToken ct = default)
    {
        KriteriaBobot bobotEfektif = bobot ?? KriteriaBobot.Default;
        bobotEfektif.PastikanValid();

        Komponen komponen = await _proyek.GetKomponenAsync(komponenId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Komponen {komponenId} tidak ditemukan.");

        ProfilIklim iklim = await _proyek.GetProfilIklimAsync(komponen.ProyekId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Proyek {komponen.ProyekId} belum memiliki profil iklim. Jalankan analisis iklim lebih dulu (PRD §F3).");

        IReadOnlyList<Bahan> bahan = await _material.ListByKindAsync(komponen.Jenis, ct).ConfigureAwait(false);
        Bahan[] kategori = bahan.Where(item => item is not null && item.Jenis == komponen.Jenis).ToArray();

        if (kategori.Length == 0)
        {
            return [];
        }

        PerhitunganSkor.PastikanKomponenValid(komponen);
        PerhitunganSkor.PastikanIklimValid(iklim);

        string hash = HitungHash(komponen, iklim, kategori, bobotEfektif, _konfigurasi);

        if (_batch is not null && _evaluasi is not null)
        {
            BatchEvaluasi? tersimpan = await CariBatchAsync(komponen, hash, ct).ConfigureAwait(false);
            if (tersimpan is not null)
            {
                IReadOnlyList<Evaluasi> lama = await _evaluasi.ListByKomponenAsync(komponen.Id, ct)
                    .ConfigureAwait(false);

                Evaluasi[] dipakai = lama
                    .Where(item => item.BatchId == tersimpan.Id
                        || (string.Equals(item.HashInput, hash, StringComparison.Ordinal)
                            && string.Equals(item.VersiModel, _konfigurasi.VersiModel, StringComparison.Ordinal)))
                    .GroupBy(item => item.BahanId)
                    .Select(grup => grup
                        .OrderByDescending(item => item.DihitungPadaUtc ?? DateTimeOffset.MinValue)
                        .First())
                    .ToArray();

                if (dipakai.Length == kategori.Length)
                {
                    return Urutkan(dipakai, kategori);
                }
            }

            IReadOnlyList<Evaluasi> baru = Rank(komponen, iklim, kategori, bobotEfektif, _konfigurasi);
            DateTimeOffset dihitungPada = _clock.GetUtcNow();
            var batchBaru = new BatchEvaluasi(
                Guid.NewGuid(),
                komponen.ProyekId,
                komponen.Id,
                iklim.RevisiLokasi,
                hash,
                _konfigurasi.VersiModel,
                bobotEfektif,
                komponen.LuasM2,
                dihitungPada);

            Evaluasi[] terlampir = baru
                .Select(item => item with
                {
                    BatchId = batchBaru.Id,
                    VersiModel = batchBaru.VersiModel,
                    HashInput = batchBaru.HashInput,
                    DihitungPadaUtc = batchBaru.DihitungPadaUtc
                })
                .ToArray();

            await _batch.SimpanBatchAsync(batchBaru, terlampir, ct).ConfigureAwait(false);
            return terlampir;
        }

        return Rank(komponen, iklim, kategori, bobotEfektif, _konfigurasi);
    }

    /// <summary>
    /// Menyusun peringkat tanpa I/O. Bobot dan seluruh masukan divalidasi ulang agar
    /// nilai yang lolos lewat <c>with</c> tidak pernah menghasilkan skor tidak sah.
    /// </summary>
    public static IReadOnlyList<Evaluasi> Rank(
        Komponen komponen,
        ProfilIklim iklim,
        IReadOnlyList<Bahan> katalog,
        KriteriaBobot? bobot = null,
        KonfigurasiModel? konfigurasi = null)
    {
        ArgumentNullException.ThrowIfNull(komponen);
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(katalog);

        PerhitunganSkor.PastikanKomponenValid(komponen);
        PerhitunganSkor.PastikanIklimValid(iklim);

        KriteriaBobot bobotEfektif = bobot ?? KriteriaBobot.Default;
        bobotEfektif.PastikanValid();
        KonfigurasiModel konfigurasiEfektif = konfigurasi ?? KonfigurasiModel.Bawaan;

        Bahan[] bahan = katalog
            .Where(item => item is not null && item.Jenis == komponen.Jenis)
            .ToArray();

        if (bahan.Length == 0)
        {
            return [];
        }

        // Normalisasi usia memakai seluruh kategori valid, bukan bahan yang kebetulan
        // lolos filter tampilan atau sedang dipilih pengguna (PRD §9.2).
        double usiaMin = bahan.Min(item => item.UsiaPakaiNominalTahun);
        double usiaMaks = bahan.Max(item => item.UsiaPakaiNominalTahun);

        var hasil = new List<Evaluasi>(bahan.Length);

        foreach (Bahan item in bahan)
        {
            PerhitunganSkor.PastikanBahanValid(item);

            SubSkorKesesuaian subSkor = PerhitunganSkor.HitungSubSkor(
                item, iklim, usiaMin, usiaMaks, konfigurasiEfektif);

            hasil.Add(new Evaluasi(
                Id: Guid.NewGuid(),
                BahanId: item.Id,
                KomponenId: komponen.Id,
                SkorKesesuaian: PerhitunganSkor.SkorKesesuaian(subSkor, bobotEfektif),
                EmisiAwalKg: PerhitunganSkor.EmisiAwal(item, komponen),
                SubSkor: subSkor,
                Alasan: PerhitunganSkor.SusunAlasan(item, iklim, subSkor, konfigurasiEfektif)));
        }

        return Urutkan(hasil, bahan);
    }

    private async Task<BatchEvaluasi?> CariBatchAsync(Komponen komponen, string hash, CancellationToken ct)
    {
        BatchEvaluasi? kandidat = await _batch!
            .GetByHashAsync(komponen.Id, hash, _konfigurasi.VersiModel, ct)
            .ConfigureAwait(false);

        if (kandidat is not null)
        {
            return kandidat;
        }

        // Fallback untuk adapter yang belum menimpa GetByHashAsync: cari seluruh
        // batch proyek agar input identik yang lebih lama tetap dipakai ulang.
        IReadOnlyList<BatchEvaluasi> semua = await _batch
            .ListBatchesByProyekAsync(komponen.ProyekId, ct)
            .ConfigureAwait(false);

        return semua.FirstOrDefault(item =>
            item.KomponenId == komponen.Id
            && string.Equals(item.HashInput, hash, StringComparison.Ordinal)
            && string.Equals(item.VersiModel, _konfigurasi.VersiModel, StringComparison.Ordinal));
    }

    /// <summary>
    /// Urutan deterministik memakai nilai penuh: skor turun, emisi naik, lalu nama
    /// dan ID bahan sebagai tie-break stabil (PRD-R10, F4-01).
    /// </summary>
    private static IReadOnlyList<Evaluasi> Urutkan(IEnumerable<Evaluasi> hasil, IReadOnlyList<Bahan> kategori)
    {
        var nama = new Dictionary<Guid, string>();
        foreach (Bahan item in kategori)
        {
            nama[item.Id] = item.Nama;
        }

        Evaluasi[] daftar = hasil.ToArray();
        Array.Sort(daftar, (kiri, kanan) => Bandingkan(kiri, kanan, nama));
        return daftar;
    }

    private static int Bandingkan(Evaluasi kiri, Evaluasi kanan, IReadOnlyDictionary<Guid, string> nama)
    {
        int urutSkor = kanan.SkorKesesuaian.CompareTo(kiri.SkorKesesuaian);
        if (urutSkor != 0)
        {
            return urutSkor;
        }

        int urutEmisi = kiri.EmisiAwalKg.CompareTo(kanan.EmisiAwalKg);
        if (urutEmisi != 0)
        {
            return urutEmisi;
        }

        string namaKiri = nama.GetValueOrDefault(kiri.BahanId, string.Empty);
        string namaKanan = nama.GetValueOrDefault(kanan.BahanId, string.Empty);
        int urutNama = string.Compare(namaKiri, namaKanan, StringComparison.OrdinalIgnoreCase);
        if (urutNama != 0)
        {
            return urutNama;
        }

        return kiri.BahanId.CompareTo(kanan.BahanId);
    }

    /// <summary>
    /// Hash kanonikal mengikat luas, iklim, seluruh bahan kategori beserta propertinya,
    /// bobot, dan konfigurasi model (PRD §4.3). Perubahan salah satunya wajib
    /// menghasilkan batch baru, bukan menimpa hasil lama.
    /// </summary>
    private static string HitungHash(
        Komponen komponen,
        ProfilIklim iklim,
        IReadOnlyList<Bahan> kategori,
        KriteriaBobot bobot,
        KonfigurasiModel konfigurasi)
    {
        string identitasKatalog = string.Join("|", kategori
            .OrderBy(item => item.Id)
            .Select(item => FormattableString.Invariant(
                $"{item.Id:N}:{item.VersiData}:{item.Nama}:{(int)item.Jenis}:{item.KonduktivitasTermal:R}:{item.KetahananLembap}:{item.KetahananKorosi}:{item.UsiaPakaiNominalTahun:R}:{item.FaktorEmisi:R}")));

        return HashInputEvaluasi.Hitung(komponen.LuasM2, iklim, bobot, konfigurasi, identitasKatalog);
    }
}
