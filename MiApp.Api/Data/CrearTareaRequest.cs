namespace MiApp.Api.Data;

public record CrearTareaRequest(
    string Nombre,
    string Email,
    string Titulo,
    string? Descripcion,
    DateTime? FechaVencimiento
);
