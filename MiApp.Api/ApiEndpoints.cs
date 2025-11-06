using MiApp.Api.Data;
using System.Linq;

namespace MiApp.Api;

public static class ApiEndpoints
{
    public static void Register(WebApplication app)
    {
        // endpoint de prueba para ver si hay conexión a la base de datos
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


        // endpoint para traer filas de la tabla dbo.Tareas; esto reemplaza la vista vieja de Usuario/Tarea
        app.MapGet("/api/tareas-usuarios", async (string? titulo, bool? estado, DbRepository repo) =>
        {
            // uso el repo que ya maneja filtros opcionales
            var filas = await repo.GetTareasAsync(titulo?.Trim(), estado);
            var proy = filas.Select(x => new { x.Id, x.Titulo, x.Descripcion, Estado = x.Estado, FechaCreacion = x.FechaCreacion, x.FechaVencimiento }).ToList();
            return Results.Ok(proy);
        });

        // endpoint para crear una fila en Tareas usando el INSERT que nos pidieron
        app.MapPost("/api/tareas-usuarios", async (CrearTareaRowRequest req, DbRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Titulo))
                return Results.BadRequest(new { error = "Titulo es requerido" });

            try
            {
                var created = await repo.CreateTareaRowAsync(req.Titulo.Trim(), req.Descripcion, req.FechaVencimiento);
                if (created == null)
                    return Results.BadRequest(new { error = "No se pudo crear la tarea" });

                // devuelvo Created con la fila insertada
                return Results.Created($"/api/tareas-usuarios/{created.Id}", created);
            }
            catch (InvalidOperationException inv)
            {
                return Results.BadRequest(new { error = inv.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: $"Error interno: {ex.Message}", statusCode: 500);
            }
        });

        // endpoint para actualizar una tarea por id
        app.MapPut("/api/tareas-usuarios/{id}", async (int id, CrearTareaRowRequest req, DbRepository repo) =>
        {
            if (id <= 0)
                return Results.BadRequest(new { error = "Id inválido" });

            if (string.IsNullOrWhiteSpace(req.Titulo))
                return Results.BadRequest(new { error = "Titulo es requerido" });

            try
            {
                var updated = await repo.UpdateTareaRowAsync(id, req.Titulo.Trim(), req.Descripcion, req.FechaVencimiento);
                if (updated == null)
                    return Results.NotFound(new { error = $"No existe tarea con id {id}" });

                return Results.Ok(updated);
            }
            catch (InvalidOperationException inv)
            {
                return Results.BadRequest(new { error = inv.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: $"Error interno: {ex.Message}", statusCode: 500);
            }
        });

        // endpoint para borrar una tarea por id
        app.MapDelete("/api/tareas-usuarios/{id}", async (int id, DbRepository repo) =>
        {
            if (id <= 0)
                return Results.BadRequest(new { error = "Id inválido" });

            try
            {
                var deleted = await repo.DeleteTareaAsync(id);
                if (!deleted)
                    return Results.NotFound(new { error = $"No existe tarea con id {id}" });

                return Results.Ok(new { deleted = true });
            }
            catch (InvalidOperationException inv)
            {
                return Results.BadRequest(new { error = inv.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: $"Error interno: {ex.Message}", statusCode: 500);
            }
        });

    }
}
