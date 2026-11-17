using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediVerify.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Workflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DispensedAt",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedBy",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DrugInteractions",
                columns: table => new
                {
                    DrugInteractionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicineAId = table.Column<int>(type: "int", nullable: false),
                    MedicineBId = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugInteractions", x => x.DrugInteractionId);
                    table.ForeignKey(
                        name: "FK_DrugInteractions_Medicines_MedicineAId",
                        column: x => x.MedicineAId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DrugInteractions_Medicines_MedicineBId",
                        column: x => x.MedicineBId,
                        principalTable: "Medicines",
                        principalColumn: "MedicineId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugInteractions_MedicineAId",
                table: "DrugInteractions",
                column: "MedicineAId");

            migrationBuilder.CreateIndex(
                name: "IX_DrugInteractions_MedicineBId",
                table: "DrugInteractions",
                column: "MedicineBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugInteractions");

            migrationBuilder.DropColumn(
                name: "DispensedAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "Prescriptions");
        }
    }
}
