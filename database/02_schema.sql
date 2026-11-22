-- Skema basis data Wastu (PRD §9, §16, docs/PRD-wastu.md).
--
-- Berkas ini memuat seluruh tabel yang aktif pada lapisan Wastu.Data V2,
-- selaras penuh dengan migrasi EF Core (InitialCreate + AddV2PersistenceHistory).

CREATE TABLE IF NOT EXISTS bahan (
    id                        uuid NOT NULL,
    nama                      text NOT NULL,
    jenis                     text NOT NULL,
    konduktivitas_termal      double precision NOT NULL,
    ketahanan_lembap          integer NOT NULL,
    ketahanan_korosi          integer NOT NULL,
    usia_pakai_nominal_tahun  double precision NOT NULL,
    faktor_emisi              double precision NOT NULL,
    sumber_data               text NOT NULL,
    susunan_ketebalan         text,
    versi_data                integer NOT NULL DEFAULT 1,
    CONSTRAINT pk_bahan PRIMARY KEY (id),
    CONSTRAINT bahan_jenis_enum CHECK (jenis IN ('dinding', 'atap')),
    CONSTRAINT bahan_konduktivitas_positif CHECK (konduktivitas_termal > 0),
    CONSTRAINT bahan_ketahanan_lembap_rentang CHECK (ketahanan_lembap BETWEEN 1 AND 5),
    CONSTRAINT bahan_ketahanan_korosi_rentang CHECK (ketahanan_korosi BETWEEN 1 AND 5),
    CONSTRAINT bahan_usia_positif CHECK (usia_pakai_nominal_tahun > 0),
    CONSTRAINT bahan_faktor_emisi_nonnegatif CHECK (faktor_emisi >= 0)
);

CREATE TABLE IF NOT EXISTS bangunan (
    id            uuid NOT NULL,
    nama          text NOT NULL,
    kota          text NOT NULL,
    latitude      double precision NOT NULL,
    longitude     double precision NOT NULL,
    dibuat_pada   timestamp with time zone NOT NULL,
    revisi_lokasi integer NOT NULL DEFAULT 1,
    zona_waktu    text,
    CONSTRAINT pk_bangunan PRIMARY KEY (id),
    CONSTRAINT bangunan_berada_di_indonesia
        CHECK (latitude BETWEEN -11 AND 6 AND longitude BETWEEN 94 AND 142)
);

