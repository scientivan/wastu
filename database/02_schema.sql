-- Skema basis data Wastu (PRD §9, docs/erd-wastu.md).
--
-- Berkas ini memuat enam tabel yang benar-benar dipakai lapisan Wastu.Data
-- (bahan, bangunan, komponen, profil_iklim, evaluasi, simulasi_emisi), setara
-- dengan migrasi EF Core `InitialCreate`. Empat entitas lain pada ERD
-- (pengguna, pekerjaan, prakiraan_cuaca, laporan) belum punya adapter di kode,
-- jadi belum masuk berkas ini.
--
-- Skema dikelola lewat migrasi EF Core (PRD §9); berkas ini adalah acuan/report
-- agar skema terbaca tanpa membangun proyek.
--
-- Penyimpangan terhadap docs/erd-wastu.md yang disengaja agar cocok dengan domain:
--   * evaluasi.id memakai uuid, bukan bigserial (domain Evaluasi.Id bertipe Guid).
--   * simulasi_emisi memakai evaluasi_id uuid sebagai primary key, bukan id bigserial.
--   * evaluasi menyimpan empat kolom sub-skor (skor_termal, skor_lembap,
--     skor_korosi, skor_usia) untuk merekonstruksi Evaluasi.SubSkor.

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
    CONSTRAINT pk_evaluasi PRIMARY KEY (id),
    CONSTRAINT evaluasi_skor_rentang CHECK (skor_kesesuaian BETWEEN 0 AND 100),
    CONSTRAINT evaluasi_emisi_awal_nonnegatif CHECK (emisi_awal_kg >= 0),
    CONSTRAINT evaluasi_alasan_tidak_kosong CHECK (length(btrim(alasan)) > 0),
    CONSTRAINT fk_evaluasi_komponen FOREIGN KEY (komponen_id)
        REFERENCES komponen (id) ON DELETE CASCADE,
    CONSTRAINT fk_evaluasi_bahan FOREIGN KEY (bahan_id)
        REFERENCES bahan (id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS simulasi_emisi (
    evaluasi_id                uuid NOT NULL,
    horizon_tahun              integer NOT NULL,
    usia_pakai_efektif_tahun   double precision NOT NULL,
    jumlah_penggantian         integer NOT NULL,
    emisi_kumulatif_kg         double precision NOT NULL,
    CONSTRAINT pk_simulasi_emisi PRIMARY KEY (evaluasi_id),
    CONSTRAINT simulasi_emisi_horizon_positif CHECK (horizon_tahun > 0),
    CONSTRAINT simulasi_emisi_usia_efektif_positif CHECK (usia_pakai_efektif_tahun > 0),
    CONSTRAINT simulasi_emisi_jumlah_penggantian_nonnegatif CHECK (jumlah_penggantian >= 0),
    CONSTRAINT simulasi_emisi_emisi_kumulatif_nonnegatif CHECK (emisi_kumulatif_kg >= 0),
    CONSTRAINT fk_simulasi_emisi_evaluasi FOREIGN KEY (evaluasi_id)
        REFERENCES evaluasi (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS bahan_jenis_idx ON bahan (jenis);
CREATE INDEX IF NOT EXISTS komponen_bangunan_id_idx ON komponen (bangunan_id);
CREATE INDEX IF NOT EXISTS evaluasi_komponen_id_idx ON evaluasi (komponen_id);
CREATE INDEX IF NOT EXISTS evaluasi_bahan_id_idx ON evaluasi (bahan_id);
