using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wastu.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddV2PersistenceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_simulasi_emisi",
                table: "simulasi_emisi");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "simulasi_emisi",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dihitung_pada_utc",
                table: "simulasi_emisi",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "faktor_penuaan_a",
                table: "simulasi_emisi",
                type: "double precision",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<string>(
                name: "versi_model",
                table: "simulasi_emisi",
                type: "text",
                nullable: false,
                defaultValue: "v2.0");

            migrationBuilder.AddColumn<string>(
                name: "metode_pesisir",
                table: "profil_iklim",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model_iklim",
                table: "profil_iklim",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "penyedia",
                table: "profil_iklim",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "periode_akhir",
                table: "profil_iklim",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "periode_awal",
                table: "profil_iklim",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provenance",
                table: "profil_iklim",
                type: "text",
                nullable: false,
                defaultValue: "LegacyUnknown");

            migrationBuilder.AddColumn<int>(
                name: "revisi_lokasi",
                table: "profil_iklim",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "status_pengambilan",
                table: "profil_iklim",
                type: "text",
                nullable: false,
                defaultValue: "Tersedia");

            migrationBuilder.AddColumn<string>(
                name: "zona_waktu",
                table: "profil_iklim",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "batch_id",
                table: "evaluasi",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dihitung_pada_utc",
                table: "evaluasi",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hash_input",
                table: "evaluasi",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "versi_model",
                table: "evaluasi",
                type: "text",
                nullable: false,
                defaultValue: "v2.0");

            migrationBuilder.AddColumn<int>(
                name: "revisi_lokasi",
                table: "bangunan",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "zona_waktu",
                table: "bangunan",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "susunan_ketebalan",
                table: "bahan",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "versi_data",
                table: "bahan",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE simulasi_emisi SET id = gen_random_uuid() WHERE id = '00000000-0000-0000-0000-000000000000' OR id IS NULL;");
            migrationBuilder.Sql("UPDATE profil_iklim SET provenance = 'LegacyUnknown' WHERE provenance IS NULL OR provenance = '';");
            migrationBuilder.Sql("UPDATE profil_iklim SET revisi_lokasi = 1 WHERE revisi_lokasi IS NULL OR revisi_lokasi < 1;");
            migrationBuilder.Sql("UPDATE bangunan SET revisi_lokasi = 1 WHERE revisi_lokasi IS NULL OR revisi_lokasi < 1;");
            migrationBuilder.Sql("UPDATE bahan SET versi_data = 1 WHERE versi_data IS NULL OR versi_data < 1;");
            migrationBuilder.Sql("UPDATE simulasi_emisi SET versi_model = 'v2.0' WHERE versi_model IS NULL OR versi_model = '';");
            migrationBuilder.Sql("UPDATE evaluasi SET versi_model = 'v2.0' WHERE versi_model IS NULL OR versi_model = '';");

            migrationBuilder.AddPrimaryKey(
                name: "PK_simulasi_emisi",
                table: "simulasi_emisi",
                column: "id");

            migrationBuilder.CreateTable(
                name: "batch_evaluasi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proyek_id = table.Column<Guid>(type: "uuid", nullable: false),
                    komponen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revisi_lokasi = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    hash_input = table.Column<string>(type: "text", nullable: false),
                    versi_model = table.Column<string>(type: "text", nullable: false, defaultValue: "v2.0"),
                    bobot_termal = table.Column<double>(type: "double precision", nullable: false),
                    bobot_lembap = table.Column<double>(type: "double precision", nullable: false),
                    bobot_korosi = table.Column<double>(type: "double precision", nullable: false),
                    bobot_usia = table.Column<double>(type: "double precision", nullable: false),
                    luas_m2 = table.Column<double>(type: "double precision", nullable: false),
                    dihitung_pada_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batch_evaluasi", x => x.id);
                    table.CheckConstraint("batch_evaluasi_luas_positif", "luas_m2 > 0");
                    table.CheckConstraint("batch_evaluasi_revisi_positif", "revisi_lokasi >= 1");
                    table.ForeignKey(
                        name: "FK_batch_evaluasi_bangunan_proyek_id",
                        column: x => x.proyek_id,
                        principalTable: "bangunan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_batch_evaluasi_komponen_komponen_id",
                        column: x => x.komponen_id,
                        principalTable: "komponen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "konfigurasi_model",
                columns: table => new
                {
                    versi_model = table.Column<string>(type: "text", nullable: false),
                    bobot_lembap_hujan_alpha = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.5),
                    acuan_curah_hujan_mm = table.Column<double>(type: "double precision", nullable: false, defaultValue: 3000.0),
                    acuan_jarak_pesisir_km = table.Column<double>(type: "double precision", nullable: false, defaultValue: 50.0),
                    pita_termal_batas_bawah = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.10000000000000001),
                    pita_termal_batas_atas_sejuk = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.59999999999999998),
                    pita_termal_batas_atas_panas = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.29999999999999999),
                    suhu_sejuk_c = table.Column<double>(type: "double precision", nullable: false, defaultValue: 26.0),
                    suhu_panas_c = table.Column<double>(type: "double precision", nullable: false, defaultValue: 32.0),
                    faktor_penuaan_a = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.5),
                    ambang_tidak_disarankan = table.Column<double>(type: "double precision", nullable: false, defaultValue: 55.0),
                    ambang_cocok = table.Column<double>(type: "double precision", nullable: false, defaultValue: 75.0),
                    aktif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    dibuat_pada_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_konfigurasi_model", x => x.versi_model);
                });

            migrationBuilder.CreateTable(
                name: "laporan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bangunan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path_berkas = table.Column<string>(type: "text", nullable: false),
                    dibuat_pada_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hash_snapshot = table.Column<string>(type: "text", nullable: true),
                    status_berkas = table.Column<string>(type: "text", nullable: false, defaultValue: "Tersimpan"),
                    checksum_sha256 = table.Column<string>(type: "text", nullable: true),
                    ukuran_byte = table.Column<long>(type: "bigint", nullable: true),
                    opsi_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laporan", x => x.id);
                    table.ForeignKey(
                        name: "FK_laporan_bangunan_bangunan_id",
                        column: x => x.bangunan_id,
                        principalTable: "bangunan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pekerjaan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bangunan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nama = table.Column<string>(type: "text", nullable: false),
                    jenis = table.Column<string>(type: "text", nullable: false),
                    tanggal_mulai = table.Column<DateOnly>(type: "date", nullable: false),
                    tanggal_selesai = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Draft"),
                    tanggal_dikunci = table.Column<DateOnly>(type: "date", nullable: true),
                    komponen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bahan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revisi_lokasi_saat_kunci = table.Column<int>(type: "integer", nullable: true),
                    override_ambang_json = table.Column<string>(type: "text", nullable: true),
                    konfirmasi_json = table.Column<string>(type: "text", nullable: true),
                    terakhir_diperbarui_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pekerjaan", x => x.id);
                    table.CheckConstraint("pekerjaan_jenis_enum", "jenis IN ('pengecoran', 'pengecatan')");
                    table.CheckConstraint("pekerjaan_rentang_tanggal_valid", "tanggal_mulai <= tanggal_selesai");
                    table.CheckConstraint("pekerjaan_status_enum", "status IN ('Draft', 'Terkunci', 'PerluDitinjau', 'Selesai')");
                    table.ForeignKey(
                        name: "FK_pekerjaan_bahan_bahan_id",
                        column: x => x.bahan_id,
                        principalTable: "bahan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pekerjaan_bangunan_bangunan_id",
                        column: x => x.bangunan_id,
                        principalTable: "bangunan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pekerjaan_komponen_komponen_id",
                        column: x => x.komponen_id,
                        principalTable: "komponen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "prakiraan_cache",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bangunan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    revisi_lokasi = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    tanggal_cuaca = table.Column<DateOnly>(type: "date", nullable: false),
                    zona_waktu = table.Column<string>(type: "text", nullable: false, defaultValue: "Asia/Jakarta"),
                    diambil_pada_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    penyedia = table.Column<string>(type: "text", nullable: false, defaultValue: "Open-Meteo"),
                    data_parsial = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    data_lengkap = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    catatan_kualitas = table.Column<string>(type: "text", nullable: true),
                    probabilitas_hujan_pct = table.Column<double>(type: "double precision", nullable: false),
                    curah_hujan_mm = table.Column<double>(type: "double precision", nullable: false),
                    suhu_min_c = table.Column<double>(type: "double precision", nullable: false),
                    suhu_maks_c = table.Column<double>(type: "double precision", nullable: false),
                    kelembapan_maks_pct = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prakiraan_cache", x => x.id);
                    table.CheckConstraint("prakiraan_cache_curah_hujan_nonnegatif", "curah_hujan_mm >= 0");
                    table.CheckConstraint("prakiraan_cache_kelembapan_rentang", "kelembapan_maks_pct BETWEEN 0 AND 100");
                    table.CheckConstraint("prakiraan_cache_probabilitas_rentang", "probabilitas_hujan_pct BETWEEN 0 AND 100");
                    table.CheckConstraint("prakiraan_cache_revisi_positif", "revisi_lokasi >= 1");
                    table.ForeignKey(
                        name: "FK_prakiraan_cache_bangunan_bangunan_id",
                        column: x => x.bangunan_id,
                        principalTable: "bangunan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sumber_properti_bahan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bahan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    properti = table.Column<string>(type: "text", nullable: false),
                    referensi = table.Column<string>(type: "text", nullable: false),
                    versi = table.Column<string>(type: "text", nullable: true),
                    tautan = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "perkiraan"),
                    catatan_konversi = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sumber_properti_bahan", x => x.id);
                    table.CheckConstraint("sumber_properti_bahan_status_enum", "status IN ('perkiraan', 'terverifikasi', 'manual')");
                    table.ForeignKey(
                        name: "FK_sumber_properti_bahan_bahan_bahan_id",
                        column: x => x.bahan_id,
                        principalTable: "bahan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ambang_pekerjaan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pekerjaan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    jenis_pekerjaan = table.Column<string>(type: "text", nullable: true),
                    probabilitas_hujan_maks_pct = table.Column<double>(type: "double precision", nullable: true),
                    curah_hujan_maks_mm = table.Column<double>(type: "double precision", nullable: true),
                    suhu_min_c = table.Column<double>(type: "double precision", nullable: true),
                    suhu_maks_c = table.Column<double>(type: "double precision", nullable: true),
                    kelembapan_maks_pct = table.Column<double>(type: "double precision", nullable: true),
                    tingkat_suhu = table.Column<string>(type: "text", nullable: false),
                    sumber_ambang = table.Column<string>(type: "text", nullable: false, defaultValue: "Default Asumsi")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ambang_pekerjaan", x => x.id);
                    table.CheckConstraint("ambang_pekerjaan_tingkat_suhu_enum", "tingkat_suhu IN ('peringatan', 'keras')");
                    table.ForeignKey(
                        name: "FK_ambang_pekerjaan_pekerjaan_pekerjaan_id",
                        column: x => x.pekerjaan_id,
                        principalTable: "pekerjaan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "keputusan_jadwal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pekerjaan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tanggal = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    catatan_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    terkunci = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    dikunci_pada_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    persetujuan_risiko = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    konfirmasi_data_lama = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    catatan_konfirmasi = table.Column<string>(type: "text", nullable: true),
                    snapshot_cuaca_json = table.Column<string>(type: "text", nullable: true),
                    snapshot_ambang_json = table.Column<string>(type: "text", nullable: true),
                    umur_data_jam = table.Column<double>(type: "double precision", nullable: true),
                    dibuat_pada_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keputusan_jadwal", x => x.id);
                    table.ForeignKey(
                        name: "FK_keputusan_jadwal_pekerjaan_pekerjaan_id",
                        column: x => x.pekerjaan_id,
                        principalTable: "pekerjaan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000001"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000002"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000003"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000004"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000005"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000006"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000007"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000008"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000009"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000010"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000011"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000012"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000013"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000014"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000015"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000016"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000017"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000018"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000019"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000020"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000021"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000022"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000023"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000024"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "bahan",
                keyColumn: "id",
                keyValue: new Guid("d0000000-0000-4000-8000-000000000025"),
                columns: new[] { "susunan_ketebalan", "versi_data" },
                values: new object[] { null, 1 });

            migrationBuilder.InsertData(
                table: "konfigurasi_model",
                columns: new[] { "versi_model", "acuan_curah_hujan_mm", "acuan_jarak_pesisir_km", "aktif", "ambang_cocok", "ambang_tidak_disarankan", "bobot_lembap_hujan_alpha", "dibuat_pada_utc", "faktor_penuaan_a", "pita_termal_batas_atas_panas", "pita_termal_batas_atas_sejuk", "pita_termal_batas_bawah", "suhu_panas_c", "suhu_sejuk_c" },
                values: new object[] { "v2.0", 3000.0, 50.0, true, 75.0, 55.0, 0.5, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 0.5, 0.29999999999999999, 0.59999999999999998, 0.10000000000000001, 32.0, 26.0 });

            migrationBuilder.CreateIndex(
                name: "uk_simulasi_emisi_evaluasi_horizon_versi",
                table: "simulasi_emisi",
                columns: new[] { "evaluasi_id", "horizon_tahun", "versi_model" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "evaluasi_batch_id_idx",
                table: "evaluasi",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ambang_pekerjaan_jenis_pekerjaan_idx",
                table: "ambang_pekerjaan",
                column: "jenis_pekerjaan");

            migrationBuilder.CreateIndex(
                name: "ambang_pekerjaan_pekerjaan_id_idx",
                table: "ambang_pekerjaan",
                column: "pekerjaan_id");

            migrationBuilder.CreateIndex(
                name: "batch_evaluasi_proyek_id_idx",
                table: "batch_evaluasi",
                column: "proyek_id");

            migrationBuilder.CreateIndex(
                name: "uk_batch_evaluasi_komponen_hash_versi",
                table: "batch_evaluasi",
                columns: new[] { "komponen_id", "hash_input", "versi_model" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "keputusan_jadwal_pekerjaan_id_idx",
                table: "keputusan_jadwal",
                column: "pekerjaan_id");

            migrationBuilder.CreateIndex(
                name: "laporan_bangunan_id_idx",
                table: "laporan",
                column: "bangunan_id");

            migrationBuilder.CreateIndex(
                name: "IX_pekerjaan_bahan_id",
                table: "pekerjaan",
                column: "bahan_id");

            migrationBuilder.CreateIndex(
                name: "IX_pekerjaan_komponen_id",
                table: "pekerjaan",
                column: "komponen_id");

            migrationBuilder.CreateIndex(
                name: "pekerjaan_bangunan_id_idx",
                table: "pekerjaan",
                column: "bangunan_id");

            migrationBuilder.CreateIndex(
                name: "prakiraan_cache_bangunan_revisi_tanggal_idx",
                table: "prakiraan_cache",
                columns: new[] { "bangunan_id", "revisi_lokasi", "tanggal_cuaca" });

            migrationBuilder.CreateIndex(
                name: "prakiraan_cache_lat_lon_revisi_tanggal_idx",
                table: "prakiraan_cache",
                columns: new[] { "latitude", "longitude", "revisi_lokasi", "tanggal_cuaca" });

            migrationBuilder.CreateIndex(
                name: "sumber_properti_bahan_bahan_id_idx",
                table: "sumber_properti_bahan",
                column: "bahan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_evaluasi_batch_evaluasi_batch_id",
                table: "evaluasi",
                column: "batch_id",
                principalTable: "batch_evaluasi",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_evaluasi_batch_evaluasi_batch_id",
                table: "evaluasi");

            migrationBuilder.DropTable(
                name: "ambang_pekerjaan");

            migrationBuilder.DropTable(
                name: "batch_evaluasi");

            migrationBuilder.DropTable(
                name: "keputusan_jadwal");

            migrationBuilder.DropTable(
                name: "konfigurasi_model");

            migrationBuilder.DropTable(
                name: "laporan");

            migrationBuilder.DropTable(
                name: "prakiraan_cache");

            migrationBuilder.DropTable(
                name: "sumber_properti_bahan");

            migrationBuilder.DropTable(
                name: "pekerjaan");

            migrationBuilder.DropPrimaryKey(
                name: "PK_simulasi_emisi",
                table: "simulasi_emisi");

            migrationBuilder.DropIndex(
                name: "uk_simulasi_emisi_evaluasi_horizon_versi",
                table: "simulasi_emisi");

            migrationBuilder.DropIndex(
                name: "evaluasi_batch_id_idx",
                table: "evaluasi");

            migrationBuilder.DropColumn(
                name: "id",
                table: "simulasi_emisi");

            migrationBuilder.DropColumn(
                name: "dihitung_pada_utc",
                table: "simulasi_emisi");

            migrationBuilder.DropColumn(
                name: "faktor_penuaan_a",
                table: "simulasi_emisi");

            migrationBuilder.DropColumn(
                name: "versi_model",
                table: "simulasi_emisi");

            migrationBuilder.DropColumn(
                name: "metode_pesisir",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "model_iklim",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "penyedia",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "periode_akhir",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "periode_awal",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "provenance",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "revisi_lokasi",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "status_pengambilan",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "zona_waktu",
                table: "profil_iklim");

            migrationBuilder.DropColumn(
                name: "batch_id",
                table: "evaluasi");

            migrationBuilder.DropColumn(
                name: "dihitung_pada_utc",
                table: "evaluasi");

            migrationBuilder.DropColumn(
                name: "hash_input",
                table: "evaluasi");

            migrationBuilder.DropColumn(
                name: "versi_model",
                table: "evaluasi");

            migrationBuilder.DropColumn(
                name: "revisi_lokasi",
                table: "bangunan");

            migrationBuilder.DropColumn(
                name: "zona_waktu",
                table: "bangunan");

            migrationBuilder.DropColumn(
                name: "susunan_ketebalan",
                table: "bahan");

            migrationBuilder.DropColumn(
                name: "versi_data",
                table: "bahan");

            migrationBuilder.AddPrimaryKey(
                name: "PK_simulasi_emisi",
                table: "simulasi_emisi",
                column: "evaluasi_id");
        }
    }
}
