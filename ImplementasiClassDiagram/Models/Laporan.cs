using System.Text;

namespace ImplementasiClassDiagram.Models;

public class Laporan
{
    private DateTime _tanggalDibuat;
    private string _format = "txt";

    public DateTime TanggalDibuat { get => _tanggalDibuat; set => _tanggalDibuat = value; }
    public string Format { get => _format; set => _format = value; }

    public FileInfo Ekspor()
    {
        var namaFile = $"laporan-{TanggalDibuat:yyyyMMdd}.{Format.TrimStart('.')}";
        File.WriteAllText(namaFile, $"Laporan dibuat: {TanggalDibuat:O}", Encoding.UTF8);
        return new FileInfo(namaFile);
    }
}
