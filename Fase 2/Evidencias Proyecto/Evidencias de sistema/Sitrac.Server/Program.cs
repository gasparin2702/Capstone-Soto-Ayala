using Microsoft.Data.SqlClient;
using Sitrac.DataAccess;
using Sitrac.Repository;
using Sitrac.Repository.Interfaces;
using Sitrac.Service;
using Sitrac.Service.Configuration;
using Sitrac.Service.Interfaces;
using EvolveDb;
using Microsoft.Extensions.Options;

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

builder.Services.AddOptions<SmtpSettings>()
    .Bind(builder.Configuration.GetSection(SmtpSettings.SectionName))
    .Validate(settings =>
        !string.IsNullOrWhiteSpace(settings.Host) &&
        !string.IsNullOrWhiteSpace(settings.UserName) &&
        !string.IsNullOrWhiteSpace(settings.Password),
        "SMTP mal configurado: Host, UserName y Password son obligatorios")
    .ValidateOnStart();

builder.Services.AddOptions<TeamsSettings>()
    .Bind(builder.Configuration.GetSection(TeamsSettings.SectionName))
    .Validate(settings =>
        !settings.Habilitado || !string.IsNullOrWhiteSpace(settings.WebhookUrl),
        "Teams habilitado pero WebhookUrl vacÃ­o")
    .ValidateOnStart();

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IIncidenteRepository, IncidenteRepository>();
builder.Services.AddScoped<ISlaRepository, SlaRepository>();
builder.Services.AddScoped<IIncidenteService, IncidenteService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IHitoRepository, HitoRepository>();
builder.Services.AddScoped<IReglaEscalamientoRepository, ReglaEscalamientoRepository>();
builder.Services.AddScoped<INotificacionLogRepository, NotificacionLogRepository>();

builder.Services.AddHostedService<LifecycleAdvancerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

EjecutarMigracionesEvolve(app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
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

