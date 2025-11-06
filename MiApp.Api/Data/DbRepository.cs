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

    public Task<List<Usuario>> GetUsuariosAsync() => _db.Usuarios.ToListAsync();

    public async Task<int> CreateTareaAsync(CrearTareaRequest request)
    {
        // Buscar o crear usuario
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (usuario == null)
        {
            usuario = new Usuario
            {
                Nombre = request.Nombre,
                Email = request.Email,
                Activo = true
            };
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
        }

        var nuevaTarea = new Tarea
        {
            UsuarioId = usuario.Id,
            Titulo = request.Titulo,
            Descripcion = request.Descripcion,
            Completada = false,
            FechaCreacion = DateTime.UtcNow,
            FechaVencimiento = request.FechaVencimiento
        };

        _db.Tareas.Add(nuevaTarea);
        await _db.SaveChangesAsync();

        // Actualizar tabla materializada
        await _db.Database.ExecuteSqlRawAsync(@"
            DELETE FROM dbo.TareaUsuario;
            INSERT INTO dbo.TareaUsuario (Nombre, Email, Titulo, Completada, FechaVencimiento)
            SELECT u.Nombre, u.Email, t.Titulo, t.Completada, t.FechaVencimiento
            FROM dbo.Tarea t
            JOIN dbo.Usuario u ON u.Id = t.UsuarioId;
        ");

        return nuevaTarea.Id;
    }

    public Task<List<object>> GetTareasUsuariosAsync()
    {
        return _db.TareasUsuarios
            .OrderBy(x => x.Nombre)
            .ThenBy(x => x.Id)
            .Select(x => new object[] { x.Nombre, x.Email, x.Titulo, x.Completada, x.FechaVencimiento })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(arr => new
            {
                Nombre = ((object[])arr)[0],
                Email = ((object[])arr)[1],
                Titulo = ((object[])arr)[2],
                Completada = ((object[])arr)[3],
                FechaVencimiento = ((object[])arr)[4]
            } as object).ToList());
    }

    // Simpler typed version used by the API
    // Use explicit SQL to select the exact columns and ordering requested.
    public Task<List<TareaUsuario>> GetTareaUsuarioRowsAsync()
    {
        // Raw SQL matching the user's request:
        var sql = @"SELECT Id, Nombre, Email, Titulo, Completada, FechaVencimiento FROM dbo.TareaUsuario ORDER BY Nombre, Id";
        return _db.TareasUsuarios.FromSqlRaw(sql).ToListAsync();
    }

    public Task<List<object>> GetTasksWithUserNameAsync()
    {
        return _db.Tareas
            .Include(t => t.Usuario)
            .Select(t => new object[] { t.Id, t.UsuarioId, t.Titulo, t.Descripcion, t.Completada, t.FechaCreacion, t.FechaVencimiento, t.Usuario != null ? t.Usuario.Nombre : string.Empty })
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(arr => new
            {
                Id = ((object[])arr)[0],
                UsuarioId = ((object[])arr)[1],
                Titulo = ((object[])arr)[2],
                Descripcion = ((object[])arr)[3],
                Completada = ((object[])arr)[4],
                FechaCreacion = ((object[])arr)[5],
                FechaVencimiento = ((object[])arr)[6],
                UsuarioNombre = ((object[])arr)[7]
            } as object).ToList());
    }
}
