using MiApp.Api.Data;

namespace MiApp.Api;

public static class ApiEndpoints
{
    public static void Register(WebApplication app)
    {
        // Test endpoint para verificar la conexión
        app.MapGet("/api/test-connection", async (DbRepository repo) =>
        {
            try
            {
                bool canConnect = await repo.TestConnectionAsync();
                return Results.Ok(new { connected = canConnect, message = "Conexión exitosa a la base de datos" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { connected = false, message = $"Error de conexión: {ex.Message}" });
            }
        });

        // Endpoint para obtener todos los usuarios
        app.MapGet("/api/usuarios", async (DbRepository repo) =>
        {
            var usuarios = await repo.GetUsuariosAsync();
            return Results.Ok(usuarios);
        });

        // Endpoint para crear nueva tarea
        app.MapPost("/api/crear-tarea", async (DbRepository repo, MiApp.Api.Data.CrearTareaRequest request) =>
        {
            try
            {
                var id = await repo.CreateTareaAsync(request);
                // Return the created tarea id
                return Results.Ok(new { mensaje = "Tarea creada exitosamente", tareaId = id });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"Error al crear tarea: {ex.Message}" });
            }
        });

        // Endpoint para obtener tareas-usuarios desde la tabla materializada dbo.TareaUsuario
        app.MapGet("/api/tareas-usuarios", async (DbRepository repo) =>
        {
            var filas = await repo.GetTareaUsuarioRowsAsync();
            var proy = filas.Select(x => new { x.Id, x.Nombre, x.Email, x.Titulo, x.Completada, x.FechaVencimiento }).ToList();
            return Results.Ok(proy);
        });

        // Return all tareas with the usuario's nombre
        app.MapGet("/tasks", async (DbRepository repo) =>
        {
            var tasks = await repo.GetTasksWithUserNameAsync();
            return Results.Ok(tasks);
        });
    }
}
