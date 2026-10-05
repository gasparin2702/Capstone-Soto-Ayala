using Microsoft.Data.SqlClient;
using Sitrac.DataAccess;
using Sitrac.Repository;
using Sitrac.Repository.Interfaces;
using Sitrac.Service;
using Sitrac.Service.Interfaces;
using EvolveDb;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings-{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IIncidenteRepository, IncidenteRepository>();
builder.Services.AddScoped<ISlaRepository, SlaRepository>();
builder.Services.AddScoped<IIncidenteService, IncidenteService>();

// Registra el motor de ciclo de vida
builder.Services.AddHostedService<LifecycleAdvancerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ===== NUEVO: registrar CORS =====
builder.Services.AddCors();

var app = builder.Build();

// ===== NUEVO: middleware CORS (antes de Evolve) =====
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