using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wastu.Data;
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
    public Bangunan Aktif => Proyek.Find(p => p.Id == ProyekAktifId) ?? Proyek[0];
    public ProfilIklim Iklim => Aktif.ProfilIklim ??= Katalog.ProfilBawaan(Aktif.Kota, 30);
    public double Luas => Aktif.LuasKomponen(Jenis);

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
        Proyek.Remove(Aktif);
        if (AdaProyek) ProyekAktifId = Proyek[0].Id;
        ResetPrakiraan();
    }

    public async Task HapusProyekAktifAsync()
    {
        if (_backend is not null)
            await _backend.Proyek.DeleteAsync(Aktif.DatabaseId!.Value);
        HapusProyekAktif();
    }

    public async Task<string?> UbahProyekAsync(string nama, string kota, double lintang, double bujur,
        double dinding, double atap, double pesisir)
    {
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

    public static async Task<AppState> MuatAsync()
    {
        var koneksi = Environment.GetEnvironmentVariable(KoneksiSupabase.NamaVariabelLingkungan);
        if (string.IsNullOrWhiteSpace(koneksi))
            return MuatLokal(postgreSql: false);

        var backend = new PostgreSqlBackend(koneksi);
        var state = MuatLokal(postgreSql: true);
        state._backend = backend;
        var dinding = await backend.Material.ListByKindAsync(Domain.JenisKomponen.Dinding);
        var atap = await backend.Material.ListByKindAsync(Domain.JenisKomponen.Atap);
        state._katalog = dinding.Concat(atap).Select(ItemKatalog.DariDomain).ToArray();

        var proyek = await backend.Proyek.ListAsync();
        var idDatabase = proyek.Select(p => p.Id).ToHashSet();
        int idLokal = state.Proyek.Select(p => p.Id).DefaultIfEmpty().Max();
        state.Proyek.RemoveAll(p => p.DatabaseId is not { } id || !idDatabase.Contains(id));
        foreach (var data in proyek)
        {
            var lokal = state.Proyek.Find(p => p.DatabaseId == data.Id);
            if (lokal is null)
            {
                lokal = new Bangunan { Id = ++idLokal, DatabaseId = data.Id };
                state.Proyek.Add(lokal);
            }
            if (state.PrakiraanProyekId == lokal.Id && (lokal.Latitude != data.Latitude || lokal.Longitude != data.Longitude))
                state.ResetPrakiraan();

            lokal.Nama = data.Nama;
            lokal.Kota = data.Kota;
            lokal.Latitude = data.Latitude;
            lokal.Longitude = data.Longitude;
            var komponen = await backend.Repository.ListKomponenAsync(data.Id);
            lokal.AturLuasKomponen(JenisKomponen.Dinding, komponen.FirstOrDefault(k => k.Jenis == Domain.JenisKomponen.Dinding)?.LuasM2 ?? 0);
            lokal.AturLuasKomponen(JenisKomponen.Atap, komponen.FirstOrDefault(k => k.Jenis == Domain.JenisKomponen.Atap)?.LuasM2 ?? 0);
            var profil = await backend.Repository.GetProfilIklimAsync(data.Id);
            if (profil is not null)
                TerapkanProfil(lokal, profil, sumber: "Profil iklim tersimpan");
            else
                lokal.ProfilIklim = Katalog.ProfilBawaan(data.Kota, JarakPesisir.HitungKm(data.Latitude, data.Longitude));
        }
        if (!state.Proyek.Any(p => p.Id == state.ProyekAktifId))
        {
            state.ProyekAktifId = state.Proyek.FirstOrDefault()?.Id ?? 0;
            state.ResetPrakiraan();
        }
        return state;
    }

    private static AppState MuatLokal(bool postgreSql)
    {
        var state = new AppState();
        var dto = StateStore.Load(postgreSql);
        if (dto is null) return state;

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
        return state;
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
        if (Revisi == 0) return;
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
        try { StateStore.Save(KeDto(), PostgreSql); }
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
            try { StateStore.Save(dto, PostgreSql); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Debug.WriteLine(e); }
        }
    }

}
