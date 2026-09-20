using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class DisponibilidadYReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Citas_PsicologoId_FechaInicio",
                table: "Citas");

            migrationBuilder.CreateTable(
                name: "Disponibilidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsicologoId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    Modalidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Lugar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disponibilidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disponibilidades_Usuarios_PsicologoId",
                        column: x => x.PsicologoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PsicologoId_FechaInicio",
                table: "Citas",
                columns: new[] { "PsicologoId", "FechaInicio" },
                unique: true,
                filter: "[Estado] IN ('Programada', 'Confirmada')");

            migrationBuilder.CreateIndex(
                name: "IX_Disponibilidades_PsicologoId_DiaSemana",
                table: "Disponibilidades",
                columns: new[] { "PsicologoId", "DiaSemana" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Disponibilidades");

            migrationBuilder.DropIndex(
                name: "IX_Citas_PsicologoId_FechaInicio",
                table: "Citas");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PsicologoId_FechaInicio",
                table: "Citas",
                columns: new[] { "PsicologoId", "FechaInicio" });
        }
    }
}
