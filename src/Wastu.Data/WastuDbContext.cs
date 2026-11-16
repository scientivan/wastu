using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wastu.Data.Entities;
using Wastu.Domain;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        AturBahan(modelBuilder);
        AturBangunan(modelBuilder);
        AturKomponen(modelBuilder);
        AturProfilIklim(modelBuilder);
        AturEvaluasi(modelBuilder);
        AturSimulasiEmisi(modelBuilder);
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
        b.HasIndex(x => x.KomponenId).HasDatabaseName("evaluasi_komponen_id_idx");
        b.HasIndex(x => x.BahanId).HasDatabaseName("evaluasi_bahan_id_idx");
        b.HasOne(x => x.Komponen)
            .WithMany()
            .HasForeignKey(x => x.KomponenId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Bahan)
            .WithMany()
            .HasForeignKey(x => x.BahanId)
            .OnDelete(DeleteBehavior.Restrict);
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

        b.HasKey(x => x.EvaluasiId);
        b.Property(x => x.EvaluasiId).HasColumnName("evaluasi_id");
        b.Property(x => x.HorizonTahun).HasColumnName("horizon_tahun");
        b.Property(x => x.UsiaPakaiEfektifTahun).HasColumnName("usia_pakai_efektif_tahun");
        b.Property(x => x.JumlahPenggantian).HasColumnName("jumlah_penggantian");
        b.Property(x => x.EmisiKumulatifKg).HasColumnName("emisi_kumulatif_kg");
        b.HasOne(x => x.Evaluasi)
            .WithOne()
            .HasForeignKey<SimulasiEmisiEntity>(x => x.EvaluasiId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
