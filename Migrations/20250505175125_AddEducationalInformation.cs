using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationalInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EducationalInformations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsBlueCardOwner = table.Column<bool>(type: "bit", nullable: false),
                    HasDualCitizenship = table.Column<bool>(type: "bit", nullable: false),
                    HasPreviousUniversityStudy = table.Column<bool>(type: "bit", nullable: false),
                    SchoolName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GraduationYear = table.Column<int>(type: "int", nullable: false),
                    GPA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LanguageProficiency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LanguageExamScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProgramSelectionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationalInformations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationalInformations_ProgramSelections_ProgramSelectionId",
                        column: x => x.ProgramSelectionId,
                        principalTable: "ProgramSelections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationalInformations_ProgramSelectionId",
                table: "EducationalInformations",
                column: "ProgramSelectionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationalInformations");
        }
    }
}
