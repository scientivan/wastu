using Wastu.Domain.Abstractions;

namespace Wastu.Domain.Tests.Fakes;

/// <summary>
/// Klien iklim yang bisa dikendalikan dari test: mengembalikan nilai yang ditentukan,
/// atau melempar galat untuk menguji jalur kegagalan PRD §8.4.
/// </summary>
internal sealed class HistoricalClimateClientStub : IHistoricalClimateClient
{
    public int JumlahPanggilan { get; private set; }

    public double LatitudeTerakhir { get; private set; }

    public double LongitudeTerakhir { get; private set; }

    /// <summary>Nilai yang dikembalikan saat panggilan berhasil.</summary>
    public ProfilIklim Hasil { get; set; } = new(2400, 82, 27.5, 3.0, TingkatKorosi.Tinggi);

    /// <summary>Saat true, panggilan melempar galat seolah jaringan gagal.</summary>
    public bool Gagal { get; set; }

    public Task<ProfilIklim> GetAnnualProfileAsync(double lat, double lon, CancellationToken ct = default)
    {
        JumlahPanggilan++;
        LatitudeTerakhir = lat;
        LongitudeTerakhir = lon;

        // Klien sungguhan akan membatalkan di sini. Stub harus ikut melakukannya,
        // kalau tidak filter OperationCanceledException di service tidak pernah
        // bisa diuji.
        ct.ThrowIfCancellationRequested();

        if (Gagal)
        {
            throw new HttpRequestException("Jaringan tidak terjangkau.");
        }

        return Task.FromResult(Hasil);
    }
}