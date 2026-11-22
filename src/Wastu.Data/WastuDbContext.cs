using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wastu.Data.Entities;
using Wastu.Domain;
using Wastu.Domain.Weather;

namespace Wastu.Data;

public sealed class WastuDbContext : DbContext
{
    // Enum disimpan sebagai teks huruf kecil sesuai CHECK pada skema.
    private static readonly ValueConverter<JenisKomponen, string> KonversiJenisKomponen =
        new(v => v == JenisKomponen.Dinding ? "dinding" : "atap",
            v => v == "dinding" ? JenisKomponen.Dinding : JenisKomponen.Atap);

    private static readonly ValueConverter<TingkatKorosi, string> KonversiTingkatKorosi =
        new(v => v == TingkatKorosi.Rendah ? "rendah" : v == TingkatKorosi.Sedang ? "sedang" : "tinggi",
            v => v == "rendah" ? TingkatKorosi.Rendah : v == "sedang" ? TingkatKorosi.Sedang : TingkatKorosi.Tinggi);

    private static readonly ValueConverter<JenisPekerjaan, string> KonversiJenisPekerjaan =
        new(v => v == JenisPekerjaan.Pengecoran ? "pengecoran" : "pengecatan",
            v => v == "pengecoran" ? JenisPekerjaan.Pengecoran : JenisPekerjaan.Pengecatan);

    private static readonly ValueConverter<StatusPekerjaan, string> KonversiStatusPekerjaan =
        new(v => v.ToString(),
            v => Enum.Parse<StatusPekerjaan>(v, true));

    private static readonly ValueConverter<TingkatPelanggaran, string> KonversiTingkatPelanggaran =
        new(v => v == TingkatPelanggaran.Peringatan ? "peringatan" : "keras",
            v => string.Equals(v, "peringatan", StringComparison.OrdinalIgnoreCase) ? TingkatPelanggaran.Peringatan : TingkatPelanggaran.Keras);

    public WastuDbContext(DbContextOptions<WastuDbContext> options)
        : base(options)
    {
    }

    public DbSet<BahanEntity> Bahan => Set<BahanEntity>();

    public DbSet<ProyekEntity> Bangunan => Set<ProyekEntity>();

    public DbSet<KomponenEntity> Komponen => Set<KomponenEntity>();

    public DbSet<ProfilIklimEntity> ProfilIklim => Set<ProfilIklimEntity>();

    public DbSet<EvaluasiEntity> Evaluasi => Set<EvaluasiEntity>();

    public DbSet<SimulasiEmisiEntity> SimulasiEmisi => Set<SimulasiEmisiEntity>();

    public DbSet<SumberPropertiBahanEntity> SumberPropertiBahan => Set<SumberPropertiBahanEntity>();

    public DbSet<BatchEvaluasiEntity> BatchEvaluasi => Set<BatchEvaluasiEntity>();

    public DbSet<PekerjaanEntity> Pekerjaan => Set<PekerjaanEntity>();

    public DbSet<KeputusanJadwalEntity> KeputusanJadwal => Set<KeputusanJadwalEntity>();

    public DbSet<PrakiraanCacheEntity> PrakiraanCache => Set<PrakiraanCacheEntity>();

    public DbSet<AmbangPekerjaanEntity> AmbangPekerjaan => Set<AmbangPekerjaanEntity>();

    public DbSet<KonfigurasiModelEntity> KonfigurasiModel => Set<KonfigurasiModelEntity>();

