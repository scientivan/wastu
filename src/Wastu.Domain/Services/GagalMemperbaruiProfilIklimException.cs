namespace Wastu.Domain.Services;

/// <summary>
/// Pelaporan kegagalan mengambil ulang profil iklim ketika data hasil analisis
/// sebelumnya masih ada (PRD §8.4).
/// </summary>
/// <remarks>
/// <para>
/// PRD §8.4 meminta dua hal sekaligus saat panggilan API gagal: memberi pesan
/// eksplisit kepada pengguna, dan memakai data cache terakhir kalau ada.
/// Satu nilai balik <c>ProfilIklim</c> saja tidak bisa memenuhi keduanya, karena
/// tidak ada tempat untuk menaruh pesannya.
/// </para>
/// <para>
/// Galat ini menjembatannya: pesannya ada di pesan galat untuk antarmuka, dan
/// data lamanya ada di <see cref="ProfilTersimpan"/> supaya perbandingan bahan tetap
/// bisa ditampilkan. Tanpa ini, aplikasi harus memilih salah satu dari dua
/// instruksi PRD tersebut, atau mengarang perilaku ketiga.
/// </para>
/// </remarks>
public sealed class GagalMemperbaruiProfilIklimException : Exception
{
    /// <summary>Profil lama yang masih bisa dipakai, atau null bila belum pernah ada.</summary>
    public ProfilIklim? ProfilTersimpan { get; }

    public GagalMemperbaruiProfilIklimException(string pesan, ProfilIklim? profilTersimpan, Exception? penyebab = null)
        : base(pesan, penyebab)
    {
        ProfilTersimpan = profilTersimpan;
    }
}