using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScholarshipFinalNotesToProgramSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FinalDecisionNotes",
                table: "ProgramSelections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScholarshipRate",
                table: "ProgramSelections",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalDecisionNotes",
                table: "ProgramSelections");

            migrationBuilder.DropColumn(
                name: "ScholarshipRate",
                table: "ProgramSelections");
        }
    }
}