    public DbSet<LaporanEntity> Laporan => Set<LaporanEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        AturBahan(modelBuilder);
        AturBangunan(modelBuilder);
        AturKomponen(modelBuilder);
        AturProfilIklim(modelBuilder);
        AturEvaluasi(modelBuilder);
        AturSimulasiEmisi(modelBuilder);
        AturSumberPropertiBahan(modelBuilder);
        AturBatchEvaluasi(modelBuilder);
        AturPekerjaan(modelBuilder);
        AturKeputusanJadwal(modelBuilder);
        AturPrakiraanCache(modelBuilder);
        AturAmbangPekerjaan(modelBuilder);
        AturKonfigurasiModel(modelBuilder);
        AturLaporan(modelBuilder);
    }

    private static void AturBahan(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<BahanEntity> b = modelBuilder.Entity<BahanEntity>();

        b.ToTable("bahan", t =>
        {
            t.HasCheckConstraint("bahan_jenis_enum", "jenis IN ('dinding', 'atap')");
            t.HasCheckConstraint("bahan_konduktivitas_positif", "konduktivitas_termal > 0");
            t.HasCheckConstraint("bahan_ketahanan_lembap_rentang", "ketahanan_lembap BETWEEN 1 AND 5");
            t.HasCheckConstraint("bahan_ketahanan_korosi_rentang", "ketahanan_korosi BETWEEN 1 AND 5");
            t.HasCheckConstraint("bahan_usia_positif", "usia_pakai_nominal_tahun > 0");
            t.HasCheckConstraint("bahan_faktor_emisi_nonnegatif", "faktor_emisi >= 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Nama).HasColumnName("nama").IsRequired();
        b.Property(x => x.Jenis).HasColumnName("jenis").HasConversion(KonversiJenisKomponen).IsRequired();
        b.Property(x => x.KonduktivitasTermal).HasColumnName("konduktivitas_termal");
        b.Property(x => x.KetahananLembap).HasColumnName("ketahanan_lembap");
        b.Property(x => x.KetahananKorosi).HasColumnName("ketahanan_korosi");
        b.Property(x => x.UsiaPakaiNominalTahun).HasColumnName("usia_pakai_nominal_tahun");
        b.Property(x => x.FaktorEmisi).HasColumnName("faktor_emisi");
        b.Property(x => x.SumberData).HasColumnName("sumber_data").IsRequired();
        b.Property(x => x.SusunanKetebalan).HasColumnName("susunan_ketebalan");
        b.Property(x => x.VersiData).HasColumnName("versi_data").HasDefaultValue(1);
        b.HasIndex(x => x.Jenis).HasDatabaseName("bahan_jenis_idx");

        b.HasData(KatalogSeed.Bahan);
    }

    private static void AturBangunan(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<ProyekEntity> b = modelBuilder.Entity<ProyekEntity>();

        b.ToTable("bangunan", t => t.HasCheckConstraint(
            "bangunan_berada_di_indonesia",
            "latitude BETWEEN -11 AND 6 AND longitude BETWEEN 94 AND 142"));

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Nama).HasColumnName("nama").IsRequired();
        b.Property(x => x.Kota).HasColumnName("kota").IsRequired();
        b.Property(x => x.Latitude).HasColumnName("latitude");
        b.Property(x => x.Longitude).HasColumnName("longitude");
        b.Property(x => x.DibuatPada).HasColumnName("dibuat_pada");
        b.Property(x => x.RevisiLokasi).HasColumnName("revisi_lokasi").HasDefaultValue(1);
        b.Property(x => x.ZonaWaktu).HasColumnName("zona_waktu");
    }

    private static void AturKomponen(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<KomponenEntity> b = modelBuilder.Entity<KomponenEntity>();

        b.ToTable("komponen", t =>
        {
            t.HasCheckConstraint("komponen_jenis_enum", "jenis IN ('dinding', 'atap')");
            t.HasCheckConstraint("komponen_luas_positif", "luas_m2 > 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.BangunanId).HasColumnName("bangunan_id");
        b.Property(x => x.Jenis).HasColumnName("jenis").HasConversion(KonversiJenisKomponen).IsRequired();
        b.Property(x => x.LuasM2).HasColumnName("luas_m2");
        b.HasIndex(x => x.BangunanId).HasDatabaseName("komponen_bangunan_id_idx");
        b.HasOne(x => x.Bangunan)
            .WithMany()
            .HasForeignKey(x => x.BangunanId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void AturProfilIklim(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<ProfilIklimEntity> b = modelBuilder.Entity<ProfilIklimEntity>();

        b.ToTable("profil_iklim", t =>
        {
            t.HasCheckConstraint("profil_iklim_curah_hujan_nonnegatif", "curah_hujan_tahunan_mm >= 0");
            t.HasCheckConstraint("profil_iklim_kelembapan_rentang", "kelembapan_rata_pct BETWEEN 0 AND 100");
            t.HasCheckConstraint("profil_iklim_suhu_rentang", "suhu_rata_c BETWEEN -10 AND 60");
            t.HasCheckConstraint("profil_iklim_jarak_pesisir_nonnegatif", "jarak_pesisir_km >= 0");
            t.HasCheckConstraint("profil_iklim_tingkat_korosi_enum", "tingkat_korosi IN ('rendah', 'sedang', 'tinggi')");
        });

        b.HasKey(x => x.BangunanId);
        b.Property(x => x.BangunanId).HasColumnName("bangunan_id");
        b.Property(x => x.CurahHujanTahunanMm).HasColumnName("curah_hujan_tahunan_mm");
        b.Property(x => x.KelembapanRataPct).HasColumnName("kelembapan_rata_pct");
        b.Property(x => x.SuhuRataC).HasColumnName("suhu_rata_c");
        b.Property(x => x.JarakPesisirKm).HasColumnName("jarak_pesisir_km");
        b.Property(x => x.TingkatKorosi).HasColumnName("tingkat_korosi").HasConversion(KonversiTingkatKorosi).IsRequired();
        b.Property(x => x.DiambilPada).HasColumnName("diambil_pada");
        b.Property(x => x.RevisiLokasi).HasColumnName("revisi_lokasi").HasDefaultValue(1);
        b.Property(x => x.Provenance).HasColumnName("provenance").HasDefaultValue("LegacyUnknown").IsRequired();
        b.Property(x => x.PeriodeAwal).HasColumnName("periode_awal");
        b.Property(x => x.PeriodeAkhir).HasColumnName("periode_akhir");
        b.Property(x => x.Penyedia).HasColumnName("penyedia");
        b.Property(x => x.ModelIklim).HasColumnName("model_iklim");
        b.Property(x => x.ZonaWaktu).HasColumnName("zona_waktu");
        b.Property(x => x.MetodePesisir).HasColumnName("metode_pesisir");
        b.Property(x => x.StatusPengambilan).HasColumnName("status_pengambilan").HasDefaultValue("Tersedia").IsRequired();

        b.HasOne(x => x.Bangunan)
            .WithOne()
            .HasForeignKey<ProfilIklimEntity>(x => x.BangunanId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void AturEvaluasi(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<EvaluasiEntity> b = modelBuilder.Entity<EvaluasiEntity>();

        b.ToTable("evaluasi", t =>
        {
            t.HasCheckConstraint("evaluasi_skor_rentang", "skor_kesesuaian BETWEEN 0 AND 100");
            t.HasCheckConstraint("evaluasi_emisi_awal_nonnegatif", "emisi_awal_kg >= 0");
            t.HasCheckConstraint("evaluasi_alasan_tidak_kosong", "length(btrim(alasan)) > 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.KomponenId).HasColumnName("komponen_id");
        b.Property(x => x.BahanId).HasColumnName("bahan_id");
        b.Property(x => x.SkorKesesuaian).HasColumnName("skor_kesesuaian");
        b.Property(x => x.EmisiAwalKg).HasColumnName("emisi_awal_kg");
        b.Property(x => x.Alasan).HasColumnName("alasan").IsRequired();
        b.Property(x => x.SkorTermal).HasColumnName("skor_termal");
        b.Property(x => x.SkorLembap).HasColumnName("skor_lembap");
        b.Property(x => x.SkorKorosi).HasColumnName("skor_korosi");
        b.Property(x => x.SkorUsia).HasColumnName("skor_usia");
        b.Property(x => x.DihitungPada).HasColumnName("dihitung_pada");
        b.Property(x => x.BatchId).HasColumnName("batch_id");
        b.Property(x => x.VersiModel).HasColumnName("versi_model").HasDefaultValue("v2.0").IsRequired();
        b.Property(x => x.HashInput).HasColumnName("hash_input");
        b.Property(x => x.DihitungPadaUtc).HasColumnName("dihitung_pada_utc");
        b.Property(x => x.InputSnapshotJson).HasColumnName("input_snapshot_json").HasColumnType("jsonb");

        b.HasIndex(x => x.KomponenId).HasDatabaseName("evaluasi_komponen_id_idx");
        b.HasIndex(x => x.BahanId).HasDatabaseName("evaluasi_bahan_id_idx");
        b.HasIndex(x => x.BatchId).HasDatabaseName("evaluasi_batch_id_idx");

        b.HasOne(x => x.Komponen)
            .WithMany()
            .HasForeignKey(x => x.KomponenId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Bahan)
            .WithMany()
            .HasForeignKey(x => x.BahanId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Batch)
            .WithMany()
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void AturSimulasiEmisi(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<SimulasiEmisiEntity> b = modelBuilder.Entity<SimulasiEmisiEntity>();

        b.ToTable("simulasi_emisi", t =>
        {
            t.HasCheckConstraint("simulasi_emisi_horizon_positif", "horizon_tahun > 0");
            t.HasCheckConstraint("simulasi_emisi_usia_efektif_positif", "usia_pakai_efektif_tahun > 0");
            t.HasCheckConstraint("simulasi_emisi_jumlah_penggantian_nonnegatif", "jumlah_penggantian >= 0");
            t.HasCheckConstraint("simulasi_emisi_emisi_kumulatif_nonnegatif", "emisi_kumulatif_kg >= 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.EvaluasiId).HasColumnName("evaluasi_id");
        b.Property(x => x.HorizonTahun).HasColumnName("horizon_tahun");
        b.Property(x => x.UsiaPakaiEfektifTahun).HasColumnName("usia_pakai_efektif_tahun");
        b.Property(x => x.JumlahPenggantian).HasColumnName("jumlah_penggantian");
        b.Property(x => x.EmisiKumulatifKg).HasColumnName("emisi_kumulatif_kg");
        b.Property(x => x.VersiModel).HasColumnName("versi_model").HasDefaultValue("v2.0").IsRequired();
        b.Property(x => x.FaktorPenuaanA).HasColumnName("faktor_penuaan_a").HasDefaultValue(0.5);
        b.Property(x => x.DihitungPadaUtc).HasColumnName("dihitung_pada_utc");

        b.HasIndex(x => new { x.EvaluasiId, x.HorizonTahun, x.VersiModel, x.FaktorPenuaanA })
            .IsUnique()
            .HasDatabaseName("uk_simulasi_emisi_evaluasi_horizon_versi");

        b.HasOne(x => x.Evaluasi)
            .WithMany()
            .HasForeignKey(x => x.EvaluasiId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void AturSumberPropertiBahan(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<SumberPropertiBahanEntity> b = modelBuilder.Entity<SumberPropertiBahanEntity>();

        b.ToTable("sumber_properti_bahan", t =>
        {
            t.HasCheckConstraint("sumber_properti_bahan_status_enum", "status IN ('perkiraan', 'terverifikasi', 'manual')");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.BahanId).HasColumnName("bahan_id");
        b.Property(x => x.Properti).HasColumnName("properti").IsRequired();
        b.Property(x => x.Referensi).HasColumnName("referensi").IsRequired();
        b.Property(x => x.Versi).HasColumnName("versi");
        b.Property(x => x.Tautan).HasColumnName("tautan");
        b.Property(x => x.Status).HasColumnName("status").HasDefaultValue("perkiraan").IsRequired();
        b.Property(x => x.CatatanKonversi).HasColumnName("catatan_konversi");

        b.HasIndex(x => x.BahanId).HasDatabaseName("sumber_properti_bahan_bahan_id_idx");
        b.HasOne(x => x.Bahan)
            .WithMany()
            .HasForeignKey(x => x.BahanId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasData(KatalogSeed.SumberProperti);
    }

    private static void AturBatchEvaluasi(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<BatchEvaluasiEntity> b = modelBuilder.Entity<BatchEvaluasiEntity>();

        b.ToTable("batch_evaluasi", t =>
        {
            t.HasCheckConstraint("batch_evaluasi_revisi_positif", "revisi_lokasi >= 1");
            t.HasCheckConstraint("batch_evaluasi_luas_positif", "luas_m2 > 0");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProyekId).HasColumnName("proyek_id");
        b.Property(x => x.KomponenId).HasColumnName("komponen_id");
        b.Property(x => x.RevisiLokasi).HasColumnName("revisi_lokasi").HasDefaultValue(1);
        b.Property(x => x.HashInput).HasColumnName("hash_input").IsRequired();
        b.Property(x => x.VersiModel).HasColumnName("versi_model").HasDefaultValue("v2.0").IsRequired();
        b.Property(x => x.BobotTermal).HasColumnName("bobot_termal");
        b.Property(x => x.BobotLembap).HasColumnName("bobot_lembap");
        b.Property(x => x.BobotKorosi).HasColumnName("bobot_korosi");
        b.Property(x => x.BobotUsia).HasColumnName("bobot_usia");
        b.Property(x => x.LuasM2).HasColumnName("luas_m2");
        b.Property(x => x.DihitungPadaUtc).HasColumnName("dihitung_pada_utc");

        b.HasIndex(x => new { x.KomponenId, x.HashInput, x.VersiModel })
            .IsUnique()
            .HasDatabaseName("uk_batch_evaluasi_komponen_hash_versi");
        b.HasIndex(x => x.ProyekId).HasDatabaseName("batch_evaluasi_proyek_id_idx");

        b.HasOne(x => x.Proyek)
            .WithMany()
            .HasForeignKey(x => x.ProyekId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Komponen)
            .WithMany()
            .HasForeignKey(x => x.KomponenId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void AturPekerjaan(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<PekerjaanEntity> b = modelBuilder.Entity<PekerjaanEntity>();

        b.ToTable("pekerjaan", t =>
        {
            t.HasCheckConstraint("pekerjaan_rentang_tanggal_valid", "tanggal_mulai <= tanggal_selesai");
            t.HasCheckConstraint("pekerjaan_jenis_enum", "jenis IN ('pengecoran', 'pengecatan')");
            t.HasCheckConstraint("pekerjaan_status_enum", "status IN ('Draft', 'Terkunci', 'PerluDitinjau', 'Selesai')");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.BangunanId).HasColumnName("bangunan_id");
        b.Property(x => x.Nama).HasColumnName("nama").IsRequired();
        b.Property(x => x.Jenis).HasColumnName("jenis").HasConversion(KonversiJenisPekerjaan).IsRequired();
        b.Property(x => x.TanggalMulai).HasColumnName("tanggal_mulai");
        b.Property(x => x.TanggalSelesai).HasColumnName("tanggal_selesai");
        b.Property(x => x.Status).HasColumnName("status").HasConversion(KonversiStatusPekerjaan).HasDefaultValue(StatusPekerjaan.Draft).IsRequired();
        b.Property(x => x.TanggalDikunci).HasColumnName("tanggal_dikunci");
        b.Property(x => x.KomponenId).HasColumnName("komponen_id");
        b.Property(x => x.BahanId).HasColumnName("bahan_id");
        b.Property(x => x.RevisiLokasiSaatKunci).HasColumnName("revisi_lokasi_saat_kunci");
        b.Property(x => x.OverrideAmbangJson).HasColumnName("override_ambang_json");
        b.Property(x => x.KonfirmasiJson).HasColumnName("konfirmasi_json");
        b.Property(x => x.TerakhirDiperbaruiUtc).HasColumnName("terakhir_diperbarui_utc");

        b.HasIndex(x => x.BangunanId).HasDatabaseName("pekerjaan_bangunan_id_idx");

        b.HasOne(x => x.Bangunan)
            .WithMany()
            .HasForeignKey(x => x.BangunanId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Komponen)
            .WithMany()
            .HasForeignKey(x => x.KomponenId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Bahan)
            .WithMany()
            .HasForeignKey(x => x.BahanId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void AturKeputusanJadwal(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<KeputusanJadwalEntity> b = modelBuilder.Entity<KeputusanJadwalEntity>();

        b.ToTable("keputusan_jadwal");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PekerjaanId).HasColumnName("pekerjaan_id");
        b.Property(x => x.Tanggal).HasColumnName("tanggal");
        b.Property(x => x.Status).HasColumnName("status").IsRequired();
        b.Property(x => x.CatatanJson).HasColumnName("catatan_json").HasDefaultValue("[]").IsRequired();
        b.Property(x => x.Terkunci).HasColumnName("terkunci").HasDefaultValue(false);
        b.Property(x => x.DikunciPadaUtc).HasColumnName("dikunci_pada_utc");
        b.Property(x => x.PersetujuanRisiko).HasColumnName("persetujuan_risiko").HasDefaultValue(false);
        b.Property(x => x.KonfirmasiDataLama).HasColumnName("konfirmasi_data_lama").HasDefaultValue(false);
        b.Property(x => x.CatatanKonfirmasi).HasColumnName("catatan_konfirmasi");
        b.Property(x => x.SnapshotCuacaJson).HasColumnName("snapshot_cuaca_json");
        b.Property(x => x.SnapshotAmbangJson).HasColumnName("snapshot_ambang_json");
        b.Property(x => x.UmurDataJam).HasColumnName("umur_data_jam");
        b.Property(x => x.DibuatPadaUtc).HasColumnName("dibuat_pada_utc");

        b.HasIndex(x => x.PekerjaanId).HasDatabaseName("keputusan_jadwal_pekerjaan_id_idx");

        b.HasOne(x => x.Pekerjaan)
            .WithMany()
            .HasForeignKey(x => x.PekerjaanId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void AturPrakiraanCache(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<PrakiraanCacheEntity> b = modelBuilder.Entity<PrakiraanCacheEntity>();

        b.ToTable("prakiraan_cache", t =>
        {
            t.HasCheckConstraint("prakiraan_cache_revisi_positif", "revisi_lokasi >= 1");
            t.HasCheckConstraint("prakiraan_cache_probabilitas_rentang", "probabilitas_hujan_pct BETWEEN 0 AND 100");
            t.HasCheckConstraint("prakiraan_cache_curah_hujan_nonnegatif", "curah_hujan_mm >= 0");
            t.HasCheckConstraint("prakiraan_cache_kelembapan_rentang", "kelembapan_maks_pct BETWEEN 0 AND 100");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.BangunanId).HasColumnName("bangunan_id");
        b.Property(x => x.Latitude).HasColumnName("latitude");
        b.Property(x => x.Longitude).HasColumnName("longitude");
        b.Property(x => x.RevisiLokasi).HasColumnName("revisi_lokasi").HasDefaultValue(1);
        b.Property(x => x.TanggalCuaca).HasColumnName("tanggal_cuaca");
        b.Property(x => x.ZonaWaktu).HasColumnName("zona_waktu").HasDefaultValue("Asia/Jakarta").IsRequired();
        b.Property(x => x.DiambilPadaUtc).HasColumnName("diambil_pada_utc");
        b.Property(x => x.Penyedia).HasColumnName("penyedia").HasDefaultValue("Open-Meteo").IsRequired();
        b.Property(x => x.DataParsial).HasColumnName("data_parsial").HasDefaultValue(false);
        b.Property(x => x.DataLengkap).HasColumnName("data_lengkap").HasDefaultValue(true);
        b.Property(x => x.CatatanKualitas).HasColumnName("catatan_kualitas");
        b.Property(x => x.ProbabilitasHujanPct).HasColumnName("probabilitas_hujan_pct");
        b.Property(x => x.CurahHujanMm).HasColumnName("curah_hujan_mm");
        b.Property(x => x.SuhuMinC).HasColumnName("suhu_min_c");
        b.Property(x => x.SuhuMaksC).HasColumnName("suhu_maks_c");
        b.Property(x => x.KelembapanMaksPct).HasColumnName("kelembapan_maks_pct");

        b.HasIndex(x => new { x.BangunanId, x.RevisiLokasi, x.TanggalCuaca })
            .HasDatabaseName("prakiraan_cache_bangunan_revisi_tanggal_idx");
        b.HasIndex(x => new { x.Latitude, x.Longitude, x.RevisiLokasi, x.TanggalCuaca })
            .HasDatabaseName("prakiraan_cache_lat_lon_revisi_tanggal_idx");

        b.HasOne(x => x.Bangunan)
            .WithMany()
            .HasForeignKey(x => x.BangunanId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void AturAmbangPekerjaan(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<AmbangPekerjaanEntity> b = modelBuilder.Entity<AmbangPekerjaanEntity>();

        b.ToTable("ambang_pekerjaan", t =>
        {
            t.HasCheckConstraint("ambang_pekerjaan_tingkat_suhu_enum", "tingkat_suhu IN ('peringatan', 'keras')");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PekerjaanId).HasColumnName("pekerjaan_id");
        b.Property(x => x.JenisPekerjaan).HasColumnName("jenis_pekerjaan").HasConversion(KonversiJenisPekerjaan);
        b.Property(x => x.ProbabilitasHujanMaksPct).HasColumnName("probabilitas_hujan_maks_pct");
        b.Property(x => x.CurahHujanMaksMm).HasColumnName("curah_hujan_maks_mm");
        b.Property(x => x.SuhuMinC).HasColumnName("suhu_min_c");
        b.Property(x => x.SuhuMaksC).HasColumnName("suhu_maks_c");
        b.Property(x => x.KelembapanMaksPct).HasColumnName("kelembapan_maks_pct");
        b.Property(x => x.TingkatSuhu).HasColumnName("tingkat_suhu").HasConversion(KonversiTingkatPelanggaran).IsRequired();
        b.Property(x => x.SumberAmbang).HasColumnName("sumber_ambang").HasDefaultValue("Default Asumsi").IsRequired();

        b.HasIndex(x => x.PekerjaanId).HasDatabaseName("ambang_pekerjaan_pekerjaan_id_idx");
        b.HasIndex(x => x.JenisPekerjaan).HasDatabaseName("ambang_pekerjaan_jenis_pekerjaan_idx");

        b.HasOne(x => x.Pekerjaan)
            .WithMany()
            .HasForeignKey(x => x.PekerjaanId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void AturKonfigurasiModel(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<KonfigurasiModelEntity> b = modelBuilder.Entity<KonfigurasiModelEntity>();

        b.ToTable("konfigurasi_model");

        b.HasKey(x => x.VersiModel);
        b.Property(x => x.VersiModel).HasColumnName("versi_model").IsRequired();
        b.Property(x => x.BobotLembapHujanAlpha).HasColumnName("bobot_lembap_hujan_alpha").HasDefaultValue(0.5);
        b.Property(x => x.AcuanCurahHujanMm).HasColumnName("acuan_curah_hujan_mm").HasDefaultValue(3000.0);
        b.Property(x => x.AcuanJarakPesisirKm).HasColumnName("acuan_jarak_pesisir_km").HasDefaultValue(50.0);
        b.Property(x => x.PitaTermalBatasBawah).HasColumnName("pita_termal_batas_bawah").HasDefaultValue(0.10);
        b.Property(x => x.PitaTermalBatasAtasSejuk).HasColumnName("pita_termal_batas_atas_sejuk").HasDefaultValue(0.60);
        b.Property(x => x.PitaTermalBatasAtasPanas).HasColumnName("pita_termal_batas_atas_panas").HasDefaultValue(0.30);
        b.Property(x => x.SuhuSejukC).HasColumnName("suhu_sejuk_c").HasDefaultValue(26.0);
        b.Property(x => x.SuhuPanasC).HasColumnName("suhu_panas_c").HasDefaultValue(32.0);
        b.Property(x => x.FaktorPenuaanA).HasColumnName("faktor_penuaan_a").HasDefaultValue(0.5);
        b.Property(x => x.AmbangTidakDisarankan).HasColumnName("ambang_tidak_disarankan").HasDefaultValue(55.0);
        b.Property(x => x.AmbangCocok).HasColumnName("ambang_cocok").HasDefaultValue(75.0);
        b.Property(x => x.Aktif).HasColumnName("aktif").HasDefaultValue(false);
        b.Property(x => x.DibuatPadaUtc).HasColumnName("dibuat_pada_utc");

        b.HasData(new KonfigurasiModelEntity
        {
            VersiModel = "v2.0",
            BobotLembapHujanAlpha = 0.5,
            AcuanCurahHujanMm = 3000.0,
            AcuanJarakPesisirKm = 50.0,
            PitaTermalBatasBawah = 0.10,
            PitaTermalBatasAtasSejuk = 0.60,
            PitaTermalBatasAtasPanas = 0.30,
            SuhuSejukC = 26.0,
            SuhuPanasC = 32.0,
            FaktorPenuaanA = 0.5,
            AmbangTidakDisarankan = 55.0,
            AmbangCocok = 75.0,
            Aktif = true,
            DibuatPadaUtc = new DateTimeOffset(2026, 11, 22, 0, 0, 0, TimeSpan.Zero)
        });
    }

    private static void AturLaporan(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<LaporanEntity> b = modelBuilder.Entity<LaporanEntity>();

        b.ToTable("laporan");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.BangunanId).HasColumnName("bangunan_id");
        b.Property(x => x.PathBerkas).HasColumnName("path_berkas").IsRequired();
        b.Property(x => x.DibuatPadaUtc).HasColumnName("dibuat_pada_utc");
        b.Property(x => x.HashSnapshot).HasColumnName("hash_snapshot");
        b.Property(x => x.StatusBerkas).HasColumnName("status_berkas").HasDefaultValue("Tersimpan").IsRequired();
        b.Property(x => x.ChecksumSha256).HasColumnName("checksum_sha256");
        b.Property(x => x.UkuranByte).HasColumnName("ukuran_byte");
        b.Property(x => x.OpsiJson).HasColumnName("opsi_json");

        b.HasIndex(x => x.BangunanId).HasDatabaseName("laporan_bangunan_id_idx");

        b.HasOne(x => x.Bangunan)
            .WithMany()
            .HasForeignKey(x => x.BangunanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
