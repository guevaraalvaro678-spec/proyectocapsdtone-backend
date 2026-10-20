using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CapstonePsicologiaAPI.Tests;

/// <summary>Postulación: CV en PDF de hasta 10 MB (HU-07), acceso al CV por rol (HU-26) y no duplicar postulaciones (HU-12).</summary>
public class PostulacionTests
{
    private readonly Mock<IPostulacionData> _postulaciones = new();
    private readonly Mock<IArchivoData> _archivos = new();
    private readonly Mock<IUsuarioData> _usuarios = new();

    // Estos casos no llegan a usar el DbContext.
    private PostulacionService Servicio() => new(_postulaciones.Object, Mock.Of<ISesionEntrevistaData>(), _archivos.Object,
        _usuarios.Object, Mock.Of<IVoluntarioData>(), Mock.Of<IPracticanteData>(), Mock.Of<ICorreoService>(),
        Mock.Of<ICuentaService>(), null!);

    private static IFormFile Archivo(string nombre, string tipo, long bytes) =>
        new FormFile(new MemoryStream(new byte[Math.Min(bytes, 1024)]), 0, bytes, "cv", nombre)
        {
            Headers = new HeaderDictionary(),
            ContentType = tipo,
        };

    [Fact]
    public async Task CV_que_no_es_PDF_se_rechaza()
    {
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            Servicio().SubirCvAsync(Archivo("cv.docx", "application/msword", 1024), usuarioId: 5));
        Assert.Equal("El CV debe ser un archivo PDF", error.Message);
    }

    [Fact]
    public async Task CV_de_mas_de_10_MB_se_rechaza()
    {
        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            Servicio().SubirCvAsync(Archivo("cv.pdf", "application/pdf", 10 * 1024 * 1024 + 1), usuarioId: 5));
        Assert.Equal("El CV pesa más de 10 MB", error.Message);
    }

    [Fact]
    public async Task CV_vacio_se_rechaza()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            Servicio().SubirCvAsync(Archivo("cv.pdf", "application/pdf", 0), usuarioId: 5));
    }

    [Fact]
    public async Task CV_en_PDF_valido_se_guarda_a_nombre_de_quien_lo_subio()
    {
        _archivos.Setup(a => a.CrearAsync(It.IsAny<Archivo>())).ReturnsAsync((Archivo a) => { a.Id = 31; return a; });

        var subido = await Servicio().SubirCvAsync(Archivo("mi cv.pdf", "application/pdf", 1024), usuarioId: 5);

        Assert.Equal(31, subido.Id);
        _archivos.Verify(a => a.CrearAsync(It.Is<Archivo>(x => x.SubidoPorUsuarioId == 5 && x.TipoContenido == "application/pdf")), Times.Once);
    }

    [Fact]
    public async Task Un_postulante_no_puede_ver_el_CV_de_otro()
    {
        _archivos.Setup(a => a.ObtenerAsync(31)).ReturnsAsync(new Archivo { Id = 31, SubidoPorUsuarioId = 5 });

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            Servicio().ObtenerArchivoAsync(31, actorId: 9, actorRol: Roles.Postulante));
        Assert.Equal(403, error.Codigo);
    }

    [Fact]
    public async Task Recursos_Humanos_si_puede_ver_el_CV()
    {
        _archivos.Setup(a => a.ObtenerAsync(31)).ReturnsAsync(new Archivo { Id = 31, SubidoPorUsuarioId = 5 });

        var archivo = await Servicio().ObtenerArchivoAsync(31, actorId: 2, actorRol: Roles.RRHH);
        Assert.Equal(31, archivo.Id);
    }

    [Fact]
    public async Task No_se_puede_enviar_una_segunda_postulacion_mientras_hay_una_en_curso()
    {
        _usuarios.Setup(u => u.ObtenerPorIdAsync(5)).ReturnsAsync(new Usuario { Id = 5, Rol = Roles.Postulante });
        _postulaciones.Setup(p => p.ObtenerUltimaDeUsuarioAsync(5)).ReturnsAsync(new Postulacion { Estado = EstadosPostulacion.Recibida });

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            Servicio().CrearAsync(5, new PostulacionCrearDto { Tipo = TiposPostulacion.Voluntario }));
        Assert.Equal(409, error.Codigo);
    }

    [Fact]
    public async Task Quien_ya_es_miembro_no_puede_volver_a_postular()
    {
        _usuarios.Setup(u => u.ObtenerPorIdAsync(5)).ReturnsAsync(new Usuario { Id = 5, Rol = Roles.Voluntario });

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            Servicio().CrearAsync(5, new PostulacionCrearDto { Tipo = TiposPostulacion.Voluntario }));
        Assert.Equal(403, error.Codigo);
    }
}
