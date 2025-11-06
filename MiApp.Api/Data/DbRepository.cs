using Microsoft.EntityFrameworkCore;

namespace MiApp.Api.Data;

public class DbRepository
{
    private readonly ApplicationDbContext _db;

    public DbRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<bool> TestConnectionAsync() => _db.Database.CanConnectAsync();
    // New: read rows from the dbo.Tareas table (the table the UI shows now)
    public Task<List<TareaRow>> GetTareasRowsAsync()
    {
        var sql = @"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas ORDER BY FechaCreacion, Id";
        return _db.TareasRows.FromSqlRaw(sql).ToListAsync();
    }
}
