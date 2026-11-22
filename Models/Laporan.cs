using System.IO;
using System.Security.Cryptography;
using Wastu.Reporting;

namespace Wastu.Models;

public class Laporan
{
    private Guid _id = Guid.NewGuid();
    private Guid _proyekId;
    private string _pathBerkas = string.Empty;
    private DateTime _tanggalDibuat = DateTime.Now;
    private DateTimeOffset _dibuatPada = DateTimeOffset.Now;
    private string _format = "pdf";
    private string? _checksumSha256;
    private string? _snapshotHash;
    private long? _ukuranByte;
    private string _status = "Tersimpan";

    public Guid Id { get => _id; set => _id = value; }
    public Guid ProyekId { get => _proyekId; set => _proyekId = value; }
    public string PathBerkas { get => _pathBerkas; set => _pathBerkas = value; }
    public DateTime TanggalDibuat { get => _tanggalDibuat; set => _tanggalDibuat = value; }
    public DateTimeOffset DibuatPada { get => _dibuatPada; set => _dibuatPada = value; }
    public string Format { get => _format; set => _format = value; }
    public string? ChecksumSha256 { get => _checksumSha256; set => _checksumSha256 = value; }
    public string? SnapshotHash { get => _snapshotHash; set => _snapshotHash = value; }
    public long? UkuranByte { get => _ukuranByte; set => _ukuranByte = value; }
    public string Status { get => _status; set => _status = value; }

    public string NamaBerkas => string.IsNullOrEmpty(_pathBerkas) ? "Laporan.pdf" : Path.GetFileName(_pathBerkas);

    public StatusBerkasLaporan StatusBerkas
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_pathBerkas) || !File.Exists(_pathBerkas))
                return StatusBerkasLaporan.Hilang;

            if (!string.IsNullOrWhiteSpace(_checksumSha256))
            {
                try
                {
                    using var stream = File.OpenRead(_pathBerkas);
                    var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
                    if (!string.Equals(hash, _checksumSha256, StringComparison.OrdinalIgnoreCase))
                        return StatusBerkasLaporan.BerubahChecksum;
                }
                catch
                {
                    return StatusBerkasLaporan.Hilang;
                }
            }

            return StatusBerkasLaporan.Tersedia;
        }
    }

    public string StatusKeterangan => StatusBerkas switch
    {
        StatusBerkasLaporan.Tersedia => Status == "MenungguPencatatan" ? "Berkas tersedia; pencatatan tertunda" : "Tersedia",
        StatusBerkasLaporan.Hilang => "Berkas Tidak Ditemukan",
        StatusBerkasLaporan.BerubahChecksum => "Checksum Berubah",
        _ => Status
    };

    /// <summary>
    /// Ekspor teks usang dihapus sesuai WP07 (§12.1 item 3).
    /// Mengembalikan FileInfo dari PathBerkas jika berkas telah diekspor.
    /// </summary>
    public FileInfo Ekspor()
    {
        if (string.IsNullOrWhiteSpace(_pathBerkas) || !File.Exists(_pathBerkas))
        {
            throw new InvalidOperationException("Gunakan ReportExportService untuk menghasilkan berkas PDF langsung.");
        }
        return new FileInfo(_pathBerkas);
    }
}
