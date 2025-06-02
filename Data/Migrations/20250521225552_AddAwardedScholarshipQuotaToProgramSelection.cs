using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAwardedScholarshipQuotaToProgramSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwardedScholarshipQuotaId",
                table: "ProgramSelections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSelections_AwardedScholarshipQuotaId",
                table: "ProgramSelections",
                column: "AwardedScholarshipQuotaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramSelections_ScholarshipQuotas_AwardedScholarshipQuotaId",
                table: "ProgramSelections",
                column: "AwardedScholarshipQuotaId",
                principalTable: "ScholarshipQuotas",
                principalColumn: "ScholarshipQuotaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramSelections_ScholarshipQuotas_AwardedScholarshipQuotaId",
                table: "ProgramSelections");

            migrationBuilder.DropIndex(
                name: "IX_ProgramSelections_AwardedScholarshipQuotaId",
                table: "ProgramSelections");

            migrationBuilder.DropColumn(
                name: "AwardedScholarshipQuotaId",
                table: "ProgramSelections");
        }
    }
}
