using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddChairReviewFieldsToProgramSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentChairNotes",
                table: "ProgramSelections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepartmentDecisionDate",
                table: "ProgramSelections",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ProgramSelections",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentChairNotes",
                table: "ProgramSelections");

            migrationBuilder.DropColumn(
                name: "DepartmentDecisionDate",
                table: "ProgramSelections");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ProgramSelections");
        }
    }
}
