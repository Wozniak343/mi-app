using Microsoft.EntityFrameworkCore;
using MiApp.Api.Data;
using MiApp.Api;

var builder = WebApplication.CreateBuilder(args);

// Centralized DB registration
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddScoped<DbRepository>();

// CORS for frontend dev
builder.Services.AddCors(options =>
    options.AddPolicy("AllowLocalhost4200", p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod())
);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost4200");

// Register API endpoints from dedicated file
ApiEndpoints.Register(app);

app.Run();
