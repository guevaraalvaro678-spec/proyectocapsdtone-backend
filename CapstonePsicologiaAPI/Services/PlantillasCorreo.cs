using System.Globalization;
using System.Net;

namespace CapstonePsicologiaAPI.Services;

/// <summary>Contenido HTML de los correos. Todo texto que viene de un usuario se codifica para evitar inyección de HTML.</summary>
public static class PlantillasCorreo
{
    private const string Verde = "#1b4332";
    private static readonly CultureInfo Cultura = new("es-PE");

    public static string Bienvenida(string nombre, string enlace) =>
        Envolver(
            $"¡Bienvenido/a, {H(PrimerNombre(nombre))}!",
            "<p>Tu cuenta ya está lista. Desde tu espacio puedes ver tus datos y tus tareas.</p>",
            enlace,
            "Ir a mi espacio");

    public static string CodigoVerificacion(string nombre, string codigo, int minutos) =>
        Envolver(
            $"Tu código de verificación, {H(PrimerNombre(nombre))}",
            "<p>Usa este código para terminar de iniciar sesión en AMAAC Perú:</p>" +
            $"<p style=\"margin:20px 0;font-size:32px;font-weight:800;letter-spacing:8px;color:{Verde}\">{H(codigo)}</p>" +
            $"<p>Vence en {minutos} minutos. Si no fuiste tú, cambia tu contraseña: alguien conoce la actual.</p>",
            null,
            null);

    public static string AlertaIntentoFallido(string nombre, DateTime fechaUtc, string enlace) =>
        Envolver(
            $"Alguien intentó entrar a tu cuenta, {H(PrimerNombre(nombre))}",
            $"<p>El <strong>{FechaLima(fechaUtc)}</strong> hubo dos intentos seguidos de entrar a tu cuenta con una contraseña incorrecta.</p>" +
            "<p>Si fuiste tú, no tienes que hacer nada. Si no, te recomendamos cambiar tu contraseña. " +
            "Tras 5 intentos fallidos la cuenta se bloquea 15 minutos.</p>",
            enlace,
            "Cambiar mi contraseña");

    public static string CuentaBloqueada(string nombre, int minutos, string enlace) =>
        Envolver(
            $"Bloqueamos tu cuenta por seguridad, {H(PrimerNombre(nombre))}",
            $"<p>Hubo varios intentos fallidos de contraseña. Tu cuenta quedará bloqueada durante {minutos} minutos.</p>" +
            "<p>Si no fuiste tú, te recomendamos cambiar tu contraseña.</p>",
            enlace,
            "Recuperar mi contraseña");

    public static string CuentaCreada(string nombre, string enlace) =>
        Envolver(
            $"¡Hola, {H(PrimerNombre(nombre))}!",
            "<p>Tu cuenta en AMAAC Perú ya está lista. El siguiente paso es completar tu postulación como voluntario/a o practicante.</p>",
            enlace,
            "Completar mi postulación");

    public static string PostulacionEnviada(string nombre, string codigo, string tipo, string enlace) =>
        Envolver(
            $"Recibimos tu postulación, {H(PrimerNombre(nombre))}",
            $"<p>Tu postulación <strong>{H(codigo)}</strong> como <strong>{H(tipo.ToLowerInvariant())}</strong> fue registrada. " +
            "El equipo la revisará y te avisaremos cuando quedes habilitado/a para la entrevista grupal.</p>",
            enlace,
            "Ver mi postulación");

    public static string EntrevistaAgendada(string nombre, string sesion, DateTime inicioUtc, string modalidad, string lugar, string enlace) =>
        Envolver(
            $"Tu entrevista grupal está agendada, {H(PrimerNombre(nombre))}",
            "<p>Te asignamos a una sesión de entrevista grupal:</p>" +
            "<table style=\"border-collapse:collapse;margin:12px 0\">" +
            Fila("Sesión", sesion) + Fila("Fecha", FechaLima(inicioUtc)) + Fila("Modalidad", modalidad) + Fila("Lugar o enlace", lugar) +
            "</table><p style=\"color:#6b7280\">Conéctate 5 minutos antes y ten a la mano tu documento de identidad.</p>",
            enlace,
            "Ver mi postulación");