CREATE TABLE IF NOT EXISTS komponen (
    id            uuid NOT NULL,
    bangunan_id   uuid NOT NULL,
    jenis         text NOT NULL,
    luas_m2       double precision NOT NULL,
    CONSTRAINT pk_komponen PRIMARY KEY (id),
    CONSTRAINT komponen_jenis_enum CHECK (jenis IN ('dinding', 'atap')),
    CONSTRAINT komponen_luas_positif CHECK (luas_m2 > 0),
    CONSTRAINT fk_komponen_bangunan FOREIGN KEY (bangunan_id)
        REFERENCES bangunan (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS profil_iklim (
    bangunan_id            uuid NOT NULL,
    curah_hujan_tahunan_mm double precision NOT NULL,
    kelembapan_rata_pct    double precision NOT NULL,
    suhu_rata_c            double precision NOT NULL,
    jarak_pesisir_km       double precision NOT NULL,
    tingkat_korosi         text NOT NULL,
    diambil_pada           timestamp with time zone NOT NULL,
    revisi_lokasi          integer NOT NULL DEFAULT 1,
    provenance             text NOT NULL DEFAULT 'LegacyUnknown',
    periode_awal           date,
    periode_akhir          date,
    penyedia               text,
    model_iklim            text,
    zona_waktu             text,
    metode_pesisir         text,
    status_pengambilan     text NOT NULL DEFAULT 'Tersedia',
    CONSTRAINT pk_profil_iklim PRIMARY KEY (bangunan_id),
    CONSTRAINT profil_iklim_curah_hujan_nonnegatif CHECK (curah_hujan_tahunan_mm >= 0),
    CONSTRAINT profil_iklim_kelembapan_rentang CHECK (kelembapan_rata_pct BETWEEN 0 AND 100),
    CONSTRAINT profil_iklim_suhu_rentang CHECK (suhu_rata_c BETWEEN -10 AND 60),
    CONSTRAINT profil_iklim_jarak_pesisir_nonnegatif CHECK (jarak_pesisir_km >= 0),
    CONSTRAINT profil_iklim_tingkat_korosi_enum
        CHECK (tingkat_korosi IN ('rendah', 'sedang', 'tinggi')),
    CONSTRAINT fk_profil_iklim_bangunan FOREIGN KEY (bangunan_id)
        REFERENCES bangunan (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS batch_evaluasi (
    id                uuid NOT NULL,
    proyek_id         uuid NOT NULL,
    komponen_id       uuid NOT NULL,
    revisi_lokasi     integer NOT NULL DEFAULT 1,
    hash_input        text NOT NULL,
    versi_model       text NOT NULL DEFAULT 'v2.0',
    bobot_termal      double precision NOT NULL,
    bobot_lembap      double precision NOT NULL,
    bobot_korosi      double precision NOT NULL,
    bobot_usia        double precision NOT NULL,
    luas_m2           double precision NOT NULL,
    dihitung_pada_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_batch_evaluasi PRIMARY KEY (id),
    CONSTRAINT uk_batch_evaluasi_komponen_hash_versi
        UNIQUE (komponen_id, hash_input, versi_model),
    CONSTRAINT batch_evaluasi_revisi_positif CHECK (revisi_lokasi >= 1),
    CONSTRAINT batch_evaluasi_luas_positif CHECK (luas_m2 > 0),
    CONSTRAINT fk_batch_evaluasi_bangunan FOREIGN KEY (proyek_id)
        REFERENCES bangunan (id) ON DELETE CASCADE,
    CONSTRAINT fk_batch_evaluasi_komponen FOREIGN KEY (komponen_id)
        REFERENCES komponen (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS evaluasi (
    id                uuid NOT NULL,
    komponen_id       uuid NOT NULL,
    bahan_id          uuid NOT NULL,
    skor_kesesuaian   double precision NOT NULL,
    emisi_awal_kg     double precision NOT NULL,
    alasan            text NOT NULL,
    skor_termal       double precision NOT NULL,
    skor_lembap       double precision NOT NULL,
    skor_korosi       double precision NOT NULL,
    skor_usia         double precision NOT NULL,
    dihitung_pada     timestamp with time zone NOT NULL,
    batch_id          uuid,
    versi_model       text NOT NULL DEFAULT 'v2.0',
    hash_input        text,
    dihitung_pada_utc timestamp with time zone,
    input_snapshot_json jsonb,
    CONSTRAINT pk_evaluasi PRIMARY KEY (id),
    CONSTRAINT evaluasi_skor_rentang CHECK (skor_kesesuaian BETWEEN 0 AND 100),
    CONSTRAINT evaluasi_emisi_awal_nonnegatif CHECK (emisi_awal_kg >= 0),
    CONSTRAINT evaluasi_alasan_tidak_kosong CHECK (length(btrim(alasan)) > 0),
    CONSTRAINT fk_evaluasi_komponen FOREIGN KEY (komponen_id)
        REFERENCES komponen (id) ON DELETE CASCADE,
    CONSTRAINT fk_evaluasi_bahan FOREIGN KEY (bahan_id)
        REFERENCES bahan (id) ON DELETE RESTRICT,
    CONSTRAINT fk_evaluasi_batch FOREIGN KEY (batch_id)
        REFERENCES batch_evaluasi (id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS simulasi_emisi (
    id                         uuid NOT NULL DEFAULT gen_random_uuid(),
    evaluasi_id                uuid NOT NULL,
    horizon_tahun              integer NOT NULL,
    usia_pakai_efektif_tahun   double precision NOT NULL,
    jumlah_penggantian         integer NOT NULL,
    emisi_kumulatif_kg         double precision NOT NULL,
    versi_model                text NOT NULL DEFAULT 'v2.0',
    faktor_penuaan_a           double precision NOT NULL DEFAULT 0.5,
    dihitung_pada_utc          timestamp with time zone,
    CONSTRAINT pk_simulasi_emisi PRIMARY KEY (id),
    CONSTRAINT uk_simulasi_emisi_evaluasi_horizon_versi
        UNIQUE (evaluasi_id, horizon_tahun, versi_model, faktor_penuaan_a),
    CONSTRAINT simulasi_emisi_horizon_positif CHECK (horizon_tahun > 0),
    CONSTRAINT simulasi_emisi_usia_efektif_positif CHECK (usia_pakai_efektif_tahun > 0),
    CONSTRAINT simulasi_emisi_jumlah_penggantian_nonnegatif CHECK (jumlah_penggantian >= 0),
    CONSTRAINT simulasi_emisi_emisi_kumulatif_nonnegatif CHECK (emisi_kumulatif_kg >= 0),
    CONSTRAINT fk_simulasi_emisi_evaluasi FOREIGN KEY (evaluasi_id)
        REFERENCES evaluasi (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS sumber_properti_bahan (
    id               uuid NOT NULL DEFAULT gen_random_uuid(),
    bahan_id         uuid NOT NULL,
    properti         text NOT NULL,
    referensi        text NOT NULL,
    versi            text,
    tautan           text,
    status           text NOT NULL DEFAULT 'perkiraan',
    catatan_konversi text,
    CONSTRAINT pk_sumber_properti_bahan PRIMARY KEY (id),
    CONSTRAINT sumber_properti_bahan_status_enum
        CHECK (status IN ('perkiraan', 'terverifikasi', 'manual')),
    CONSTRAINT fk_sumber_properti_bahan_bahan FOREIGN KEY (bahan_id)
        REFERENCES bahan (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS pekerjaan (
    id                      uuid NOT NULL,
    bangunan_id             uuid NOT NULL,
    nama                    text NOT NULL,
    jenis                   text NOT NULL,
    tanggal_mulai           date NOT NULL,
    tanggal_selesai         date NOT NULL,
    status                  text NOT NULL DEFAULT 'Draft',
    tanggal_dikunci         date,
    komponen_id             uuid,
    bahan_id                uuid,
    revisi_lokasi_saat_kunci integer,
    override_ambang_json    text,
    konfirmasi_json         text,
    terakhir_diperbarui_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_pekerjaan PRIMARY KEY (id),
    CONSTRAINT pekerjaan_rentang_tanggal_valid CHECK (tanggal_mulai <= tanggal_selesai),
    CONSTRAINT pekerjaan_jenis_enum CHECK (jenis IN ('pengecoran', 'pengecatan')),
    CONSTRAINT pekerjaan_status_enum CHECK (status IN ('Draft', 'Terkunci', 'PerluDitinjau', 'Selesai')),
    CONSTRAINT fk_pekerjaan_bangunan FOREIGN KEY (bangunan_id)
        REFERENCES bangunan (id) ON DELETE CASCADE,
    CONSTRAINT fk_pekerjaan_komponen FOREIGN KEY (komponen_id)
        REFERENCES komponen (id) ON DELETE SET NULL,
    CONSTRAINT fk_pekerjaan_bahan FOREIGN KEY (bahan_id)
        REFERENCES bahan (id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS keputusan_jadwal (
    id                   uuid NOT NULL DEFAULT gen_random_uuid(),
    pekerjaan_id         uuid NOT NULL,
    tanggal              date NOT NULL,
    status               text NOT NULL,
    catatan_json         text NOT NULL DEFAULT '[]',
    terkunci             boolean NOT NULL DEFAULT false,
    dikunci_pada_utc     timestamp with time zone,
    persetujuan_risiko   boolean NOT NULL DEFAULT false,
    konfirmasi_data_lama boolean NOT NULL DEFAULT false,
    catatan_konfirmasi   text,
    snapshot_cuaca_json  text,
    snapshot_ambang_json text,
    umur_data_jam        double precision,
    dibuat_pada_utc      timestamp with time zone NOT NULL,
    CONSTRAINT pk_keputusan_jadwal PRIMARY KEY (id),
    CONSTRAINT fk_keputusan_jadwal_pekerjaan FOREIGN KEY (pekerjaan_id)
        REFERENCES pekerjaan (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS prakiraan_cache (
    id                    uuid NOT NULL DEFAULT gen_random_uuid(),
    bangunan_id           uuid,
    latitude              double precision NOT NULL,
    longitude             double precision NOT NULL,
    revisi_lokasi         integer NOT NULL DEFAULT 1,
    tanggal_cuaca         date NOT NULL,
    zona_waktu            text NOT NULL DEFAULT 'Asia/Jakarta',
    diambil_pada_utc      timestamp with time zone NOT NULL,
    penyedia              text NOT NULL DEFAULT 'Open-Meteo',
    data_parsial          boolean NOT NULL DEFAULT false,
    data_lengkap          boolean NOT NULL DEFAULT true,
    catatan_kualitas      text,
    probabilitas_hujan_pct double precision NOT NULL,
    curah_hujan_mm        double precision NOT NULL,
    suhu_min_c            double precision NOT NULL,
    suhu_maks_c           double precision NOT NULL,
    kelembapan_maks_pct   double precision NOT NULL,
    CONSTRAINT pk_prakiraan_cache PRIMARY KEY (id),
    CONSTRAINT prakiraan_cache_revisi_positif CHECK (revisi_lokasi >= 1),
    CONSTRAINT prakiraan_cache_probabilitas_rentang CHECK (probabilitas_hujan_pct BETWEEN 0 AND 100),
    CONSTRAINT prakiraan_cache_curah_hujan_nonnegatif CHECK (curah_hujan_mm >= 0),
    CONSTRAINT prakiraan_cache_kelembapan_rentang CHECK (kelembapan_maks_pct BETWEEN 0 AND 100),
    CONSTRAINT fk_prakiraan_cache_bangunan FOREIGN KEY (bangunan_id)
        REFERENCES bangunan (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ambang_pekerjaan (
    id                         uuid NOT NULL DEFAULT gen_random_uuid(),
    pekerjaan_id               uuid,
    jenis_pekerjaan            text,
    probabilitas_hujan_maks_pct double precision,
    curah_hujan_maks_mm        double precision,
    suhu_min_c                 double precision,
    suhu_maks_c                double precision,
    kelembapan_maks_pct        double precision,
    tingkat_suhu               text NOT NULL DEFAULT 'peringatan',
    sumber_ambang              text NOT NULL DEFAULT 'Default Asumsi',
    CONSTRAINT pk_ambang_pekerjaan PRIMARY KEY (id),
    CONSTRAINT ambang_pekerjaan_tingkat_suhu_enum CHECK (tingkat_suhu IN ('peringatan', 'keras')),
    CONSTRAINT fk_ambang_pekerjaan_pekerjaan FOREIGN KEY (pekerjaan_id)
        REFERENCES pekerjaan (id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS konfigurasi_model (
    versi_model                text NOT NULL,
    bobot_lembap_hujan_alpha   double precision NOT NULL DEFAULT 0.5,
    acuan_curah_hujan_mm       double precision NOT NULL DEFAULT 3000.0,
    acuan_jarak_pesisir_km     double precision NOT NULL DEFAULT 50.0,
    pita_termal_batas_bawah    double precision NOT NULL DEFAULT 0.10,
    pita_termal_batas_atas_sejuk double precision NOT NULL DEFAULT 0.60,
    pita_termal_batas_atas_panas double precision NOT NULL DEFAULT 0.30,
    suhu_sejuk_c               double precision NOT NULL DEFAULT 26.0,
    suhu_panas_c               double precision NOT NULL DEFAULT 32.0,
    faktor_penuaan_a           double precision NOT NULL DEFAULT 0.5,
    ambang_tidak_disarankan    double precision NOT NULL DEFAULT 55.0,
    ambang_cocok               double precision NOT NULL DEFAULT 75.0,
    aktif                      boolean NOT NULL DEFAULT false,
    dibuat_pada_utc            timestamp with time zone NOT NULL,
    CONSTRAINT pk_konfigurasi_model PRIMARY KEY (versi_model)
);

CREATE TABLE IF NOT EXISTS laporan (
    id              uuid NOT NULL,
    bangunan_id     uuid NOT NULL,
    path_berkas     text NOT NULL,
    dibuat_pada_utc timestamp with time zone NOT NULL,
    hash_snapshot   text,
    status_berkas   text NOT NULL DEFAULT 'Tersimpan',
    checksum_sha256 text,
    ukuran_byte     bigint,
    opsi_json       text,
    CONSTRAINT pk_laporan PRIMARY KEY (id),
    CONSTRAINT fk_laporan_bangunan FOREIGN KEY (bangunan_id)
        REFERENCES bangunan (id) ON DELETE CASCADE
);

-- Indeks
CREATE INDEX IF NOT EXISTS bahan_jenis_idx ON bahan (jenis);
CREATE INDEX IF NOT EXISTS komponen_bangunan_id_idx ON komponen (bangunan_id);
CREATE INDEX IF NOT EXISTS evaluasi_komponen_id_idx ON evaluasi (komponen_id);
CREATE INDEX IF NOT EXISTS evaluasi_bahan_id_idx ON evaluasi (bahan_id);
CREATE INDEX IF NOT EXISTS evaluasi_batch_id_idx ON evaluasi (batch_id);
CREATE INDEX IF NOT EXISTS batch_evaluasi_proyek_id_idx ON batch_evaluasi (proyek_id);
CREATE INDEX IF NOT EXISTS sumber_properti_bahan_bahan_id_idx ON sumber_properti_bahan (bahan_id);
CREATE INDEX IF NOT EXISTS pekerjaan_bangunan_id_idx ON pekerjaan (bangunan_id);
CREATE INDEX IF NOT EXISTS keputusan_jadwal_pekerjaan_id_idx ON keputusan_jadwal (pekerjaan_id);
CREATE INDEX IF NOT EXISTS prakiraan_cache_bangunan_revisi_tanggal_idx ON prakiraan_cache (bangunan_id, revisi_lokasi, tanggal_cuaca);
CREATE INDEX IF NOT EXISTS prakiraan_cache_lat_lon_revisi_tanggal_idx ON prakiraan_cache (latitude, longitude, revisi_lokasi, tanggal_cuaca);
CREATE INDEX IF NOT EXISTS ambang_pekerjaan_pekerjaan_id_idx ON ambang_pekerjaan (pekerjaan_id);
CREATE INDEX IF NOT EXISTS ambang_pekerjaan_jenis_pekerjaan_idx ON ambang_pekerjaan (jenis_pekerjaan);
CREATE INDEX IF NOT EXISTS laporan_bangunan_id_idx ON laporan (bangunan_id);
