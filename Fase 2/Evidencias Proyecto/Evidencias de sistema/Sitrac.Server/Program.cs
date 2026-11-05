using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using Sitrac.DataAccess;
using Sitrac.Repository;
using Sitrac.Repository.Interfaces;
using Sitrac.Service;
using Sitrac.Service.Configuration;
using Sitrac.Service.Interfaces;
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
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.Configure<TeamsSettings>(builder.Configuration.GetSection(TeamsSettings.SectionName));
builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection(FrontendSettings.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Dependency Injection
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IIncidenteRepository, IncidenteRepository>();
builder.Services.AddScoped<ISlaRepository, SlaRepository>();
builder.Services.AddScoped<IIncidenteService, IncidenteService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IHitoRepository, HitoRepository>();
builder.Services.AddScoped<IReglaEscalamientoRepository, ReglaEscalamientoRepository>();
builder.Services.AddScoped<INotificacionLogRepository, NotificacionLogRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHostedService<LifecycleAdvancerService>();
builder.Services.AddHostedService<NotificationDispatcherService>();
builder.Services.AddHostedService<SlaEscalationService>();

// Autenticación JWT
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
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
        var connectionString = configuration.GetConnectionString("SitracConnection");
        using var connection = new SqlConnection(connectionString);
        var evolve = new Evolve(connection, msg => Console.WriteLine(msg))
        {
            Locations = new[] { "../Sitrac.DbMigration/Scripts" },
            IsEraseDisabled = true,
            Placeholders = new Dictionary<string, string> { { "${db}", "DbSitrac" } }
        };
        evolve.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al ejecutar las migraciones de BD: {ex.Message}");
        throw;
    }
}