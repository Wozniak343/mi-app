using MiApp.Api.Data;
using System.Linq;

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

        // Endpoint to create a new Tarea row following the exact INSERT pattern requested.
        app.MapPost("/api/tareas-usuarios", async (CrearTareaRowRequest req, DbRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Titulo))
                return Results.BadRequest(new { error = "Titulo es requerido" });

            var created = await repo.CreateTareaRowAsync(req.Titulo.Trim(), req.Descripcion);
            if (created == null)
                return Results.BadRequest(new { error = "No se pudo crear la tarea" });

            // Return Created with the inserted row
            return Results.Created($"/api/tareas-usuarios/{created.Id}", created);
        });

        // Note: endpoints related to Usuario/Tarea/TareaUsuario were removed per request.
    }
}
