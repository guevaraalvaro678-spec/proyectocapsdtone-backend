using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaDonacionesAnunciosAlertas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Donaciones",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "DestinoEntrega",
                table: "Donaciones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEntrega",
                table: "Donaciones",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlertasSeguridad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Revisada = table.Column<bool>(type: "bit", nullable: false),
                    RevisadaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasSeguridad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasSeguridad_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Anuncios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Texto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Enlace = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Desde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anuncios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistorialDonaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonacionId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialDonaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialDonaciones_Donaciones_DonacionId",
                        column: x => x.DonacionId,
                        principalTable: "Donaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialDonaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasSeguridad_Revisada_Fecha",
                table: "AlertasSeguridad",
                columns: new[] { "Revisada", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasSeguridad_UsuarioId",
                table: "AlertasSeguridad",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Anuncios_Activo_Desde",
                table: "Anuncios",
                columns: new[] { "Activo", "Desde" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialDonaciones_DonacionId",
                table: "HistorialDonaciones",
                column: "DonacionId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialDonaciones_UsuarioId",
                table: "HistorialDonaciones",
                column: "UsuarioId");

            // Indicadores del proceso de selección (sección 3.7 del informe), calculados en la base.
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_IndicadoresProceso
AS
BEGIN
    SET NOCOUNT ON;

    WITH Decision AS (
        -- Primera vez que cada postulación llegó a un resultado final.
        SELECT PostulacionId, MIN(Fecha) AS Fecha
        FROM HistorialPostulaciones
        WHERE Estado IN ('Aceptada', 'NoAdmitida')
        GROUP BY PostulacionId
    ),
    PrimeraRespuesta AS (
        -- Primer movimiento del equipo después de recibirla.
        SELECT PostulacionId, MIN(Fecha) AS Fecha
        FROM HistorialPostulaciones
        WHERE Estado <> 'Recibida'
        GROUP BY PostulacionId
    ),
    Base AS (
        SELECT p.Id, p.Tipo, p.Estado, p.FechaEnvio, p.UsuarioId,
               d.Fecha AS FechaDecision, r.Fecha AS FechaRespuesta, a.Nombre AS NombreCv,
               CASE WHEN p.Nombres <> '' AND p.Apellidos <> '' AND p.Telefono <> ''
                         AND (p.Tipo = 'Voluntario'
                              OR (ISNULL(p.Dni, '') <> '' AND ISNULL(p.Universidad, '') <> '' AND ISNULL(p.Carrera, '') <> ''
                                  AND ISNULL(p.Ciclo, '') <> '' AND ISNULL(p.Departamento, '') <> '' AND p.CvArchivoId IS NOT NULL))
                    THEN 1 ELSE 0 END AS Completa
        FROM Postulaciones p
        LEFT JOIN Decision d ON d.PostulacionId = p.Id
        LEFT JOIN PrimeraRespuesta r ON r.PostulacionId = p.Id
        LEFT JOIN Archivos a ON a.Id = p.CvArchivoId
    )
    SELECT
        COUNT(*) AS TotalPostulaciones,
        ISNULL(SUM(CASE WHEN Estado IN ('Aceptada', 'NoAdmitida') THEN 1 ELSE 0 END), 0) AS Decididas,
        ISNULL(SUM(CASE WHEN Estado = 'Aceptada' THEN 1 ELSE 0 END), 0) AS Aceptadas,
        CAST(100.0 * SUM(CASE WHEN Estado = 'Aceptada' THEN 1 ELSE 0 END)
             / NULLIF(SUM(CASE WHEN Estado IN ('Aceptada', 'NoAdmitida') THEN 1 ELSE 0 END), 0) AS decimal(5, 1)) AS TasaAceptacion,
        CAST(AVG(CASE WHEN FechaDecision IS NOT NULL THEN DATEDIFF(MINUTE, FechaEnvio, FechaDecision) / 1440.0 END) AS decimal(9, 1)) AS PromedioDiasDecision,
        CAST(100.0 * SUM(CASE WHEN FechaRespuesta IS NOT NULL AND DATEDIFF(MINUTE, FechaEnvio, FechaRespuesta) <= 3 * 1440 THEN 1 ELSE 0 END)
             / NULLIF(COUNT(*), 0) AS decimal(5, 1)) AS PorcentajeRespuestaATiempo,
        (SELECT CAST(AVG(DATEDIFF(MINUTE, b.FechaDecision, pr.FechaRevisionHorario) / 1440.0) AS decimal(9, 1))
           FROM Base b
           JOIN Practicantes pr ON pr.UsuarioId = b.UsuarioId
          WHERE b.Tipo = 'Practicante' AND b.Estado = 'Aceptada' AND b.FechaDecision IS NOT NULL
            AND pr.EstadoHorario = 'Aceptado' AND pr.FechaRevisionHorario IS NOT NULL) AS PromedioDiasHorario,
        CAST(100.0 * SUM(Completa) / NULLIF(COUNT(*), 0) AS decimal(5, 1)) AS PorcentajeRegistrosCompletos,
        CAST(100.0 * SUM(CASE WHEN NombreCv LIKE 'CV[_]%[_]%.pdf' THEN 1 ELSE 0 END)
             / NULLIF(SUM(CASE WHEN NombreCv IS NOT NULL THEN 1 ELSE 0 END), 0) AS decimal(5, 1)) AS PorcentajeCvIdentificables
    FROM Base;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_IndicadoresProceso;");

            migrationBuilder.DropTable(
                name: "AlertasSeguridad");

            migrationBuilder.DropTable(
                name: "Anuncios");

            migrationBuilder.DropTable(
                name: "HistorialDonaciones");

            migrationBuilder.DropColumn(
                name: "DestinoEntrega",
                table: "Donaciones");

            migrationBuilder.DropColumn(
                name: "FechaEntrega",
                table: "Donaciones");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Donaciones",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);
        }
    }
}
