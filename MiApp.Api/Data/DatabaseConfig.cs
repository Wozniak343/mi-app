using Microsoft.EntityFrameworkCore;

namespace MiApp.Api.Data;

public static class DatabaseConfig
{
    // Register DbContext and related services. Builds a full connection string from explicit settings
    // If no explicit settings are provided, falls back to ConnectionStrings:Default
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Try to read explicit DB settings (env vars or configuration)
    var server = configuration["DB_SERVER"] ?? configuration.GetValue<string>("DbSettings:Server") ?? "localhost";
    var port = configuration["DB_PORT"] ?? configuration.GetValue<string>("DbSettings:Port") ?? "1433";
    var database = configuration["DB_NAME"] ?? configuration.GetValue<string>("DbSettings:Database") ?? "MiAppDB";
    var user = configuration["DB_USER"] ?? configuration.GetValue<string>("DbSettings:User") ?? "sa";
    var password = configuration["DB_PASSWORD"] ?? configuration.GetValue<string>("DbSettings:Password") ?? "ProyectoTecnico2025**";

        string connectionString;
        if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(database) && !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(password))
        {
            // Use provided values; default SQL Server encrypt and trust server cert for local dev
            var serverPart = !string.IsNullOrWhiteSpace(port) ? $"{server},{port}" : server;
            connectionString = $"Server={serverPart};Database={database};User Id={user};Password={password};Encrypt=True;TrustServerCertificate=True";
        }
        else
        {
            // Fallback to standard connection string in appsettings
            connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("No database configuration found.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }

    // Intentionally do not auto-initialize or seed the database here.
    // Per request, we avoid creating or seeding the DB automatically.
    public static WebApplication InitializeDatabase(this WebApplication app)
    {
        // No-op: leave DB initialization to the environment/DBAs. This prevents unexpected seeding.
        return app;
    }
}
