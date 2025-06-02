using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHasDualCitizenshipToPersonalInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasDualCitizenship",
                table: "EducationalInformations");

            migrationBuilder.AddColumn<bool>(
                name: "HasDualCitizenship",
                table: "PersonalInformations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasDualCitizenship",
                table: "PersonalInformations");

            migrationBuilder.AddColumn<bool>(
                name: "HasDualCitizenship",
                table: "EducationalInformations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
