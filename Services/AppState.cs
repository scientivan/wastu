using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wastu.Data;
using Wastu.Data.Snapshots;
using Wastu.Domain.Scoring;
using Wastu.Domain.Services;
using Wastu.Models;

namespace Wastu.Services;

// State tunggal aplikasi. Model domain menjadi sumber data; DTO hanya dipakai saat menyimpan.
public sealed class AppState
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
    private static readonly OpenMeteo IklimHistoris = new();

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
    public Bangunan Aktif => Proyek.Find(p => p.Id == ProyekAktifId) ?? Proyek[0];
    public ProfilIklim Iklim => Aktif.ProfilIklim ??= Katalog.ProfilBawaan(Aktif.Kota, 30);
    public double Luas => Aktif.LuasKomponen(Jenis);

    public IReadOnlyList<ItemKatalog> KatalogAktif => _katalog;

    public async Task<bool> SegarkanKatalogAsync(CancellationToken ct = default)
    {
        try
        {
            var koneksi = _koneksi ?? KoneksiSupabase.Cari();
            var be = _backend ?? (!string.IsNullOrWhiteSpace(koneksi) ? new PostgreSqlBackend(koneksi) : null);
            if (be is not null)
            {
                var dinding = await be.Material.ListByKindAsync(Domain.JenisKomponen.Dinding, ct).ConfigureAwait(false);
                var atap = await be.Material.ListByKindAsync(Domain.JenisKomponen.Atap, ct).ConfigureAwait(false);
                var gabung = dinding.Concat(atap).Select(ItemKatalog.DariDomain).ToArray();
                if (gabung.Length > 0)
                {
                    _katalog = gabung;
                    _backend ??= be;
                    _koneksi ??= koneksi;
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
        evaluasi.ProyeksikanEmisi(Horizon);
        return evaluasi;
    }

    public Evaluasi[] Peringkat() => Peringkat(Jenis);

    private Evaluasi[] Peringkat(JenisKomponen jenis)
    {
        double luas = Aktif.LuasKomponen(jenis);
        if (!double.IsFinite(luas) || luas <= 0)
            return [];

        var komponen = new Domain.Komponen(Guid.Empty, Aktif.DatabaseId ?? Guid.Empty, (Domain.JenisKomponen)jenis, luas);
        var bahan = _katalog.Where(item => item.Jenis == jenis).ToDictionary(item => item.DataDomain.Id);
        return SuitabilityScoringService.Rank(komponen, Iklim.KeDomain(),
                bahan.Values.Select(item => item.DataDomain).ToArray())
            .Select(e => new Evaluasi(bahan[e.BahanId].Bahan, e)).ToArray();
    }

    public List<ItemKatalog> Terpilih()
    {
        if (Luas <= 0) return [];
        var hasil = Dipilih.Select(kode => _katalog.FirstOrDefault(item => item.Kode == kode && item.Jenis == Jenis))
            .OfType<ItemKatalog>().Distinct().Take(2).ToList();
        return hasil.Count > 0 ? hasil : Peringkat().Take(2).Select(e => Item(e.Bahan)).ToList();
    }

    public void AturJenis(JenisKomponen jenis)
    {
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
        if (!AdaProyek || PrakiraanProyekId != Aktif.Id) return [];
        var hariIni = DateTime.Today;
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
        ProyekAktifId = id;
        ResetPrakiraan();
    }

    public async Task<string?> TambahProyekAsync(string nama, string kota, double lintang, double bujur,
        double dinding, double atap, double pesisir)
    {
        TolakTulis();
        if (_backend is null)
        {
            TambahProyek(nama, kota, lintang, bujur, dinding, atap, pesisir);
            return null;
        }

        pesisir = JarakPesisir.HitungKm(lintang, bujur);
        var data = await _backend.Proyek.CreateProjectAsync(nama, kota, lintang, bujur, dinding, atap);
        var lokal = TambahProyek(nama, kota, lintang, bujur, dinding, atap, pesisir, data.Id);
        return await PerbaruiIklimAsync(lokal);
    }

    public Bangunan TambahProyek(string nama, string kota, double lintang, double bujur, double dinding, double atap,
        double pesisir, Guid? databaseId = null)
    {
        TolakTulis();
        var proyek = new Bangunan
        {
            Id = Proyek.Count == 0 ? 1 : Proyek.Max(p => p.Id) + 1,
            DatabaseId = databaseId,
            Nama = nama,
            Kota = kota,
            Latitude = lintang,
            Longitude = bujur,
            ProfilIklim = Katalog.ProfilBawaan(kota, pesisir)
        };
        proyek.TambahKomponen(new Komponen { Jenis = JenisKomponen.Dinding, Luas = dinding });
        proyek.TambahKomponen(new Komponen { Jenis = JenisKomponen.Atap, Luas = atap });
        Proyek.Add(proyek);

        ProyekAktifId = proyek.Id;
        Jenis = JenisKomponen.Dinding;
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
        Proyek.Remove(Aktif);
        if (AdaProyek) ProyekAktifId = Proyek[0].Id;
        ResetPrakiraan();
    }

    public async Task HapusProyekAktifAsync()
    {
        TolakTulis();
        // Proyek lokal (tanpa DatabaseId) tidak punya padanan di database; jangan menyentuh null.
        if (_backend is not null && Aktif.DatabaseId is { } idDatabase)
            await _backend.Proyek.DeleteAsync(idDatabase);
        HapusProyekAktif();
    }

    public async Task<string?> UbahProyekAsync(string nama, string kota, double lintang, double bujur,
        double dinding, double atap, double pesisir)
    {
        TolakTulis();
        var proyek = Aktif;
        bool pindah = proyek.Latitude != lintang || proyek.Longitude != bujur;
        string? pesan = null;
        Domain.ProfilIklim? profil = null;

        if (_backend is not null)
        {
            if (pindah)
                pesisir = JarakPesisir.HitungKm(lintang, bujur);
            Guid id = proyek.DatabaseId!.Value;
            try
            {
                profil = await _backend.Proyek.UpdateProjectAsync(id, nama, kota, lintang, bujur, dinding, atap);
            }
            catch (GagalMemperbaruiProfilIklimException e)
            {
                pesan = e.Message;
            }
        }

        proyek.Nama = nama;
        proyek.Kota = kota;
        proyek.Latitude = lintang;
        proyek.Longitude = bujur;
        proyek.AturLuasKomponen(JenisKomponen.Dinding, dinding);
        proyek.AturLuasKomponen(JenisKomponen.Atap, atap);
        if (pindah)
        {
            proyek.ProfilIklim = Katalog.ProfilBawaan(kota, pesisir);
            ResetPrakiraan();
            if (_backend is null)
                return await PerbaruiIklimAsync(proyek);
            if (profil is not null)
                TerapkanProfil(proyek, profil);
        }
        else if (!PostgreSql)
        {
            Iklim.JarakPesisirKm = pesisir;
        }
        return pesan;
    }

    public async Task<string?> PerbaruiIklimAsync(Bangunan proyek)
    {
        double lintang = proyek.Latitude;
        double bujur = proyek.Longitude;
        try
        {
            var profil = _backend is null
                ? await IklimHistoris.GetAnnualProfileAsync(lintang, bujur)
                : await _backend.Iklim.RefreshProfileAsync(proyek.DatabaseId!.Value);
            if (proyek.Latitude == lintang && proyek.Longitude == bujur)
                TerapkanProfil(proyek, profil, _backend is null ? proyek.ProfilIklim?.JarakPesisirKm : null);
            return null;
        }
        catch (GagalMemperbaruiProfilIklimException e)
        {
            if (e.ProfilTersimpan is { } tersimpan)
                TerapkanProfil(proyek, tersimpan, sumber: "Profil iklim tersimpan");
            return e.Message;
        }
        catch (Exception e) when (e is HttpRequestException or TimeoutException or InvalidDataException or JsonException)
        {
            return "Iklim belum dapat diperbarui. Periksa koneksi atau coba lagi nanti.";
        }
        catch (Exception e) when (_backend is not null &&
            e is DbException or DbUpdateException or InvalidOperationException or KeyNotFoundException)
        {
            Debug.WriteLine(e);
            return "Profil iklim belum dapat diperbarui di database. Coba lagi nanti.";
        }
    }

    private static void TerapkanProfil(Bangunan proyek, Domain.ProfilIklim profil,
        double? pesisirManual = null, string? sumber = null)
    {
        proyek.ProfilIklim = new ProfilIklim
        {
            CurahHujanTahunan = profil.CurahHujanTahunanMm,
            KelembapanRata = profil.KelembapanRataPct,
            SuhuRata = profil.SuhuRataC,
            JarakPesisirKm = pesisirManual ?? profil.JarakPesisirKm,
            Sumber = sumber ?? (pesisirManual is null
                ? "Open-Meteo dan garis pantai Natural Earth"
                : "Open-Meteo, jarak pesisir manual"),
            DiperbaruiPada = Format.WaktuLokal((profil.DiambilPada ?? DateTimeOffset.UtcNow).LocalDateTime)
        };
    }

    public static Task<AppState> MuatAsync() => MuatAsync(paksaJsonLokal: false, CancellationToken.None);

    public static async Task<AppState> MuatAsync(bool paksaJsonLokal, CancellationToken cancellationToken = default)
    {
        if (paksaJsonLokal)
            return await MuatModeJsonLokalAsync(cancellationToken).ConfigureAwait(false);

        var koneksi = KoneksiSupabase.Cari();
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

            var komponen = await backend.Repository.ListKomponenAsync(data.Id, ct).ConfigureAwait(false);
            var dinding = komponen.FirstOrDefault(k => k.Jenis == Domain.JenisKomponen.Dinding)?.LuasM2 ?? 0;
            var atap = komponen.FirstOrDefault(k => k.Jenis == Domain.JenisKomponen.Atap)?.LuasM2 ?? 0;
            var profil = await backend.Repository.GetProfilIklimAsync(data.Id, ct).ConfigureAwait(false);

            hasilProyek.Add(new ProyekDto
            {
                Id = id,
                DatabaseId = data.Id,
                Nama = data.Nama,
                Kota = data.Kota,
                Lintang = data.Latitude,
                Bujur = data.Longitude,
                Dinding = dinding,
                Atap = atap,
                JarakPesisir = profil?.JarakPesisirKm ?? JarakPesisir.HitungKm(data.Latitude, data.Longitude),
                Iklim = profil is not null
                    ? new IklimDto
                    {
                        CurahHujan = profil.CurahHujanTahunanMm,
                        Kelembapan = profil.KelembapanRataPct,
                        Suhu = profil.SuhuRataC,
                        Sumber = "Profil iklim tersimpan",
                        Diperbarui = Format.WaktuLokal((profil.DiambilPada ?? DateTimeOffset.UtcNow).LocalDateTime)
                    }
                    : null,
                Pekerjaan = lokal?.Pekerjaan ?? []
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
        foreach (var p in dto.Proyek)
        {
            var proyek = new Bangunan
            {
                Id = p.Id,
                DatabaseId = p.DatabaseId,
                Nama = p.Nama,
                Kota = p.Kota,
                Latitude = p.Lintang,
                Longitude = p.Bujur,
                ProfilIklim = p.Iklim is { } i
                    ? new ProfilIklim
                    {
                        CurahHujanTahunan = i.CurahHujan,
                        KelembapanRata = i.Kelembapan,
                        SuhuRata = i.Suhu,
                        JarakPesisirKm = p.JarakPesisir,
                        Sumber = i.Sumber,
                        DiperbaruiPada = i.Diperbarui
                    }
                    : Katalog.ProfilBawaan(p.Kota, p.JarakPesisir)
            };
            proyek.TambahKomponen(new Komponen { Jenis = JenisKomponen.Dinding, Luas = p.Dinding });
            proyek.TambahKomponen(new Komponen { Jenis = JenisKomponen.Atap, Luas = p.Atap });
            foreach (var j in p.Pekerjaan)
            {
                var pekerjaan = new Pekerjaan
                {
                    Nama = j.Nama,
                    Jenis = j.Jenis,
                    TanggalMulaiRencana = j.Mulai.ToDateTime(TimeOnly.MinValue),
                    TanggalSelesaiRencana = j.Selesai.ToDateTime(TimeOnly.MinValue)
                };
                pekerjaan.PulihkanJadwal(j.Dikunci?.ToDateTime(TimeOnly.MinValue));
                proyek.JadwalkanPekerjaan(pekerjaan);
            }
            state.Proyek.Add(proyek);
        }

        state.ProyekAktifId = dto.ProyekAktif ?? state.Proyek.FirstOrDefault()?.Id ?? 0;
        state.Jenis = dto.Jenis;
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
    }

    private StateDto KeDto() => new()
    {
        Proyek = Proyek.ConvertAll(p => new ProyekDto
        {
            Id = p.Id,
            DatabaseId = p.DatabaseId,
            Nama = p.Nama,
            Kota = p.Kota,
            Lintang = p.Latitude,
            Bujur = p.Longitude,
            Dinding = p.LuasKomponen(JenisKomponen.Dinding),
            Atap = p.LuasKomponen(JenisKomponen.Atap),
            JarakPesisir = p.ProfilIklim?.JarakPesisirKm ?? 0,
            Iklim = p.ProfilIklim is { SudahDiperbarui: true } i
                ? new IklimDto
                {
                    CurahHujan = i.CurahHujanTahunan,
                    Kelembapan = i.KelembapanRata,
                    Suhu = i.SuhuRata,
                    Sumber = i.Sumber,
                    Diperbarui = i.DiperbaruiPada!
                }
                : null,
            Pekerjaan = p.Pekerjaan.Select(j => new PekerjaanDto
            {
                Nama = j.Nama,
                Jenis = j.Jenis,
                Mulai = DateOnly.FromDateTime(j.TanggalMulaiRencana),
                Selesai = DateOnly.FromDateTime(j.TanggalSelesaiRencana),
                Dikunci = j.TanggalDikunci is { } t ? DateOnly.FromDateTime(t) : null
            }).ToList()
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
            StateDto? dto;
            lock (_gate)
            {
                dto = _antrean;
                _antrean = null;
                if (dto is null) { _menulis = false; return; }
            }
            if (_toko is null || TulisanDitolak) continue;
            try { StateStore.Simpan(dto, _toko); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Debug.WriteLine(e); }
        }
    }

}
