using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wastu.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedCanonicalCatalogProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000001"),
                column: "susunan_ketebalan",
                value: "Dinding beton bertulang in-situ tebal 150 mm dengan tulangan ganda dan selimut beton 25 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000002"),
                column: "susunan_ketebalan",
                value: "Blok AAC tebal 100 mm + spesi perekat tipis 3 mm + plesteran aci 10 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000003"),
                column: "susunan_ketebalan",
                value: "Blok polibeton/hebel tebal 100 mm + plesteran 10 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000004"),
                column: "susunan_ketebalan",
                value: "Dinding bata merah 1/2 batu tebal 100 mm + plester aci 15 mm di kedua sisi (total 130 mm)");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000005"),
                column: "susunan_ketebalan",
                value: "Dinding bata merah ekspos 1/2 batu tebal 100 mm tanpa plesteran pelindung");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000006"),
                column: "susunan_ketebalan",
                value: "Batako semen berongga tebal 100 mm + plesteran spesi 15 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000007"),
                column: "susunan_ketebalan",
                value: "Blok kalsium silikat tebal 100 mm + mortar perekat 3 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000008"),
                column: "susunan_ketebalan",
                value: "Blok beton seluler berstruktur sarang lebah tebal 100 mm + acian 5 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000009"),
                column: "susunan_ketebalan",
                value: "Papan gipsum tebal 12 mm ganda pada rangka baja ringan 50 mm (total 74 mm)");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000010"),
                column: "susunan_ketebalan",
                value: "Papan kayu kamper/meranti tebal 20 mm dengan lapisan pelindung politur dan rangka kayu");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000011"),
                column: "susunan_ketebalan",
                value: "Panel komposit aluminium tebal 4 mm (kulit Al 0,5 mm + inti mineral) pada sub-konstruksi hollow");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000012"),
                column: "susunan_ketebalan",
                value: "Panel insulasi expanded polystyrene tebal 50 mm dengan kawat kasa dan plesteran polimer 10 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000013"),
                column: "susunan_ketebalan",
                value: "Papan semen fiber tebal 8 mm rangkap pada rangka stud baja ringan 50 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000014"),
                column: "susunan_ketebalan",
                value: "Pelapis batu andesit tebal 20 mm direkatkan pada dinding bata pendukung dengan mortar 20 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000015"),
                column: "susunan_ketebalan",
                value: "Dinding panel precast beton geopolimer abu terbang (fly ash) tebal 100 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000016"),
                column: "susunan_ketebalan",
                value: "Plat dak beton bertulang tebal 120 mm + lapisan screed waterproofing 30 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000017"),
                column: "susunan_ketebalan",
                value: "Genting beton tebal nominal 12 mm terpasang pada reng kayu/baja ringan dengan tumpangan standar");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000018"),
                column: "susunan_ketebalan",
                value: "Genting keramik berglasir tebal nominal 12 mm pada reng baja ringan");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000019"),
                column: "susunan_ketebalan",
                value: "Genting tanah liat tradisional bakar tebal nominal 10 mm pada reng dan usuk bambu/kayu");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000020"),
                column: "susunan_ketebalan",
                value: "Genteng shingle bitumen tebal 3 mm di atas underlayment tahan air dan papan multipleks 9 mm");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000021"),
                column: "susunan_ketebalan",
                value: "Lembaran baja profil gelombang 0,4 mm + insulasi glass wool foil 50 mm (sistem perakitan atap)");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000022"),
                column: "susunan_ketebalan",
                value: "Lembaran profil galvalum (55% Al, 43.5% Zn) tebal 0,45 mm + insulasi bubble foil peredam panas");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000023"),
                column: "susunan_ketebalan",
                value: "Lembaran seng gelombang BJLS tebal 0,3 mm tanpa insulasi termal tambahan");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000024"),
                column: "susunan_ketebalan",
                value: "Lembaran atap serat krisotil/fiber semen tebal 4 mm gelombang pada gording baja ringan");

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000025"),
                column: "susunan_ketebalan",
                value: "Panel komposit atap kulit galvalum 0,35 mm ganda dengan inti peredam EPS tebal 50 mm");

            migrationBuilder.InsertData(
                table: "sumber_properti_bahan",
                columns: new[] { "id", "bahan_id", "catatan_konversi", "properti", "referensi", "status", "tautan", "versi" },
                values: new object[,]
                {
                    { new Guid("d0000000-0001-4000-8000-000000000001"), new Guid("d0000000-0000-4000-8000-000000000001"), "Nilai konduktivitas termal ekuivalen beton bertulang normal (1,4 W/(m·K)) untuk dinding struktur masif", "konduktivitas_termal", "SNI 03-6389-2000 & literatur termal", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000002"), new Guid("d0000000-0000-4000-8000-000000000002"), "Konduktivitas termal blok aerasi 0,12 W/(m·K) memberikan sifat insulasi termal superior pada selubung bangunan", "konduktivitas_termal", "SNI 03-6389-2000 & katalog teknis AAC", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000003"), new Guid("d0000000-0000-4000-8000-000000000003"), "Konduktivitas 0,10 W/(m·K) untuk blok polibeton beragregat polistirena", "konduktivitas_termal", "Studi konduktivitas beton polimer ringan", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0001-4000-8000-000000000004"), new Guid("d0000000-0000-4000-8000-000000000004"), "Konduktivitas termal 0,70 W/(m·K) untuk dinding bata merah konvensional berplester aci dua sisi", "konduktivitas_termal", "SNI 03-6389-2000 (Konservasi Energi Selubung)", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000005"), new Guid("d0000000-0000-4000-8000-000000000005"), "Konduktivitas termal 0,70 W/(m·K) untuk pasangan bata merah ekspos", "konduktivitas_termal", "SNI 03-6389-2000", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000006"), new Guid("d0000000-0000-4000-8000-000000000006"), "Konduktivitas termal 0,60 W/(m·K) memperhitungkan rongga udara internal batako beton", "konduktivitas_termal", "SNI 03-6389-2000", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000007"), new Guid("d0000000-0000-4000-8000-000000000007"), "Konduktivitas 0,11 W/(m·K) dari struktur mikro berpori kalsium silikat hidrat", "konduktivitas_termal", "EPD kalsium silikat regional & data pabrikan", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0001-4000-8000-000000000008"), new Guid("d0000000-0000-4000-8000-000000000008"), "Konduktivitas 0,09 W/(m·K) berkat geometri sel berongga sarang lebah yang memerangkap udara", "konduktivitas_termal", "Studi termal beton seluler sarang lebah", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0001-4000-8000-000000000009"), new Guid("d0000000-0000-4000-8000-000000000009"), "Konduktivitas termal 0,25 W/(m·K) untuk papan gipsum standar", "konduktivitas_termal", "SNI 03-6389-2000 & katalog teknis gipsum", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000010"), new Guid("d0000000-0000-4000-8000-000000000010"), "Konduktivitas 0,15 W/(m·K) untuk kayu olahan tropis kering udara", "konduktivitas_termal", "SNI 03-6389-2000 & literatur kayu tropis", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000011"), new Guid("d0000000-0000-4000-8000-000000000011"), "Konduktivitas 0,20 W/(m·K) merepresentasikan sistem panel komposit Al-polietilena/mineral", "konduktivitas_termal", "Katalog teknis ACP PVDF & uji laboratorium", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0001-4000-8000-000000000012"), new Guid("d0000000-0000-4000-8000-000000000012"), "Konduktivitas 0,04 W/(m·K) mencerminkan daya insulasi termal superior busa polistirena tebal 50 mm", "konduktivitas_termal", "ISO 10456 / katalog sistem EIFS", "perkiraan", null, "2019" },
                    { new Guid("d0000000-0001-4000-8000-000000000013"), new Guid("d0000000-0000-4000-8000-000000000013"), "Konduktivitas 0,25 W/(m·K) untuk papan komposit semen berserat selulosa", "konduktivitas_termal", "SNI 03-6389-2000 & uji pabrikan papan semen", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000014"), new Guid("d0000000-0000-4000-8000-000000000014"), "Konduktivitas 3,00 W/(m·K) merepresentasikan sifat batu andesit padat yang sangat cepat menghantarkan panas", "konduktivitas_termal", "SNI 03-6389-2000 & literatur sifat batuan beku", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000015"), new Guid("d0000000-0000-4000-8000-000000000015"), "Konduktivitas 0,45 W/(m·K) lebih rendah daripada beton konvensional berkat struktur pori alkali-teraktivasi", "konduktivitas_termal", "Studi sifat termofisik beton geopolimer fly-ash", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0001-4000-8000-000000000016"), new Guid("d0000000-0000-4000-8000-000000000016"), "Konduktivitas termal 1,40 W/(m·K) untuk plat beton bertulang horizontal tebal 120 mm", "konduktivitas_termal", "SNI 03-6389-2000", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000017"), new Guid("d0000000-0000-4000-8000-000000000017"), "Konduktivitas 0,40 W/(m·K) merepresentasikan sistem penutup atap genting beton dengan celah ventilasi reng", "konduktivitas_termal", "SNI 03-6389-2000", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000018"), new Guid("d0000000-0000-4000-8000-000000000018"), "Konduktivitas 0,30 W/(m·K) merepresentasikan perakitan genting keramik dengan bantalan udara bawah atap", "konduktivitas_termal", "SNI 03-6389-2000", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000019"), new Guid("d0000000-0000-4000-8000-000000000019"), "Konduktivitas 0,60 W/(m·K) untuk genting tanah liat pres konvensional tanpa glasir", "konduktivitas_termal", "SNI 03-6389-2000", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000020"), new Guid("d0000000-0000-4000-8000-000000000020"), "Konduktivitas 0,08 W/(m·K) mencerminkan perakitan shingle aspal di atas papan kayu underlayment", "konduktivitas_termal", "Katalog teknis atap shingle bitumen", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0001-4000-8000-000000000021"), new Guid("d0000000-0000-4000-8000-000000000021"), "Nilai 0,05 W/(m·K) merepresentasikan konduktivitas termal ekuivalen sistem perakitan atap berinsulasi (lembaran baja profil + insulasi glass wool), bukan lembaran baja mentah (k ~50 W/(m·K))", "konduktivitas_termal", "SNI 03-6389-2000 & katalog sistem atap terinsulasi", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000022"), new Guid("d0000000-0000-4000-8000-000000000022"), "Nilai 0,05 W/(m·K) merepresentasikan konduktivitas termal ekuivalen perakitan atap dengan foil/insulasi termal penahan panas tropis, bukan lembaran logam telanjang", "konduktivitas_termal", "Katalog teknis atap galvalum berinsulasi", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0001-4000-8000-000000000023"), new Guid("d0000000-0000-4000-8000-000000000023"), "Nilai 0,11 W/(m·K) merepresentasikan konduktivitas ekuivalen susunan atap lembaran seng tipis dengan bantalan udara reng", "konduktivitas_termal", "SNI 03-6389-2000", "perkiraan", null, "2000" },
                    { new Guid("d0000000-0001-4000-8000-000000000024"), new Guid("d0000000-0000-4000-8000-000000000024"), "Konduktivitas 0,07 W/(m·K) mencerminkan sifat isolasi termal komposit semen serat bergelombang", "konduktivitas_termal", "Katalog teknis atap fiber semen gelombang", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0001-4000-8000-000000000025"), new Guid("d0000000-0000-4000-8000-000000000025"), "Nilai 0,04 W/(m·K) merepresentasikan konduktivitas termal efektif inti insulasi EPS tebal 50 mm", "konduktivitas_termal", "ISO 10456 / katalog panel sandwich insulasi", "perkiraan", null, "2019" },
                    { new Guid("d0000000-0002-4000-8000-000000000001"), new Guid("d0000000-0000-4000-8000-000000000001"), "Indeks 4/5: beton bertulang bermutu baik dengan selimut memadai memiliki ketahanan tinggi terhadap penetrasi air dan kelembapan", "ketahanan_lembap", "Tinjauan durabilitas beton tropis", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000002"), new Guid("d0000000-0000-4000-8000-000000000002"), "Indeks 5/5: struktur pori tertutup AAC menghambat penyerapan kapiler air bila diberi plesteran pelindung", "ketahanan_lembap", "Tinjauan material AAC pada iklim lembap", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000003"), new Guid("d0000000-0000-4000-8000-000000000003"), "Indeks 4/5: serapan air rendah berkat komponen polimer sintetik penolak air", "ketahanan_lembap", "Karakteristik serapan air komposit semen-polimer", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000004"), new Guid("d0000000-0000-4000-8000-000000000004"), "Indeks 3/5: plesteran melindungi bata dari air hujan langsung, namun rembesan kapiler dapat terjadi bila waterproofing kurang baik", "ketahanan_lembap", "Pedoman Bahan Bangunan Tropis Lembap", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000005"), new Guid("d0000000-0000-4000-8000-000000000005"), "Indeks 2/5: tanpa lapisan plester, air hujan mudah terserap ke dalam pori bata merah", "ketahanan_lembap", "Studi porositas bata merah lokal", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000006"), new Guid("d0000000-0000-4000-8000-000000000006"), "Indeks 4/5: ketahanan lembap baik dengan plesteran semen kedap air", "ketahanan_lembap", "Karakteristik batako beton semen", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000007"), new Guid("d0000000-0000-4000-8000-000000000007"), "Indeks 5/5: ketahanan terhadap kelembapan tinggi dan stabilitas dimensi sangat baik", "ketahanan_lembap", "Tinjauan ketahanan lembap silikat berpori", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000008"), new Guid("d0000000-0000-4000-8000-000000000008"), "Indeks 4/5: dinding seluler tahan air dengan pelapisan skim coat kedap", "ketahanan_lembap", "Tinjauan durabilitas beton seluler", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000009"), new Guid("d0000000-0000-4000-8000-000000000009"), "Indeks 1/5: gipsum standar sangat rentan melunak dan berjamur pada kelembapan tinggi tropis", "ketahanan_lembap", "Karakteristik gipsum pada RH tinggi", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000010"), new Guid("d0000000-0000-4000-8000-000000000010"), "Indeks 1/5: kayu sangat rentan pelapukan dan serangan jamur/rayap pada iklim lembap tinggi", "ketahanan_lembap", "Studi keawetan kayu tropis terhadap rayap dan jamur", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000011"), new Guid("d0000000-0000-4000-8000-000000000011"), "Indeks 5/5: lembaran aluminium kedap air sempurna tidak terpengaruh kelembapan udara", "ketahanan_lembap", "Uji cuaca buatan cladding aluminium", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000012"), new Guid("d0000000-0000-4000-8000-000000000012"), "Indeks 4/5: plester polimer elastis menghambat penetrasi air hujan ke inti insulasi", "ketahanan_lembap", "Tinjauan sistem plester polimer tahan cuaca", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000013"), new Guid("d0000000-0000-4000-8000-000000000013"), "Indeks 5/5: bahan tidak lapuk atau membengkak bila terkena air hujan terus-menerus", "ketahanan_lembap", "Uji rendam dan ketahanan air semen fiber", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000014"), new Guid("d0000000-0000-4000-8000-000000000014"), "Indeks 5/5: batuan vulkanik andesit memiliki densitas tinggi dan ketahanan luar biasa terhadap air", "ketahanan_lembap", "Karakteristik serapan air batu alam andesit", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000015"), new Guid("d0000000-0000-4000-8000-000000000015"), "Indeks 4/5: mikrostruktur padat memberikan ketahanan penetrasi air dan kelembapan yang solid", "ketahanan_lembap", "Uji permeabilitas beton geopolimer", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000016"), new Guid("d0000000-0000-4000-8000-000000000016"), "Indeks 5/5: ketahanan lembap maksimal bila didukung membran waterproofing dan kemiringan drainase memadai", "ketahanan_lembap", "Standar waterproofing atap dak beton", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000017"), new Guid("d0000000-0000-4000-8000-000000000017"), "Indeks 5/5: lapisan cat polimer akrilik eksterior mencegah infiltrasi air hujan tropis lebat", "ketahanan_lembap", "Uji impermeabilitas genting beton", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000018"), new Guid("d0000000-0000-4000-8000-000000000018"), "Indeks 5/5: lapisan glasir vitreous membuat genting hampir nol serapan air (<1%)", "ketahanan_lembap", "Uji daya serap air genting keramik berglasir", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000019"), new Guid("d0000000-0000-4000-8000-000000000019"), "Indeks 4/5: mengalirkan air hujan dengan baik pada sudut kemiringan atap >30 derajat", "ketahanan_lembap", "Karakteristik tanah liat bakar lokal", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000020"), new Guid("d0000000-0000-4000-8000-000000000020"), "Indeks 3/5: bitumen kedap air tetapi rentan terhadap genangan air dan pelunakan suhu ekstrem", "ketahanan_lembap", "Evaluasi atap bitumen pada iklim tropis basah", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000021"), new Guid("d0000000-0000-4000-8000-000000000021"), "Indeks 3/5: lembaran baja kedap air tetapi rentan kebocoran pada sekrup pengikat dan sambungan tumpang", "ketahanan_lembap", "Tinjauan atap lembaran logam profil", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000022"), new Guid("d0000000-0000-4000-8000-000000000022"), "Indeks 4/5: kedap air sangat baik dengan sistem sekrup berpenutup karet EPDM", "ketahanan_lembap", "Kinerja pelapis aluminium-seng pada iklim tropis", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000023"), new Guid("d0000000-0000-4000-8000-000000000023"), "Indeks 2/5: rentan kondensasi di sisi bawah lembaran dan karat putih bila terjebak air", "ketahanan_lembap", "Perilaku seng gelombang terhadap kondensasi tropis", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000024"), new Guid("d0000000-0000-4000-8000-000000000024"), "Indeks 4/5: tidak melengkung atau lapuk akibat air hujan, ventilasi atap tetap diperlukan", "ketahanan_lembap", "Uji impermeabilitas lembaran semen berserat", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0002-4000-8000-000000000025"), new Guid("d0000000-0000-4000-8000-000000000025"), "Indeks 4/5: sistem sambungan overlap dengan sealant mencegah infiltrasi air hujan lebat", "ketahanan_lembap", "Uji kekedapan sambungan interlocking atap sandwich", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000001"), new Guid("d0000000-0000-4000-8000-000000000001"), "Indeks 5/5: ketahanan korosi sangat baik bila rasio air-semen dan ketebalan selimut beton memenuhi standar paparan laut", "ketahanan_korosi", "Tinjauan durabilitas beton terhadap klorida pesisir", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000002"), new Guid("d0000000-0000-4000-8000-000000000002"), "Indeks 3/5: bahan matriks silikat tidak berkarat namun pengikat logam instalasi rentan terhadap udara garam", "ketahanan_korosi", "Literatur keawetan dinding anorganik", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000003"), new Guid("d0000000-0000-4000-8000-000000000003"), "Indeks 3/5: tahan korosi kimia moderat pada lingkungan garam atmosferik", "ketahanan_korosi", "Tinjauan korosi pengikat dinding komposit", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000004"), new Guid("d0000000-0000-4000-8000-000000000004"), "Indeks 4/5: keramik tanah liat bakar stabil terhadap udara pesisir, degradasi hanya pada mortar spesi bila terkena garam tinggi", "ketahanan_korosi", "Ketahanan atmosferik pasangan bata keramik", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000005"), new Guid("d0000000-0000-4000-8000-000000000005"), "Indeks 4/5: tanah liat bakar tidak terkorosi oleh garam, namun pengkristalan garam dapat memicu spalling", "ketahanan_korosi", "Ketahanan kimia tanah liat bakar", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000006"), new Guid("d0000000-0000-4000-8000-000000000006"), "Indeks 4/5: tahan terhadap paparan garam pesisir bila spesi diplester rapat", "ketahanan_korosi", "Ketahanan atmosferik beton agregat pasir", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000007"), new Guid("d0000000-0000-4000-8000-000000000007"), "Indeks 3/5: ketahanan kimia moderat terhadap lingkungan garam", "ketahanan_korosi", "Evaluasi ketahanan kimia dinding anorganik", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000008"), new Guid("d0000000-0000-4000-8000-000000000008"), "Indeks 3/5: ketahanan korosi sedang terhadap salinitas pesisir", "ketahanan_korosi", "Ketahanan kimia dinding ringan", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000009"), new Guid("d0000000-0000-4000-8000-000000000009"), "Indeks 1/5: sekrup dan rangka hollow sangat rentan korosi di dekat pesisir bila terpapar udara laut", "ketahanan_korosi", "Evaluasi rangka hollow dan gipsum", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000010"), new Guid("d0000000-0000-4000-8000-000000000010"), "Indeks 1/5: garam mempercepat pelapukan serat kayu dan korosi pada paku/pengencang logam", "ketahanan_korosi", "Degradasi material organik pada lingkungan garam", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000011"), new Guid("d0000000-0000-4000-8000-000000000011"), "Indeks 5/5: lapisan PVDF memberikan proteksi maksimal terhadap udara bergaram pesisir", "ketahanan_korosi", "Uji ketahanan kabut garam ASTM B117 coating PVDF", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000012"), new Guid("d0000000-0000-4000-8000-000000000012"), "Indeks 4/5: EPS kebal terhadap garam, kawat kasa galvanis terlindungi plester tebal", "ketahanan_korosi", "Ketahanan atmosferik material sintetis", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000013"), new Guid("d0000000-0000-4000-8000-000000000013"), "Indeks 5/5: matriks semen tidak korosif terhadap udara asin laut", "ketahanan_korosi", "Evaluasi ketahanan kimia semen komposit di daerah pesisir", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000014"), new Guid("d0000000-0000-4000-8000-000000000014"), "Indeks 5/5: sangat tahan terhadap udara pesisir bersalinitas tinggi dan asam lingkungan", "ketahanan_korosi", "Ketahanan pelapukan kimia batuan beku", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000015"), new Guid("d0000000-0000-4000-8000-000000000015"), "Indeks 4/5: ketahanan kimia terhadap klorida garam lebih unggul dibanding semen Portland biasa", "ketahanan_korosi", "Ketahanan korosi sulfat dan klorida geopolimer", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000016"), new Guid("d0000000-0000-4000-8000-000000000016"), "Indeks 5/5: beton bermutu tinggi dengan aditif kedap air sangat tahan terhadap garam pesisir", "ketahanan_korosi", "Evaluasi selimut beton pelindung tulangan pesisir", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000017"), new Guid("d0000000-0000-4000-8000-000000000017"), "Indeks 5/5: bahan matriks beton anorganik tidak berkarat di daerah pesisir", "ketahanan_korosi", "Durabilitas genting semen di lingkungan garam", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000018"), new Guid("d0000000-0000-4000-8000-000000000018"), "Indeks 5/5: sangat kebal terhadap asam, garam laut, dan radiasi ultraviolet", "ketahanan_korosi", "Ketahanan kimia lapisan glasir keramik", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000019"), new Guid("d0000000-0000-4000-8000-000000000019"), "Indeks 4/5: tanah liat bakar kebal karat, lumut dapat tumbuh di daerah sangat lembap", "ketahanan_korosi", "Ketahanan atmosferik produk keramik tradisional", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000020"), new Guid("d0000000-0000-4000-8000-000000000020"), "Indeks 2/5: butiran pelindung dapat terkikis angin laut dan paku shingle rentan korosi", "ketahanan_korosi", "Degradasi material bitumen akibat radiasi UV dan garam", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000021"), new Guid("d0000000-0000-4000-8000-000000000021"), "Indeks 2/5: lapisan galvanis seng standar cepat terkorosi pada jarak <5 km dari pantai akibat garam laut", "ketahanan_korosi", "Studi laju korosi lapisan seng galvanis di pesisir", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000022"), new Guid("d0000000-0000-4000-8000-000000000022"), "Indeks 4/5: paduan aluminium-seng memberikan perlindungan korosi hingga 4 kali lebih tahan dibanding galvanis murni", "ketahanan_korosi", "Uji korosi atmosferik paduan aluminium-seng 55%", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000023"), new Guid("d0000000-0000-4000-8000-000000000023"), "Indeks 1/5: sangat cepat berkarat dan berlubang pada jarak dekat pantai karena lapisan pelindung sangat tipis", "ketahanan_korosi", "Laju korosi lembaran seng tipis pada atmosfer pantai", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000024"), new Guid("d0000000-0000-4000-8000-000000000024"), "Indeks 3/5: lembaran semen tidak berkarat namun pengikat sekrup dapat berkarat di dekat laut", "ketahanan_korosi", "Ketahanan atmosferik semen fiber terhadap salinitas", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0003-4000-8000-000000000025"), new Guid("d0000000-0000-4000-8000-000000000025"), "Indeks 4/5: lapisan luar galvalum ber-coating cat oven tahan terhadap lingkungan korosif moderat", "ketahanan_korosi", "Durabilitas lapisan galvalum pada panel insulasi", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0004-4000-8000-000000000001"), new Guid("d0000000-0000-4000-8000-000000000001"), "Usia pakai nominal 50 tahun untuk struktur beton bertulang dengan pemeliharaan teratur sebelum penyesuaian iklim proyek", "usia_pakai_nominal", "Panduan Penilaian Kondisi Bangunan Gedung (Kementerian PUPR)", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000002"), new Guid("d0000000-0000-4000-8000-000000000002"), "Usia pakai nominal 30 tahun dalam kondisi terlindung finishing plesteran luar", "usia_pakai_nominal", "Pedoman Teknis Umur Bangunan PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000003"), new Guid("d0000000-0000-4000-8000-000000000003"), "Usia nominal 25 tahun sebelum penggantian atau renovasi selubung dinding", "usia_pakai_nominal", "Estimasi siklus hidup bahan bangunan alternatif", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0004-4000-8000-000000000004"), new Guid("d0000000-0000-4000-8000-000000000004"), "Usia nominal 30 tahun dalam pemeliharaan berkala pengecatan ulang selubung", "usia_pakai_nominal", "Panduan Penilaian Kondisi Bangunan Gedung (Kementerian PUPR)", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000005"), new Guid("d0000000-0000-4000-8000-000000000005"), "Usia pakai nominal 20 tahun lebih pendek karena erosi cuaca langsung pada permukaan bata", "usia_pakai_nominal", "Pedoman Teknis PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000006"), new Guid("d0000000-0000-4000-8000-000000000006"), "Usia nominal 40 tahun untuk dinding blok beton dengan perawatan berkala", "usia_pakai_nominal", "Pedoman Bangunan PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000007"), new Guid("d0000000-0000-4000-8000-000000000007"), "Usia nominal 30 tahun pada iklim tropis dengan perlindungan selubung eksterior", "usia_pakai_nominal", "Estimasi umur teknis bahan silikat", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0004-4000-8000-000000000008"), new Guid("d0000000-0000-4000-8000-000000000008"), "Usia nominal 30 tahun dalam pemeliharaan standar selubung", "usia_pakai_nominal", "Panduan teknis umur beton seluler", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0004-4000-8000-000000000009"), new Guid("d0000000-0000-4000-8000-000000000009"), "Usia nominal 20 tahun bila dipakai pada partisi interior kering terlindung", "usia_pakai_nominal", "Buku Pedoman Teknis Konstruksi Kering", "perkiraan", null, "2019" },
                    { new Guid("d0000000-0004-4000-8000-000000000010"), new Guid("d0000000-0000-4000-8000-000000000010"), "Usia pakai nominal 15 tahun membutuhkan pemeliharaan pelapisan pelindung rutin setiap 2-3 tahun", "usia_pakai_nominal", "Pedoman Teknis Keawetan Kayu Konstruksi PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000011"), new Guid("d0000000-0000-4000-8000-000000000011"), "Usia nominal 25 tahun untuk panel eksterior dengan coating fluorokarbon", "usia_pakai_nominal", "Garansi teknis pabrikan cladding arsitektural", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0004-4000-8000-000000000012"), new Guid("d0000000-0000-4000-8000-000000000012"), "Usia pakai nominal 25 tahun bila plesteran terawat tanpa retak tembus", "usia_pakai_nominal", "Pedoman sistem insulasi dinding eksterior", "perkiraan", null, "2019" },
                    { new Guid("d0000000-0004-4000-8000-000000000013"), new Guid("d0000000-0000-4000-8000-000000000013"), "Usia pakai nominal 30 tahun tahan rayap, jamur, dan cuaca tropis basah", "usia_pakai_nominal", "Katalog teknis umur layan semen fiber", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0004-4000-8000-000000000014"), new Guid("d0000000-0000-4000-8000-000000000014"), "Usia nominal 60 tahun terpanjang di antara bahan dinding dengan daya tahan mekanik tinggi", "usia_pakai_nominal", "Pedoman Teknis Bangunan Warisan dan Monumen PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000015"), new Guid("d0000000-0000-4000-8000-000000000015"), "Usia pakai nominal 45 tahun untuk komponen pracetak berkualitas terkontrol", "usia_pakai_nominal", "Estimasi durabilitas geopolimer jangka panjang", "perkiraan", null, "v1.0" },
                    { new Guid("d0000000-0004-4000-8000-000000000016"), new Guid("d0000000-0000-4000-8000-000000000016"), "Usia nominal 40 tahun dengan inspeksi dan pelapisan ulang waterproofing berkala", "usia_pakai_nominal", "Pedoman Teknis Bangunan Gedung PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000017"), new Guid("d0000000-0000-4000-8000-000000000017"), "Usia nominal 30 tahun dengan pengecatan ulang setiap 10 tahun untuk menjaga kerapatan air", "usia_pakai_nominal", "Panduan Spesifikasi Material Atap PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000018"), new Guid("d0000000-0000-4000-8000-000000000018"), "Usia nominal 35 tahun dengan ketahanan warna dan bentuk yang sangat stabil", "usia_pakai_nominal", "Pedoman Teknis Konstruksi Atap PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000019"), new Guid("d0000000-0000-4000-8000-000000000019"), "Usia nominal 30 tahun umum digunakan pada rumah tapak tradisional Indonesia", "usia_pakai_nominal", "Pedoman Umur Bangunan PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000020"), new Guid("d0000000-0000-4000-8000-000000000020"), "Usia nominal 20 tahun sebelum kehilangan butiran mineral dan kelenturan bitumen", "usia_pakai_nominal", "Spesifikasi atap aspal fleksibel", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0004-4000-8000-000000000021"), new Guid("d0000000-0000-4000-8000-000000000021"), "Usia pakai nominal 15 tahun sebelum karat merusak lembaran atap pada kondisi lingkungan non-agresif", "usia_pakai_nominal", "Pedoman Teknis Atap Logam PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000022"), new Guid("d0000000-0000-4000-8000-000000000022"), "Usia nominal 20 tahun di lingkungan tropis atmosferik normal", "usia_pakai_nominal", "Garansi teknis pabrikan baja lapis AZ150", "perkiraan", null, "2020" },
                    { new Guid("d0000000-0004-4000-8000-000000000023"), new Guid("d0000000-0000-4000-8000-000000000023"), "Usia nominal 12 tahun sebelum karat tembus memerlukan penggantian lembaran atap", "usia_pakai_nominal", "Pedoman Teknis PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000024"), new Guid("d0000000-0000-4000-8000-000000000024"), "Usia nominal 25 tahun untuk penutup atap komposit semen serat", "usia_pakai_nominal", "Pedoman Spesifikasi Material Penutup Atap PUPR", "perkiraan", null, "2018" },
                    { new Guid("d0000000-0004-4000-8000-000000000025"), new Guid("d0000000-0000-4000-8000-000000000025"), "Usia nominal 25 tahun selama lapisan pelindung dan sambungan sealant terjaga", "usia_pakai_nominal", "Spesifikasi umur layan panel atap terinsulasi", "perkiraan", null, "2019" },
                    { new Guid("d0000000-0005-4000-8000-000000000001"), new Guid("d0000000-0000-4000-8000-000000000001"), "Faktor emisi 120 kgCO2e/m2 dinormalisasi untuk dinding beton tebal 150 mm dengan rasio tulangan 1%", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000002"), new Guid("d0000000-0000-4000-8000-000000000002"), "Faktor emisi 60 kgCO2e/m2 untuk dinding AAC tebal 100 mm termasuk mortar tipis perekat", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000003"), new Guid("d0000000-0000-4000-8000-000000000003"), "Faktor emisi 55 kgCO2e/m2 dinormalisasi untuk perakitan dinding polibeton tebal 100 mm", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000004"), new Guid("d0000000-0000-4000-8000-000000000004"), "Faktor emisi 65 kgCO2e/m2 mencakup pembakaran bata merah tradisional dan semen mortar plesteran", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000005"), new Guid("d0000000-0000-4000-8000-000000000005"), "Faktor emisi 35 kgCO2e/m2 lebih rendah karena tidak menggunakan semen plesteran acian ganda", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000006"), new Guid("d0000000-0000-4000-8000-000000000006"), "Faktor emisi 55 kgCO2e/m2 untuk batako berongga tebal 100 mm", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000007"), new Guid("d0000000-0000-4000-8000-000000000007"), "Faktor emisi 62 kgCO2e/m2 berdasarkan deklarasi lingkungan produk kalsium silikat", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000008"), new Guid("d0000000-0000-4000-8000-000000000008"), "Faktor emisi 50 kgCO2e/m2 karena pengurangan massa bahan baku semen per m2 dinding", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000009"), new Guid("d0000000-0000-4000-8000-000000000009"), "Faktor emisi 28 kgCO2e/m2 untuk sistem partisi gipsum ringan", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000010"), new Guid("d0000000-0000-4000-8000-000000000010"), "Faktor emisi 32 kgCO2e/m2 mencakup pemanenan, penggergajian, pengeringan kiln, dan coating", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000011"), new Guid("d0000000-0000-4000-8000-000000000011"), "Faktor emisi tinggi 190 kgCO2e/m2 disebabkan energi peleburan bijih aluminium primer", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000012"), new Guid("d0000000-0000-4000-8000-000000000012"), "Faktor emisi 70 kgCO2e/m2 mencakup bahan petrokimia polistirena dan aditif polimer", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000013"), new Guid("d0000000-0000-4000-8000-000000000013"), "Faktor emisi 75 kgCO2e/m2 untuk lembaran semen fiber 8 mm dan subframe baja ringan", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000014"), new Guid("d0000000-0000-4000-8000-000000000014"), "Faktor emisi 45 kgCO2e/m2 rendah karena proses produksi hanya pemotongan batu tanpa pembakaran kimia", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000015"), new Guid("d0000000-0000-4000-8000-000000000015"), "Faktor emisi 90 kgCO2e/m2 menghemat jejak karbon klinker semen dengan memanfaatkan abu terbang industri", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000016"), new Guid("d0000000-0000-4000-8000-000000000016"), "Faktor emisi 150 kgCO2e/m2 mencakup massa beton bertulang tebal dan lapisan screed pelindung", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000017"), new Guid("d0000000-0000-4000-8000-000000000017"), "Faktor emisi 95 kgCO2e/m2 mencakup pencetakan beton dan curing uap genting per m2 atap terpasang", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000018"), new Guid("d0000000-0000-4000-8000-000000000018"), "Faktor emisi 110 kgCO2e/m2 akibat proses pembakaran kiln suhu tinggi >1.100 C", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000019"), new Guid("d0000000-0000-4000-8000-000000000019"), "Faktor emisi 85 kgCO2e/m2 untuk proses pembakaran genting tanah liat", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000020"), new Guid("d0000000-0000-4000-8000-000000000020"), "Faktor emisi 80 kgCO2e/m2 mencakup matriks serat kaca, aspal modifikasi, dan substrat papan", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000021"), new Guid("d0000000-0000-4000-8000-000000000021"), "Faktor emisi 90 kgCO2e/m2 untuk lembaran baja galvanis tebal 0,4 mm termasuk rangka sekrup", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000022"), new Guid("d0000000-0000-4000-8000-000000000022"), "Faktor emisi 105 kgCO2e/m2 mencakup baja lembaran lapis paduan Al-Zn tebal 0,45 mm", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000023"), new Guid("d0000000-0000-4000-8000-000000000023"), "Faktor emisi 130 kgCO2e/m2 untuk lembaran seng galvanis tipis", "faktor_emisi", "ICE Database v3.0 (University of Bath)", "perkiraan", "https://circularecology.com/embodied-carbon-footprint-database.html", "v3.0" },
                    { new Guid("d0000000-0005-4000-8000-000000000024"), new Guid("d0000000-0000-4000-8000-000000000024"), "Faktor emisi 100 kgCO2e/m2 mencakup proses autoclaving dan penguatan serat komposit", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" },
                    { new Guid("d0000000-0005-4000-8000-000000000025"), new Guid("d0000000-0000-4000-8000-000000000025"), "Faktor emisi 95 kgCO2e/m2 untuk panel sandwich 50 mm lengkap dengan dua sisi lembaran galvalum", "faktor_emisi", "EPD generik pabrikan regional", "perkiraan", null, "2021" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000011"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000012"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000013"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000015"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000016"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000017"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000018"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000019"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000020"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000021"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000022"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000024"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0001-4000-8000-000000000025"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000011"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000012"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000013"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000015"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000016"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000017"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000018"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000019"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000020"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000021"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000022"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000024"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0002-4000-8000-000000000025"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000011"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000012"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000013"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000015"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000016"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000017"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000018"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000019"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000020"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000021"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000022"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000024"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0003-4000-8000-000000000025"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000011"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000012"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000013"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000015"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000016"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000017"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000018"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000019"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000020"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000021"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000022"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000024"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0004-4000-8000-000000000025"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000001"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000002"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000003"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000004"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000005"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000006"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000007"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000010"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000011"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000012"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000013"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000014"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000015"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000016"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000017"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000018"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000019"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000020"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000021"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000022"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000023"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000024"));

            migrationBuilder.DeleteData(
                table: "sumber_properti_bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0005-4000-8000-000000000025"));

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000001"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000002"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000003"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000004"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000005"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000006"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000007"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000008"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000009"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000010"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000011"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000012"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000013"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000014"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000015"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000016"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000017"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000018"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000019"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000020"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000021"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000022"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000023"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000024"),
                column: "susunan_ketebalan",
                value: null);

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000025"),
                column: "susunan_ketebalan",
                value: null);
        }
    }
}
