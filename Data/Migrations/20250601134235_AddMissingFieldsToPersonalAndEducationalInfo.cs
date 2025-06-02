using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingFieldsToPersonalAndEducationalInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CityOfResidence",
                table: "PersonalInformations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProgramSelectionId1",
                table: "PersonalInformations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Degree",
                table: "EducationalInformations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "EducationalInformations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProgramSelectionId1",
                table: "EducationalInformations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalInformations_ProgramSelectionId1",
                table: "PersonalInformations",
                column: "ProgramSelectionId1",
                unique: true,
                filter: "[ProgramSelectionId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EducationalInformations_ProgramSelectionId1",
                table: "EducationalInformations",
                column: "ProgramSelectionId1",
                unique: true,
                filter: "[ProgramSelectionId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_EducationalInformations_ProgramSelections_ProgramSelectionId1",
                table: "EducationalInformations",
                column: "ProgramSelectionId1",
                principalTable: "ProgramSelections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalInformations_ProgramSelections_ProgramSelectionId1",
                table: "PersonalInformations",
                column: "ProgramSelectionId1",
                principalTable: "ProgramSelections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EducationalInformations_ProgramSelections_ProgramSelectionId1",
                table: "EducationalInformations");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalInformations_ProgramSelections_ProgramSelectionId1",
                table: "PersonalInformations");

            migrationBuilder.DropIndex(
                name: "IX_PersonalInformations_ProgramSelectionId1",
                table: "PersonalInformations");

            migrationBuilder.DropIndex(
                name: "IX_EducationalInformations_ProgramSelectionId1",
                table: "EducationalInformations");

            migrationBuilder.DropColumn(
                name: "CityOfResidence",
                table: "PersonalInformations");

            migrationBuilder.DropColumn(
                name: "ProgramSelectionId1",
                table: "PersonalInformations");

            migrationBuilder.DropColumn(
                name: "Degree",
                table: "EducationalInformations");

            migrationBuilder.DropColumn(
                name: "Major",
                table: "EducationalInformations");

            migrationBuilder.DropColumn(
                name: "ProgramSelectionId1",
                table: "EducationalInformations");
        }
    }
}