    public static string PostulacionAceptada(string nombre, string tipo, string enlace) =>
        Envolver(
            $"¡Bienvenido/a a AMAAC Perú, {H(PrimerNombre(nombre))}!",
            $"<p>Tu postulación fue <strong>aceptada</strong>. Ya formas parte del equipo como <strong>{H(tipo.ToLowerInvariant())}</strong>. " +
            "Inicia sesión para entrar a tu portal.</p>",
            enlace,
            "Ir a mi portal");

    public static string PostulacionNoAdmitida(string nombre, string? nota) =>
        Envolver(
            $"Sobre tu postulación, {H(PrimerNombre(nombre))}",
            "<p>Gracias por tu interés en AMAAC Perú. Luego de la evaluación, en esta oportunidad tu postulación no fue admitida.</p>" +
            (string.IsNullOrWhiteSpace(nota) ? "" : $"<p><strong>Comentario:</strong> {H(nota)}</p>") +
            "<p>Puedes volver a postular en una próxima convocatoria.</p>",
            null,
            null);

    public static string BienvenidaVoluntario(string nombre, string correo, string clave, string enlace) =>
        Envolver(
            $"¡Gracias por sumarte, {H(PrimerNombre(nombre))}!",
            "<p>Ya eres parte de nuestro voluntariado. Estos son tus datos de acceso:</p>" +
            "<table style=\"border-collapse:collapse;margin:12px 0\">" + Fila("Usuario", correo) + Fila("Contraseña temporal", clave) + "</table>" +
            "<p style=\"color:#6b7280\">Puedes cambiar tu contraseña desde \"¿Olvidaste tu contraseña?\" en la pantalla de inicio de sesión.</p>",
            enlace,
            "Iniciar sesión");

    public static string BienvenidaPracticante(string nombre, string enlace) =>
        Envolver(
            $"¡Bienvenido/a, {H(PrimerNombre(nombre))}!",
            "<p>Tu cuenta de practicante ya está lista. Lo primero es enviar el horario de tu universidad desde tu panel: Recursos Humanos lo revisará y luego te asignará tu horario de prácticas.</p>",
            enlace,
            "Ir a mi panel");

    public static string HorarioAceptado(string nombre, string enlace) =>
        Envolver(
            $"Tu horario fue confirmado, {H(PrimerNombre(nombre))}",
            "<p>Recursos Humanos revisó tu horario y ya tienes asignado tu horario de prácticas. Puedes verlo confirmado en tu panel.</p>",
            enlace,
            "Ver mi horario");

    public static string BienvenidaPsicologo(string nombre, string enlace) =>
        Envolver(
            $"¡Bienvenido/a, {H(PrimerNombre(nombre))}!",
            "<p>Se creó tu cuenta de psicólogo. Desde tu panel puedes ver tus citas y las solicitudes de orientación que te asignen.</p>" +
            "<p style=\"color:#6b7280\">Tu contraseña inicial te la entregó el administrador. Puedes cambiarla desde \"¿Olvidaste tu contraseña?\" en la pantalla de inicio de sesión.</p>",
            enlace,
            "Ir a mi panel");

    public static string PostulacionRecibida(string nombre) =>
        Envolver(
            $"Recibimos tu postulación, {H(PrimerNombre(nombre))}",
            "<p>Gracias por querer colaborar con nosotros. El equipo administrador revisará tu solicitud y te avisaremos por este medio cuando tengamos una respuesta.</p>",
            null,
            null);

