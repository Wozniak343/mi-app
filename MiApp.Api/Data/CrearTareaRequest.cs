namespace MiApp.Api.Data;

// request para crear una tarea; trae los campos básicos del usuario y la tarea. si falta algo, se añade después.

public record CrearTareaRequest(
    string Nombre,
    string Email,
    string Titulo,
    string? Descripcion,
    DateTime? FechaVencimiento
);
