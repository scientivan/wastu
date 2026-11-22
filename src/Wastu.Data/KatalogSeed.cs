using Wastu.Data.Entities;
using Wastu.Domain;

namespace Wastu.Data;

public static class KatalogSeed
{
    private const string Ice =
        "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)";

    private const string Epd =
        "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)";

    public static IReadOnlyList<BahanEntity> Bahan { get; } =
    [
        Dinding("000000000001", "Beton bertulang (in-situ)", 1.40, 4, 5, 50, 120, Ice, "Dinding beton bertulang in-situ tebal 150 mm dengan tulangan ganda dan selimut beton 25 mm"),
        Dinding("000000000002", "Beton ringan (hebel autoclave)", 0.12, 5, 3, 30, 60, Ice, "Blok AAC tebal 100 mm + spesi perekat tipis 3 mm + plesteran aci 10 mm"),
        Dinding("000000000003", "Beton polibeton (hebel)", 0.1, 4, 3, 25, 55, Ice, "Blok polibeton/hebel tebal 100 mm + plesteran 10 mm"),
        Dinding("000000000004", "Bata merah + plester aci", 0.70, 3, 4, 30, 65, Ice, "Dinding bata merah 1/2 batu tebal 100 mm + plester aci 15 mm di kedua sisi (total 130 mm)"),
        Dinding("000000000005", "Bata merah tanpa plester", 0.70, 2, 4, 20, 35, Ice, "Dinding bata merah ekspos 1/2 batu tebal 100 mm tanpa plesteran pelindung"),
        Dinding("000000000006", "Bata beton (concrete block)", 0.60, 4, 4, 40, 55, Ice, "Batako semen berongga tebal 100 mm + plesteran spesi 15 mm"),
        Dinding("000000000007", "Bata silikat berpori", 0.11, 5, 3, 30, 62, Epd, "Blok kalsium silikat tebal 100 mm + mortar perekat 3 mm"),
        Dinding("000000000008", "Bata ringan honeycomb (AHCC)", 0.09, 4, 3, 30, 50, Epd, "Blok beton seluler berstruktur sarang lebah tebal 100 mm + acian 5 mm"),
        Dinding("000000000009", "Panel gipsum (dry wall)", 0.25, 1, 1, 20, 28, Ice, "Papan gipsum tebal 12 mm ganda pada rangka baja ringan 50 mm (total 74 mm)"),
        Dinding("000000000010", "Papan kayu + pelindung", 0.15, 1, 1, 15, 32, Ice, "Papan kayu kamper/meranti tebal 20 mm dengan lapisan pelindung politur dan rangka kayu"),
        Dinding("000000000011", "Panel ACP (aluminium composite)", 0.2, 5, 5, 25, 190, Epd, "Panel komposit aluminium tebal 4 mm (kulit Al 0,5 mm + inti mineral) pada sub-konstruksi hollow"),
        Dinding("000000000012", "Panel EPS + plester", 0.04, 4, 4, 25, 70, Epd, "Panel insulasi expanded polystyrene tebal 50 mm dengan kawat kasa dan plesteran polimer 10 mm"),
        Dinding("000000000013", "Semen fiber reinforced board", 0.25, 5, 5, 30, 75, Epd, "Papan semen fiber tebal 8 mm rangkap pada rangka stud baja ringan 50 mm"),
        Dinding("000000000014", "Batu andesit (dinding batu alam)", 3, 5, 5, 60, 45, Ice, "Pelapis batu andesit tebal 20 mm direkatkan pada dinding bata pendukung dengan mortar 20 mm"),
        Dinding("000000000015", "Beton geopolimer", 0.45, 4, 4, 45, 90, Epd, "Dinding panel precast beton geopolimer abu terbang (fly ash) tebal 100 mm"),
        Atap("000000000016", "Dak beton bertulang", 1.40, 5, 5, 40, 150, Ice, "Plat dak beton bertulang tebal 120 mm + lapisan screed waterproofing 30 mm"),
        Atap("000000000017", "Genting beton", 0.40, 5, 5, 30, 95, Ice, "Genting beton tebal nominal 12 mm terpasang pada reng kayu/baja ringan dengan tumpangan standar"),
        Atap("000000000018", "Genting keramik", 0.30, 5, 5, 35, 110, Ice, "Genting keramik berglasir tebal nominal 12 mm pada reng baja ringan"),
        Atap("000000000019", "Genting tanah liat", 0.60, 4, 4, 30, 85, Ice, "Genting tanah liat tradisional bakar tebal nominal 10 mm pada reng dan usuk bambu/kayu"),
        Atap("000000000020", "Genteng aspal (shingle)", 0.08, 3, 2, 20, 80, Epd, "Genteng shingle bitumen tebal 3 mm di atas underlayment tahan air dan papan multipleks 9 mm"),
        Atap("000000000021", "Baja galvanis (trapezium)", 0.05, 3, 2, 15, 90, Ice, "Lembaran baja profil gelombang 0,4 mm + insulasi glass wool foil 50 mm (sistem perakitan atap)"),
        Atap("000000000022", "Baja AZ / galvalum", 0.05, 4, 4, 20, 105, Epd, "Lembaran profil galvalum (55% Al, 43.5% Zn) tebal 0,45 mm + insulasi bubble foil peredam panas"),
        Atap("000000000023", "Seng (zink)", 0.11, 2, 1, 12, 130, Ice, "Lembaran seng gelombang BJLS tebal 0,3 mm tanpa insulasi termal tambahan"),
        Atap("000000000024", "Constantra / fiber cement", 0.07, 4, 3, 25, 100, Epd, "Lembaran atap serat krisotil/fiber semen tebal 4 mm gelombang pada gording baja ringan"),
        Atap("000000000025", "Panel sandwich (inti EPS)", 0.04, 4, 4, 25, 95, Epd, "Panel komposit atap kulit galvalum 0,35 mm ganda dengan inti peredam EPS tebal 50 mm"),
    ];

