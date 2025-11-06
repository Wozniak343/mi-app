using Microsoft.EntityFrameworkCore;
using System.Data;

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

    // Get tasks that match the exact title (parameterized)
    public Task<List<TareaRow>> GetTareasByTituloAsync(string titulo)
    {
        // Keep existing exact-title helper
        return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Titulo = {titulo} ORDER BY FechaCreacion, Id").ToListAsync();
    }

    // General fetch with optional filters: titulo (exact) and estado (bit)
    public Task<List<TareaRow>> GetTareasAsync(string? titulo, bool? estado)
    {
        if (string.IsNullOrWhiteSpace(titulo) && !estado.HasValue)
        {
            return GetTareasRowsAsync();
        }

        // Build SQL with appropriate WHERE clauses and parameterize via FromSqlInterpolated
        if (!string.IsNullOrWhiteSpace(titulo) && estado.HasValue)
        {
            return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Titulo = {titulo} AND Estado = {estado.Value} ORDER BY FechaCreacion, Id").ToListAsync();
        }
        else if (!string.IsNullOrWhiteSpace(titulo))
        {
            return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Titulo = {titulo} ORDER BY FechaCreacion, Id").ToListAsync();
        }
        else // estado.HasValue only
        {
            return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Estado = {estado.Value} ORDER BY FechaCreacion, Id").ToListAsync();
        }
    }

    // Create a new Tarea row using the exact INSERT pattern requested and return the inserted row.
    public async Task<TareaRow?> CreateTareaRowAsync(string titulo, string? descripcion, DateTime? fechaVencimiento)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new InvalidOperationException("Titulo es requerido.");

        // Normalize title
        var tituloTrim = titulo.Trim();

        // Check for duplicate title (exact match)
        var exists = await _db.TareasRows.AnyAsync(t => t.Titulo == tituloTrim);
        if (exists)
            throw new InvalidOperationException("Ya existe una tarea con el mismo título.");

        // Validate fechaVencimiento when provided (must be >= today)
        if (fechaVencimiento.HasValue)
        {
            var today = DateTime.Now.Date;
            if (fechaVencimiento.Value.Date < today)
                throw new InvalidOperationException("La fecha de vencimiento debe ser mayor o igual a la fecha actual.");
        }

    // Use a DB transaction and OUTPUT to return the inserted row
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        // Use a native DbTransaction on the connection so we can attach it to the DbCommand
        await using var dbTrans = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = dbTrans;
            if (fechaVencimiento.HasValue)
            {
                cmd.CommandText = @"INSERT INTO dbo.Tareas (Titulo, Descripcion, FechaVencimiento)
OUTPUT INSERTED.Id, INSERTED.Titulo, INSERTED.Descripcion, INSERTED.Estado, INSERTED.FechaCreacion, INSERTED.FechaVencimiento
VALUES (@titulo, @descripcion, @fechaVencimiento);";
            }
            else
            {
                cmd.CommandText = @"INSERT INTO dbo.Tareas (Titulo, Descripcion, FechaVencimiento)
OUTPUT INSERTED.Id, INSERTED.Titulo, INSERTED.Descripcion, INSERTED.Estado, INSERTED.FechaCreacion, INSERTED.FechaVencimiento
VALUES (@titulo, @descripcion, DATEADD(DAY,7,CAST(SYSDATETIME() AS DATE)));";
            }

            var pTitulo = cmd.CreateParameter();
            pTitulo.ParameterName = "@titulo";
            pTitulo.Value = tituloTrim;
            cmd.Parameters.Add(pTitulo);

            var pDesc = cmd.CreateParameter();
            pDesc.ParameterName = "@descripcion";
            pDesc.Value = (object?)descripcion ?? DBNull.Value;
            cmd.Parameters.Add(pDesc);


            if (fechaVencimiento.HasValue)
            {
                var pFecha = cmd.CreateParameter();
                pFecha.ParameterName = "@fechaVencimiento";
                pFecha.Value = fechaVencimiento.Value.Date;
                cmd.Parameters.Add(pFecha);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            TareaRow? result = null;
            if (await reader.ReadAsync())
            {
                result = new TareaRow
                {
                    Id = reader.GetInt32(0),
                    Titulo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Estado = !reader.IsDBNull(3) && reader.GetBoolean(3),
                    FechaCreacion = reader.GetDateTime(4),
                    FechaVencimiento = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                };
            }

            await reader.CloseAsync();
            await dbTrans.CommitAsync();
            return result;
        }
        catch
        {
            try { await dbTrans.RollbackAsync(); } catch { }
            throw;
        }
    }

    // Update an existing Tarea by id. Returns the updated row, or null if not found.
    public async Task<TareaRow?> UpdateTareaRowAsync(int id, string titulo, string? descripcion, DateTime? fechaVencimiento)
    {
        if (id <= 0) throw new InvalidOperationException("Id inválido.");
        if (string.IsNullOrWhiteSpace(titulo)) throw new InvalidOperationException("Titulo es requerido.");

        var tituloTrim = titulo.Trim();

        // Check duplicate title excluding this id
        var exists = await _db.TareasRows.AnyAsync(t => t.Titulo == tituloTrim && t.Id != id);
        if (exists) throw new InvalidOperationException("Ya existe una tarea con el mismo título.");

        // If fecha provided, validate
        if (fechaVencimiento.HasValue)
        {
            var today = DateTime.Now.Date;
            if (fechaVencimiento.Value.Date < today)
                throw new InvalidOperationException("La fecha de vencimiento debe ser mayor o igual a la fecha actual.");
        }

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        await using var dbTrans = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = dbTrans;
            cmd.CommandText = @"UPDATE dbo.Tareas
SET Titulo = @titulo,
    Descripcion = @descripcion,
    FechaVencimiento = CASE WHEN @fechaVencimiento IS NULL THEN FechaVencimiento ELSE @fechaVencimiento END
OUTPUT INSERTED.Id, INSERTED.Titulo, INSERTED.Descripcion, INSERTED.Estado, INSERTED.FechaCreacion, INSERTED.FechaVencimiento
WHERE Id = @id;";

            var pId = cmd.CreateParameter();
            pId.ParameterName = "@id";
            pId.Value = id;
            cmd.Parameters.Add(pId);

            var pTitulo = cmd.CreateParameter();
            pTitulo.ParameterName = "@titulo";
            pTitulo.Value = tituloTrim;
            cmd.Parameters.Add(pTitulo);

            var pDesc = cmd.CreateParameter();
            pDesc.ParameterName = "@descripcion";
            pDesc.Value = (object?)descripcion ?? DBNull.Value;
            cmd.Parameters.Add(pDesc);

            var pFecha = cmd.CreateParameter();
            pFecha.ParameterName = "@fechaVencimiento";
            pFecha.Value = (object?)fechaVencimiento?.Date ?? DBNull.Value;
            cmd.Parameters.Add(pFecha);

            await using var reader = await cmd.ExecuteReaderAsync();
            TareaRow? result = null;
            if (await reader.ReadAsync())
            {
                result = new TareaRow
                {
                    Id = reader.GetInt32(0),
                    Titulo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Estado = !reader.IsDBNull(3) && reader.GetBoolean(3),
                    FechaCreacion = reader.GetDateTime(4),
                    FechaVencimiento = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                };
            }

            await reader.CloseAsync();
            await dbTrans.CommitAsync();
            return result;
        }
        catch
        {
            try { await dbTrans.RollbackAsync(); } catch { }
            throw;
        }
    }
}
