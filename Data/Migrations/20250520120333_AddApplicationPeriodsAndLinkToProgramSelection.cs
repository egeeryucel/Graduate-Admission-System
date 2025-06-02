using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationPeriodsAndLinkToProgramSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicationPeriodId",
                table: "ProgramSelections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationPeriods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramSelections_ApplicationPeriodId",
                table: "ProgramSelections",
                column: "ApplicationPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramSelections_ApplicationPeriods_ApplicationPeriodId",
                table: "ProgramSelections",
                column: "ApplicationPeriodId",
                principalTable: "ApplicationPeriods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgramSelections_ApplicationPeriods_ApplicationPeriodId",
                table: "ProgramSelections");

            migrationBuilder.DropTable(
                name: "ApplicationPeriods");

            migrationBuilder.DropIndex(
                name: "IX_ProgramSelections_ApplicationPeriodId",
                table: "ProgramSelections");

            migrationBuilder.DropColumn(
                name: "ApplicationPeriodId",
                table: "ProgramSelections");
        }
    }
}
