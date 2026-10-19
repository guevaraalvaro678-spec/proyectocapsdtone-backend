using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class VerificacionDosPasos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoVerificacionExpira",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoVerificacionHash",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodigoVerificacionIntentos",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DesafioExpira",
                table: "Usuarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesafioHash",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoVerificacionExpira",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoVerificacionHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoVerificacionIntentos",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DesafioExpira",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DesafioHash",
                table: "Usuarios");
        }
    }
}
