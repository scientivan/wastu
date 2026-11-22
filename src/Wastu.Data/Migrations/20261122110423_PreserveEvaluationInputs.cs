using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wastu.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreserveEvaluationInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uk_simulasi_emisi_evaluasi_horizon_versi",
                table: "simulasi_emisi");

            migrationBuilder.AddColumn<string>(
                name: "input_snapshot_json",
                table: "evaluasi",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uk_simulasi_emisi_evaluasi_horizon_versi",
                table: "simulasi_emisi",
                columns: new[] { "evaluasi_id", "horizon_tahun", "versi_model", "faktor_penuaan_a" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uk_simulasi_emisi_evaluasi_horizon_versi",
                table: "simulasi_emisi");

            migrationBuilder.DropColumn(
                name: "input_snapshot_json",
                table: "evaluasi");

            migrationBuilder.CreateIndex(
                name: "uk_simulasi_emisi_evaluasi_horizon_versi",
                table: "simulasi_emisi",
                columns: new[] { "evaluasi_id", "horizon_tahun", "versi_model" },
                unique: true);
        }
    }
}
