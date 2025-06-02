using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedChairToProgramSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDepartmentChairId",
                table: "ProgramSelections",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSelections_AssignedDepartmentChairId",
                table: "ProgramSelections",
                column: "AssignedDepartmentChairId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramSelections_AspNetUsers_AssignedDepartmentChairId",
                table: "ProgramSelections",
                column: "AssignedDepartmentChairId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramSelections_AspNetUsers_AssignedDepartmentChairId",
                table: "ProgramSelections");

            migrationBuilder.DropIndex(
                name: "IX_ProgramSelections_AssignedDepartmentChairId",
                table: "ProgramSelections");

            migrationBuilder.DropColumn(
                name: "AssignedDepartmentChairId",
                table: "ProgramSelections");
        }
    }
}
