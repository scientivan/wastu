using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wastu.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreservePartialForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "prakiraan_cache_curah_hujan_nonnegatif",
                table: "prakiraan_cache");

            migrationBuilder.DropCheckConstraint(
                name: "prakiraan_cache_kelembapan_rentang",
                table: "prakiraan_cache");

            migrationBuilder.DropCheckConstraint(
                name: "prakiraan_cache_probabilitas_rentang",
                table: "prakiraan_cache");

            migrationBuilder.AddCheckConstraint(
                name: "prakiraan_cache_curah_hujan_nonnegatif",
                table: "prakiraan_cache",
                sql: "(curah_hujan_mm >= 0 AND curah_hujan_mm < 'Infinity'::double precision) OR (NOT data_lengkap AND curah_hujan_mm = 'NaN'::double precision)");

            migrationBuilder.AddCheckConstraint(
                name: "prakiraan_cache_kelembapan_rentang",
                table: "prakiraan_cache",
                sql: "kelembapan_maks_pct BETWEEN 0 AND 100 OR (NOT data_lengkap AND kelembapan_maks_pct = 'NaN'::double precision)");

            migrationBuilder.AddCheckConstraint(
                name: "prakiraan_cache_probabilitas_rentang",
                table: "prakiraan_cache",
                sql: "probabilitas_hujan_pct BETWEEN 0 AND 100 OR (NOT data_lengkap AND probabilitas_hujan_pct = 'NaN'::double precision)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "prakiraan_cache_curah_hujan_nonnegatif",
                table: "prakiraan_cache");

            migrationBuilder.DropCheckConstraint(
                name: "prakiraan_cache_kelembapan_rentang",
                table: "prakiraan_cache");

            migrationBuilder.DropCheckConstraint(
                name: "prakiraan_cache_probabilitas_rentang",
                table: "prakiraan_cache");

            migrationBuilder.AddCheckConstraint(
                name: "prakiraan_cache_curah_hujan_nonnegatif",
                table: "prakiraan_cache",
                sql: "curah_hujan_mm >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "prakiraan_cache_kelembapan_rentang",
                table: "prakiraan_cache",
                sql: "kelembapan_maks_pct BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "prakiraan_cache_probabilitas_rentang",
                table: "prakiraan_cache",
                sql: "probabilitas_hujan_pct BETWEEN 0 AND 100");
        }
    }
}
