using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wastu.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bahan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nama = table.Column<string>(type: "text", nullable: false),
                    jenis = table.Column<string>(type: "text", nullable: false),
                    konduktivitas_termal = table.Column<double>(type: "double precision", nullable: false),
                    ketahanan_lembap = table.Column<int>(type: "integer", nullable: false),
                    ketahanan_korosi = table.Column<int>(type: "integer", nullable: false),
                    usia_pakai_nominal_tahun = table.Column<double>(type: "double precision", nullable: false),
                    faktor_emisi = table.Column<double>(type: "double precision", nullable: false),
                    sumber_data = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bahan", x => x.id);
                    table.CheckConstraint("bahan_faktor_emisi_nonnegatif", "faktor_emisi >= 0");
                    table.CheckConstraint("bahan_jenis_enum", "jenis IN ('dinding', 'atap')");
                    table.CheckConstraint("bahan_ketahanan_korosi_rentang", "ketahanan_korosi BETWEEN 1 AND 5");
                    table.CheckConstraint("bahan_ketahanan_lembap_rentang", "ketahanan_lembap BETWEEN 1 AND 5");
                    table.CheckConstraint("bahan_konduktivitas_positif", "konduktivitas_termal > 0");
                    table.CheckConstraint("bahan_usia_positif", "usia_pakai_nominal_tahun > 0");
                });

            migrationBuilder.CreateTable(
                name: "bangunan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nama = table.Column<string>(type: "text", nullable: false),
                    kota = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    dibuat_pada = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bangunan", x => x.id);
                    table.CheckConstraint("bangunan_berada_di_indonesia", "latitude BETWEEN -11 AND 6 AND longitude BETWEEN 94 AND 142");
                });

            migrationBuilder.CreateTable(
                name: "komponen",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bangunan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jenis = table.Column<string>(type: "text", nullable: false),
                    luas_m2 = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_komponen", x => x.id);
                    table.CheckConstraint("komponen_jenis_enum", "jenis IN ('dinding', 'atap')");
                    table.CheckConstraint("komponen_luas_positif", "luas_m2 > 0");
                    table.ForeignKey(
                        name: "FK_komponen_bangunan_bangunan_id",
                        column: x => x.bangunan_id,
                        principalTable: "bangunan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profil_iklim",
                columns: table => new
                {
                    bangunan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    curah_hujan_tahunan_mm = table.Column<double>(type: "double precision", nullable: false),
                    kelembapan_rata_pct = table.Column<double>(type: "double precision", nullable: false),
                    suhu_rata_c = table.Column<double>(type: "double precision", nullable: false),
                    jarak_pesisir_km = table.Column<double>(type: "double precision", nullable: false),
                    tingkat_korosi = table.Column<string>(type: "text", nullable: false),
                    diambil_pada = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profil_iklim", x => x.bangunan_id);
                    table.CheckConstraint("profil_iklim_curah_hujan_nonnegatif", "curah_hujan_tahunan_mm >= 0");
                    table.CheckConstraint("profil_iklim_jarak_pesisir_nonnegatif", "jarak_pesisir_km >= 0");
                    table.CheckConstraint("profil_iklim_kelembapan_rentang", "kelembapan_rata_pct BETWEEN 0 AND 100");
                    table.CheckConstraint("profil_iklim_suhu_rentang", "suhu_rata_c BETWEEN -10 AND 60");
                    table.CheckConstraint("profil_iklim_tingkat_korosi_enum", "tingkat_korosi IN ('rendah', 'sedang', 'tinggi')");
                    table.ForeignKey(
                        name: "FK_profil_iklim_bangunan_bangunan_id",
                        column: x => x.bangunan_id,
                        principalTable: "bangunan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluasi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    komponen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bahan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skor_kesesuaian = table.Column<double>(type: "double precision", nullable: false),
                    emisi_awal_kg = table.Column<double>(type: "double precision", nullable: false),
                    alasan = table.Column<string>(type: "text", nullable: false),
                    skor_termal = table.Column<double>(type: "double precision", nullable: false),
                    skor_lembap = table.Column<double>(type: "double precision", nullable: false),
                    skor_korosi = table.Column<double>(type: "double precision", nullable: false),
                    skor_usia = table.Column<double>(type: "double precision", nullable: false),
                    dihitung_pada = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluasi", x => x.id);
                    table.CheckConstraint("evaluasi_alasan_tidak_kosong", "length(btrim(alasan)) > 0");
                    table.CheckConstraint("evaluasi_emisi_awal_nonnegatif", "emisi_awal_kg >= 0");
                    table.CheckConstraint("evaluasi_skor_rentang", "skor_kesesuaian BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_evaluasi_bahan_bahan_id",
                        column: x => x.bahan_id,
                        principalTable: "bahan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluasi_komponen_komponen_id",
                        column: x => x.komponen_id,
                        principalTable: "komponen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulasi_emisi",
                columns: table => new
                {
                    evaluasi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    horizon_tahun = table.Column<int>(type: "integer", nullable: false),
                    usia_pakai_efektif_tahun = table.Column<double>(type: "double precision", nullable: false),
                    jumlah_penggantian = table.Column<int>(type: "integer", nullable: false),
                    emisi_kumulatif_kg = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulasi_emisi", x => x.evaluasi_id);
                    table.CheckConstraint("simulasi_emisi_emisi_kumulatif_nonnegatif", "emisi_kumulatif_kg >= 0");
                    table.CheckConstraint("simulasi_emisi_horizon_positif", "horizon_tahun > 0");
                    table.CheckConstraint("simulasi_emisi_jumlah_penggantian_nonnegatif", "jumlah_penggantian >= 0");
                    table.CheckConstraint("simulasi_emisi_usia_efektif_positif", "usia_pakai_efektif_tahun > 0");
                    table.ForeignKey(
                        name: "FK_simulasi_emisi_evaluasi_evaluasi_id",
                        column: x => x.evaluasi_id,
                        principalTable: "evaluasi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "bahan",
                columns: new[] { "id", "faktor_emisi", "jenis", "ketahanan_korosi", "ketahanan_lembap", "konduktivitas_termal", "nama", "sumber_data", "usia_pakai_nominal_tahun" },
                values: new object[,]
                {
                    { new Guid("d0000000-0000-4000-8000-000000000001"), 120.0, "dinding", 5, 4, 1.3999999999999999, "Beton bertulang (in-situ)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 50.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000002"), 60.0, "dinding", 3, 5, 0.12, "Beton ringan (hebel autoclave)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 30.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000003"), 55.0, "dinding", 3, 4, 0.10000000000000001, "Beton polibeton (hebel)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 25.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000004"), 65.0, "dinding", 4, 3, 0.69999999999999996, "Bata merah + plester aci", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 30.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000005"), 35.0, "dinding", 4, 2, 0.69999999999999996, "Bata merah tanpa plester", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 20.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000006"), 55.0, "dinding", 4, 4, 0.59999999999999998, "Bata beton (concrete block)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 40.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000007"), 62.0, "dinding", 3, 5, 0.11, "Bata silikat berpori", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 30.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000008"), 50.0, "dinding", 3, 4, 0.089999999999999997, "Bata ringan honeycomb (AHCC)", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 30.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000009"), 28.0, "dinding", 1, 1, 0.25, "Panel gipsum (dry wall)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 20.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000010"), 32.0, "dinding", 1, 1, 0.14999999999999999, "Papan kayu + pelindung", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 15.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000011"), 190.0, "dinding", 5, 5, 0.20000000000000001, "Panel ACP (aluminium composite)", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 25.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000012"), 70.0, "dinding", 4, 4, 0.040000000000000001, "Panel EPS + plester", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 25.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000013"), 75.0, "dinding", 5, 5, 0.25, "Semen fiber reinforced board", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 30.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000014"), 45.0, "dinding", 5, 5, 3.0, "Batu andesit (dinding batu alam)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 60.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000015"), 90.0, "dinding", 4, 4, 0.45000000000000001, "Beton geopolimer", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 45.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000016"), 150.0, "atap", 5, 5, 1.3999999999999999, "Dak beton bertulang", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 40.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000017"), 95.0, "atap", 5, 5, 0.40000000000000002, "Genting beton", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 30.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000018"), 110.0, "atap", 5, 5, 0.29999999999999999, "Genting keramik", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 35.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000019"), 85.0, "atap", 4, 4, 0.59999999999999998, "Genting tanah liat", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 30.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000020"), 80.0, "atap", 2, 3, 0.080000000000000002, "Genteng aspal (shingle)", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 20.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000021"), 90.0, "atap", 2, 3, 0.050000000000000003, "Baja galvanis (trapezium)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 15.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000022"), 105.0, "atap", 4, 4, 0.050000000000000003, "Baja AZ / galvalum", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 20.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000023"), 130.0, "atap", 1, 2, 0.11, "Seng (zink)", "ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)", 12.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000024"), 100.0, "atap", 3, 4, 0.070000000000000007, "Constantra / fiber cement", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 25.0 },
                    { new Guid("d0000000-0000-4000-8000-000000000025"), 95.0, "atap", 4, 4, 0.040000000000000001, "Panel sandwich (inti EPS)", "EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)", 25.0 }
                });

            migrationBuilder.CreateIndex(
                name: "bahan_jenis_idx",
                table: "bahan",
                column: "jenis");

            migrationBuilder.CreateIndex(
                name: "evaluasi_bahan_id_idx",
                table: "evaluasi",
                column: "bahan_id");

            migrationBuilder.CreateIndex(
                name: "evaluasi_komponen_id_idx",
                table: "evaluasi",
                column: "komponen_id");

            migrationBuilder.CreateIndex(
                name: "komponen_bangunan_id_idx",
                table: "komponen",
                column: "bangunan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profil_iklim");

            migrationBuilder.DropTable(
                name: "simulasi_emisi");

            migrationBuilder.DropTable(
                name: "evaluasi");

            migrationBuilder.DropTable(
                name: "bahan");

            migrationBuilder.DropTable(
                name: "komponen");

            migrationBuilder.DropTable(
                name: "bangunan");
        }
    }
}
