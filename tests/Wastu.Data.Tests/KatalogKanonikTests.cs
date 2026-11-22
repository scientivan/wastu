using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Wastu.Data.Entities;
using Wastu.Data.Repositories;
using Wastu.Domain;
using Wastu.InMemory;

namespace Wastu.Data.Tests;

public class KatalogKanonikTests
{
    private static string TemukanRootRepositori()
    {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "database", "02_schema.sql")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new FileNotFoundException("Root repositori wastu tidak ditemukan dari " + AppContext.BaseDirectory);
    }

    [Fact]
    public void JumlahDanValidasiRentangKatalogKanonik_X02_X09()
    {
        // X02: minimal 15 bahan dinding dan minimal 10 bahan atap
        var bahanList = KatalogSeed.Bahan;
        int dindingCount = bahanList.Count(b => b.Jenis == JenisKomponen.Dinding);
        int atapCount = bahanList.Count(b => b.Jenis == JenisKomponen.Atap);

        Assert.True(dindingCount >= 15, $"Dinding harus >= 15, aktual: {dindingCount}");
        Assert.True(atapCount >= 10, $"Atap harus >= 10, aktual: {atapCount}");
        Assert.Equal(25, bahanList.Count);

        // Keunikan ID dan Nama per Jenis
        var ids = bahanList.Select(b => b.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());

        var namaJenis = bahanList.Select(b => (b.Nama, b.Jenis)).ToList();
        Assert.Equal(namaJenis.Count, namaJenis.Distinct().Count());

        // Validasi batasan numerik dan kelengkapan string
        foreach (var b in bahanList)
        {
            Assert.False(string.IsNullOrWhiteSpace(b.Nama));
            Assert.True(double.IsFinite(b.KonduktivitasTermal) && b.KonduktivitasTermal > 0);
            Assert.InRange(b.KetahananLembap, 1, 5);
            Assert.InRange(b.KetahananKorosi, 1, 5);
            Assert.True(double.IsFinite(b.UsiaPakaiNominalTahun) && b.UsiaPakaiNominalTahun > 0);
            Assert.True(double.IsFinite(b.FaktorEmisi) && b.FaktorEmisi >= 0);
            Assert.False(string.IsNullOrWhiteSpace(b.SumberData));
            Assert.False(string.IsNullOrWhiteSpace(b.SusunanKetebalan), $"Bahan {b.Nama} belum memiliki SusunanKetebalan");
            Assert.True(b.VersiData >= 1);
        }

        // Validasi SumberProperti: harus ada tepat 5 entri per bahan
        var sumberList = KatalogSeed.SumberProperti;
        Assert.Equal(125, sumberList.Count);

        var sumberIds = sumberList.Select(s => s.Id).ToList();
        Assert.Equal(sumberIds.Count, sumberIds.Distinct().Count());

        string[] requiredProps =
        [
            "konduktivitas_termal",
            "ketahanan_lembap",
            "ketahanan_korosi",
            "usia_pakai_nominal",
            "faktor_emisi"
        ];

        foreach (var b in bahanList)
        {
            var sumberBahan = sumberList.Where(s => s.BahanId == b.Id).ToList();
            Assert.Equal(5, sumberBahan.Count);

            foreach (var prop in requiredProps)
            {
                var item = sumberBahan.FirstOrDefault(s => s.Properti == prop);
                Assert.NotNull(item);
                Assert.False(string.IsNullOrWhiteSpace(item!.Referensi), $"Referensi untuk {b.Nama} ({prop}) kosong");
                Assert.False(string.IsNullOrWhiteSpace(item.Status), $"Status untuk {b.Nama} ({prop}) kosong");
                Assert.False(string.IsNullOrWhiteSpace(item.CatatanKonversi), $"Catatan konversi untuk {b.Nama} ({prop}) kosong");
            }
        }
    }

    [Fact]
    public void ParitasKatalogAntaraSeedDanKatalogDemo()
    {
        var seedBahan = KatalogSeed.BahanDomain;
        var demoBahan = KatalogDemo.Semua;

        Assert.Equal(seedBahan.Count, demoBahan.Count);

        foreach (var seed in seedBahan)
        {
            var demo = demoBahan.FirstOrDefault(d => d.Id == seed.Id);
            Assert.NotNull(demo);

            Assert.Equal(seed.Nama, demo!.Nama);
            Assert.Equal(seed.Jenis, demo.Jenis);
            Assert.Equal(seed.KonduktivitasTermal, demo.KonduktivitasTermal, 3);
            Assert.Equal(seed.KetahananLembap, demo.KetahananLembap);
            Assert.Equal(seed.KetahananKorosi, demo.KetahananKorosi);
            Assert.Equal(seed.UsiaPakaiNominalTahun, demo.UsiaPakaiNominalTahun, 3);
            Assert.Equal(seed.FaktorEmisi, demo.FaktorEmisi, 3);
            Assert.Equal(seed.SumberData, demo.SumberData);
            Assert.Equal(seed.SusunanKetebalan, demo.SusunanKetebalan);
            Assert.Equal(seed.VersiData, demo.VersiData);

            Assert.NotNull(demo.SumberProperti);
            Assert.Equal(5, demo.SumberProperti!.Count);

            foreach (var sProp in seed.SumberProperti!)
            {
                var dProp = demo.SumberProperti.FirstOrDefault(p => p.Properti == sProp.Properti);
                Assert.NotNull(dProp);
                Assert.Equal(sProp.Referensi, dProp!.Referensi);
                Assert.Equal(sProp.Status, dProp.Status);
                Assert.Equal(sProp.CatatanKonversi, dProp.CatatanKonversi);
                Assert.Equal(sProp.Versi, dProp.Versi);
                Assert.Equal(sProp.Tautan, dProp.Tautan);
            }
        }
    }

    [Fact]
    public async Task ParitasKatalogDenganSqlSeed_03_seed_supabase()
    {
        string root = TemukanRootRepositori();
        string sqlPath = Path.Combine(root, "database", "03_seed_supabase.sql");
        string sql = await File.ReadAllTextAsync(sqlPath);

        // Parse baris bahan dari SQL
        var rowsBahan = ParseSqlValues(sql, "bahan");
        Assert.Equal(25, rowsBahan.Count);

        foreach (var fields in rowsBahan)
        {
            var id = new Guid(fields[0]);
            var nama = fields[1];
            var jenisStr = fields[2];
            var konduktivitas = double.Parse(fields[3], CultureInfo.InvariantCulture);
            var lembap = int.Parse(fields[4]);
            var korosi = int.Parse(fields[5]);
            var usia = double.Parse(fields[6], CultureInfo.InvariantCulture);
            var emisi = double.Parse(fields[7], CultureInfo.InvariantCulture);
            var susunan = fields[9];

            var seed = KatalogSeed.Bahan.FirstOrDefault(b => b.Id == id);
            Assert.NotNull(seed);
            Assert.Equal(seed!.Nama, nama);
            Assert.Equal(seed.Jenis == JenisKomponen.Dinding ? "dinding" : "atap", jenisStr);
            Assert.Equal(seed.KonduktivitasTermal, konduktivitas, 2);
            Assert.Equal(seed.KetahananLembap, lembap);
            Assert.Equal(seed.KetahananKorosi, korosi);
            Assert.Equal(seed.UsiaPakaiNominalTahun, usia, 2);
            Assert.Equal(seed.FaktorEmisi, emisi, 2);
            Assert.Equal(seed.SusunanKetebalan, susunan);
        }

        // Parse baris sumber_properti_bahan dari SQL
        var rowsSumber = ParseSqlValues(sql, "sumber_properti_bahan");
        Assert.Equal(125, rowsSumber.Count);

        foreach (var fields in rowsSumber)
        {
            var id = new Guid(fields[0]);
            var bahanId = new Guid(fields[1]);
            var properti = fields[2];
            var referensi = fields[3];
            var status = fields[6];
            var catatan = fields[7];

            var seedProp = KatalogSeed.SumberProperti.FirstOrDefault(s => s.Id == id);
            Assert.NotNull(seedProp);
            Assert.Equal(seedProp!.BahanId, bahanId);
            Assert.Equal(seedProp.Properti, properti);
            Assert.Equal(seedProp.Referensi, referensi);
            Assert.Equal(seedProp.Status.ToLowerInvariant(), status.ToLowerInvariant());
            Assert.Equal(seedProp.CatatanKonversi, catatan);
        }
    }

    private static List<List<string>> ParseSqlValues(string sql, string tableName)
    {
        var results = new List<List<string>>();
        int tableIdx = sql.IndexOf($"INSERT INTO {tableName}", StringComparison.OrdinalIgnoreCase);
        while (tableIdx != -1)
        {
            int valuesIdx = sql.IndexOf("VALUES", tableIdx, StringComparison.OrdinalIgnoreCase);
            if (valuesIdx == -1) break;
            int onConflictIdx = sql.IndexOf("ON CONFLICT", valuesIdx, StringComparison.OrdinalIgnoreCase);
            if (onConflictIdx == -1) onConflictIdx = sql.Length;

            string block = sql.Substring(valuesIdx + 6, onConflictIdx - (valuesIdx + 6));

            int i = 0;
            while (i < block.Length)
            {
                if (block[i] == '(')
                {
                    i++;
                    var fields = new List<string>();
                    while (i < block.Length && block[i] != ')')
                    {
                        while (i < block.Length && (char.IsWhiteSpace(block[i]) || block[i] == ',')) i++;
                        if (i >= block.Length || block[i] == ')') break;

                        if (block[i] == '\'')
                        {
                            i++;
                            var sb = new System.Text.StringBuilder();
                            while (i < block.Length)
                            {
                                if (block[i] == '\'' && i + 1 < block.Length && block[i + 1] == '\'')
                                {
                                    sb.Append('\'');
                                    i += 2;
                                }
                                else if (block[i] == '\'')
                                {
                                    i++;
                                    break;
                                }
                                else
                                {
                                    sb.Append(block[i]);
                                    i++;
                                }
                            }
                            fields.Add(sb.ToString().Trim());
                        }
                        else
                        {
                            var sb = new System.Text.StringBuilder();
                            while (i < block.Length && block[i] != ',' && block[i] != ')')
                            {
                                sb.Append(block[i]);
                                i++;
                            }
                            fields.Add(sb.ToString().Trim());
                        }
                    }
                    if (fields.Count > 0)
                    {
                        results.Add(fields);
                    }
                }
                i++;
            }

            tableIdx = sql.IndexOf($"INSERT INTO {tableName}", onConflictIdx, StringComparison.OrdinalIgnoreCase);
        }
        return results;
    }

    [PerluPostgreSql]
    public async Task T39_F204_TambahBahanLewatDbDanRefresh_TampilTanpaRebuild()
    {
        await using var testDb = await PostgreSqlTestDatabase.CreateAsync();

        string root = TemukanRootRepositori();
        string schemaSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "02_schema.sql"));
        string seedSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "03_seed_supabase.sql"));

        await testDb.ExecuteSqlScriptAsync(schemaSql);
        await testDb.ExecuteSqlScriptAsync(seedSql);

        var materialRepo = new MaterialRepositoryEf(testDb.Factory);
        var sumberRepo = new SumberBahanRepositoryEf(testDb.Factory);

        // Kondisi awal: 15 dinding
        var dindingAwal = await materialRepo.ListByKindAsync(JenisKomponen.Dinding);
        Assert.Equal(15, dindingAwal.Count);

        // Tambah bahan baru langsung ke PostgreSQL tanpa mengubah kode C# atau rebuild (F2-04, T39)
        var newMaterialId = Guid.NewGuid();
        string insertSql = $@"
        INSERT INTO bahan (id, nama, jenis, konduktivitas_termal, ketahanan_lembap, ketahanan_korosi, usia_pakai_nominal_tahun, faktor_emisi, sumber_data, susunan_ketebalan, versi_data)
        VALUES ('{newMaterialId}', 'Bahan Uji Dinamis T39', 'dinding', 0.18, 4, 3, 35, 48, 'Pengujian Dinamis F2-04', 'Panel komposit dinding uji 80 mm', 1);

        INSERT INTO sumber_properti_bahan (id, bahan_id, properti, referensi, versi, tautan, status, catatan_konversi) VALUES
        ('{Guid.NewGuid()}', '{newMaterialId}', 'konduktivitas_termal', 'Uji Lab Termal Dinamis', '2026', NULL, 'terverifikasi', 'Hasil uji hot box konduktivitas 0.18 W/mK'),
        ('{Guid.NewGuid()}', '{newMaterialId}', 'ketahanan_lembap', 'Uji Siklus Basah Dinamis', '2026', NULL, 'terverifikasi', 'Uji permeabilitas indeks 4'),
        ('{Guid.NewGuid()}', '{newMaterialId}', 'ketahanan_korosi', 'Uji Kabut Garam Dinamis', '2026', NULL, 'terverifikasi', 'Uji paparan kabut garam 500 jam indeks 3'),
        ('{Guid.NewGuid()}', '{newMaterialId}', 'usia_pakai_nominal', 'Estimasi Siklus Hidup Dinamis', '2026', NULL, 'perkiraan', 'Usia nominal 35 tahun kondisi standar'),
        ('{Guid.NewGuid()}', '{newMaterialId}', 'faktor_emisi', 'LCA EPD Dinamis', '2026', NULL, 'terverifikasi', 'Jejak karbon manufaktur 48 kgCO2e/m2');
        ";

        await testDb.ExecuteSqlScriptAsync(insertSql);

        // Segarkan katalog lewat repository: bahan baru langsung muncul dengan properti lengkap
        var dindingSetelah = await materialRepo.ListByKindAsync(JenisKomponen.Dinding);
        Assert.Equal(16, dindingSetelah.Count);

        var loaded = await materialRepo.GetByIdAsync(newMaterialId);
        Assert.NotNull(loaded);
        Assert.Equal("Bahan Uji Dinamis T39", loaded!.Nama);
        Assert.Equal(JenisKomponen.Dinding, loaded.Jenis);
        Assert.Equal(0.18, loaded.KonduktivitasTermal, 2);
        Assert.Equal(4, loaded.KetahananLembap);
        Assert.Equal(3, loaded.KetahananKorosi);
        Assert.Equal(35, loaded.UsiaPakaiNominalTahun);
        Assert.Equal(48, loaded.FaktorEmisi);
        Assert.Equal("Panel komposit dinding uji 80 mm", loaded.SusunanKetebalan);
        Assert.Equal("Pengujian Dinamis F2-04", loaded.SumberData);

        // Verifikasi bahwa sumber properti termuat bersama bahan
        Assert.NotNull(loaded.SumberProperti);
        Assert.Equal(5, loaded.SumberProperti!.Count);

        // Verifikasi juga query langsung lewat SumberBahanRepositoryEf
        var sumberList = await sumberRepo.ListByBahanIdAsync(newMaterialId);
        Assert.Equal(5, sumberList.Count);
        var sTermal = sumberList.FirstOrDefault(s => s.Properti == "konduktivitas_termal");
        Assert.NotNull(sTermal);
        Assert.Equal(StatusVerifikasiProperti.Terverifikasi, sTermal!.Status);
        Assert.Equal("Uji Lab Termal Dinamis", sTermal.Referensi);

        var sEmisi = sumberList.FirstOrDefault(s => s.Properti == "faktor_emisi");
        Assert.NotNull(sEmisi);
        Assert.Equal("LCA EPD Dinamis", sEmisi!.Referensi);
        Assert.Equal("Jejak karbon manufaktur 48 kgCO2e/m2", sEmisi.CatatanKonversi);
    }

    [PerluPostgreSql]
    public async Task MigrateAsync_Memiliki25BahanDenganSusunanKetebalanDan125SumberPropertiIdentikSql()
    {
        await using var testDb = await PostgreSqlTestDatabase.CreateAsync();

        await using (var db = testDb.Factory.CreateDbContext())
        {
            await db.Database.MigrateAsync();

            var bahanEf = await db.Bahan.ToListAsync();
            Assert.Equal(25, bahanEf.Count);
            Assert.All(bahanEf, b => Assert.False(string.IsNullOrWhiteSpace(b.SusunanKetebalan)));

            var sumberEf = await db.SumberPropertiBahan.ToListAsync();
            Assert.Equal(125, sumberEf.Count);

            string root = TemukanRootRepositori();
            string sqlPath = Path.Combine(root, "database", "03_seed_supabase.sql");
            string sql = await File.ReadAllTextAsync(sqlPath);

            var rowsBahan = ParseSqlValues(sql, "bahan");
            Assert.Equal(25, rowsBahan.Count);
            foreach (var r in rowsBahan)
            {
                var id = new Guid(r[0]);
                var b = bahanEf.FirstOrDefault(x => x.Id == id);
                Assert.NotNull(b);
                Assert.Equal(r[1], b!.Nama);
                Assert.Equal(r[9], b.SusunanKetebalan);
            }

            var rowsSumber = ParseSqlValues(sql, "sumber_properti_bahan");
            Assert.Equal(125, rowsSumber.Count);
            foreach (var r in rowsSumber)
            {
                var id = new Guid(r[0]);
                var s = sumberEf.FirstOrDefault(x => x.Id == id);
                Assert.NotNull(s);
                Assert.Equal(new Guid(r[1]), s!.BahanId);
                Assert.Equal(r[2], s.Properti);
                Assert.Equal(r[3], s.Referensi);
                Assert.Equal(r[6].ToLowerInvariant(), s.Status.ToLowerInvariant());
                Assert.Equal(r[7], s.CatatanKonversi);
            }
        }
    }
}
