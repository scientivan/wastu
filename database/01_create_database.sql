-- Membuat basis data Wastu untuk PostgreSQL lokal (PRD §9, docs/erd-wastu.md).
--
-- Pada Supabase, basis data "postgres" sudah tersedia sehingga berkas ini TIDAK
-- diperlukan; cukup jalankan 02_schema.sql dan 03_seed_supabase.sql pada proyek
-- Supabase. Berkas ini hanya untuk menjalankan Wastu di instans PostgreSQL lokal.
--
-- Jalankan:
--   psql -U postgres -f database/01_create_database.sql
--   psql -U postgres -d wastu -f database/02_schema.sql
--   psql -U postgres -d wastu -f database/03_seed_supabase.sql

CREATE DATABASE wastu;
