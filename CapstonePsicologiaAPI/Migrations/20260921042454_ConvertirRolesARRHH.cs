using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapstonePsicologiaAPI.Migrations
{
    /// <inheritdoc />
    public partial class ConvertirRolesARRHH : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El módulo de psicología se reemplaza: los psicólogos pasan a Recursos Humanos y los usuarios a voluntarios.
            migrationBuilder.Sql("UPDATE Usuarios SET Rol = 'RRHH' WHERE Rol = 'Psicologo'");
            migrationBuilder.Sql("UPDATE Usuarios SET Rol = 'Voluntario' WHERE Rol = 'Usuario'");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
