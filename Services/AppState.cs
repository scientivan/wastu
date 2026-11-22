using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wastu.Data;
using Wastu.Data.Snapshots;
using Wastu.Domain;
using Wastu.Domain.Climate;
using Wastu.Domain.Scoring;
using Wastu.Domain.Services;
using Wastu.Models;
using JenisKomponen = Wastu.Models.JenisKomponen;
using ProfilIklim = Wastu.Models.ProfilIklim;
using Bangunan = Wastu.Models.Bangunan;
using Komponen = Wastu.Models.Komponen;
using Bahan = Wastu.Models.Bahan;
using Evaluasi = Wastu.Models.Evaluasi;

namespace Wastu.Services;

// State tunggal aplikasi. Model domain menjadi sumber data; DTO hanya dipakai saat menyimpan.
public sealed partial class AppState
{
    private readonly object _gate = new();
    private StateDto? _antrean;
    private bool _menulis;
    private PostgreSqlBackend? _backend;
    // Satu instance toko per profil selama aplikasi hidup (kontrak SnapshotStore).
    private SnapshotStore<StateDto>? _toko;
    // Hanya di memori; tidak pernah masuk StateDto, nama berkas, pesan UI, atau log.
    private string? _koneksi;
    private IReadOnlyList<ItemKatalog> _katalog = Katalog.Semua;
    internal Domain.Abstractions.IHistoricalClimateClient HistoricalClient { get; set; } = new OpenMeteo();
    public event Action? Changed;
    private CancellationTokenSource? _climateFetchCts;

    public List<Bangunan> Proyek { get; } = [];
    public int ProyekAktifId { get; set; }
    public JenisKomponen Jenis { get; set; } = JenisKomponen.Dinding;
    public string[] Dipilih { get; set; } = ["m0", "m1"];
    public int Horizon { get; set; } = 30;
    public string Cari { get; set; } = "";
    public JenisPekerjaan JenisKerja { get; set; } = JenisPekerjaan.Pengecatan;
    public List<PrakiraanCuaca> Prakiraan { get; set; } = [];
    public string PrakiraanDiperbarui { get; set; } = "";
    public int? PrakiraanProyekId { get; set; }
    public DateTime? HariDipilih { get; set; }

    // Naik setiap kali state berubah; halaman yang sudah dirender pada revisi yang sama tidak dibangun ulang.
    public int Revisi { get; private set; }

    public bool AdaProyek => Proyek.Count > 0;
    public bool PostgreSql => _backend is not null;

    // ===== Status mode (WP01 §6.2) =====
    /// <summary>True bila data dibuka dari snapshot offline database (luring baca-saja).</summary>
    public bool ModeBacaSaja { get; private set; }

    /// <summary>True bila database gagal dan tidak ada snapshot yang dapat digunakan.</summary>
    public bool ButuhPemulihan { get; private set; }

    /// <summary>Status snapshot yang mendasari keputusan startup, bila ada.</summary>
    public SnapshotStatus? StatusSnapshot { get; private set; }

    public bool DipulihkanDariCadangan { get; private set; }

    /// <summary>Waktu pengambilan data asli snapshot (UTC), bukan waktu pembacaan berkas.</summary>
    public DateTimeOffset? SnapshotDiambilPadaUtc { get; private set; }

    /// <summary>Ringkasan status koneksi untuk UI; teks aman tanpa kredensial atau detail mentah.</summary>
    public string? PesanStatusKoneksi { get; private set; }

    /// <summary>
    /// Label penyimpanan untuk UI: PostgreSQL (baca-tulis), Luring (Baca-Saja), atau Lokal (JSON).
    /// Mode JSON lokal tidak pernah dinyatakan tersimpan di PostgreSQL.
    /// </summary>
    public string LabelModePenyimpanan => ModeBacaSaja || ButuhPemulihan ? "Luring (Baca-Saja)"
        : PostgreSql ? "PostgreSQL"
        : "Lokal (JSON)";

    private bool TulisanDitolak => ModeBacaSaja || ButuhPemulihan;

    private const string PesanTulisDitolak =
        "Mode luring baca-saja: perubahan tidak dapat disimpan karena database sedang tidak tersedia.";

    // Penolakan untuk operasi baca-tulis; pesan tidak memuat kredensial atau detail koneksi.
    private void TolakTulis()
    {
        if (TulisanDitolak)
            throw new InvalidOperationException(PesanTulisDitolak);
    }
    public Bangunan Aktif => Proyek.Find(p => p.Id == ProyekAktifId) ?? (Proyek.Count > 0 ? Proyek[0] : null!);
    public ProfilIklim Iklim
    {
        get
        {
            if (Aktif is null) return Katalog.ProfilBawaan("Jakarta", 30);
            return Aktif.ProfilIklim ??= Katalog.ProfilBawaan(Aktif.Kota ?? "Jakarta", 30);
        }
    }
    public double Luas => Aktif?.LuasKomponen(Jenis) ?? 0;

    public IReadOnlyList<ItemKatalog> KatalogAktif => _katalog;

    public void EnsureJenisValid()
    {
        if (!AdaProyek || Aktif is null) return;
        if (!Aktif.AdaKomponen(Jenis))
        {
            var aktif = Aktif.JenisKomponenAktif;
            if (aktif.Count > 0)
            {
                Jenis = aktif[0];
                Dipilih = Peringkat().Take(2).Select(e => Item(e.Bahan).Kode).ToArray();
            }
        }
    }

