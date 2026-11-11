using CapstonePsicologiaAPI.Data;
using CapstonePsicologiaAPI.Models;
using CapstonePsicologiaAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Secretos locales (p. ej. la contraseña del correo): este archivo no se sube al git.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
// En Azure la configuración (cadena de conexión, CORS, FrontUrl) llega como variables de entorno del App Service:
// se vuelven a cargar al final para que ganen sobre appsettings.Local.json.
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

// La base es serverless (plan gratis de Azure): se pausa sola y tarda hasta un minuto en despertar.
// Mientras despierta rechaza conexiones (40613, 40925), así que se reintenta en vez de fallar.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 6,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: new[] { 40613, 40925 })));

// Contraseñas en Base64 (ver ContrasenaBase64).
builder.Services.AddScoped<IPasswordHasher<Usuario>, ContrasenaBase64>();
builder.Services.AddScoped<IAdminData, AdminData>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.Configure<GoogleOpciones>(builder.Configuration.GetSection("Google"));
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IUsuarioData, UsuarioData>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<ISolicitudData, SolicitudData>();
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<IEventoData, EventoData>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IContactoData, ContactoData>();
builder.Services.AddScoped<IContactoService, ContactoService>();
builder.Services.AddScoped<ICitaData, CitaData>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<IPsicologoService, PsicologoService>();
builder.Services.AddScoped<IColaboradorData, ColaboradorData>();
builder.Services.AddScoped<IVoluntarioData, VoluntarioData>();
builder.Services.AddScoped<ISitioData, SitioData>();
builder.Services.AddScoped<ISitioService, SitioService>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<IPostulacionData, PostulacionData>();
builder.Services.AddScoped<ISesionEntrevistaData, SesionEntrevistaData>();
builder.Services.AddScoped<IArchivoData, ArchivoData>();
builder.Services.AddScoped<IPostulacionService, PostulacionService>();
builder.Services.AddScoped<ISesionEntrevistaService, SesionEntrevistaService>();
builder.Services.AddScoped<IDonacionData, DonacionData>();
builder.Services.AddScoped<IDonacionService, DonacionService>();
builder.Services.AddScoped<INoticiaData, NoticiaData>();
builder.Services.AddScoped<INoticiaService, NoticiaService>();
builder.Services.AddScoped<IDashboardOngService, DashboardOngService>();
builder.Services.AddScoped<IVoluntarioService, VoluntarioService>();
builder.Services.AddScoped<IPracticanteData, PracticanteData>();
builder.Services.AddScoped<IPracticanteService, PracticanteService>();
builder.Services.AddScoped<ITareaData, TareaData>();
builder.Services.AddScoped<ITareaService, TareaService>();
builder.Services.AddScoped<IMarcacionData, MarcacionData>();
builder.Services.AddScoped<IMarcacionService, MarcacionService>();
builder.Services.AddScoped<IColaboradorService, ColaboradorService>();
builder.Services.AddScoped<IDisponibilidadData, DisponibilidadData>();
builder.Services.AddScoped<IDisponibilidadService, DisponibilidadService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IDashboardData, DashboardData>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// Correo: cada correo se guarda en la base y el proceso en segundo plano lo envía si hay SMTP configurado.
builder.Services.Configure<CorreoOpciones>(builder.Configuration.GetSection("Correo"));
builder.Services.AddSingleton<ICorreoEnvio, SmtpCorreoEnvio>();

// SMS para el código de verificación: solo se ofrece si Sms:Twilio está configurado.
builder.Services.AddHttpClient<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<ICorreoData, CorreoData>();
builder.Services.AddScoped<ICorreoService, CorreoService>();
builder.Services.AddHostedService<EnvioCorreoWorker>();

builder.Services.AddScoped<IAnuncioService, AnuncioService>();
builder.Services.AddScoped<IAlertaService, AlertaService>();
builder.Services.AddScoped<IRespaldoService, RespaldoService>();
builder.Services.AddHostedService<RespaldoWorker>();

// HU-20: clave del cifrado de datos sensibles (si falta, se deriva de la clave del JWT para no dejar la API sin arrancar).
CifradoDatos.Configurar(builder.Configuration["Seguridad:ClaveCifrado"] is { Length: > 0 } clave ? clave : builder.Configuration["Jwt:Key"]!);

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // Un token firmado sigue siendo válido hasta que vence, así que además se comprueba en la base que la
        // cuenta siga activa: al desactivar a alguien, su sesión deja de funcionar en la siguiente petición.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                var idTexto = contexto.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var usuarios = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioData>();

                if (!int.TryParse(idTexto, out var id) || !await usuarios.EstaActivoAsync(id))
                    contexto.Fail("La cuenta no existe o está desactivada");
            },
        };
    });
builder.Services.AddAuthorization();

// Orígenes permitidos: Cors:Origenes en appsettings (local) o Cors__Origenes__0, __1… en el App Service.
var origenesFront = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() is { Length: > 0 } origenes
    ? origenes
    : new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
    options.AddPolicy("Front", policy =>
        policy.WithOrigins(origenesFront)
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "CapstonePsicologiaAPI v1"));
}

// Crea el usuario admin inicial si todavía no existe ningún usuario.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();

// En Azure el front Angular va en wwwroot del mismo App Service (en local no hay wwwroot y esto no hace nada).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("Front");

// Las reglas del negocio (dato inválido, sin permiso, no existe, ya ocupado) responden con su código y mensaje.
app.Use(async (contexto, siguiente) =>
{
    try
    {
        await siguiente();
    }
    catch (ReglaNegocioException ex) when (!contexto.Response.HasStarted)
    {
        contexto.Response.StatusCode = ex.Codigo;
        await contexto.Response.WriteAsJsonAsync(new { mensaje = ex.Message });
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Rutas del front (/admin/bandeja, /postular…): si no es de la API ni un archivo, se devuelve index.html para que
// Angular la resuelva. Las rutas /api que no existen siguen respondiendo 404.
app.MapFallbackToFile("{*ruta:regex(^(?!api\\b).*$):nonfile}", "index.html");

app.Run();
