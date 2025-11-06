using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace MiApp.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // aquí representa la nueva tabla dbo.Tareas (creo que es la que se muestra en la tabla principal de la interfaz)
    public DbSet<TareaRow> TareasRows { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // antes estaban las entidades Usuario, Tarea y TareaUsuario, pero se quitaron; ahora la app usa dbo.Tareas por medio de TareaRow

        modelBuilder.Entity<TareaRow>(eb =>
        {
            eb.ToTable("Tareas");
            eb.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
            eb.Property(x => x.Estado).HasDefaultValue(false);
            eb.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
        });
    }
}
