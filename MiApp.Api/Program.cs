using Microsoft.EntityFrameworkCore;
using MiApp.Api.Data;
using MiApp.Api;

var builder = WebApplication.CreateBuilder(args);

// registro centralizado de la base de datos
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddScoped<DbRepository>();

// CORS para desarrollo del frontend (localhost:4200)
builder.Services.AddCors(options =>
    options.AddPolicy("AllowLocalhost4200", p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod())
);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost4200");

// registro de endpoints de la API desde un archivo separado
ApiEndpoints.Register(app);

app.Run();
