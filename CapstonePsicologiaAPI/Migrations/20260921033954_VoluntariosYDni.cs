using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class VoluntariosYDni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "Psicologos",
                type: "nchar(8)",
                fixedLength: true,
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Voluntarios",
                columns: table => new
                {
                    Dni = table.Column<string>(type: "nchar(8)", fixedLength: true, maxLength: 8, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Universidad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Carrera = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ciclo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voluntarios", x => x.Dni);
                    table.ForeignKey(
                        name: "FK_Voluntarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Psicologos_Dni",
                table: "Psicologos",
                column: "Dni",
                unique: true,
                filter: "[Dni] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Voluntarios_UsuarioId",
                table: "Voluntarios",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Voluntarios");

            migrationBuilder.DropIndex(
                name: "IX_Psicologos_Dni",
                table: "Psicologos");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "Psicologos");
        }
    }
}
