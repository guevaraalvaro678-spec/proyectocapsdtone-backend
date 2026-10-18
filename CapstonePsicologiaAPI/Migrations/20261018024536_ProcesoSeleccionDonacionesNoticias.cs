using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class ProcesoSeleccionDonacionesNoticias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HorarioConfirmado",
                table: "Practicantes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CreadaPorUsuarioId",
                table: "Iniciativas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Iniciativas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Aprobada");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Iniciativas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MotivoRechazo",
                table: "Iniciativas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Eventos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Evento");

            migrationBuilder.AddColumn<string>(
                name: "Lugar",
                table: "BloquesHorario",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Modalidad",
                table: "BloquesHorario",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Archivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Datos = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SubidoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Archivos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DonanteNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Noticias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Resumen = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ImagenId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Noticias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Noticias_Imagenes_ImagenId",
                        column: x => x.ImagenId,
                        principalTable: "Imagenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SesionesEntrevista",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modalidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LugarOEnlace = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Cupo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesEntrevista", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonacionArticulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonacionId = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonacionArticulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonacionArticulos_Donaciones_DonacionId",
                        column: x => x.DonacionId,
                        principalTable: "Donaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Postulaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Dni = table.Column<string>(type: "nchar(8)", fixedLength: true, maxLength: 8, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Distrito = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Universidad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Carrera = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Ciclo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Departamento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartamentoSecundario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CvArchivoId = table.Column<int>(type: "int", nullable: true),
                    ParticipaIniciativa = table.Column<bool>(type: "bit", nullable: false),
                    IniciativaDescripcion = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SesionEntrevistaId = table.Column<int>(type: "int", nullable: true),
                    NotaEvaluacion = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postulaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Postulaciones_Archivos_CvArchivoId",
                        column: x => x.CvArchivoId,
                        principalTable: "Archivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Postulaciones_SesionesEntrevista_SesionEntrevistaId",
                        column: x => x.SesionEntrevistaId,
                        principalTable: "SesionesEntrevista",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Postulaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Iniciativas_CreadaPorUsuarioId",
                table: "Iniciativas",
                column: "CreadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DonacionArticulos_DonacionId",
                table: "DonacionArticulos",
                column: "DonacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Donaciones_Codigo",
                table: "Donaciones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donaciones_Estado",
                table: "Donaciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Noticias_Estado_Fecha",
                table: "Noticias",
                columns: new[] { "Estado", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Noticias_ImagenId",
                table: "Noticias",
                column: "ImagenId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_Codigo",
                table: "Postulaciones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_CvArchivoId",
                table: "Postulaciones",
                column: "CvArchivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_Estado",
                table: "Postulaciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_SesionEntrevistaId",
                table: "Postulaciones",
                column: "SesionEntrevistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Postulaciones_UsuarioId",
                table: "Postulaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesEntrevista_Codigo",
                table: "SesionesEntrevista",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionesEntrevista_Inicio",
                table: "SesionesEntrevista",
                column: "Inicio");

            migrationBuilder.AddForeignKey(
                name: "FK_Iniciativas_Usuarios_CreadaPorUsuarioId",
                table: "Iniciativas",
                column: "CreadaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Iniciativas_Usuarios_CreadaPorUsuarioId",
                table: "Iniciativas");

            migrationBuilder.DropTable(
                name: "DonacionArticulos");

            migrationBuilder.DropTable(
                name: "Noticias");

            migrationBuilder.DropTable(
                name: "Postulaciones");

            migrationBuilder.DropTable(
                name: "Donaciones");

            migrationBuilder.DropTable(
                name: "Archivos");

            migrationBuilder.DropTable(
                name: "SesionesEntrevista");

            migrationBuilder.DropIndex(
                name: "IX_Iniciativas_CreadaPorUsuarioId",
                table: "Iniciativas");

            migrationBuilder.DropColumn(
                name: "HorarioConfirmado",
                table: "Practicantes");

            migrationBuilder.DropColumn(
                name: "CreadaPorUsuarioId",
                table: "Iniciativas");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Iniciativas");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Iniciativas");

            migrationBuilder.DropColumn(
                name: "MotivoRechazo",
                table: "Iniciativas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "Lugar",
                table: "BloquesHorario");

            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "BloquesHorario");
        }
    }
}
