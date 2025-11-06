namespace MiApp.Api.Data;

// request para crear una fila de tarea en la tabla Tareas. Solo datos de la tarea; sin info de usuario.

public record CrearTareaRowRequest(
    string Titulo,
    string? Descripcion,
    DateTime? FechaVencimiento
);
