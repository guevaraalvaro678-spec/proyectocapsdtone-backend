using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class PostulacionVoluntarioSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Voluntarios",
                table: "Voluntarios");

            migrationBuilder.DropIndex(
                name: "IX_Voluntarios_UsuarioId",
                table: "Voluntarios");

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Voluntarios",
                type: "nchar(8)",
                fixedLength: true,
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nchar(8)",
                oldFixedLength: true,
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "Provincia",
                table: "Postulaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Postulaciones",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Postulaciones",
                type: "nchar(8)",
                fixedLength: true,
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nchar(8)",
                oldFixedLength: true,
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "Distrito",
                table: "Postulaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Postulaciones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Departamento",
                table: "Postulaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Voluntarios",
                table: "Voluntarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Voluntarios_Dni",
                table: "Voluntarios",
                column: "Dni",
                unique: true,
                filter: "[Dni] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Voluntarios",
                table: "Voluntarios");

            migrationBuilder.DropIndex(
                name: "IX_Voluntarios_Dni",
                table: "Voluntarios");

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Voluntarios",
                type: "nchar(8)",
                fixedLength: true,
                maxLength: 8,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nchar(8)",
                oldFixedLength: true,
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Provincia",
                table: "Postulaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Postulaciones",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Dni",
                table: "Postulaciones",
                type: "nchar(8)",
                fixedLength: true,
                maxLength: 8,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nchar(8)",
                oldFixedLength: true,
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Distrito",
                table: "Postulaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Postulaciones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Departamento",
                table: "Postulaciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Voluntarios",
                table: "Voluntarios",
                column: "Dni");

            migrationBuilder.CreateIndex(
                name: "IX_Voluntarios_UsuarioId",
                table: "Voluntarios",
                column: "UsuarioId",
                unique: true);
        }
    }
}
