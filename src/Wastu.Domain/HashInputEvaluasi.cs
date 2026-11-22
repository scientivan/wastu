using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Wastu.Domain;

/// <summary>
/// Helper kanonikal untuk menghitung hash masukan evaluasi secara deterministik (PRD §4.3).
/// Mengikat luas komponen, profil iklim, versi katalog/bahan, bobot, dan konfigurasi model.
/// </summary>
public static class HashInputEvaluasi
{
    public static string Hitung(
        double luasM2,
        ProfilIklim iklim,
        KriteriaBobot bobot,
        KonfigurasiModel? model = null,
        string katalogIdentitas = "v1")
    {
        ArgumentNullException.ThrowIfNull(iklim);
        ArgumentNullException.ThrowIfNull(bobot);
        ArgumentException.ThrowIfNullOrWhiteSpace(katalogIdentitas);

        if (!double.IsFinite(luasM2) || luasM2 <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(luasM2), luasM2, "Luas komponen harus bernilai finite positif.");
        }

        bobot.PastikanValid();
        model ??= KonfigurasiModel.Bawaan;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"luas:{luasM2:R}|");
        sb.Append(CultureInfo.InvariantCulture, $"iklim:{iklim.CurahHujanTahunanMm:R},{iklim.KelembapanRataPct:R},{iklim.SuhuRataC:R},{iklim.JarakPesisirKm:R},{(int)iklim.TingkatKorosi},{iklim.RevisiLokasi}|");
        sb.Append($"provenance:{iklim.Provenance}:{System.Text.Json.JsonSerializer.Serialize(iklim.Metadata)}|");
        sb.Append(CultureInfo.InvariantCulture, $"bobot:{bobot.Termal:R},{bobot.Lembap:R},{bobot.Korosi:R},{bobot.Usia:R}|");
        sb.Append(CultureInfo.InvariantCulture, $"model:{model.VersiModel},{model.BobotLembapHujanAlpha:R},{model.AcuanCurahHujanMm:R},{model.AcuanJarakPesisirKm:R},{model.PitaTermalBatasBawah:R},{model.PitaTermalBatasAtasSejuk:R},{model.PitaTermalBatasAtasPanas:R},{model.SuhuSejukC:R},{model.SuhuPanasC:R},{model.FaktorPenuaanA:R},{model.AmbangTidakDisarankan:R},{model.AmbangCocok:R}|");
        sb.Append($"katalog:{katalogIdentitas}");

        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
