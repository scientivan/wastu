using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Wastu.Models;
using Wastu.Reporting;
using Wastu.Services;
using JenisKomponen = Wastu.Models.JenisKomponen;
using Evaluasi = Wastu.Models.Evaluasi;
using SimulasiEmisi = Wastu.Models.SimulasiEmisi;

namespace Wastu.Views.Pages;

public partial class ReportPage : PageBase
{
    private JenisKomponen _selectedJenis = JenisKomponen.Dinding;
    private int _selectedHorizon = 30;
    private bool _includeSchedule = true;
    private readonly HashSet<string> _selectedMaterialCodes = [];
    private readonly ReportExportService _exportService;
    private bool _syncingControls;
    private int? _lastProjectId;

    public ReportPage()
    {
        InitializeComponent();
        _exportService = new ReportExportService();
    }

    protected override void Refresh()
    {
        var currentProjectId = S.AdaProyek ? S.Aktif.Id : (int?)null;
        if (_lastProjectId != currentProjectId)
        {
            _lastProjectId = currentProjectId;
            _selectedMaterialCodes.Clear();
            _selectedJenis = S.Jenis;
            _selectedHorizon = S.Horizon;
        }

        if (S.AdaProyek)
        {
            if (!S.Aktif.AdaKomponen(_selectedJenis))
            {
                _selectedJenis = S.Jenis;
            }
        }

        _syncingControls = true;
        try
        {
            if (CompWallRadio is not null)
            {
                var ada = !S.AdaProyek || S.Aktif.AdaKomponen(JenisKomponen.Dinding);
                CompWallRadio.Visibility = ada ? Visibility.Visible : Visibility.Collapsed;
                CompWallRadio.IsEnabled = ada;
                CompWallRadio.IsChecked = _selectedJenis == JenisKomponen.Dinding;
            }
            if (CompRoofRadio is not null)
            {
                var ada = !S.AdaProyek || S.Aktif.AdaKomponen(JenisKomponen.Atap);
                CompRoofRadio.Visibility = ada ? Visibility.Visible : Visibility.Collapsed;
                CompRoofRadio.IsEnabled = ada;
                CompRoofRadio.IsChecked = _selectedJenis == JenisKomponen.Atap;
            }

            if (Hor20Radio is not null) Hor20Radio.IsChecked = _selectedHorizon == 20;
            if (Hor30Radio is not null) Hor30Radio.IsChecked = _selectedHorizon == 30;
            if (Hor50Radio is not null) Hor50Radio.IsChecked = _selectedHorizon == 50;

            if (IncludeScheduleCheck is not null) IncludeScheduleCheck.IsChecked = _includeSchedule;
        }
        finally
        {
            _syncingControls = false;
        }

        S.AturJenis(_selectedJenis);
        S.Horizon = _selectedHorizon;

        // Default to all selected materials in state if empty
        if (_selectedMaterialCodes.Count == 0)
        {
            var rank = S.Peringkat();
            foreach (var item in S.Terpilih())
            {
                _selectedMaterialCodes.Add(item.Kode);
            }
            if (_selectedMaterialCodes.Count == 0 && rank.Length > 0)
            {
                foreach (var e in rank.Take(3))
                {
                    _selectedMaterialCodes.Add(S.Item(e.Bahan).Kode);
                }
            }
        }

        DataContext = new ReportData(S, _selectedJenis, _selectedHorizon, _includeSchedule, _selectedMaterialCodes);
    }

    private void Comp_Checked(object sender, RoutedEventArgs e)
    {
        if (_syncingControls) return;
        if (sender is RadioButton rb && rb.Tag is string tag && Enum.TryParse<JenisKomponen>(tag, out var jenis))
        {
            _selectedJenis = jenis;
            S.AturJenis(_selectedJenis);
            _selectedMaterialCodes.Clear();
            Refresh();
        }
    }

    private void Horizon_Checked(object sender, RoutedEventArgs e)
    {
        if (_syncingControls) return;
        if (sender is RadioButton rb && rb.Tag is string tag && int.TryParse(tag, out var h))
        {
            _selectedHorizon = h;
            S.Horizon = _selectedHorizon;
            Refresh();
        }
    }

