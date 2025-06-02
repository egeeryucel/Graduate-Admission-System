using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmittedByAgencyIdToProgramSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmittedByAgencyUserId",
                table: "ProgramSelections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PersonalInformations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedByAgencyUserId",
                table: "ProgramSelections");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "PersonalInformations");
        }
    }
}
