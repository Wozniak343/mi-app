using Microsoft.EntityFrameworkCore;
using MiApp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Minimal services: DbContext and CORS for frontend dev
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddCors(options =>
    options.AddPolicy("AllowLocalhost4200", p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod())
);

var app = builder.Build();

// Test endpoint para verificar la conexión
app.MapGet("/api/test-connection", async (ApplicationDbContext db) =>
{
    try
    {
        // Intenta conectar a la base de datos
        bool canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { connected = canConnect, message = "Conexión exitosa a la base de datos" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { connected = false, message = $"Error de conexión: {ex.Message}" });
    }
});

// Ensure DB is created and seed minimal data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    if (!db.Usuarios.Any())
    {
        var u1 = new Usuario { Nombre = "Alice", Email = "alice@example.com", Activo = true };
        var u2 = new Usuario { Nombre = "Bob", Email = "bob@example.com", Activo = true };
        db.Usuarios.AddRange(u1, u2);
        db.SaveChanges();

        db.Tareas.AddRange(
            new Tarea { UsuarioId = u1.Id, Titulo = "Comprar leche", Descripcion = "Ir al supermercado", Completada = false, FechaCreacion = DateTime.UtcNow },
            new Tarea { UsuarioId = u1.Id, Titulo = "Enviar informe", Descripcion = "Enviar informe semanal", Completada = true, FechaCreacion = DateTime.UtcNow },
            new Tarea { UsuarioId = u2.Id, Titulo = "Revisar PR", Descripcion = "Revisar pull request #42", Completada = false, FechaCreacion = DateTime.UtcNow }
        );
        db.SaveChanges();
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost4200");

// Endpoint para obtener todos los usuarios
app.MapGet("/api/usuarios", async (ApplicationDbContext db) =>
{
    var usuarios = await db.Usuarios.ToListAsync();
    return Results.Ok(usuarios);
});

// Endpoint para obtener tareas-usuarios desde la tabla materializada dbo.TareaUsuario
app.MapGet("/api/tareas-usuarios", async (ApplicationDbContext db) =>
{
    var filas = await db.TareasUsuarios
        .OrderBy(x => x.Nombre)
        .ThenBy(x => x.Id)
        .Select(x => new {
            x.Nombre,
            x.Email,
            x.Titulo,
            x.Completada,
            x.FechaVencimiento
        })
        .ToListAsync();

    return Results.Ok(filas);
});

// Return all tareas with the usuario's nombre
app.MapGet("/tasks", async (ApplicationDbContext db) =>
{
    var tasks = await db.Tareas
                        .Include(t => t.Usuario)
                        .Select(t => new {
                            t.Id,
                            t.UsuarioId,
                            t.Titulo,
                            t.Descripcion,
                            t.Completada,
                            t.FechaCreacion,
                            t.FechaVencimiento,
                            UsuarioNombre = t.Usuario != null ? t.Usuario.Nombre : string.Empty
                        })
                        .ToListAsync();
    return Results.Ok(tasks);
});

app.Run();
