using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiApp.Api.Data;

[Table("TareaUsuario")]
public class TareaUsuario
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Titulo { get; set; } = null!;

    public bool Completada { get; set; }

    public DateTime? FechaVencimiento { get; set; }
}