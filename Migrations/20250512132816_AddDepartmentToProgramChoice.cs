using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToProgramChoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ProgramChoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramChoices_DepartmentId",
                table: "ProgramChoices",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramChoices_Departments_DepartmentId",
                table: "ProgramChoices",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramChoices_Departments_DepartmentId",
                table: "ProgramChoices");

            migrationBuilder.DropIndex(
                name: "IX_ProgramChoices_DepartmentId",
                table: "ProgramChoices");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ProgramChoices");
        }
    }
}
