using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Sitrac.AccesoDatos;
using Sitrac.Repositorio;
using Sitrac.Repositorio.Interfaces;
using Sitrac.Servicio;
using Sitrac.Servicio.Configuration;
using Sitrac.Servicio.Interfaces;
using Sitrac.Servicio.Seguridad;
using EvolveDb;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// Options
builder.Services.Configure<ConfiguracionSmtp>(builder.Configuration.GetSection(ConfiguracionSmtp.SectionName));
builder.Services.Configure<ConfiguracionTeams>(builder.Configuration.GetSection(ConfiguracionTeams.SectionName));
builder.Services.Configure<ConfiguracionFrontend>(builder.Configuration.GetSection(ConfiguracionFrontend.SectionName));
builder.Services.Configure<ConfiguracionJwt>(builder.Configuration.GetSection(ConfiguracionJwt.SectionName));
builder.Services.Configure<ConfiguracionSeguridad>(builder.Configuration.GetSection(ConfiguracionSeguridad.SectionName));

// Dependency Injection
builder.Services.AddSingleton<IFabricaConexionBd, FabricaConexionBd>();
builder.Services.AddScoped<IIncidenteRepositorio, IncidenteRepositorio>();
builder.Services.AddScoped<ISlaRepositorio, SlaRepositorio>();
builder.Services.AddScoped<IIncidenteServicio, IncidenteServicio>();
builder.Services.AddScoped<INotificacionServicio, NotificacionServicio>();
builder.Services.AddScoped<IHitoRepositorio, HitoRepositorio>();
builder.Services.AddScoped<IReglaEscalamientoRepositorio, ReglaEscalamientoRepositorio>();
builder.Services.AddScoped<IRegistroNotificacionRepositorio, RegistroNotificacionRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IAutenticacionServicio, AutenticacionServicio>();
builder.Services.AddScoped<ISeguridadRepositorio, SeguridadRepositorio>();
builder.Services.AddScoped<ISeguridadServicio, SeguridadServicio>();

builder.Services.AddHostedService<AvanzadorCicloVidaServicio>();
builder.Services.AddHostedService<DespachadorNotificacionServicio>();
builder.Services.AddHostedService<EscalamientoSlaServicio>();

// Autenticación JWT
var jwtSettings = builder.Configuration.GetSection(ConfiguracionJwt.SectionName).Get<ConfiguracionJwt>();
var key = Encoding.ASCII.GetBytes(jwtSettings?.SecretKey ?? "ClavePorDefectoDeDesarrollo2026");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings?.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings?.Audience,
        ValidateLifetime = true
    };
});

var habilitarAuthTx = builder.Configuration
    .GetSection(ConfiguracionSeguridad.SectionName)
    .GetValue<bool>(nameof(ConfiguracionSeguridad.HabilitarAutorizacionPorTransaccion));

builder.Services.AddAuthorization(options =>
{
    foreach (var codigo in TransaccionesCatalogo.Todas)
    {
        options.AddPolicy(codigo, policy =>
        {
            if (habilitarAuthTx)
                policy.RequireClaim("transaccion", codigo);
            else
                policy.RequireAuthenticatedUser(); // solo autenticado
        });
    }
});

// Rate Limiting (GLOBAL) - 100 peticiones por minuto por IP/Usuario
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Usamos la IP o el nombre de usuario como clave de partición
        var partitionKey = httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });
    
    // Respuesta cuando se excede el límite
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS Estricto
builder.Services.AddCors(options =>
{
    options.AddPolicy("SitracPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5500", "http://127.0.0.1:5500")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("SitracPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

EjecutarMigracionesEvolve(app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

void EjecutarMigracionesEvolve(IConfiguration configuration)
{
    try
    {
        var connectionString = configuration.GetConnectionString("SitracConnection")
            ?? throw new InvalidOperationException("La cadena de conexión SitracConnection no está configurada.");
        connectionString = connectionString.Trim().Trim('"', '\'');

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("La cadena de conexión SitracConnection no especifica una base de datos.");
        }

        // Evolve no crea la base de datos: solo crea esquemas y aplica scripts.
        // Se usa una conexión temporal a "master" para crearla si no existe.
        var masterBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        using (var masterConnection = new SqlConnection(masterBuilder.ConnectionString))
        {
            masterConnection.Open();
            using var command = masterConnection.CreateCommand();
            command.CommandText = @"
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    DECLARE @Sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
    EXEC(@Sql);
END";
            command.Parameters.AddWithValue("@DatabaseName", databaseName);
            command.ExecuteNonQuery();
        }

        using var connection = new SqlConnection(builder.ConnectionString);
        var evolve = new Evolve(connection, msg => Console.WriteLine(msg))
        {
            Locations = new[] { "../Sitrac.MigracionBd/Scripts" },
            IsEraseDisabled = true,
            Placeholders = new Dictionary<string, string> { { "${db}", databaseName } }
        };
        evolve.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al ejecutar las migraciones de BD: {ex.Message}");
        throw;
    }
}