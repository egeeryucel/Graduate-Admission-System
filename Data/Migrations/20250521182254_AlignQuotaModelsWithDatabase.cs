using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationAdmissionSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignQuotaModelsWithDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgramQuotas",
                columns: table => new
                {
                    ProgramQuotaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramId = table.Column<int>(type: "int", nullable: false),
                    ApplicationPeriodId = table.Column<int>(type: "int", nullable: false),
                    TotalQuota = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramQuotas", x => x.ProgramQuotaId);
                    table.ForeignKey(
                        name: "FK_ProgramQuotas_ApplicationPeriods_ApplicationPeriodId",
                        column: x => x.ApplicationPeriodId,
                        principalTable: "ApplicationPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgramQuotas_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "ProgramId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScholarshipQuotas",
                columns: table => new
                {
                    ScholarshipQuotaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramQuotaId = table.Column<int>(type: "int", nullable: false),
                    ScholarshipPercentage = table.Column<int>(type: "int", nullable: false),
                    AllocatedQuota = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScholarshipQuotas", x => x.ScholarshipQuotaId);
                    table.ForeignKey(
                        name: "FK_ScholarshipQuotas_ProgramQuotas_ProgramQuotaId",
                        column: x => x.ProgramQuotaId,
                        principalTable: "ProgramQuotas",
                        principalColumn: "ProgramQuotaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramQuotas_ApplicationPeriodId",
                table: "ProgramQuotas",
                column: "ApplicationPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramQuotas_ProgramId",
                table: "ProgramQuotas",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipQuotas_ProgramQuotaId",
                table: "ScholarshipQuotas",
                column: "ProgramQuotaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScholarshipQuotas");

            migrationBuilder.DropTable(
                name: "ProgramQuotas");
        }
    }
}
