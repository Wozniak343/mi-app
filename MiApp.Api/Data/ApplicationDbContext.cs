using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace MiApp.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Represents the new table dbo.Tareas (used for the main UI table)
    public DbSet<TareaRow> TareasRows { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Previous entities Usuario, Tarea and TareaUsuario removed - application now uses dbo.Tareas via TareaRow

        modelBuilder.Entity<TareaRow>(eb =>
        {
            eb.ToTable("Tareas");
            eb.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
            eb.Property(x => x.Estado).HasDefaultValue(false);
            eb.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
        });
    }
}