    public static string PostulacionAprobada(string nombre, string enlace) =>
        Envolver(
            $"¡Tu postulación fue aprobada, {H(PrimerNombre(nombre))}!",
            "<p>Ya eres parte del equipo. Inicia sesión con el correo y la contraseña que registraste para empezar a participar.</p>",
            enlace,
            "Ir a mi panel");

    public static string PostulacionRechazada(string nombre, string? motivo) =>
        Envolver(
            $"Sobre tu postulación, {H(PrimerNombre(nombre))}",
            "<p>Revisamos tu solicitud y por ahora no podemos incorporarte al equipo.</p>" +
            (string.IsNullOrWhiteSpace(motivo) ? "" : $"<p><strong>Motivo:</strong> {H(motivo)}</p>") +
            "<p>Gracias por tu interés.</p>",
            null,
            null);

    public static string EstadoSolicitud(string nombre, string estado, string? psicologo, string enlace)
    {
        var (titulo, detalle) = estado switch
        {
            "EnRevision" => ("Estamos revisando tu solicitud", "Nuestro equipo ya está revisando tu solicitud de orientación. Te avisaremos cuando avance."),
            "Asignada" => ("Ya tienes un profesional asignado",
                psicologo is null
                    ? "Tu solicitud fue asignada a un profesional. Pronto te contactaremos para agendar tu cita."
                    : $"Tu solicitud fue asignada a <strong>{H(psicologo)}</strong>. Pronto te contactaremos para agendar tu cita."),
            "Cerrada" => ("Tu solicitud fue cerrada", "Tu solicitud de orientación fue cerrada. Si necesitas más apoyo, puedes enviar una nueva desde tu espacio."),
            _ => ("Actualización de tu solicitud", "Hubo una actualización en tu solicitud de orientación."),
        };

        return Envolver($"Hola, {H(PrimerNombre(nombre))}", $"<h3 style=\"margin:0 0 8px\">{titulo}</h3><p>{detalle}</p>", enlace, "Ver mi solicitud");
    }

    public static string RecuperarPassword(string nombre, string enlace) =>
        Envolver(
            $"Hola, {H(PrimerNombre(nombre))}",
            "<p>Recibimos una solicitud para restablecer tu contraseña. El enlace vale por <strong>1 hora</strong>.</p>" +
            "<p style=\"color:#6b7280\">Si no fuiste tú, ignora este correo: tu contraseña no cambiará.</p>",
            enlace,
            "Restablecer contraseña");

    public static string NuevaCita(string nombre, string conQuien, bool paraPsicologo, DateTime fechaUtc, string modalidad, string? lugar, string motivo, string enlace) =>
        Envolver(
            $"Hola, {H(PrimerNombre(nombre))}",
            $"<p>Se agendó una cita {(paraPsicologo ? "con tu paciente" : "con")} <strong>{H(conQuien)}</strong>.</p>{DetalleCita(fechaUtc, modalidad, lugar, motivo)}",
            enlace,
            "Ver mis citas");

    public static string CitaConfirmada(string nombre, string conQuien, DateTime fechaUtc, string modalidad, string? lugar, string motivo, string enlace) =>
        Envolver(
            $"¡Tu cita fue confirmada, {H(PrimerNombre(nombre))}!",
            $"<p><strong>{H(conQuien)}</strong> aceptó tu solicitud de cita.</p>{DetalleCita(fechaUtc, modalidad, lugar, motivo)}",
            enlace,
            "Ver mis citas");

    public static string RecordatorioCita(string nombre, string conQuien, DateTime fechaUtc, string modalidad, string? lugar, string motivo, string enlace) =>
        Envolver(
            $"Recordatorio de tu cita, {H(PrimerNombre(nombre))}",
            $"<p>Te recordamos que tienes una cita próxima con <strong>{H(conQuien)}</strong>.</p>{DetalleCita(fechaUtc, modalidad, lugar, motivo)}",
            enlace,
            "Ver mis citas");

