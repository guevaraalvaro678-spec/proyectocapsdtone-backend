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

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IAdminData, AdminData>();
builder.Services.AddScoped<IAdminService, AdminService>();
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
builder.Services.AddScoped<ICorreoData, CorreoData>();
builder.Services.AddScoped<ICorreoService, CorreoService>();
builder.Services.AddHostedService<EnvioCorreoWorker>();

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

builder.Services.AddCors(options =>
    options.AddPolicy("Front", policy =>
        policy.WithOrigins("http://localhost:4200")
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

app.Run();
