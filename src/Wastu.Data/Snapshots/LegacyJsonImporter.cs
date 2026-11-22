namespace Wastu.Data.Snapshots;

public enum LegacyImportStatus
{
    /// <summary>Berkas legacy tidak ada; memulai dengan data kosong itu sah.</summary>
    TidakAda,

    /// <summary>Snapshot berversi sudah ada; impor dilewati agar tidak pernah menggandakan data.</summary>
    SudahTersimpan,

    /// <summary>Legacy valid diimpor sekali ke snapshot berversi.</summary>
    Berhasil,

    /// <summary>Legacy kosong/rusak/tidak terbaca; berkas tidak diubah sama sekali.</summary>
    Rusak,

    /// <summary>Payload valid tetapi snapshot baru gagal disimpan; berkas legacy tetap dipertahankan.</summary>
    GagalMenyimpan
}

/// <summary>Hasil impor legacy untuk UI: tidak pernah memuat jalur berkas atau isi payload mentah.</summary>
public sealed record HasilImporLegacy<T>(LegacyImportStatus Status, T? Data, DateTimeOffset? WaktuSnapshotUtc,
    string Pesan) where T : notnull;

/// <summary>
/// Impor satu kali dan idempoten dari berkas JSON legacy tanpa versi (state.json) ke snapshot
/// berversi. Setelah impor berhasil berkas legacy dipindahkan menjadi cadangan .legacy.bak sehingga
/// startup berikutnya tidak mengimpor ulang. Berkas legacy yang rusak tidak pernah ditimpa,
/// dihapus, atau diganti dengan data kosong.
/// </summary>
public sealed class LegacyJsonImporter<T> where T : notnull
{
    private readonly SnapshotStore<T> _store;
    private readonly Func<string, T?> _bacaPayload;

    public LegacyJsonImporter(string jalurLegacy, SnapshotStore<T> store, Func<string, T?> bacaPayload)
    {
        if (string.IsNullOrWhiteSpace(jalurLegacy))
            throw new ArgumentException("Jalur berkas legacy wajib diisi.", nameof(jalurLegacy));
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(bacaPayload);
        JalurLegacy = jalurLegacy;
        _store = store;
        _bacaPayload = bacaPayload;
    }

    public string JalurLegacy { get; }

    /// <summary>Lokasi hasil pemindahan berkas legacy setelah impor berhasil.</summary>
    public string JalurCadangan => JalurLegacy + ".legacy.bak";

    public async Task<HasilImporLegacy<T>> ImporAsync(CancellationToken cancellationToken = default)
    {
        // Penanda idempoten utama: ada berkas snapshot berversi (utama atau cadangan) → jangan impor.
        if (File.Exists(_store.SnapshotPath) || File.Exists(_store.BackupPath))
            return new(LegacyImportStatus.SudahTersimpan, default, default,
                "Snapshot berversi sudah ada; impor legacy tidak dijalankan lagi.");

        if (!File.Exists(JalurLegacy))
            return new(LegacyImportStatus.TidakAda, default, default, "Berkas state.json legacy tidak ditemukan.");

        string isi;
        try
        {
            isi = await File.ReadAllTextAsync(JalurLegacy, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new(LegacyImportStatus.Rusak, default, default,
                "Berkas state.json legacy tidak dapat dibaca sehingga tidak diimpor. Berkas tetap dipertahankan.");
        }

        T? payload;
        try
        {
            payload = _bacaPayload(isi);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            payload = default;
        }
        if (payload is null)
            return new(LegacyImportStatus.Rusak, default, default,
                "Berkas state.json legacy rusak atau tidak valid sehingga tidak diimpor. " +
                "Berkas tetap dipertahankan apa adanya; perbaiki atau pindahkan berkas lalu pilih Coba Lagi.");

        // Waktu data = waktu ubah terakhir berkas legacy (UTC), bukan waktu pembacaan.
        DateTimeOffset waktu;
        try
        {
            var mentah = File.GetLastWriteTimeUtc(JalurLegacy);
            waktu = mentah == default ? DateTimeOffset.UtcNow : new DateTimeOffset(mentah);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            waktu = DateTimeOffset.UtcNow;
        }

        try
        {
            await _store.SaveAsync(payload, waktu, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            return new(LegacyImportStatus.GagalMenyimpan, default, default,
                "Data legacy valid tetapi snapshot baru belum dapat disimpan. Berkas legacy tetap dipertahankan.");
        }

        // Pemindahan hanya pelengkap: bila gagal pun impor tidak terulang karena snapshot sudah ada.
        try
        {
            File.Move(JalurLegacy, JalurCadangan, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
        return new(LegacyImportStatus.Berhasil, payload, waktu,
            "Data legacy dari state.json berhasil diimpor sekali ke penyimpanan snapshot. " +
            "Berkas lama disimpan sebagai state.json.legacy.bak.");
    }
}
