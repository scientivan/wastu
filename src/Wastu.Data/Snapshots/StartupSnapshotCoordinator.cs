using System.Globalization;

namespace Wastu.Data.Snapshots;

/// <summary>
/// Koordinator startup bebas-UI dan deterministik. Urutan keputusan untuk mode PostgreSQL:
/// data hidup → snapshot profil valid (luring baca-saja) → permintaan pemulihan. Kegagalan database
/// tidak pernah menghentikan aplikasi, tidak pernah menghapus berkas snapshot, dan tidak pernah
/// menanam data contoh. Pesan hasil berupa teks tetap; pengecualian database tidak pernah diteruskan
/// karena dapat memuat string koneksi atau kredensial.
/// </summary>
public sealed class StartupSnapshotCoordinator<T> where T : notnull
{
    public const string PesanOnline =
        "Terhubung ke PostgreSQL. Data dimuat langsung dari database dan snapshot lokal diperbarui.";

    private readonly SnapshotStore<T> _store;
    private readonly Func<DateTimeOffset> _waktuUtc;

    /// <param name="store">Satu instance snapshot per profil dan tipe payload.</param>
    /// <param name="waktuUtc">Jam yang dapat disuntik untuk pengujian; hasil dinormalkan ke UTC.</param>
    public StartupSnapshotCoordinator(SnapshotStore<T> store, Func<DateTimeOffset>? waktuUtc = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _waktuUtc = waktuUtc ?? (static () => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Mode PostgreSQL. Bila loader sukses, payload disimpan sebagai snapshot berversi dengan stempel
    /// waktu UTC; kegagalan penulisan snapshot hanya menjadi peringatan dan tidak menghapus data hidup.
    /// Bila loader gagal, jatuh ke snapshot luring baca-saja atau permintaan pemulihan.
    /// </summary>
    public async Task<HasilStartup<T>> JalankanAsync(Func<CancellationToken, Task<T>> muatDataAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(muatDataAsync);

        T data;
        try
        {
            data = await muatDataAsync(cancellationToken).ConfigureAwait(false);
            if (data is null)
                throw new InvalidOperationException("Hasil muat database kosong.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Detail pengecualian database tidak pernah diteruskan ke UI atau log.
            return await JalurLuringAsync(cancellationToken).ConfigureAwait(false);
        }

        var waktu = WaktuUtc();
        var pesan = PesanOnline;
        try
        {
            await _store.SaveAsync(data, waktu, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Penulisan snapshot gagal; data hidup tetap dipakai dan kegagalan hanya diperingatkan.
            pesan = PesanOnline + " Snapshot lokal belum dapat disimpan; data di database tetap utuh.";
        }

        return new HasilStartup<T>
        {
            Mode = ModeOperasi.DatabaseOnline, Data = data,
            SnapshotDiambilPadaUtc = waktu, Pesan = pesan
        };
    }

    /// <summary>
    /// Mode kompatibilitas JSON lokal. Snapshot berversi menang; bila belum ada, impor one-time dan
    /// idempoten dari state.json legacy dijalankan secara eksplisit. Snapshot rusak/versi tak didukung
    /// tidak pernah ditimpa dan tidak pernah mengembalikan data kosong diam-diam.
    /// </summary>
    public async Task<HasilStartup<T>> JalankanModeJsonAsync(LegacyJsonImporter<T> importer, Func<T> stateKosong,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentNullException.ThrowIfNull(stateKosong);

        var hasil = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (hasil.Status is SnapshotStatus.ValidPrimary or SnapshotStatus.RecoveredBackup)
            return DariSnapshotJson(hasil);
        if (hasil.Status != SnapshotStatus.Missing)
            return ButuhPemulihan(hasil);

        var impor = await importer.ImporAsync(cancellationToken).ConfigureAwait(false);
        switch (impor.Status)
        {
            case LegacyImportStatus.Berhasil:
                return new HasilStartup<T>
                {
                    Mode = ModeOperasi.ModeJsonLokal, Data = impor.Data!,
                    StatusSnapshot = SnapshotStatus.ValidPrimary,
                    SnapshotDiambilPadaUtc = impor.WaktuSnapshotUtc,
                    ImporLegacyDilakukan = true, Pesan = impor.Pesan
                };
            case LegacyImportStatus.TidakAda:
                return new HasilStartup<T>
                {
                    Mode = ModeOperasi.ModeJsonLokal, Data = stateKosong(),
                    StatusSnapshot = SnapshotStatus.Missing,
                    Pesan = "Belum ada data lokal. Aplikasi dimulai dengan data kosong di mode JSON lokal."
                };
            case LegacyImportStatus.SudahTersimpan:
                var ulang = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
                if (ulang.Status is SnapshotStatus.ValidPrimary or SnapshotStatus.RecoveredBackup)
                    return DariSnapshotJson(ulang);
                return ButuhPemulihan(ulang);
            default:
                // Legacy rusak atau penyimpanan baru gagal: berkas lama tetap dipertahankan.
                return new HasilStartup<T>
                {
                    Mode = ModeOperasi.ButuhPemulihan,
                    StatusSnapshot = SnapshotStatus.Corrupt,
                    Pesan = impor.Pesan
                };
        }
    }

    private async Task<HasilStartup<T>> JalurLuringAsync(CancellationToken cancellationToken)
    {
        var hasil = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (hasil.Status is SnapshotStatus.ValidPrimary)
            return BacaSaja(hasil, cadangan: false);
        if (hasil.Status is SnapshotStatus.RecoveredBackup)
            return BacaSaja(hasil, cadangan: true);
        return ButuhPemulihan(hasil);
    }

    private static HasilStartup<T> DariSnapshotJson(SnapshotLoadResult<T> hasil)
    {
        var snapshot = hasil.Snapshot!;
        return new HasilStartup<T>
        {
            Mode = ModeOperasi.ModeJsonLokal, Data = snapshot.Payload,
            StatusSnapshot = hasil.Status,
            SnapshotDiambilPadaUtc = snapshot.DatabaseSnapshotAtUtc,
            DipulihkanDariCadangan = hasil.Status == SnapshotStatus.RecoveredBackup,
            Pesan = "Data dimuat dari snapshot mode JSON lokal (bukan PostgreSQL), waktu data "
                + Waktu(snapshot.DatabaseSnapshotAtUtc) + " UTC."
        };
    }

    private static HasilStartup<T> BacaSaja(SnapshotLoadResult<T> hasil, bool cadangan)
    {
        var snapshot = hasil.Snapshot!;
        return new HasilStartup<T>
        {
            Mode = ModeOperasi.LuringBacaSaja, Data = snapshot.Payload,
            StatusSnapshot = hasil.Status,
            SnapshotDiambilPadaUtc = snapshot.DatabaseSnapshotAtUtc,
            DipulihkanDariCadangan = cadangan,
            Pesan = "Database tidak tersedia. Aplikasi dibuka dalam mode luring baca-saja dari snapshot per "
                + Waktu(snapshot.DatabaseSnapshotAtUtc) + " UTC."
                + (cadangan ? " Snapshot utama rusak, cadangan terakhir yang dipakai." : "")
        };
    }

    private static HasilStartup<T> ButuhPemulihan(SnapshotLoadResult<T> hasil) => new()
    {
        Mode = ModeOperasi.ButuhPemulihan,
        StatusSnapshot = hasil.Status,
        Pesan = PesanPemulihan(hasil.Status)
    };

    /// <summary>Pesan pemulihan per status; teks tetap tanpa detail teknis mentah.</summary>
    public static string PesanPemulihan(SnapshotStatus status) => status switch
    {
        SnapshotStatus.Missing =>
            "Database tidak tersedia dan belum ada snapshot untuk profil ini. Periksa konfigurasi .env atau " +
            "WASTU_CONNECTION_STRING, lalu pilih Coba Lagi. Tidak ada data contoh yang dibuat otomatis.",
        SnapshotStatus.Corrupt =>
            "Database tidak tersedia dan snapshot rusak atau tidak dapat dibaca. Berkas snapshot tetap " +
            "dipertahankan untuk pemulihan. Periksa konfigurasi database lalu pilih Coba Lagi.",
        SnapshotStatus.UnsupportedVersion =>
            "Database tidak tersedia dan versi skema snapshot belum didukung aplikasi ini. Berkas tetap " +
            "dipertahankan; perbarui aplikasi atau pulihkan cadangan yang sesuai.",
        SnapshotStatus.ProfileMismatch =>
            "Database tidak tersedia dan snapshot berasal dari profil penyimpanan lain. Berkas tidak diubah; " +
            "gunakan profil koneksi yang sesuai lalu pilih Coba Lagi.",
        _ => "Database tidak tersedia dan snapshot belum dapat digunakan. Periksa konfigurasi lalu pilih Coba Lagi."
    };

    private DateTimeOffset WaktuUtc()
    {
        var waktu = _waktuUtc();
        return waktu == default ? DateTimeOffset.UtcNow : new DateTimeOffset(waktu.UtcDateTime);
    }

    private static string Waktu(DateTimeOffset waktu)
        => waktu.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
