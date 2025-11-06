using Microsoft.EntityFrameworkCore;

namespace MiApp.Api.Data;

public static class DatabaseConfig
{
    // registro del DbContext y servicios. armo el connection string desde las opciones explícitas
    // si no vienen esos valores, uso ConnectionStrings:Default del appsettings
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // leo primero las variables directas (env o config) para el servidor, puerto, base, usuario y clave
    var server = configuration["DB_SERVER"] ?? configuration.GetValue<string>("DbSettings:Server") ?? "localhost";
    var port = configuration["DB_PORT"] ?? configuration.GetValue<string>("DbSettings:Port") ?? "1433";
    var database = configuration["DB_NAME"] ?? configuration.GetValue<string>("DbSettings:Database") ?? "MiAppDB";
    var user = configuration["DB_USER"] ?? configuration.GetValue<string>("DbSettings:User") ?? "sa";
    var password = configuration["DB_PASSWORD"] ?? configuration.GetValue<string>("DbSettings:Password") ?? "ProyectoTecnico2025**";

        string connectionString;
        if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(database) && !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
        {
            // uso los valores dados; por defecto activo Encrypt y confío en el certificado del servidor para desarrollo local
            var serverPart = !string.IsNullOrWhiteSpace(port) ? $"{server},{port}" : server;
            connectionString = $"Server={serverPart};Database={database};User Id={user};Password={password};Encrypt=True;TrustServerCertificate=True";
        }
        else
        {
            // si no hay datos explícitos, tomo el connection string Default del appsettings
            connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("No database configuration found.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }

    // no inicializo ni hago seed automático aquí a propósito
    // por solicitud, dejamos la creación y seed del lado del entorno/DBA
    public static WebApplication InitializeDatabase(this WebApplication app)
    {
        // no-op: dejo la inicialización a quien administre la base; así evitamos datos sembrados sin querer
        return app;
    }
}
