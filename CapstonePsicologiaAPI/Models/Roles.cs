namespace CapstonePsicologiaAPI.Models;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";

    /// <summary>Recursos Humanos: revisa y aprueba los horarios de los practicantes.</summary>
    public const string RRHH = "RRHH";
    public const string Voluntario = "Voluntario";
    public const string Practicante = "Practicante";

    // Roles del módulo de psicología anterior; ninguna cuenta nueva los usa.
    public const string Psicologo = "Psicologo";
    public const string Usuario = "Usuario";
}
