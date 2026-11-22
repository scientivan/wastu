-- Benih data awal Wastu untuk PostgreSQL / Supabase (PRD §4.1).
--
-- Idempotent: aman dijalankan berulang kali berkat ON CONFLICT DO NOTHING.

-- ---------------------------------------------------------------------------
-- Katalog bahan: 15 dinding (PRD §4.1)
-- ---------------------------------------------------------------------------
INSERT INTO bahan (id, nama, jenis, konduktivitas_termal, ketahanan_lembap, ketahanan_korosi, usia_pakai_nominal_tahun, faktor_emisi, sumber_data, versi_data) VALUES
    ('d0000000-0000-4000-8000-000000000001', 'Beton bertulang (in-situ)',            'dinding', 1.40, 4, 5, 50, 120, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000002', 'Beton ringan (hebel autoclave)',       'dinding', 0.12, 5, 3, 30,  60, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000003', 'Beton polibeton (hebel)',              'dinding', 0.10, 4, 3, 25,  55, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000004', 'Bata merah + plester aci',             'dinding', 0.70, 3, 4, 30,  65, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000005', 'Bata merah tanpa plester',             'dinding', 0.70, 2, 4, 20,  35, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000006', 'Bata beton (concrete block)',          'dinding', 0.60, 4, 4, 40,  55, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000007', 'Bata silikat berpori',                 'dinding', 0.11, 5, 3, 30,  62, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000008', 'Bata ringan honeycomb (AHCC)',         'dinding', 0.09, 4, 3, 30,  50, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000009', 'Panel gipsum (dry wall)',              'dinding', 0.25, 1, 1, 20,  28, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000010', 'Papan kayu + pelindung',               'dinding', 0.15, 1, 1, 15,  32, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000011', 'Panel ACP (aluminium composite)',      'dinding', 0.20, 5, 5, 25, 190, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000012', 'Panel EPS + plester',                  'dinding', 0.04, 4, 4, 25,  70, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000013', 'Semen fiber reinforced board',         'dinding', 0.25, 5, 5, 30,  75, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000014', 'Batu andesit (dinding batu alam)',     'dinding', 3.00, 5, 5, 60,  45, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000015', 'Beton geopolimer',                     'dinding', 0.45, 4, 4, 45,  90, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Katalog bahan: 10 atap (PRD §4.1)
-- ---------------------------------------------------------------------------
INSERT INTO bahan (id, nama, jenis, konduktivitas_termal, ketahanan_lembap, ketahanan_korosi, usia_pakai_nominal_tahun, faktor_emisi, sumber_data, versi_data) VALUES
    ('d0000000-0000-4000-8000-000000000016', 'Dak beton bertulang',              'atap', 1.40, 5, 5, 40, 150, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000017', 'Genting beton',                    'atap', 0.40, 5, 5, 30,  95, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000018', 'Genting keramik',                  'atap', 0.30, 5, 5, 35, 110, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000019', 'Genting tanah liat',               'atap', 0.60, 4, 4, 30,  85, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000020', 'Genteng aspal (shingle)',          'atap', 0.08, 3, 2, 20,  80, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000021', 'Baja galvanis (trapezium)',        'atap', 0.05, 3, 2, 15,  90, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000022', 'Baja AZ / galvalum',               'atap', 0.05, 4, 4, 20, 105, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000023', 'Seng (zink)',                      'atap', 0.11, 2, 1, 12, 130, 'ICE Database v3 (Bath) — perkiraan literatur, belum dikalibrasi untuk pasar Indonesia (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000024', 'Constantra / fiber cement',        'atap', 0.07, 4, 3, 25, 100, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1),
    ('d0000000-0000-4000-8000-000000000025', 'Panel sandwich (inti EPS)',        'atap', 0.04, 4, 4, 25,  95, 'EPD generik pabrikan regional — perkiraan literatur (PRD §11.4)', 1)
ON CONFLICT (id) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Konfigurasi model default: v2.0 aktif
-- ---------------------------------------------------------------------------
INSERT INTO konfigurasi_model (versi_model, bobot_lembap_hujan_alpha, acuan_curah_hujan_mm, acuan_jarak_pesisir_km, pita_termal_batas_bawah, pita_termal_batas_atas_sejuk, pita_termal_batas_atas_panas, suhu_sejuk_c, suhu_panas_c, faktor_penuaan_a, ambang_tidak_disarankan, ambang_cocok, aktif, dibuat_pada_utc) VALUES
    ('v2.0', 0.5, 3000.0, 50.0, 0.10, 0.60, 0.30, 26.0, 32.0, 0.5, 55.0, 75.0, true, now())
ON CONFLICT (versi_model) DO NOTHING;

-- ---------------------------------------------------------------------------
-- Contoh proyek: rumah tapak pesisir Bandar Lampung
-- ---------------------------------------------------------------------------
INSERT INTO bangunan (id, nama, kota, latitude, longitude, dibuat_pada, revisi_lokasi, zona_waktu) VALUES
    ('b0000000-0000-4000-8000-000000000001', 'Rumah Tapak Pesisir', 'Bandar Lampung', -5.45, 105.26, now(), 1, 'Asia/Jakarta')
ON CONFLICT (id) DO NOTHING;

INSERT INTO komponen (id, bangunan_id, jenis, luas_m2) VALUES
    ('c0000000-0000-4000-8000-000000000001', 'b0000000-0000-4000-8000-000000000001', 'dinding', 120),
    ('c0000000-0000-4000-8000-000000000002', 'b0000000-0000-4000-8000-000000000001', 'atap', 90)
ON CONFLICT (id) DO NOTHING;

-- Profil iklim demo ditandai dengan provenance = 'Contoh' (PRD §6.3, T37)
INSERT INTO profil_iklim (bangunan_id, curah_hujan_tahunan_mm, kelembapan_rata_pct, suhu_rata_c, jarak_pesisir_km, tingkat_korosi, diambil_pada, revisi_lokasi, provenance, penyedia, status_pengambilan) VALUES
    ('b0000000-0000-4000-8000-000000000001', 2400, 82, 27.5, 3.0, 'tinggi', now(), 1, 'Contoh', 'Contoh Sintetis', 'Tersedia')
ON CONFLICT (bangunan_id) DO NOTHING;

-- Dua contoh penilaian. Usia pakai efektif = usia nominal × (0,5 + 0,5 × skor/100)
-- (PRD §11.3): beton bertulang 50 × 0,86 = 43 tahun; baja galvanis 15 × 0,625 = 9,375 tahun.
INSERT INTO evaluasi (id, komponen_id, bahan_id, skor_kesesuaian, emisi_awal_kg, alasan, skor_termal, skor_lembap, skor_korosi, skor_usia, dihitung_pada, versi_model, dihitung_pada_utc) VALUES
    ('e0000000-0000-4000-8000-000000000001', 'c0000000-0000-4000-8000-000000000001', 'd0000000-0000-4000-8000-000000000001',
     72, 14400, 'Konduktivitas termal 1,4 W/mK termasuk tinggi untuk suhu rata-rata 27,5 C di lokasi Anda.', 100, 50, 88, 50, now(), 'v2.0', now()),
    ('e0000000-0000-4000-8000-000000000002', 'c0000000-0000-4000-8000-000000000002', 'd0000000-0000-4000-8000-000000000021',
     25, 8100, 'Ketahanan korosi 2 rendah untuk lokasi yang berjarak 3 km dari garis pantai sehingga terkena udara bergaram.', 60, 40, 0, 0, now(), 'v2.0', now())
ON CONFLICT (id) DO NOTHING;

-- Emisi kumulatif = emisi_awal × (1 + jumlah_penggantian) (PRD §11.3).
-- Multi-horizon PK: (evaluasi_id, horizon_tahun, versi_model)
INSERT INTO simulasi_emisi (id, evaluasi_id, horizon_tahun, usia_pakai_efektif_tahun, jumlah_penggantian, emisi_kumulatif_kg, versi_model, faktor_penuaan_a, dihitung_pada_utc) VALUES
    ('s0000000-0000-4000-8000-000000000001', 'e0000000-0000-4000-8000-000000000001', 30, 43.0,   0, 14400, 'v2.0', 0.5, now()),
    ('s0000000-0000-4000-8000-000000000002', 'e0000000-0000-4000-8000-000000000002', 30,  9.375, 3, 32400, 'v2.0', 0.5, now())
ON CONFLICT (evaluasi_id, horizon_tahun, versi_model) DO NOTHING;