    public static IReadOnlyList<SumberPropertiBahanEntity> SumberProperti { get; } =
    [
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000001"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000001"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 & literatur termal",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Nilai konduktivitas termal ekuivalen beton bertulang normal (1,4 W/(m·K)) untuk dinding struktur masif"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000001"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000001"),
            Properti = "ketahanan_lembap",
            Referensi = "Tinjauan durabilitas beton tropis",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: beton bertulang bermutu baik dengan selimut memadai memiliki ketahanan tinggi terhadap penetrasi air dan kelembapan"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000001"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000001"),
            Properti = "ketahanan_korosi",
            Referensi = "Tinjauan durabilitas beton terhadap klorida pesisir",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: ketahanan korosi sangat baik bila rasio air-semen dan ketebalan selimut beton memenuhi standar paparan laut"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000001"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000001"),
            Properti = "usia_pakai_nominal",
            Referensi = "Panduan Penilaian Kondisi Bangunan Gedung (Kementerian PUPR)",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 50 tahun untuk struktur beton bertulang dengan pemeliharaan teratur sebelum penyesuaian iklim proyek"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000001"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000001"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 120 kgCO2e/m2 dinormalisasi untuk dinding beton tebal 150 mm dengan rasio tulangan 1%"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000002"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000002"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 & katalog teknis AAC",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas termal blok aerasi 0,12 W/(m·K) memberikan sifat insulasi termal superior pada selubung bangunan"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000002"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000002"),
            Properti = "ketahanan_lembap",
            Referensi = "Tinjauan material AAC pada iklim lembap",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: struktur pori tertutup AAC menghambat penyerapan kapiler air bila diberi plesteran pelindung"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000002"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000002"),
            Properti = "ketahanan_korosi",
            Referensi = "Literatur keawetan dinding anorganik",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: bahan matriks silikat tidak berkarat namun pengikat logam instalasi rentan terhadap udara garam"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000002"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000002"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis Umur Bangunan PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 30 tahun dalam kondisi terlindung finishing plesteran luar"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000002"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000002"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 60 kgCO2e/m2 untuk dinding AAC tebal 100 mm termasuk mortar tipis perekat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000003"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000003"),
            Properti = "konduktivitas_termal",
            Referensi = "Studi konduktivitas beton polimer ringan",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,10 W/(m·K) untuk blok polibeton beragregat polistirena"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000003"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000003"),
            Properti = "ketahanan_lembap",
            Referensi = "Karakteristik serapan air komposit semen-polimer",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: serapan air rendah berkat komponen polimer sintetik penolak air"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000003"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000003"),
            Properti = "ketahanan_korosi",
            Referensi = "Tinjauan korosi pengikat dinding komposit",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: tahan korosi kimia moderat pada lingkungan garam atmosferik"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000003"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000003"),
            Properti = "usia_pakai_nominal",
            Referensi = "Estimasi siklus hidup bahan bangunan alternatif",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 25 tahun sebelum penggantian atau renovasi selubung dinding"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000003"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000003"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 55 kgCO2e/m2 dinormalisasi untuk perakitan dinding polibeton tebal 100 mm"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000004"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000004"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 (Konservasi Energi Selubung)",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas termal 0,70 W/(m·K) untuk dinding bata merah konvensional berplester aci dua sisi"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000004"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000004"),
            Properti = "ketahanan_lembap",
            Referensi = "Pedoman Bahan Bangunan Tropis Lembap",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: plesteran melindungi bata dari air hujan langsung, namun rembesan kapiler dapat terjadi bila waterproofing kurang baik"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000004"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000004"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan atmosferik pasangan bata keramik",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: keramik tanah liat bakar stabil terhadap udara pesisir, degradasi hanya pada mortar spesi bila terkena garam tinggi"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000004"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000004"),
            Properti = "usia_pakai_nominal",
            Referensi = "Panduan Penilaian Kondisi Bangunan Gedung (Kementerian PUPR)",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 30 tahun dalam pemeliharaan berkala pengecatan ulang selubung"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000004"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000004"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 65 kgCO2e/m2 mencakup pembakaran bata merah tradisional dan semen mortar plesteran"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000005"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000005"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas termal 0,70 W/(m·K) untuk pasangan bata merah ekspos"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000005"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000005"),
            Properti = "ketahanan_lembap",
            Referensi = "Studi porositas bata merah lokal",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 2/5: tanpa lapisan plester, air hujan mudah terserap ke dalam pori bata merah"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000005"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000005"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan kimia tanah liat bakar",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: tanah liat bakar tidak terkorosi oleh garam, namun pengkristalan garam dapat memicu spalling"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000005"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000005"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 20 tahun lebih pendek karena erosi cuaca langsung pada permukaan bata"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000005"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000005"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 35 kgCO2e/m2 lebih rendah karena tidak menggunakan semen plesteran acian ganda"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000006"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000006"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas termal 0,60 W/(m·K) memperhitungkan rongga udara internal batako beton"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000006"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000006"),
            Properti = "ketahanan_lembap",
            Referensi = "Karakteristik batako beton semen",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: ketahanan lembap baik dengan plesteran semen kedap air"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000006"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000006"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan atmosferik beton agregat pasir",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: tahan terhadap paparan garam pesisir bila spesi diplester rapat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000006"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000006"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Bangunan PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 40 tahun untuk dinding blok beton dengan perawatan berkala"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000006"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000006"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 55 kgCO2e/m2 untuk batako berongga tebal 100 mm"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000007"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000007"),
            Properti = "konduktivitas_termal",
            Referensi = "EPD kalsium silikat regional & data pabrikan",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,11 W/(m·K) dari struktur mikro berpori kalsium silikat hidrat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000007"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000007"),
            Properti = "ketahanan_lembap",
            Referensi = "Tinjauan ketahanan lembap silikat berpori",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: ketahanan terhadap kelembapan tinggi dan stabilitas dimensi sangat baik"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000007"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000007"),
            Properti = "ketahanan_korosi",
            Referensi = "Evaluasi ketahanan kimia dinding anorganik",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: ketahanan kimia moderat terhadap lingkungan garam"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000007"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000007"),
            Properti = "usia_pakai_nominal",
            Referensi = "Estimasi umur teknis bahan silikat",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 30 tahun pada iklim tropis dengan perlindungan selubung eksterior"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000007"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000007"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 62 kgCO2e/m2 berdasarkan deklarasi lingkungan produk kalsium silikat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000008"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000008"),
            Properti = "konduktivitas_termal",
            Referensi = "Studi termal beton seluler sarang lebah",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,09 W/(m·K) berkat geometri sel berongga sarang lebah yang memerangkap udara"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000008"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000008"),
            Properti = "ketahanan_lembap",
            Referensi = "Tinjauan durabilitas beton seluler",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: dinding seluler tahan air dengan pelapisan skim coat kedap"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000008"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000008"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan kimia dinding ringan",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: ketahanan korosi sedang terhadap salinitas pesisir"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000008"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000008"),
            Properti = "usia_pakai_nominal",
            Referensi = "Panduan teknis umur beton seluler",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 30 tahun dalam pemeliharaan standar selubung"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000008"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000008"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 50 kgCO2e/m2 karena pengurangan massa bahan baku semen per m2 dinding"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000009"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000009"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 & katalog teknis gipsum",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas termal 0,25 W/(m·K) untuk papan gipsum standar"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000009"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000009"),
            Properti = "ketahanan_lembap",
            Referensi = "Karakteristik gipsum pada RH tinggi",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 1/5: gipsum standar sangat rentan melunak dan berjamur pada kelembapan tinggi tropis"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000009"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000009"),
            Properti = "ketahanan_korosi",
            Referensi = "Evaluasi rangka hollow dan gipsum",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 1/5: sekrup dan rangka hollow sangat rentan korosi di dekat pesisir bila terpapar udara laut"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000009"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000009"),
            Properti = "usia_pakai_nominal",
            Referensi = "Buku Pedoman Teknis Konstruksi Kering",
            Versi = "2019",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 20 tahun bila dipakai pada partisi interior kering terlindung"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000009"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000009"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 28 kgCO2e/m2 untuk sistem partisi gipsum ringan"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000010"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000010"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 & literatur kayu tropis",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,15 W/(m·K) untuk kayu olahan tropis kering udara"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000010"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000010"),
            Properti = "ketahanan_lembap",
            Referensi = "Studi keawetan kayu tropis terhadap rayap dan jamur",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 1/5: kayu sangat rentan pelapukan dan serangan jamur/rayap pada iklim lembap tinggi"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000010"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000010"),
            Properti = "ketahanan_korosi",
            Referensi = "Degradasi material organik pada lingkungan garam",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 1/5: garam mempercepat pelapukan serat kayu dan korosi pada paku/pengencang logam"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000010"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000010"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis Keawetan Kayu Konstruksi PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 15 tahun membutuhkan pemeliharaan pelapisan pelindung rutin setiap 2-3 tahun"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000010"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000010"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 32 kgCO2e/m2 mencakup pemanenan, penggergajian, pengeringan kiln, dan coating"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000011"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000011"),
            Properti = "konduktivitas_termal",
            Referensi = "Katalog teknis ACP PVDF & uji laboratorium",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,20 W/(m·K) merepresentasikan sistem panel komposit Al-polietilena/mineral"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000011"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000011"),
            Properti = "ketahanan_lembap",
            Referensi = "Uji cuaca buatan cladding aluminium",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: lembaran aluminium kedap air sempurna tidak terpengaruh kelembapan udara"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000011"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000011"),
            Properti = "ketahanan_korosi",
            Referensi = "Uji ketahanan kabut garam ASTM B117 coating PVDF",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: lapisan PVDF memberikan proteksi maksimal terhadap udara bergaram pesisir"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000011"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000011"),
            Properti = "usia_pakai_nominal",
            Referensi = "Garansi teknis pabrikan cladding arsitektural",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 25 tahun untuk panel eksterior dengan coating fluorokarbon"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000011"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000011"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi tinggi 190 kgCO2e/m2 disebabkan energi peleburan bijih aluminium primer"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000012"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000012"),
            Properti = "konduktivitas_termal",
            Referensi = "ISO 10456 / katalog sistem EIFS",
            Versi = "2019",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,04 W/(m·K) mencerminkan daya insulasi termal superior busa polistirena tebal 50 mm"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000012"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000012"),
            Properti = "ketahanan_lembap",
            Referensi = "Tinjauan sistem plester polimer tahan cuaca",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: plester polimer elastis menghambat penetrasi air hujan ke inti insulasi"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000012"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000012"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan atmosferik material sintetis",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: EPS kebal terhadap garam, kawat kasa galvanis terlindungi plester tebal"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000012"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000012"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman sistem insulasi dinding eksterior",
            Versi = "2019",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 25 tahun bila plesteran terawat tanpa retak tembus"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000012"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000012"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 70 kgCO2e/m2 mencakup bahan petrokimia polistirena dan aditif polimer"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000013"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000013"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 & uji pabrikan papan semen",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,25 W/(m·K) untuk papan komposit semen berserat selulosa"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000013"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000013"),
            Properti = "ketahanan_lembap",
            Referensi = "Uji rendam dan ketahanan air semen fiber",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: bahan tidak lapuk atau membengkak bila terkena air hujan terus-menerus"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000013"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000013"),
            Properti = "ketahanan_korosi",
            Referensi = "Evaluasi ketahanan kimia semen komposit di daerah pesisir",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: matriks semen tidak korosif terhadap udara asin laut"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000013"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000013"),
            Properti = "usia_pakai_nominal",
            Referensi = "Katalog teknis umur layan semen fiber",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 30 tahun tahan rayap, jamur, dan cuaca tropis basah"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000013"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000013"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 75 kgCO2e/m2 untuk lembaran semen fiber 8 mm dan subframe baja ringan"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000014"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000014"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 & literatur sifat batuan beku",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 3,00 W/(m·K) merepresentasikan sifat batu andesit padat yang sangat cepat menghantarkan panas"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000014"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000014"),
            Properti = "ketahanan_lembap",
            Referensi = "Karakteristik serapan air batu alam andesit",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: batuan vulkanik andesit memiliki densitas tinggi dan ketahanan luar biasa terhadap air"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000014"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000014"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan pelapukan kimia batuan beku",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: sangat tahan terhadap udara pesisir bersalinitas tinggi dan asam lingkungan"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000014"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000014"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis Bangunan Warisan dan Monumen PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 60 tahun terpanjang di antara bahan dinding dengan daya tahan mekanik tinggi"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000014"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000014"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 45 kgCO2e/m2 rendah karena proses produksi hanya pemotongan batu tanpa pembakaran kimia"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000015"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000015"),
            Properti = "konduktivitas_termal",
            Referensi = "Studi sifat termofisik beton geopolimer fly-ash",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,45 W/(m·K) lebih rendah daripada beton konvensional berkat struktur pori alkali-teraktivasi"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000015"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000015"),
            Properti = "ketahanan_lembap",
            Referensi = "Uji permeabilitas beton geopolimer",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: mikrostruktur padat memberikan ketahanan penetrasi air dan kelembapan yang solid"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000015"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000015"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan korosi sulfat dan klorida geopolimer",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: ketahanan kimia terhadap klorida garam lebih unggul dibanding semen Portland biasa"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000015"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000015"),
            Properti = "usia_pakai_nominal",
            Referensi = "Estimasi durabilitas geopolimer jangka panjang",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 45 tahun untuk komponen pracetak berkualitas terkontrol"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000015"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000015"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 90 kgCO2e/m2 menghemat jejak karbon klinker semen dengan memanfaatkan abu terbang industri"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000016"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000016"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas termal 1,40 W/(m·K) untuk plat beton bertulang horizontal tebal 120 mm"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000016"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000016"),
            Properti = "ketahanan_lembap",
            Referensi = "Standar waterproofing atap dak beton",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: ketahanan lembap maksimal bila didukung membran waterproofing dan kemiringan drainase memadai"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000016"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000016"),
            Properti = "ketahanan_korosi",
            Referensi = "Evaluasi selimut beton pelindung tulangan pesisir",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: beton bermutu tinggi dengan aditif kedap air sangat tahan terhadap garam pesisir"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000016"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000016"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis Bangunan Gedung PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 40 tahun dengan inspeksi dan pelapisan ulang waterproofing berkala"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000016"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000016"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 150 kgCO2e/m2 mencakup massa beton bertulang tebal dan lapisan screed pelindung"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000017"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000017"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,40 W/(m·K) merepresentasikan sistem penutup atap genting beton dengan celah ventilasi reng"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000017"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000017"),
            Properti = "ketahanan_lembap",
            Referensi = "Uji impermeabilitas genting beton",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: lapisan cat polimer akrilik eksterior mencegah infiltrasi air hujan tropis lebat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000017"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000017"),
            Properti = "ketahanan_korosi",
            Referensi = "Durabilitas genting semen di lingkungan garam",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: bahan matriks beton anorganik tidak berkarat di daerah pesisir"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000017"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000017"),
            Properti = "usia_pakai_nominal",
            Referensi = "Panduan Spesifikasi Material Atap PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 30 tahun dengan pengecatan ulang setiap 10 tahun untuk menjaga kerapatan air"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000017"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000017"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 95 kgCO2e/m2 mencakup pencetakan beton dan curing uap genting per m2 atap terpasang"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000018"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000018"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,30 W/(m·K) merepresentasikan perakitan genting keramik dengan bantalan udara bawah atap"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000018"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000018"),
            Properti = "ketahanan_lembap",
            Referensi = "Uji daya serap air genting keramik berglasir",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: lapisan glasir vitreous membuat genting hampir nol serapan air (<1%)"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000018"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000018"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan kimia lapisan glasir keramik",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 5/5: sangat kebal terhadap asam, garam laut, dan radiasi ultraviolet"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000018"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000018"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis Konstruksi Atap PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 35 tahun dengan ketahanan warna dan bentuk yang sangat stabil"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000018"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000018"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 110 kgCO2e/m2 akibat proses pembakaran kiln suhu tinggi >1.100 C"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000019"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000019"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,60 W/(m·K) untuk genting tanah liat pres konvensional tanpa glasir"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000019"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000019"),
            Properti = "ketahanan_lembap",
            Referensi = "Karakteristik tanah liat bakar lokal",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: mengalirkan air hujan dengan baik pada sudut kemiringan atap >30 derajat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000019"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000019"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan atmosferik produk keramik tradisional",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: tanah liat bakar kebal karat, lumut dapat tumbuh di daerah sangat lembap"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000019"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000019"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Umur Bangunan PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 30 tahun umum digunakan pada rumah tapak tradisional Indonesia"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000019"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000019"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 85 kgCO2e/m2 untuk proses pembakaran genting tanah liat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000020"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000020"),
            Properti = "konduktivitas_termal",
            Referensi = "Katalog teknis atap shingle bitumen",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,08 W/(m·K) mencerminkan perakitan shingle aspal di atas papan kayu underlayment"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000020"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000020"),
            Properti = "ketahanan_lembap",
            Referensi = "Evaluasi atap bitumen pada iklim tropis basah",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: bitumen kedap air tetapi rentan terhadap genangan air dan pelunakan suhu ekstrem"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000020"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000020"),
            Properti = "ketahanan_korosi",
            Referensi = "Degradasi material bitumen akibat radiasi UV dan garam",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 2/5: butiran pelindung dapat terkikis angin laut dan paku shingle rentan korosi"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000020"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000020"),
            Properti = "usia_pakai_nominal",
            Referensi = "Spesifikasi atap aspal fleksibel",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 20 tahun sebelum kehilangan butiran mineral dan kelenturan bitumen"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000020"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000020"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 80 kgCO2e/m2 mencakup matriks serat kaca, aspal modifikasi, dan substrat papan"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000021"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000021"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000 & katalog sistem atap terinsulasi",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Nilai 0,05 W/(m·K) merepresentasikan konduktivitas termal ekuivalen sistem perakitan atap berinsulasi (lembaran baja profil + insulasi glass wool), bukan lembaran baja mentah (k ~50 W/(m·K))"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000021"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000021"),
            Properti = "ketahanan_lembap",
            Referensi = "Tinjauan atap lembaran logam profil",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: lembaran baja kedap air tetapi rentan kebocoran pada sekrup pengikat dan sambungan tumpang"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000021"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000021"),
            Properti = "ketahanan_korosi",
            Referensi = "Studi laju korosi lapisan seng galvanis di pesisir",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 2/5: lapisan galvanis seng standar cepat terkorosi pada jarak <5 km dari pantai akibat garam laut"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000021"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000021"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis Atap Logam PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia pakai nominal 15 tahun sebelum karat merusak lembaran atap pada kondisi lingkungan non-agresif"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000021"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000021"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 90 kgCO2e/m2 untuk lembaran baja galvanis tebal 0,4 mm termasuk rangka sekrup"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000022"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000022"),
            Properti = "konduktivitas_termal",
            Referensi = "Katalog teknis atap galvalum berinsulasi",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Nilai 0,05 W/(m·K) merepresentasikan konduktivitas termal ekuivalen perakitan atap dengan foil/insulasi termal penahan panas tropis, bukan lembaran logam telanjang"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000022"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000022"),
            Properti = "ketahanan_lembap",
            Referensi = "Kinerja pelapis aluminium-seng pada iklim tropis",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: kedap air sangat baik dengan sistem sekrup berpenutup karet EPDM"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000022"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000022"),
            Properti = "ketahanan_korosi",
            Referensi = "Uji korosi atmosferik paduan aluminium-seng 55%",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: paduan aluminium-seng memberikan perlindungan korosi hingga 4 kali lebih tahan dibanding galvanis murni"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000022"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000022"),
            Properti = "usia_pakai_nominal",
            Referensi = "Garansi teknis pabrikan baja lapis AZ150",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 20 tahun di lingkungan tropis atmosferik normal"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000022"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000022"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 105 kgCO2e/m2 mencakup baja lembaran lapis paduan Al-Zn tebal 0,45 mm"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000023"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000023"),
            Properti = "konduktivitas_termal",
            Referensi = "SNI 03-6389-2000",
            Versi = "2000",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Nilai 0,11 W/(m·K) merepresentasikan konduktivitas ekuivalen susunan atap lembaran seng tipis dengan bantalan udara reng"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000023"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000023"),
            Properti = "ketahanan_lembap",
            Referensi = "Perilaku seng gelombang terhadap kondensasi tropis",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 2/5: rentan kondensasi di sisi bawah lembaran dan karat putih bila terjebak air"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000023"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000023"),
            Properti = "ketahanan_korosi",
            Referensi = "Laju korosi lembaran seng tipis pada atmosfer pantai",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 1/5: sangat cepat berkarat dan berlubang pada jarak dekat pantai karena lapisan pelindung sangat tipis"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000023"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000023"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Teknis PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 12 tahun sebelum karat tembus memerlukan penggantian lembaran atap"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000023"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000023"),
            Properti = "faktor_emisi",
            Referensi = "ICE Database v3.0 (University of Bath)",
            Versi = "v3.0",
            Tautan = "https://circularecology.com/embodied-carbon-footprint-database.html",
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 130 kgCO2e/m2 untuk lembaran seng galvanis tipis"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000024"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000024"),
            Properti = "konduktivitas_termal",
            Referensi = "Katalog teknis atap fiber semen gelombang",
            Versi = "2020",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Konduktivitas 0,07 W/(m·K) mencerminkan sifat isolasi termal komposit semen serat bergelombang"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000024"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000024"),
            Properti = "ketahanan_lembap",
            Referensi = "Uji impermeabilitas lembaran semen berserat",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: tidak melengkung atau lapuk akibat air hujan, ventilasi atap tetap diperlukan"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000024"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000024"),
            Properti = "ketahanan_korosi",
            Referensi = "Ketahanan atmosferik semen fiber terhadap salinitas",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 3/5: lembaran semen tidak berkarat namun pengikat sekrup dapat berkarat di dekat laut"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000024"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000024"),
            Properti = "usia_pakai_nominal",
            Referensi = "Pedoman Spesifikasi Material Penutup Atap PUPR",
            Versi = "2018",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 25 tahun untuk penutup atap komposit semen serat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000024"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000024"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 100 kgCO2e/m2 mencakup proses autoclaving dan penguatan serat komposit"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0001-4000-8000-000000000025"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000025"),
            Properti = "konduktivitas_termal",
            Referensi = "ISO 10456 / katalog panel sandwich insulasi",
            Versi = "2019",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Nilai 0,04 W/(m·K) merepresentasikan konduktivitas termal efektif inti insulasi EPS tebal 50 mm"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0002-4000-8000-000000000025"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000025"),
            Properti = "ketahanan_lembap",
            Referensi = "Uji kekedapan sambungan interlocking atap sandwich",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: sistem sambungan overlap dengan sealant mencegah infiltrasi air hujan lebat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0003-4000-8000-000000000025"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000025"),
            Properti = "ketahanan_korosi",
            Referensi = "Durabilitas lapisan galvalum pada panel insulasi",
            Versi = "v1.0",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Indeks 4/5: lapisan luar galvalum ber-coating cat oven tahan terhadap lingkungan korosif moderat"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0004-4000-8000-000000000025"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000025"),
            Properti = "usia_pakai_nominal",
            Referensi = "Spesifikasi umur layan panel atap terinsulasi",
            Versi = "2019",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Usia nominal 25 tahun selama lapisan pelindung dan sambungan sealant terjaga"
        },
        new SumberPropertiBahanEntity
        {
            Id = new Guid("d0000000-0005-4000-8000-000000000025"),
            BahanId = new Guid("d0000000-0000-4000-8000-000000000025"),
            Properti = "faktor_emisi",
            Referensi = "EPD generik pabrikan regional",
            Versi = "2021",
            Tautan = null,
            Status = "perkiraan",
            CatatanKonversi = "Faktor emisi 95 kgCO2e/m2 untuk panel sandwich 50 mm lengkap dengan dua sisi lembaran galvalum"
        },
    ];

    public static IReadOnlyList<SumberPropertiBahan> SumberPropertiDomain { get; } =
        SumberProperti.Select(s => new SumberPropertiBahan(
            s.BahanId,
            s.Properti,
            s.Referensi,
            s.Versi,
            s.Tautan,
            Enum.TryParse<StatusVerifikasiProperti>(s.Status, true, out var st) ? st : StatusVerifikasiProperti.Perkiraan,
            s.CatatanKonversi)).ToList();

    public static IReadOnlyList<Bahan> BahanDomain { get; } =
        Bahan.Select(b =>
        {
            var sumberList = SumberPropertiDomain.Where(s => s.BahanId == b.Id).ToList();
            return new Bahan(
                b.Id,
                b.Nama,
                b.Jenis,
                b.KonduktivitasTermal,
                b.KetahananLembap,
                b.KetahananKorosi,
                b.UsiaPakaiNominalTahun,
                b.FaktorEmisi,
                b.SumberData,
                b.SusunanKetebalan,
                b.VersiData,
                sumberList);
        }).ToList();

    private static BahanEntity Dinding(
        string idAkhir, string nama, double konduktivitas, int lembap, int korosi,
        double usia, double emisi, string sumber, string susunan)
        => Buat(idAkhir, nama, JenisKomponen.Dinding, konduktivitas, lembap, korosi, usia, emisi, sumber, susunan);

    private static BahanEntity Atap(
        string idAkhir, string nama, double konduktivitas, int lembap, int korosi,
        double usia, double emisi, string sumber, string susunan)
        => Buat(idAkhir, nama, JenisKomponen.Atap, konduktivitas, lembap, korosi, usia, emisi, sumber, susunan);

    private static BahanEntity Buat(
        string idAkhir, string nama, JenisKomponen jenis, double konduktivitas, int lembap,
        int korosi, double usia, double emisi, string sumber, string susunan)
        => new()
        {
            Id = new Guid($"d0000000-0000-4000-8000-{idAkhir}"),
            Nama = nama,
            Jenis = jenis,
            KonduktivitasTermal = konduktivitas,
            KetahananLembap = lembap,
            KetahananKorosi = korosi,
            UsiaPakaiNominalTahun = usia,
            FaktorEmisi = emisi,
            SumberData = sumber,
            SusunanKetebalan = susunan,
            VersiData = 1
        };
}
