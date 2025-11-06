using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiApp.Api.Data;

[Table("Tarea")]
public class Tarea
{
    [Key]
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Titulo { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool Completada { get; set; } = false;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    // Navigation property
    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }
}
