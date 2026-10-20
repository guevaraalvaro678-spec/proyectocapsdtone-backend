using System.Reflection;
using System.Runtime.ExceptionServices;
using CapstonePsicologiaAPI.DTOs;
using CapstonePsicologiaAPI.Services;

namespace CapstonePsicologiaAPI.Tests;

/// <summary>Reglas del horario de prácticas (HU-31): lunes a sábado, máximo 5 h por día y 30 h por semana.</summary>
public class HorarioPracticasTests
{
    private static void Validar(params (int Dia, string Inicio, string Fin)[] bloques)
    {
        var regla = typeof(PracticanteService).GetMethod("ValidarReglasPracticas", BindingFlags.NonPublic | BindingFlags.Static)!;
        var lista = bloques.Select(b => new BloqueHorarioDto { DiaSemana = b.Dia, HoraInicio = b.Inicio, HoraFin = b.Fin }).ToList();
        try
        {
            regla.Invoke(null, new object[] { lista });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    [Fact]
    public void Horario_de_lunes_a_viernes_de_4_horas_es_valido()
    {
        var error = Record.Exception(() => Validar((1, "08:00", "12:00"), (2, "08:00", "12:00"), (3, "08:00", "12:00"),
            (4, "08:00", "12:00"), (5, "08:00", "12:00")));
        Assert.Null(error);
    }

    [Fact]
    public void Treinta_horas_semanales_exactas_se_aceptan()
    {
        var error = Record.Exception(() => Validar((1, "08:00", "13:00"), (2, "08:00", "13:00"), (3, "08:00", "13:00"),
            (4, "08:00", "13:00"), (5, "08:00", "13:00"), (6, "08:00", "13:00")));
        Assert.Null(error);
    }

    [Fact]
    public void Domingo_no_se_permite()
    {
        var error = Assert.Throws<ReglaNegocioException>(() => Validar((7, "09:00", "12:00")));
        Assert.Equal("Las prácticas son de lunes a sábado", error.Message);
    }

    [Fact]
    public void Mas_de_5_horas_en_un_dia_se_rechaza()
    {
        var error = Assert.Throws<ReglaNegocioException>(() => Validar((1, "08:00", "14:00")));
        Assert.StartsWith("Máximo 5 horas por día", error.Message);
    }

    [Fact]
    public void Dos_bloques_del_mismo_dia_suman_sus_horas()
    {
        var error = Assert.Throws<ReglaNegocioException>(() => Validar((2, "08:00", "11:00"), (2, "14:00", "17:00")));
        Assert.StartsWith("Máximo 5 horas por día", error.Message);
    }
}