    public static string CitaCancelada(string nombre, string conQuien, DateTime fechaUtc, string enlace) =>
        Envolver(
            $"Hola, {H(PrimerNombre(nombre))}",
            $"<p>La cita con <strong>{H(conQuien)}</strong> del <strong>{FechaLima(fechaUtc)}</strong> fue <strong>cancelada</strong>.</p>",
            enlace,
            "Ver mis citas");

    public static string RespuestaContacto(string nombre, string respuesta, string mensajeOriginal) =>
        Envolver(
            $"Hola, {H(PrimerNombre(nombre))}",
            $"<p>Gracias por escribirnos. Esta es nuestra respuesta:</p>{Parrafos(respuesta)}" +
            $"<p style=\"margin-top:20px;color:#6b7280;font-size:13px\">Tu mensaje original:</p>" +
            $"<blockquote style=\"margin:0;padding-left:12px;border-left:3px solid #d1d5db;color:#6b7280\">{Parrafos(mensajeOriginal)}</blockquote>",
            null,
            null);

    public static string Masivo(string nombre, string mensaje) =>
        Envolver($"Hola, {H(PrimerNombre(nombre))}", Parrafos(mensaje), null, null);

    /// <summary>Fecha en hora de Lima (UTC-5, sin horario de verano), por ejemplo "lunes 5 de octubre, 3:00 p. m.".</summary>
    public static string FechaLima(DateTime fechaUtc)
    {
        var lima = DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc).AddHours(-5);
        return lima.ToString("dddd d 'de' MMMM, h:mm tt", Cultura);
    }

    private static string DetalleCita(DateTime fechaUtc, string modalidad, string? lugar, string motivo) =>
        "<table style=\"border-collapse:collapse;margin:12px 0\">" +
        Fila("Fecha", FechaLima(fechaUtc)) +
        Fila("Modalidad", modalidad) +
        (string.IsNullOrWhiteSpace(lugar) ? "" : Fila("Lugar o enlace", lugar)) +
        Fila("Motivo", motivo) +
        "</table>";

    private static string Fila(string etiqueta, string valor) =>
        $"<tr><td style=\"padding:4px 16px 4px 0;color:#6b7280\">{H(etiqueta)}</td><td style=\"padding:4px 0\"><strong>{H(valor)}</strong></td></tr>";

    private static string Envolver(string titulo, string contenido, string? enlace, string? textoBoton)
    {
        var boton = enlace is null
            ? ""
            : $"<p style=\"margin:24px 0 0\"><a href=\"{H(enlace)}\" style=\"display:inline-block;padding:12px 22px;background:{Verde};color:#ffffff;text-decoration:none;border-radius:8px;font-weight:600\">{H(textoBoton ?? "Abrir")}</a></p>" +
              $"<p style=\"margin:16px 0 0;font-size:12px;color:#9ca3af\">Si el botón no funciona, copia este enlace en tu navegador:<br>{H(enlace)}</p>";

        return $"""
            <div style="background:#f8f3e6;padding:24px;font-family:Arial,Helvetica,sans-serif;color:#1f2937">
              <div style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden">
                <div style="background:{Verde};padding:18px 24px;color:#ffffff;font-size:18px;font-weight:700">AMAAC Perú</div>
                <div style="padding:24px;line-height:1.55">
                  <h2 style="margin:0 0 12px;color:{Verde}">{titulo}</h2>
                  {contenido}
                  {boton}
                </div>
                <div style="padding:14px 24px;background:#f3f4f6;font-size:12px;color:#6b7280">
                  Este es un mensaje automático de AMAAC Perú.
                </div>
              </div>
            </div>
            """;
    }

    /// <summary>Convierte saltos de línea en párrafos, codificando el texto.</summary>
    private static string Parrafos(string texto) =>
        string.Join("", texto.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(linea => $"<p style=\"margin:0 0 10px\">{H(linea)}</p>"));

    private static string PrimerNombre(string nombre) =>
        nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;

    private static string H(string texto) => WebUtility.HtmlEncode(texto);
}
