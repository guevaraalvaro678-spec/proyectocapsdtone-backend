using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class VoluntariosPracticantesRRHH : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ciclo",
                table: "Voluntarios");

            migrationBuilder.DropColumn(
                name: "Universidad",
                table: "Voluntarios");

            migrationBuilder.RenameColumn(
                name: "Carrera",
                table: "Voluntarios",
                newName: "Apellidos");

            migrationBuilder.CreateTable(
                name: "BloquesHorario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    HoraInicio = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    HoraFin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloquesHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloquesHorario_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marcaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Entrada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Salida = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marcaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Practicantes",
                columns: table => new
                {
                    Dni = table.Column<string>(type: "nchar(8)", fixedLength: true, maxLength: 8, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Universidad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Carrera = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ciclo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DepartamentoPrincipal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartamentoSecundario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstadoHorario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NotaHorario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaEnvioHorario = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRevisionHorario = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevisadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Practicantes", x => x.Dni);
                    table.ForeignKey(
                        name: "FK_Practicantes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaLimite = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Urgente = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreadaPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tareas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tareas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloquesHorario_UsuarioId_Tipo",
                table: "BloquesHorario",
                columns: new[] { "UsuarioId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Marcaciones_UsuarioId_Entrada",
                table: "Marcaciones",
                columns: new[] { "UsuarioId", "Entrada" });

            migrationBuilder.CreateIndex(
                name: "IX_Practicantes_EstadoHorario",
                table: "Practicantes",
                column: "EstadoHorario");

            migrationBuilder.CreateIndex(
                name: "IX_Practicantes_UsuarioId",
                table: "Practicantes",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_UsuarioId_Estado",
                table: "Tareas",
                columns: new[] { "UsuarioId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloquesHorario");

            migrationBuilder.DropTable(
                name: "Marcaciones");

            migrationBuilder.DropTable(
                name: "Practicantes");

            migrationBuilder.DropTable(
                name: "Tareas");

            migrationBuilder.RenameColumn(
                name: "Apellidos",
                table: "Voluntarios",
                newName: "Carrera");

            migrationBuilder.AddColumn<string>(
                name: "Ciclo",
                table: "Voluntarios",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Universidad",
                table: "Voluntarios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }
    }
}
