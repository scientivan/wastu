using System.Diagnostics;
using System.IO;
using Wastu.Models;

namespace Wastu.Services;

// State tunggal aplikasi. Model domain menjadi sumber data; DTO hanya dipakai saat menyimpan.
public sealed class AppState
{
    private readonly object _gate = new();
    private StateDto? _antrean;
    private bool _menulis;

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
    public Bangunan Aktif => Proyek.Find(p => p.Id == ProyekAktifId) ?? Proyek[0];
    public ProfilIklim Iklim => Aktif.ProfilIklim ??= Katalog.ProfilBawaan(Aktif.Kota, 30);
    public double Luas => Aktif.LuasKomponen(Jenis);

    public Evaluasi Nilai(ItemKatalog item)
    {
        var (min, max) = RentangUsia(item.Jenis);
        var evaluasi = new Evaluasi(item.Bahan) { EmisiAwal = item.Bahan.FaktorEmisi * Aktif.LuasKomponen(item.Jenis) };
        evaluasi.HitungSkor(Iklim, min, max);
        return evaluasi;
    }

    public Evaluasi Simulasikan(ItemKatalog item)
    {
        var evaluasi = Nilai(item);
        evaluasi.ProyeksikanEmisi(Horizon);
        return evaluasi;
    }

    public Evaluasi[] Peringkat()
    {
        var komponen = new Komponen { Jenis = Jenis, Luas = Luas };
        foreach (var item in Katalog.Semua)
            if (item.Jenis == Jenis)
                komponen.TambahEvaluasi(Nilai(item));
        return komponen.BandingkanBahan();
    }

    public List<ItemKatalog> Terpilih()
    {
        var hasil = new List<ItemKatalog>(2);
        foreach (var kode in Dipilih)
            if (Katalog.Cari(kode) is { } item && item.Jenis == Jenis)
                hasil.Add(item);
        if (hasil.Count == 0)
            foreach (var e in Peringkat().Take(2))
                hasil.Add(Katalog.Dari(e.Bahan));
        return hasil;
    }

    public void AturJenis(JenisKomponen jenis)
    {
        Jenis = jenis;
        Dipilih = Peringkat().Take(2).Select(e => Katalog.Dari(e.Bahan).Kode).ToArray();
    }

    public void PilihUntukBanding(string kode)
    {
        var lain = Peringkat().First(e => Katalog.Dari(e.Bahan).Kode != kode);
        Dipilih = [kode, Katalog.Dari(lain.Bahan).Kode];
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

    public Bangunan TambahProyek(string nama, string kota, double lintang, double bujur, double dinding, double atap, double pesisir)
    {
        var proyek = new Bangunan
        {
            Id = Proyek.Count == 0 ? 1 : Proyek.Max(p => p.Id) + 1,
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
        Dipilih = ["m0", "m1"];
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

    public static AppState Muat()
    {
        var state = new AppState();
        var dto = StateStore.Load();
        if (dto is null) return state;

        foreach (var p in dto.Proyek)
        {
            var proyek = new Bangunan
            {
                Id = p.Id,
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
        try { StateStore.Save(KeDto()); }
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
            try { StateStore.Save(dto); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Debug.WriteLine(e); }
        }
    }

    private static readonly Dictionary<JenisKomponen, (int, int)> _rentang = new();

    private static (int Min, int Max) RentangUsia(JenisKomponen jenis)
    {
        if (_rentang.TryGetValue(jenis, out var r)) return r;
        var usia = Katalog.Semua.Where(i => i.Jenis == jenis).Select(i => i.Bahan.UsiaPakaiTahun).ToArray();
        return _rentang[jenis] = (usia.Min(), usia.Max());
    }
}