    private void ScheduleCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_syncingControls) return;
        _includeSchedule = IncludeScheduleCheck?.IsChecked == true;
        Refresh();
    }

    private void MaterialCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_syncingControls) return;
        if (sender is CheckBox cb && cb.DataContext is SelectableMaterialItem item)
        {
            if (cb.IsChecked == true)
                _selectedMaterialCodes.Add(item.Kode);
            else
                _selectedMaterialCodes.Remove(item.Kode);

            Refresh();
        }
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        var p = S.Aktif;
        if (p is null)
        {
            MessageBox.Show("Tidak ada proyek aktif untuk diekspor.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_selectedMaterialCodes.Count == 0)
        {
            MessageBox.Show(
                "Pilih minimal satu material untuk diekspor ke dalam laporan PDF.",
                "Pilihan Material Kosong",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var rawName = string.IsNullOrWhiteSpace(p.Nama) ? "Proyek" : p.Nama;
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleanName = new string(rawName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleanName)) cleanName = "Proyek";

        var dialog = new SaveFileDialog
        {
            Filter = "Berkas PDF (*.pdf)|*.pdf",
            FileName = $"Laporan-Wastu-{cleanName}-{DateTime.Now:yyyyMMdd-HHmm}.pdf",
            Title = "Simpan Laporan Rekomendasi PDF"
        };

        // If user cancels SaveFileDialog, do nothing and show NO success toast (T35)
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var snapshot = BuildSnapshot(p);
            var laporanExport = await _exportService.ExportSnapshotAsync(snapshot, dialog.FileName);

            var modelLaporan = new Models.Laporan
            {
                Id = laporanExport.Id,
                ProyekId = laporanExport.ProyekId,
                PathBerkas = laporanExport.PathBerkas,
                TanggalDibuat = laporanExport.DibuatPada.LocalDateTime,
                DibuatPada = laporanExport.DibuatPada,
                ChecksumSha256 = laporanExport.ChecksumSha256,
                SnapshotHash = laporanExport.SnapshotHash,
                UkuranByte = laporanExport.UkuranByte,
                Status = laporanExport.Status
            };

            if (!S.ModeBacaSaja)
            {
                p.TambahLaporan(modelLaporan);
                S.Ubah();
                Refresh();

                MessageBox.Show(
                    $"Laporan PDF berhasil disimpan ke:\n{laporanExport.PathBerkas}",
                    "Ekspor PDF Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                Refresh();

                MessageBox.Show(
                    $"Laporan PDF berhasil disimpan ke:\n{laporanExport.PathBerkas}\n\nCatatan: Riwayat laporan pada proyek tidak diubah karena aplikasi sedang berada dalam mode luring baca-saja.",
                    "Ekspor PDF Berhasil (Mode Baca-Saja)",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Gagal mengekspor laporan PDF: {ex.Message}",
                "Ekspor Gagal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ReportHistoryItem historyItem)
        {
            // Buka PDF (Process.Start with UseShellExecute = true ONLY after checking File.Exists! - T34)
            if (!File.Exists(historyItem.PathBerkas))
            {
                MessageBox.Show(
                    $"Berkas PDF tidak ditemukan di lokasi:\n{historyItem.PathBerkas}\n\nBerkas mungkin telah dipindahkan atau dihapus.",
                    "Berkas Tidak Ditemukan",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Refresh();
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = historyItem.PathBerkas,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Gagal membuka aplikasi pembaca PDF: {ex.Message}",
                    "Kesalahan Membuka Berkas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private SnapshotLaporan BuildSnapshot(Bangunan p)
    {
        var climate = S.Iklim;
        S.AturJenis(_selectedJenis);
        S.Horizon = _selectedHorizon;
        var rank = S.Peringkat();

        // Map components
        var compList = p.Komponen.Select(k => new SnapshotKomponen(
            k.Jenis == Models.JenisKomponen.Dinding ? Wastu.Domain.JenisKomponen.Dinding : Wastu.Domain.JenisKomponen.Atap,
            k.Luas)).ToList();

        var snapIklim = SnapshotIklimMapper.Buat(climate, S.ModeBacaSaja);

        // Selected materials (including any rank e.g. rank 10!)
        var selectedBahan = new List<SnapshotBahan>();
        for (int i = 0; i < rank.Length; i++)
        {
            var e = rank[i];
            var item = S.Item(e.Bahan);
            if (_selectedMaterialCodes.Contains(item.Kode))
            {
                selectedBahan.Add(SnapshotBahanMapper.Buat(item, e,
                    p.Komponen.Single(k => k.Jenis == _selectedJenis).Id, i + 1));
            }
        }

        // Selected simulations
        var simList = new List<SnapshotSimulasi>();
        foreach (var mat in selectedBahan)
        {
            var evaluasi = rank.FirstOrDefault(r => r.Bahan.Nama == mat.Nama);
            if (evaluasi is not null)
            {
                var item = S.Item(evaluasi.Bahan);
                var sim = S.Simulasikan(item).SimulasiEmisi;
                if (sim is not null)
                {
                    simList.Add(new SnapshotSimulasi
                    {
                        EvaluasiId = Guid.NewGuid(),
                        BahanNama = mat.Nama,
                        Jenis = mat.Jenis,
                        HorizonTahun = _selectedHorizon,
                        UsiaPakaiNominal = evaluasi.Bahan.UsiaPakaiTahun,
                        UsiaPakaiEfektif = sim.UsiaPakaiEfektif,
                        JumlahPenggantian = sim.JumlahPenggantian,
                        EmisiAwalKg = evaluasi.EmisiAwal,
                        EmisiKumulatifKg = sim.EmisiKumulatif,
                        FaktorPenuaanA = sim.FaktorPenuaanA,
                        VersiModel = "v2.0"
                    });
                }
            }
        }

        // Locked jobs
        var lockedJobs = new List<SnapshotPekerjaan>();
        if (_includeSchedule)
        {
            foreach (var j in p.Pekerjaan.Where(w => w.TanggalDikunci.HasValue))
            {
                lockedJobs.Add(new SnapshotPekerjaan
                {
                    PekerjaanId = Guid.NewGuid(),
                    Nama = j.Nama,
                    Jenis = j.Jenis == Models.JenisPekerjaan.Pengecoran ? Wastu.Domain.Weather.JenisPekerjaan.Pengecoran : Wastu.Domain.Weather.JenisPekerjaan.Pengecatan,
                    TanggalMulai = DateOnly.FromDateTime(j.TanggalMulaiRencana),
                    TanggalSelesai = DateOnly.FromDateTime(j.TanggalSelesaiRencana),
                    TanggalDikunci = DateOnly.FromDateTime(j.TanggalDikunci!.Value),
                    StatusCuaca = "Layak",
                    AlasanCuaca = ["Kondisi cuaca dalam rentang ambang aman."]
                });
            }
        }

        return new SnapshotLaporan
        {
            ProyekId = p.StableId,
            NamaProyek = p.Nama,
            Kota = p.Kota,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            Komponen = compList,
            Iklim = snapIklim,
            BahanTerpilih = selectedBahan,
            SimulasiHorizon = simList,
            SertakanJadwal = _includeSchedule,
            JadwalTerkunci = lockedJobs,
            DibuatPadaUtc = DateTimeOffset.UtcNow,
            DibuatPadaWib = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)),
            VersiModel = Domain.KonfigurasiModel.Bawaan.VersiModel,
            Bobot = S.BobotAktif
        };
    }
}

public sealed class SelectableMaterialItem : ViewData
{
    public SelectableMaterialItem(int rank, string kode, string name, double score, double emission, string reason, bool isSelected)
    {
        Rank = rank;
        Kode = kode;
        Name = name;
        Score = score;
        Emission = emission;
        Reason = reason;
        IsSelected = isSelected;
    }

    public int Rank { get; }
    public string RankText => $"#{Rank}";
    public string Kode { get; }
    public string Name { get; }
    public double Score { get; }
    public string ScoreText => $"{Format.Angka(Score)}/100";
    public double Emission { get; }
    public string EmissionText => $"{Format.Angka(Emission)} kg";
    public string Reason { get; }
    public string ReasonPreview => Reason.Length > 65 ? Reason[..62] + "..." : Reason;
    public bool IsSelected { get; set; }
}

public sealed class ReportHistoryItem : ViewData
{
    public ReportHistoryItem(Models.Laporan l)
    {
        Id = l.Id;
        PathBerkas = l.PathBerkas;
        NamaBerkas = l.NamaBerkas;
        TanggalFormat = l.DibuatPada.ToOffset(TimeSpan.FromHours(7)).ToString("dd MMMM yyyy, HH:mm") + " WIB";
        StatusBerkas = l.StatusBerkas;
        StatusTeks = l.StatusKeterangan;
        IsAvailable = StatusBerkas == StatusBerkasLaporan.Tersedia;
    }

    public Guid Id { get; }
    public string PathBerkas { get; }
    public string NamaBerkas { get; }
    public string TanggalFormat { get; }
    public StatusBerkasLaporan StatusBerkas { get; }
    public string StatusTeks { get; }
    public bool IsAvailable { get; }
    public Visibility MissingVisibility => IsAvailable ? Visibility.Collapsed : Visibility.Visible;
    public string BadgeStyle => IsAvailable ? "BadgeTersedia" : "BadgeHilang";
    public string StatusFg => IsAvailable ? "#166534" : "#B91C1C";
}

public sealed class ReportData : ViewData
{
    public ReportData(AppState s, JenisKomponen jenis, int horizon, bool includeSchedule, HashSet<string> selectedCodes)
    {
        var p = s.Aktif;
        var climate = s.Iklim;
        Subtitle = Format.Tanggal(DateTime.Today) + " " + p.Nama;
        Location = $"{p.Kota} ({Format.Angka(p.Latitude)}, {Format.Angka(p.Longitude)})";
        Areas = $"Dinding {Format.Angka(p.LuasKomponen(Models.JenisKomponen.Dinding))} m² Atap {Format.Angka(p.LuasKomponen(Models.JenisKomponen.Atap))} m²";
        Climate = climate.SudahDiperbarui
            ? $"{Format.Angka(climate.CurahHujanTahunan)} mm hujan per tahun, {Format.Angka(climate.KelembapanRata)}% kelembapan"
            : "Profil iklim belum diperbarui.";
        ClimateDetail = $"Suhu {Format.Angka(climate.SuhuRata)}°C. Jarak pesisir {Format.Angka(climate.JarakPesisirKm)} km";
        HasClimate = climate.SudahDiperbarui ? Visibility.Visible : Visibility.Collapsed;

        RankingTitle = "Peringkat material " + (jenis == JenisKomponen.Dinding ? "dinding" : "atap");

        s.AturJenis(jenis);
        s.Horizon = horizon;
        var rank = s.Peringkat();
        SelectableMaterials = rank.Select((e, i) =>
        {
            var code = s.Item(e.Bahan).Kode;
            return new SelectableMaterialItem(i + 1, code, e.Bahan.Nama, e.SkorKesesuaian, e.EmisiAwal, e.Alasan, selectedCodes.Contains(code));
        }).ToList();

        // Selected rows (no 5-item cutoff!)
        var chosenEvaluasi = rank.Where(e => selectedCodes.Contains(s.Item(e.Bahan).Kode)).ToArray();
        if (chosenEvaluasi.Length == 0 && rank.Length > 0)
        {
            chosenEvaluasi = rank.Take(3).ToArray();
        }
        Rows = chosenEvaluasi.Select((e, i) => new ReportRow(e, i == chosenEvaluasi.Length - 1)).ToList();

        SimulationTitle = $"Simulasi {horizon} tahun";
        Simulations = chosenEvaluasi.Select(e =>
        {
            var item = s.Item(e.Bahan);
            return new ReportSimulation(item.Bahan.Nama, s.Simulasikan(item).SimulasiEmisi!);
        }).ToList();

        if (includeSchedule)
        {
            Locked = p.Pekerjaan.Where(j => j.TanggalDikunci is not null)
                .Select(j => $"{j.Nama} pada {Format.Iso(j.TanggalDikunci!.Value)}").ToList();
        }
        else
        {
            Locked = [];
        }
        NoLocked = Locked.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // History
        ReportHistory = p.Laporan.Select(l => new ReportHistoryItem(l)).ToList();
        NoHistory = ReportHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public string Subtitle { get; }
    public string Location { get; }
    public string Areas { get; }
    public string Climate { get; }
    public string ClimateDetail { get; }
    public Visibility HasClimate { get; }
    public string RankingTitle { get; }
    public List<SelectableMaterialItem> SelectableMaterials { get; }
    public List<ReportRow> Rows { get; }
    public string SimulationTitle { get; }
    public List<ReportSimulation> Simulations { get; }
    public List<string> Locked { get; }
    public Visibility NoLocked { get; }
    public List<ReportHistoryItem> ReportHistory { get; }
    public Visibility NoHistory { get; }
}

public sealed class ReportRow(Evaluasi e, bool last) : ViewData
{
    public string Name { get; } = e.Bahan.Nama;
    public string Score { get; } = Format.Angka(e.SkorKesesuaian) + "/100";
    public string Emission { get; } = Format.Angka(e.EmisiAwal);
    public string Reason { get; } = e.Alasan;
    public Thickness RowBorder { get; } = new(0, 0, 0, last ? 0 : 1);
}

public sealed class ReportSimulation(string name, SimulasiEmisi sim) : ViewData
{
    public string Name { get; } = name;
    public string Total { get; } = Format.Angka(sim.EmisiKumulatif) + " kgCO₂e";
    public string Detail { get; } = $"{sim.JumlahPenggantian} penggantian, {Format.Angka(sim.UsiaPakaiEfektif)} tahun usia efektif";
}
