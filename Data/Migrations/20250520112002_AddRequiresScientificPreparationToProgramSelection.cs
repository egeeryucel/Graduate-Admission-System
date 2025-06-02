using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiresScientificPreparationToProgramSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresScientificPreparation",
                table: "ProgramSelections",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresScientificPreparation",
                table: "ProgramSelections");
        }
    }
}
