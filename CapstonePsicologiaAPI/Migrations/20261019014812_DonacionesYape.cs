using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class DonacionesYape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "YapeNumero",
                table: "SitioConfig",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YapeQrImagenId",
                table: "SitioConfig",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YapeTitular",
                table: "SitioConfig",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SitioConfig_YapeQrImagenId",
                table: "SitioConfig",
                column: "YapeQrImagenId");

            migrationBuilder.AddForeignKey(
                name: "FK_SitioConfig_Imagenes_YapeQrImagenId",
                table: "SitioConfig",
                column: "YapeQrImagenId",
                principalTable: "Imagenes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SitioConfig_Imagenes_YapeQrImagenId",
                table: "SitioConfig");

            migrationBuilder.DropIndex(
                name: "IX_SitioConfig_YapeQrImagenId",
                table: "SitioConfig");

            migrationBuilder.DropColumn(
                name: "YapeNumero",
                table: "SitioConfig");

            migrationBuilder.DropColumn(
                name: "YapeQrImagenId",
                table: "SitioConfig");

            migrationBuilder.DropColumn(
                name: "YapeTitular",
                table: "SitioConfig");
        }
    }
}