    public async Task<bool> SegarkanKatalogAsync(CancellationToken ct = default)
    {
        try
        {
            if (ModeBacaSaja) return false;
            var be = _backend;
            if (be is not null)
            {
                var dinding = await be.Material.ListByKindAsync(Domain.JenisKomponen.Dinding, ct).ConfigureAwait(false);
                var atap = await be.Material.ListByKindAsync(Domain.JenisKomponen.Atap, ct).ConfigureAwait(false);
                var gabung = dinding.Concat(atap).Select(ItemKatalog.DariDomain).ToArray();
                if (gabung.Length > 0)
                {
                    _katalog = gabung;
                    Ubah();
                    return true;
                }
            }
            else
            {
                _katalog = Katalog.Semua;
                Ubah();
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public ItemKatalog Item(Bahan bahan) => _katalog[bahan.Id];

    public Evaluasi Nilai(ItemKatalog item) => Peringkat(item.Jenis).First(e => e.Bahan == item.Bahan);

    public Evaluasi Simulasikan(ItemKatalog item)
    {
        var evaluasi = Nilai(item);
        evaluasi.ProyeksikanEmisi(Horizon, Aktif.FaktorPenuaanA);
        return evaluasi;
    }

    public KriteriaBobot BobotAktif => Aktif is { GunakanBobotKustom: true } p ? p.Bobot : KriteriaBobot.Default;

    public void AturModeBobot(bool kustom)
    {
        TolakTulis();
        if (Aktif is null) return;
        Aktif.GunakanBobotKustom = kustom;
        if (!kustom)
        {
            Aktif.Bobot = KriteriaBobot.Default;
        }
        Ubah();
    }

    public string? AturBobotPersentase(double termalPct, double lembapPct, double korosiPct, double usiaPct)
    {
        TolakTulis();
        if (Aktif is null) return "Tidak ada proyek aktif.";

        KriteriaBobot bobotBaru;
        try
        {
            bobotBaru = KriteriaBobot.DariPersentase(termalPct, lembapPct, korosiPct, usiaPct);
        }
        catch (ArgumentOutOfRangeException)
        {
            return "Isi keempat bobot dengan angka tidak negatif dan total tepat 100%.";
        }
        Aktif.Bobot = bobotBaru;
        Aktif.GunakanBobotKustom = true;
        Ubah();
        return null;
    }

    public void ResetBobotAktif()
    {
        TolakTulis();
        if (Aktif is null) return;
        Aktif.Bobot = KriteriaBobot.Default;
        Aktif.GunakanBobotKustom = false;
        Ubah();
    }

    private sealed class PeringkatTersimpan(string hash, Evaluasi[] hasil)
    {
        public string Hash { get; } = hash;
        public Evaluasi[] Hasil { get; } = hasil;
        public Task<IReadOnlyList<Domain.Evaluasi>>? Penyimpanan { get; set; }
    }

    private readonly Dictionary<Guid, PeringkatTersimpan> _peringkat = [];
    private readonly SuitabilityScoringService? _skoring;

    public AppState() { }

    internal AppState(SuitabilityScoringService skoring) => _skoring = skoring;

    public Evaluasi[] Peringkat() => Peringkat(Jenis);

    public Evaluasi[] Peringkat(JenisKomponen jenis)
    {
        if (!AdaProyek || Aktif is null || Aktif.MasalahData is not null) return [];
        var komp = Aktif.Komponen.FirstOrDefault(k => k.Jenis == jenis);
        if (komp is null) return [];
        double luas = komp.Luas;
        if (!double.IsFinite(luas) || luas <= 0)
            return [];

        var domainKomponen = new Domain.Komponen(komp.Id, Aktif.StableId, (Domain.JenisKomponen)jenis, luas);
        var bahanDict = _katalog.Where(item => item.Jenis == jenis).ToDictionary(item => item.DataDomain.Id);
        var bahanList = bahanDict.Values.Select(item => item.DataDomain).ToArray();
        if (bahanList.Length == 0) return [];
        var bobot = BobotAktif;
        var iklim = Iklim.KeDomain(Aktif.ZonaWaktu);
        string hash = SuitabilityScoringService.HitungHash(domainKomponen, iklim, bahanList, bobot);

        if (!_peringkat.TryGetValue(komp.Id, out var cache) || cache.Hash != hash)
        {
            var hasil = SuitabilityScoringService.Rank(domainKomponen, iklim, bahanList, bobot,
                    proyek: WorkflowMapper.KeDomain(Aktif))
                .Select(e => new Evaluasi(bahanDict[e.BahanId].Bahan, e)).ToArray();
            cache = new PeringkatTersimpan(hash, hasil);
            _peringkat[komp.Id] = cache;
        }

        var skoring = _skoring ?? _backend?.SuitabilityScoring;
        if (skoring is not null && !TulisanDitolak
            && Aktif.DatabaseId is { } dbId && dbId != Guid.Empty
            && komp.DatabaseId is { } kompDbId && kompDbId != Guid.Empty
            && (cache.Penyimpanan is null || cache.Penyimpanan.IsFaulted || cache.Penyimpanan.IsCanceled))
        {
            // Capture the exact displayed inputs before any asynchronous work or project switch.
            cache.Penyimpanan = skoring.RankSnapshotAsync(domainKomponen, iklim, bahanList, bobot,
                proyek: WorkflowMapper.KeDomain(Aktif));
            _ = AmatiPenyimpananAsync(cache.Penyimpanan);
        }

        return cache.Hasil;
    }

    private static async Task AmatiPenyimpananAsync(Task penyimpanan)
    {
        try
        {
            await penyimpanan.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The faulted task remains retryable on the next render; never mark a failure as saved.
            Debug.WriteLine("Gagal menyimpan evaluasi batch: " + ex.GetType().Name);
        }
    }

    public List<ItemKatalog> Terpilih()
    {
        if (Aktif?.MasalahData is not null) return [];
        if (Luas <= 0) return [];
        var hasil = Dipilih.Select(kode => _katalog.FirstOrDefault(item => item.Kode == kode && item.Jenis == Jenis))
            .OfType<ItemKatalog>().Distinct().Take(2).ToList();
        return hasil.Count > 0 ? hasil : Peringkat().Take(2).Select(e => Item(e.Bahan)).ToList();
    }

    public void AturJenis(JenisKomponen jenis)
    {
        if (Jenis == jenis) return;
        if (AdaProyek && Aktif is not null && !Aktif.AdaKomponen(jenis)) return;
        Jenis = jenis;
        Dipilih = Peringkat().Take(2).Select(e => Item(e.Bahan).Kode).ToArray();
    }

    public void PilihUntukBanding(string kode)
    {
        var lain = Peringkat().Select(e => Item(e.Bahan).Kode).FirstOrDefault(k => k != kode);
        Dipilih = lain is null ? [kode] : [kode, lain];
    }

    public List<PrakiraanCuaca> PrakiraanAktif()
    {
        if (!AdaProyek || Aktif is null || PrakiraanProyekId != Aktif.Id) return [];
        var hariIni = Domain.Weather.KebijakanCachePrakiraan.HariIniPadaZona(DateTimeOffset.UtcNow, Aktif.ZonaWaktu).ToDateTime(TimeOnly.MinValue);
        return Prakiraan.FindAll(d => d.Tanggal >= hariIni);
    }

    public void ResetPrakiraan()
    {
        Prakiraan = [];
        PrakiraanProyekId = null;
        HariDipilih = null;
    }

    public void PilihProyek(int id)
    {
        var old = Interlocked.Exchange(ref _climateFetchCts, null);
        try
        {
            old?.Cancel();
        }
        catch (ObjectDisposedException) { }

        ProyekAktifId = id;
        EnsureJenisValid();
        ResetPrakiraan();
        PulihkanPrakiraanAktif();
    }

    public Task<string?> TambahProyekAsync(string nama, string kota, double lintang, double bujur,
        double dinding, double atap, double pesisir)
        => TambahProyekAsync(nama, kota, lintang, bujur,
            [(JenisKomponen.Dinding, dinding), (JenisKomponen.Atap, atap)],
            pesisir);

    public async Task<string?> TambahProyekAsync(string nama, string kota, double lintang, double bujur,
        IReadOnlyList<(JenisKomponen Jenis, double Luas)> komponen, double? pesisirManual = null)
    {
        TolakTulis();
        ValidasiInputProyek(nama, kota, lintang, bujur, komponen, pesisirManual);
        double pesisir = pesisirManual ?? JarakPesisir.CobaHitungKm(lintang, bujur) ?? 0;

        Bangunan lokal;
        if (_backend is null)
        {
            lokal = TambahProyek(nama, kota, lintang, bujur, komponen, pesisir);
        }
        else
        {
            var domainKomponen = komponen.Select(k => new Domain.InputKomponen((Domain.JenisKomponen)k.Jenis, k.Luas)).ToArray();
            var data = await _backend.Proyek.CreateProjectAsync(nama, kota, lintang, bujur, domainKomponen);
            lokal = TambahProyek(nama, kota, lintang, bujur, komponen, pesisir, data.Id);
            var savedComponents = await _backend.Repository.ListKomponenAsync(data.Id);
            foreach (var k in lokal.Komponen.ToArray()) lokal.HapusKomponen(k.Jenis);
            foreach (var k in savedComponents) lokal.TambahKomponen(new Komponen
            { Id = k.Id, DatabaseId = k.Id, Jenis = (JenisKomponen)k.Jenis, Luas = k.LuasM2 });
        }

        Ubah();

        // Trigger automatic async climate fetch in background in ALL writable modes (local & PG)
        _ = Task.Run(async () =>
        {
            await PerbaruiIklimAsync(lokal);
        });

        return null;
    }

    public Bangunan TambahProyek(string nama, string kota, double lintang, double bujur, double dinding, double atap,
        double pesisir, Guid? databaseId = null)
        => TambahProyek(nama, kota, lintang, bujur,
            [(JenisKomponen.Dinding, dinding), (JenisKomponen.Atap, atap)],
            pesisir, databaseId);

    public Bangunan TambahProyek(string nama, string kota, double lintang, double bujur,
        IReadOnlyList<(JenisKomponen Jenis, double Luas)> komponen, double pesisir, Guid? databaseId = null)
    {
        TolakTulis();
        ValidasiInputProyek(nama, kota, lintang, bujur, komponen, pesisir);
        var proyek = new Bangunan
        {
            Id = Proyek.Count == 0 ? 1 : Proyek.Max(p => p.Id) + 1,
            DatabaseId = databaseId,
            StableId = databaseId ?? Guid.NewGuid(),
            Nama = nama,
            Kota = kota,
            Latitude = lintang,
            Longitude = bujur,
            RevisiLokasi = 1,
            ZonaWaktu = "Asia/Jakarta",
            GunakanBobotKustom = false,
            Bobot = Domain.KriteriaBobot.Default,
            ProfilIklim = Katalog.ProfilBawaan(kota, pesisir)
        };
        proyek.ProfilIklim.PesisirTersedia = JarakPesisir.CobaHitungKm(lintang, bujur).HasValue;
        proyek.AturKomponen(komponen);
        Proyek.Add(proyek);

        ProyekAktifId = proyek.Id;
        EnsureJenisValid();
        Dipilih = databaseId is null ? ["m0", "m1"] : [];
        Horizon = 30;
        Cari = "";
        Prakiraan = [];
        PrakiraanDiperbarui = "";
        PrakiraanProyekId = null;
        HariDipilih = null;
        return proyek;
    }

    public void HapusProyekAktif()
    {
        TolakTulis();
        if (Aktif is not null)
        {
            Proyek.Remove(Aktif);
            if (AdaProyek)
            {
                ProyekAktifId = Proyek[0].Id;
                EnsureJenisValid();
            }
        }
        ResetPrakiraan();
    }

    public async Task HapusProyekAktifAsync()
    {
        TolakTulis();
        if (_backend is not null && Aktif?.DatabaseId is { } idDatabase)
            await _backend.Proyek.DeleteAsync(idDatabase);
        HapusProyekAktif();
    }

    public Task<string?> UbahProyekAsync(string nama, string kota, double lintang, double bujur,
        double dinding, double atap, double pesisir)
        => UbahProyekAsync(nama, kota, lintang, bujur,
            [(JenisKomponen.Dinding, dinding), (JenisKomponen.Atap, atap)],
            pesisir);

    public async Task<string?> UbahProyekAsync(string nama, string kota, double lintang, double bujur,
        IReadOnlyList<(JenisKomponen Jenis, double Luas)> komponen, double? pesisirManual = null)
    {
        TolakTulis();
        ValidasiInputProyek(nama, kota, lintang, bujur, komponen, pesisirManual);
        var proyek = Aktif;
        if (proyek is null) return null;

        bool pindah = proyek.Latitude != lintang || proyek.Longitude != bujur;
        string? pesan = null;
        Domain.ProfilIklim? profil = null;

        double pesisir = pesisirManual ?? JarakPesisir.CobaHitungKm(lintang, bujur) ?? 0;

        if (_backend is not null)
        {
            Guid id = proyek.DatabaseId!.Value;
            var domainKomponen = komponen.Select(k => new Domain.InputKomponen((Domain.JenisKomponen)k.Jenis, k.Luas)).ToArray();
            try
            {
                profil = await _backend.Proyek.UpdateProjectAsync(id, nama, kota, lintang, bujur, domainKomponen);
            }
            catch (GagalMemperbaruiProfilIklimException e)
            {
                pesan = e.Message;
                if (e.ProfilTersimpan is { } tersimpan)
                {
                    profil = tersimpan;
                }
            }
        }

        proyek.Nama = nama;
        proyek.Kota = kota;
        if (_backend is null) proyek.AturKomponen(komponen);
        else
        {
            var savedComponents = await _backend.Repository.ListKomponenAsync(proyek.StableId);
            foreach (var k in proyek.Komponen.ToArray()) proyek.HapusKomponen(k.Jenis);
            foreach (var k in savedComponents) proyek.TambahKomponen(new Komponen
            { Id = k.Id, DatabaseId = k.Id, Jenis = (JenisKomponen)k.Jenis, Luas = k.LuasM2 });
        }
        EnsureJenisValid();

        if (pindah)
        {
            proyek.Latitude = lintang;
            proyek.Longitude = bujur;
            proyek.RevisiLokasi++;
            foreach (var pekerjaan in proyek.Pekerjaan.Where(j => j.TanggalDikunci.HasValue))
                pekerjaan.Status = "Perlu ditinjau";
            ResetPrakiraan();

            if (proyek.ProfilIklim is not null)
            {
                proyek.ProfilIklim.KualitasCache = KualitasCacheIklim.TidakCocokLokasi;
                proyek.ProfilIklim.StatusPengambilan = StatusPengambilanIklim.BelumTersedia;
                proyek.ProfilIklim.JarakPesisirKm = pesisir;
                proyek.ProfilIklim.PesisirTersedia = JarakPesisir.CobaHitungKm(lintang, bujur).HasValue;
            }

            if (_backend is null)
            {
                proyek.ProfilIklim = Katalog.ProfilBawaan(kota, pesisir);
                proyek.ProfilIklim.KualitasCache = KualitasCacheIklim.TidakCocokLokasi;
                proyek.ProfilIklim.PesisirTersedia = JarakPesisir.CobaHitungKm(lintang, bujur).HasValue;
                Ubah();
                _ = Task.Run(async () =>
                {
                    await PerbaruiIklimAsync(proyek);
                });
                return null;
            }

            if (profil is not null)
            {
                TerapkanProfil(proyek, profil, pesisirManual);
            }
            else
            {
                _ = Task.Run(async () =>
                {
                    await PerbaruiIklimAsync(proyek);
                });
            }
        }
        else if (pesisirManual.HasValue && proyek.ProfilIklim is not null)
        {
            proyek.ProfilIklim.JarakPesisirKm = pesisir;
            proyek.ProfilIklim.CatatanOverridePesisir = "Manual override";
        }

        Ubah();
        return pesan;
    }

    public async Task<string?> PerbaruiIklimAsync(Bangunan proyek, CancellationToken externalCt = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var old = Interlocked.Exchange(ref _climateFetchCts, cts);
        try
        {
            old?.Cancel();
        }
        catch (ObjectDisposedException) { }

        var token = cts.Token;

        int proyekId = proyek.Id;
        int expectedRevisi = proyek.RevisiLokasi;
        double lintang = proyek.Latitude;
        double bujur = proyek.Longitude;

        if (proyek.ProfilIklim is not null)
        {
            proyek.ProfilIklim.StatusPengambilan = StatusPengambilanIklim.Mengambil;
        }

        try
        {
            Domain.ProfilIklim profil;
            if (_backend is null)
            {
                profil = await HistoricalClient.GetAnnualProfileAsync(lintang, bujur, token).ConfigureAwait(false);
            }
            else
            {
                profil = await _backend.Iklim.RefreshProfileAsync(proyek.DatabaseId!.Value, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();

            var target = Proyek.Find(p => p.Id == proyekId);
            if (target is null || target.RevisiLokasi != expectedRevisi ||
                target.Latitude != lintang || target.Longitude != bujur)
            {
                return null;
            }

            bool adaOverrideManual = target.ProfilIklim?.Provenance == ProvenanceIklim.ManualOverride ||
                !string.IsNullOrWhiteSpace(target.ProfilIklim?.CatatanOverridePesisir);
            double? manualKm = adaOverrideManual ? target.ProfilIklim?.JarakPesisirKm : null;
            string? catatanManual = adaOverrideManual ? target.ProfilIklim?.CatatanOverridePesisir : null;

            TerapkanProfil(target, profil, manualKm);
            if (adaOverrideManual && target.ProfilIklim is not null)
            {
                target.ProfilIklim.CatatanOverridePesisir = catatanManual;
                target.ProfilIklim.Provenance = ProvenanceIklim.ManualOverride;
            }
            target.ProfilIklim!.StatusPengambilan = StatusPengambilanIklim.Tersedia;
            target.ProfilIklim.KualitasCache = KualitasCacheIklim.CocokLokasi;
            Ubah();
            return null;
        }
        catch (OperationCanceledException)
        {
            if ((ReferenceEquals(_climateFetchCts, cts) || !ReferenceEquals(Aktif, proyek)) && proyek.RevisiLokasi == expectedRevisi
                && proyek.ProfilIklim is { } current)
            {
                current.StatusPengambilan = current.DiambilPadaUtc.HasValue
                    ? StatusPengambilanIklim.Tersedia : StatusPengambilanIklim.BelumTersedia;
                Ubah();
            }
            return "Pengambilan iklim dibatalkan.";
        }
        catch (GagalMemperbaruiProfilIklimException e)
        {
            var target = Proyek.Find(p => p.Id == proyekId);
            if (target is not null && target.RevisiLokasi == expectedRevisi)
            {
                if (target.ProfilIklim is not null)
                {
                    target.ProfilIklim.StatusPengambilan = StatusPengambilanIklim.GagalMemperbarui;
                }
                if (e.ProfilTersimpan is { } tersimpan)
                {
                    TerapkanProfil(target, tersimpan, sumber: "Profil iklim tersimpan");
                    target.ProfilIklim!.StatusPengambilan = StatusPengambilanIklim.GagalMemperbarui;
                    if (tersimpan.Metadata?.KualitasCache == KualitasCacheIklim.TidakCocokLokasi)
                    {
                        target.ProfilIklim.KualitasCache = KualitasCacheIklim.TidakCocokLokasi;
                    }
                }
                Ubah();
            }
            return e.Message;
        }
        catch (Exception e) when (e is HttpRequestException or TimeoutException or InvalidDataException or JsonException)
        {
            var target = Proyek.Find(p => p.Id == proyekId);
            if (target?.ProfilIklim is not null && target.RevisiLokasi == expectedRevisi)
            {
                target.ProfilIklim.StatusPengambilan = StatusPengambilanIklim.GagalMemperbarui;
                Ubah();
            }
            return "Iklim belum dapat diperbarui. Periksa koneksi atau coba lagi nanti.";
        }
        catch (Exception e) when (_backend is not null &&
            e is DbException or DbUpdateException or InvalidOperationException or KeyNotFoundException)
        {
            Debug.WriteLine(e);
            var target = Proyek.Find(p => p.Id == proyekId);
            if (target?.ProfilIklim is not null && target.RevisiLokasi == expectedRevisi)
            {
                target.ProfilIklim.StatusPengambilan = StatusPengambilanIklim.GagalMemperbarui;
                Ubah();
            }
            return "Profil iklim belum dapat diperbarui di database. Coba lagi nanti.";
        }
        finally
        {
            Interlocked.CompareExchange(ref _climateFetchCts, null, cts);
            try
            {
                cts.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
    }

    private static void TerapkanProfil(Bangunan proyek, Domain.ProfilIklim profil,
        double? pesisirManual = null, string? sumber = null)
    {
        double jarakPesisir = pesisirManual ?? profil.JarakPesisirKm;
        if (!string.IsNullOrWhiteSpace(profil.Metadata?.ZonaWaktu)) proyek.ZonaWaktu = profil.Metadata.ZonaWaktu;
        proyek.ProfilIklim = new ProfilIklim
        {
            CurahHujanTahunan = profil.CurahHujanTahunanMm,
            KelembapanRata = profil.KelembapanRataPct,
            SuhuRata = profil.SuhuRataC,
            JarakPesisirKm = jarakPesisir,
            Sumber = sumber ?? (pesisirManual is null
                ? "Open-Meteo dan garis pantai Natural Earth"
                : "Open-Meteo, jarak pesisir manual"),
            DiperbaruiPada = Format.WaktuLokal((profil.DiambilPada ?? DateTimeOffset.UtcNow).LocalDateTime),
            Provenance = profil.Provenance,
            StatusPengambilan = profil.Metadata?.StatusPengambilan ?? StatusPengambilanIklim.Tersedia,
            KualitasCache = profil.Metadata?.KualitasCache ?? KualitasCacheIklim.CocokLokasi,
            DiambilPadaUtc = profil.Metadata?.DiambilPadaUtc ?? profil.DiambilPada,
            PeriodeMulai = profil.Metadata?.PeriodeAwal,
            PeriodeSelesai = profil.Metadata?.PeriodeAkhir,
            Provider = profil.Metadata?.Penyedia ?? (!string.IsNullOrWhiteSpace(profil.Metadata?.Provider) ? profil.Metadata.Provider : "Open-Meteo Historical Weather API"),
            Model = profil.Metadata?.Model,
            ZonaWaktu = profil.Metadata?.ZonaWaktu ?? proyek.ZonaWaktu,
            VersiAlgoritmePesisir = profil.Metadata?.VersiAlgoritmePesisir ?? JarakPesisirCalculator.VersiAlgoritme,
            RevisiLokasi = profil.Metadata?.RevisiLokasi ?? profil.RevisiLokasi,
            PesisirTersedia = jarakPesisir > 0 || JarakPesisir.CobaHitungKm(proyek.Latitude, proyek.Longitude).HasValue
        };
    }

    public static Task<AppState> MuatAsync() => MuatAsync(paksaJsonLokal: false, CancellationToken.None);

    public static async Task<AppState> MuatAsync(bool paksaJsonLokal, CancellationToken cancellationToken = default)
    {
        if (paksaJsonLokal)
            return await MuatModeJsonLokalAsync(cancellationToken).ConfigureAwait(false);

        string? koneksi;
        try { koneksi = KoneksiSupabase.Cari(); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return new AppState { ButuhPemulihan = true,
                PesanStatusKoneksi = "Konfigurasi database tidak lengkap atau tidak valid. Periksa .env atau variabel lingkungan." };
        }
        if (string.IsNullOrWhiteSpace(koneksi))
            return await MuatModeJsonLokalAsync(cancellationToken).ConfigureAwait(false);

        var toko = StateStore.TokoPostgreSql(koneksi);
        if (toko is null)
        {
            return new AppState
            {
                _koneksi = koneksi,
                ButuhPemulihan = true,
                StatusSnapshot = SnapshotStatus.Corrupt,
                PesanStatusKoneksi = "Format string koneksi database tidak valid. Periksa konfigurasi .env atau WASTU_CONNECTION_STRING."
            };
        }

        var koordinator = new StartupSnapshotCoordinator<StateDto>(toko);
        PostgreSqlBackend? backend = null;
        IReadOnlyList<ItemKatalog>? katalogDb = null;

        var hasil = await koordinator.JalankanAsync(async ct =>
        {
            var be = new PostgreSqlBackend(koneksi);
            var dinding = await be.Material.ListByKindAsync(Domain.JenisKomponen.Dinding, ct).ConfigureAwait(false);
            var atap = await be.Material.ListByKindAsync(Domain.JenisKomponen.Atap, ct).ConfigureAwait(false);
            katalogDb = dinding.Concat(atap).Select(ItemKatalog.DariDomain).ToArray();
            backend = be;

            var draf = await StateStore.BacaDrafAsync(toko, ct).ConfigureAwait(false) ?? StateStore.BacaLegacyPostgres();

            return await BentukDtoDariDatabaseAsync(be, draf, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);

        var state = new AppState
        {
            _koneksi = koneksi,
            _toko = toko,
            ModeBacaSaja = hasil.Mode == ModeOperasi.LuringBacaSaja,
            ButuhPemulihan = hasil.Mode == ModeOperasi.ButuhPemulihan,
            StatusSnapshot = hasil.StatusSnapshot,
            DipulihkanDariCadangan = hasil.DipulihkanDariCadangan,
            SnapshotDiambilPadaUtc = hasil.SnapshotDiambilPadaUtc,
            PesanStatusKoneksi = hasil.Pesan
        };

        if (hasil.Mode == ModeOperasi.DatabaseOnline)
        {
            state._backend = backend;
            if (katalogDb is not null)
                state._katalog = katalogDb;
        }

        if (hasil.Data is not null)
        {
            TerapkanDto(state, hasil.Data);
        }

        try { await state.Workflows.LoadReportsAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { Debug.WriteLine("Riwayat laporan belum diperbarui: " + ex.GetType().Name); }
        return state;
    }

    private static async Task<AppState> MuatModeJsonLokalAsync(CancellationToken cancellationToken)
    {
        var toko = StateStore.TokoJsonLokal();
        var importer = new LegacyJsonImporter<StateDto>(
            StateStore.BerkasLegacyJson,
            toko,
            StateStore.BacaLegacyPayload);

        var koordinator = new StartupSnapshotCoordinator<StateDto>(toko);
        var hasil = await koordinator.JalankanModeJsonAsync(
            importer,
            () => new StateDto(),
            cancellationToken).ConfigureAwait(false);

        var state = new AppState
        {
            _toko = toko,
            ModeBacaSaja = false,
            ButuhPemulihan = hasil.Mode == ModeOperasi.ButuhPemulihan,
            StatusSnapshot = hasil.StatusSnapshot,
            DipulihkanDariCadangan = hasil.DipulihkanDariCadangan,
            SnapshotDiambilPadaUtc = hasil.SnapshotDiambilPadaUtc,
            PesanStatusKoneksi = hasil.Pesan
        };

        if (hasil.Data is not null)
        {
            TerapkanDto(state, hasil.Data);
        }

        return state;
    }

    private static async Task<StateDto> BentukDtoDariDatabaseAsync(PostgreSqlBackend backend, StateDto? draf, CancellationToken ct)
    {
        var proyekDb = await backend.Proyek.ListAsync(ct).ConfigureAwait(false);
        var hasilProyek = new List<ProyekDto>(proyekDb.Count);
        int idLokal = draf?.Proyek.Select(p => p.Id).DefaultIfEmpty(0).Max() ?? 0;

        foreach (var data in proyekDb)
        {
            var lokal = draf?.Proyek.Find(p => p.DatabaseId == data.Id);
            int id = lokal?.Id ?? ++idLokal;
            if (lokal is { JadwalTersimpanDiDatabase: false })
            {
                for (int index = 0; index < lokal.Pekerjaan.Count; index++)
                {
                    var legacy = lokal.Pekerjaan[index];
                    legacy.Id = WorkflowMapper.LegacyJobId(data.Id, index, legacy);
                    if (await backend.PekerjaanRepository.GetByIdAsync(legacy.Id.Value, ct) is null)
                        await backend.PekerjaanRepository.AddAsync(
                            WorkflowMapper.KeDomain(WorkflowMapper.DariDto(legacy), data.Id), ct);
                }
            }

            var komponen = await backend.Repository.ListKomponenAsync(data.Id, ct).ConfigureAwait(false);
            var dinding = komponen.FirstOrDefault(k => k.Jenis == Domain.JenisKomponen.Dinding)?.LuasM2 ?? 0;
            var atap = komponen.FirstOrDefault(k => k.Jenis == Domain.JenisKomponen.Atap)?.LuasM2 ?? 0;
            var profil = await backend.Repository.GetProfilIklimAsync(data.Id, ct).ConfigureAwait(false);

            var komponenList = komponen.Select(k => new KomponenDto
            {
                Id = k.Id,
                DatabaseId = k.Id,
                Jenis = (JenisKomponen)k.Jenis,
                Luas = k.LuasM2
            }).ToList();

            double jarakPesisir = profil?.JarakPesisirKm ?? JarakPesisir.CobaHitungKm(data.Latitude, data.Longitude) ?? 0;

            hasilProyek.Add(new ProyekDto
            {
                Id = id,
                StableId = data.Id,
                DatabaseId = data.Id,
                Nama = data.Nama,
                Kota = data.Kota,
                Lintang = data.Latitude,
                Bujur = data.Longitude,
                Dinding = dinding,
                Atap = atap,
                JarakPesisir = jarakPesisir,
                RevisiLokasi = data.RevisiLokasi,
                ZonaWaktu = profil?.Metadata?.ZonaWaktu ?? data.ZonaWaktu ?? "Asia/Jakarta",
                GunakanBobotKustom = lokal?.GunakanBobotKustom ?? false,
                BobotTermal = lokal?.BobotTermal ?? 0.25,
                BobotLembap = lokal?.BobotLembap ?? 0.25,
                BobotKorosi = lokal?.BobotKorosi ?? 0.25,
                BobotUsia = lokal?.BobotUsia ?? 0.25,
                Komponen = komponenList,
                Iklim = profil is not null
                    ? new IklimDto
                    {
                        CurahHujan = profil.CurahHujanTahunanMm,
                        Kelembapan = profil.KelembapanRataPct,
                        Suhu = profil.SuhuRataC,
                        Sumber = "Profil iklim tersimpan",
                        Diperbarui = Format.WaktuLokal((profil.DiambilPada ?? DateTimeOffset.UtcNow).LocalDateTime),
                        Provenance = profil.Provenance,
                        StatusPengambilan = profil.Metadata?.StatusPengambilan ?? StatusPengambilanIklim.Tersedia,
                        KualitasCache = profil.Metadata?.KualitasCache ?? KualitasCacheIklim.CocokLokasi,
                        DiambilPadaUtc = profil.Metadata?.DiambilPadaUtc ?? profil.DiambilPada,
                        PeriodeMulai = profil.Metadata?.PeriodeAwal,
                        PeriodeSelesai = profil.Metadata?.PeriodeAkhir,
                        Provider = profil.Metadata?.Penyedia ?? "Open-Meteo Historical Weather API",
                        Model = profil.Metadata?.Model,
                        ZonaWaktu = profil.Metadata?.ZonaWaktu ?? data.ZonaWaktu,
                        VersiAlgoritmePesisir = profil.Metadata?.VersiAlgoritmePesisir ?? JarakPesisirCalculator.VersiAlgoritme,
                        RevisiLokasi = profil.Metadata?.RevisiLokasi ?? profil.RevisiLokasi,
                        PesisirTersedia = jarakPesisir > 0 || JarakPesisir.CobaHitungKm(data.Latitude, data.Longitude).HasValue
                    }
                    : null,
                JadwalTersimpanDiDatabase = true,
                FaktorPenuaanA = lokal?.FaktorPenuaanA ?? 0.5,
                RiwayatSimulasi = lokal?.RiwayatSimulasi ?? [],
                PrakiraanCache = await backend.PrakiraanCacheRepository.GetCacheAsync(data.Latitude, data.Longitude, data.RevisiLokasi, ct),
                Pekerjaan = (await backend.PekerjaanRepository.ListByProyekAsync(data.Id, ct)).Select(WorkflowMapper.KeDto).ToList(),
                Laporan = (await backend.LaporanRepository.ListByProyekAsync(data.Id, ct)).ToList()
            });
        }

        int? aktifId = draf?.ProyekAktif;
        if (aktifId is null || !hasilProyek.Any(p => p.Id == aktifId))
        {
            aktifId = hasilProyek.FirstOrDefault()?.Id;
        }

        return new StateDto
        {
            Proyek = hasilProyek,
            Katalog = (await backend.Material.ListByKindAsync(Domain.JenisKomponen.Dinding, ct))
                .Concat(await backend.Material.ListByKindAsync(Domain.JenisKomponen.Atap, ct)).ToList(),
            ProyekAktif = aktifId,
            Jenis = draf?.Jenis ?? JenisKomponen.Dinding,
            Dipilih = draf?.Dipilih is { Length: > 0 } d ? d : ["m0", "m1"],
            Horizon = draf?.Horizon is 20 or 30 or 50 ? draf.Horizon : 30,
            JenisKerja = draf?.JenisKerja ?? JenisPekerjaan.Pengecatan,
            Prakiraan = draf?.Prakiraan ?? [],
            PrakiraanDiperbarui = draf?.PrakiraanDiperbarui ?? "",
            PrakiraanProyek = draf?.PrakiraanProyek,
            HariDipilih = draf?.HariDipilih
        };
    }

    private static void TerapkanDto(AppState state, StateDto dto)
    {
        state.Proyek.Clear();
        if (dto.Katalog.Count > 0) state._katalog = dto.Katalog.Select(ItemKatalog.DariDomain).ToArray();
        foreach (var p in dto.Proyek)
        {
            var proyek = new Bangunan
            {
                Id = p.Id,
                DatabaseId = p.DatabaseId,
                StableId = p.StableId ?? p.DatabaseId ?? Guid.NewGuid(),
                Nama = p.Nama,
                Kota = p.Kota,
                Latitude = p.Lintang,
                Longitude = p.Bujur,
                RevisiLokasi = p.RevisiLokasi > 0 ? p.RevisiLokasi : 1,
                ZonaWaktu = !string.IsNullOrWhiteSpace(p.ZonaWaktu) ? p.ZonaWaktu : "Asia/Jakarta",
                GunakanBobotKustom = p.GunakanBobotKustom,
                JadwalTersimpanDiDatabase = p.JadwalTersimpanDiDatabase,
                FaktorPenuaanA = double.IsFinite(p.FaktorPenuaanA) && p.FaktorPenuaanA > 0 && p.FaktorPenuaanA <= 1 ? p.FaktorPenuaanA : 0.5,
                PrakiraanCache = p.PrakiraanCache,
                ProfilIklim = p.Iklim is { } i
                    ? new ProfilIklim
                    {
                        CurahHujanTahunan = i.CurahHujan,
                        KelembapanRata = i.Kelembapan,
                        SuhuRata = i.Suhu,
                        JarakPesisirKm = p.JarakPesisir,
                        Sumber = i.Sumber,
                        DiperbaruiPada = i.Diperbarui,
                        Provenance = i.Provenance,
                        StatusPengambilan = i.StatusPengambilan,
                        KualitasCache = i.KualitasCache,
                        DiambilPadaUtc = i.DiambilPadaUtc,
                        PeriodeMulai = i.PeriodeMulai,
                        PeriodeSelesai = i.PeriodeSelesai,
                        Provider = i.Provider,
                        Model = i.Model,
                        ZonaWaktu = i.ZonaWaktu ?? p.ZonaWaktu,
                        VersiAlgoritmePesisir = string.IsNullOrEmpty(i.VersiAlgoritmePesisir) ? JarakPesisirCalculator.VersiAlgoritme : i.VersiAlgoritmePesisir,
                        RevisiLokasi = i.RevisiLokasi ?? p.RevisiLokasi,
                        CatatanOverridePesisir = i.CatatanOverridePesisir,
                        PesisirTersedia = i.PesisirTersedia
                    }
                    : Katalog.ProfilBawaan(p.Kota, p.JarakPesisir)
            };

            try
            {
                if (p.GunakanBobotKustom)
                {
                    proyek.Bobot = new Domain.KriteriaBobot(p.BobotTermal, p.BobotLembap, p.BobotKorosi, p.BobotUsia);
                }
                else
                {
                    proyek.Bobot = Domain.KriteriaBobot.Default;
                }
            }
            catch
            {
                proyek.GunakanBobotKustom = false;
                proyek.Bobot = Domain.KriteriaBobot.Default;
            }

            if (p.Komponen.Count > 0)
            {
                foreach (var k in p.Komponen)
                {
                    proyek.TambahKomponen(new Komponen
                    {
                        Id = k.Id ?? k.DatabaseId ?? Guid.NewGuid(),
                        DatabaseId = k.DatabaseId,
                        Jenis = k.Jenis,
                        Luas = k.Luas
                    });
                }
            }
            else
            {
                if (p.Dinding > 0)
                    proyek.TambahKomponen(new Komponen { Jenis = JenisKomponen.Dinding, Luas = p.Dinding });
                if (p.Atap > 0)
                    proyek.TambahKomponen(new Komponen { Jenis = JenisKomponen.Atap, Luas = p.Atap });
            }

            foreach (var j in p.Pekerjaan) proyek.JadwalkanPekerjaan(WorkflowMapper.DariDto(j));
            foreach (var l in p.Laporan) proyek.TambahLaporan(WorkflowMapper.DariDomain(l));
            proyek.RiwayatSimulasi.AddRange(p.RiwayatSimulasi);
            state.Proyek.Add(proyek);
        }

        state.ProyekAktifId = dto.ProyekAktif ?? state.Proyek.FirstOrDefault()?.Id ?? 0;
        state.EnsureJenisValid();
        state.Jenis = dto.Jenis;
        state.EnsureJenisValid();
        state.Dipilih = dto.Dipilih is { Length: > 0 } d ? d : ["m0", "m1"];
        state.Horizon = dto.Horizon is 20 or 30 or 50 ? dto.Horizon : 30;
        state.JenisKerja = dto.JenisKerja;
        state.Prakiraan = dto.Prakiraan.ConvertAll(c => new PrakiraanCuaca
        {
            Tanggal = c.Tanggal.ToDateTime(TimeOnly.MinValue),
            ProbabilitasHujan = c.PeluangHujan,
            CurahHujan = c.CurahHujan,
            SuhuMin = c.SuhuMin,
            SuhuMaks = c.SuhuMaks,
            Kelembapan = c.Kelembapan
        });
        state.PrakiraanDiperbarui = dto.PrakiraanDiperbarui;
        state.PrakiraanProyekId = dto.PrakiraanProyek;
        state.HariDipilih = dto.HariDipilih?.ToDateTime(TimeOnly.MinValue);
        state.PulihkanPrakiraanAktif();
    }

    private StateDto KeDto() => new()
    {
        Katalog = _katalog.Select(k => k.DataDomain).ToList(),
        Proyek = Proyek.ConvertAll(p => new ProyekDto
        {
            Id = p.Id,
            StableId = p.StableId,
            DatabaseId = p.DatabaseId,
            Nama = p.Nama,
            Kota = p.Kota,
            Lintang = p.Latitude,
            Bujur = p.Longitude,
            Dinding = p.LuasKomponen(JenisKomponen.Dinding),
            Atap = p.LuasKomponen(JenisKomponen.Atap),
            JarakPesisir = p.ProfilIklim?.JarakPesisirKm ?? 0,
            RevisiLokasi = p.RevisiLokasi,
            ZonaWaktu = p.ZonaWaktu,
            GunakanBobotKustom = p.GunakanBobotKustom,
            JadwalTersimpanDiDatabase = p.JadwalTersimpanDiDatabase,
            FaktorPenuaanA = p.FaktorPenuaanA,
            PrakiraanCache = p.PrakiraanCache,
            RiwayatSimulasi = p.RiwayatSimulasi.ToList(),
            Laporan = p.Laporan.Select(WorkflowMapper.KeDomain).ToList(),
            BobotTermal = p.Bobot.Termal,
            BobotLembap = p.Bobot.Lembap,
            BobotKorosi = p.Bobot.Korosi,
            BobotUsia = p.Bobot.Usia,
            Komponen = p.Komponen.Select(k => new KomponenDto
            {
                Id = k.Id,
                DatabaseId = k.DatabaseId,
                Jenis = k.Jenis,
                Luas = k.Luas
            }).ToList(),
            Iklim = p.ProfilIklim is { } i
                ? new IklimDto
                {
                    CurahHujan = i.CurahHujanTahunan,
                    Kelembapan = i.KelembapanRata,
                    Suhu = i.SuhuRata,
                    Sumber = i.Sumber,
                    Diperbarui = i.DiperbaruiPada ?? "",
                    Provenance = i.Provenance,
                    StatusPengambilan = i.StatusPengambilan,
                    KualitasCache = i.KualitasCache,
                    DiambilPadaUtc = i.DiambilPadaUtc,
                    PeriodeMulai = i.PeriodeMulai,
                    PeriodeSelesai = i.PeriodeSelesai,
                    Provider = i.Provider,
                    Model = i.Model,
                    ZonaWaktu = i.ZonaWaktu ?? p.ZonaWaktu,
                    VersiAlgoritmePesisir = i.VersiAlgoritmePesisir,
                    RevisiLokasi = i.RevisiLokasi,
                    CatatanOverridePesisir = i.CatatanOverridePesisir,
                    PesisirTersedia = i.PesisirTersedia
                }
                : null,
            Pekerjaan = p.Pekerjaan.Select(WorkflowMapper.KeDto).ToList()
        }),
        ProyekAktif = AdaProyek ? ProyekAktifId : null,
        Jenis = Jenis,
        Dipilih = Dipilih,
        Horizon = Horizon,
        JenisKerja = JenisKerja,
        Prakiraan = Prakiraan.ConvertAll(c => new CuacaDto
        {
            Tanggal = DateOnly.FromDateTime(c.Tanggal),
            PeluangHujan = c.ProbabilitasHujan,
            CurahHujan = c.CurahHujan,
            SuhuMin = c.SuhuMin,
            SuhuMaks = c.SuhuMaks,
            Kelembapan = c.Kelembapan
        }),
        PrakiraanDiperbarui = PrakiraanDiperbarui,
        PrakiraanProyek = PrakiraanProyekId,
        HariDipilih = HariDipilih is { } h ? DateOnly.FromDateTime(h) : null
    };

    public void Ubah()
    {
        Revisi++;
        Simpan();
        Changed?.Invoke();
    }

    // Penulisan berkas berjalan di thread pool; perubahan beruntun digabung menjadi satu tulisan.
    public void Simpan()
    {
        if (TulisanDitolak || _toko is null) return;
        var dto = KeDto();
        lock (_gate)
        {
            _antrean = dto;
            if (_menulis) return;
            _menulis = true;
        }
        Task.Run(TulisAntrean);
    }

    // Saat aplikasi ditutup: tunggu tulisan latar selesai lalu simpan versi terakhir. State yang belum
    // pernah berubah (termasuk bila jendela ditutup sebelum data termuat) tidak ditulis ulang.
    public void SimpanSekarang()
    {
        if (TulisanDitolak || Revisi == 0 || _toko is null) return;
        var batas = Stopwatch.StartNew();
        while (true)
        {
            lock (_gate)
            {
                if (!_menulis) { _menulis = true; break; }
            }
            if (batas.ElapsedMilliseconds > 2000) return;
            Thread.Sleep(5);
        }
        try { StateStore.Simpan(KeDto(), _toko); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Debug.WriteLine(e); }
        finally { lock (_gate) _menulis = false; }
    }

    private void TulisAntrean()
    {
        while (true)
        {
            StateDto? snapshot;
            lock (_gate)
            {
                snapshot = _antrean;
                _antrean = null;
                if (snapshot is null)
                {
                    _menulis = false;
                    return;
                }
            }
            try
            {
                if (_toko is not null)
                    StateStore.Simpan(snapshot, _toko);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
