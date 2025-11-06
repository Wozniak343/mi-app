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


        // Endpoint para obtener las filas desde la nueva tabla dbo.Tareas (reemplaza la vista previa de Usuario/Tarea)
        app.MapGet("/api/tareas-usuarios", async (DbRepository repo) =>
        {
            // NOTE: keeping the same route to minimize frontend changes; it now returns rows from dbo.Tareas
            var filas = await repo.GetTareasRowsAsync();
            var proy = filas.Select(x => new { x.Id, x.Titulo, x.Descripcion, Estado = x.Estado, FechaCreacion = x.FechaCreacion, x.FechaVencimiento }).ToList();
            return Results.Ok(proy);
        });

        // Note: endpoints related to Usuario/Tarea/TareaUsuario were removed per request.
    }
}
