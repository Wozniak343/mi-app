using Microsoft.EntityFrameworkCore;

namespace MiApp.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Tarea> Tareas { get; set; } = null!;
    public DbSet<TareaUsuario> TareasUsuarios { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(eb =>
        {
            eb.ToTable("Usuario");
            eb.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_Usuario_Email");
            eb.Property(u => u.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Tarea>(eb =>
        {
            eb.ToTable("Tarea");
            eb.Property(t => t.Completada).HasDefaultValue(false);
            eb.Property(t => t.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
            eb.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_Tarea_Usuario");
        });

        modelBuilder.Entity<TareaUsuario>(eb =>
        {
            eb.ToTable("TareaUsuario");
        });
    }
}
