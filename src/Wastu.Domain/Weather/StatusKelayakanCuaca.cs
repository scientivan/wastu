namespace Wastu.Domain.Weather;

/// <summary>
/// Status kelayakan cuaca untuk pelaksanaan pekerjaan lapangan (PRD §12, PRD-R08, PRD-R10).
/// </summary>
public enum StatusKelayakanCuaca
{
    /// <summary>Cuaca memenuhi seluruh ambang batas aman pelaksanaan pekerjaan.</summary>
    Layak,

    /// <summary>Cuaca melanggar ambang batas lunak/peringatan (misal suhu di atas ambang pada pengecoran).</summary>
    Waspada,

    /// <summary>Cuaca melanggar ambang batas keras (hujan lebat, kelembapan berlebih, dll).</summary>
    TidakLayak,

    /// <summary>Data prakiraan cuaca tidak tersedia atau terjadi kegagalan jaringan tanpa cache valid.</summary>
    DataTidakTersedia
}
