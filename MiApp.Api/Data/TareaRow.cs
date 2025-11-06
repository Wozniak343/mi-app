using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiApp.Api.Data;

[Table("Tareas")]
public class TareaRow
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaVencimiento { get; set; }
}
