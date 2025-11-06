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
    // nuevo: lee filas de la tabla dbo.Tareas (es la tabla que muestra la UI ahora)
    public Task<List<TareaRow>> GetTareasRowsAsync()
    {
        var sql = @"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas ORDER BY FechaCreacion, Id";
        return _db.TareasRows.FromSqlRaw(sql).ToListAsync();
    }

    // obtiene tareas que coinciden con el título exacto (parametrizado)
    public Task<List<TareaRow>> GetTareasByTituloAsync(string titulo)
    {
        // dejo el helper de título exacto
        return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Titulo = {titulo} ORDER BY FechaCreacion, Id").ToListAsync();
    }

    // consulta general con filtros opcionales: titulo (exacto) y estado (bit)
    public Task<List<TareaRow>> GetTareasAsync(string? titulo, bool? estado)
    {
        if (string.IsNullOrWhiteSpace(titulo) && !estado.HasValue)
        {
            return GetTareasRowsAsync();
        }

        // armo el SQL con WHERE según filtros y parametrizo con FromSqlInterpolated
        if (!string.IsNullOrWhiteSpace(titulo) && estado.HasValue)
        {
            return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Titulo = {titulo} AND Estado = {estado.Value} ORDER BY FechaCreacion, Id").ToListAsync();
        }
        else if (!string.IsNullOrWhiteSpace(titulo))
        {
            return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Titulo = {titulo} ORDER BY FechaCreacion, Id").ToListAsync();
        }
        else // solo estado.HasValue
        {
            return _db.TareasRows.FromSqlInterpolated($"SELECT Id, Titulo, Descripcion, Estado, FechaCreacion, FechaVencimiento FROM dbo.Tareas WHERE Estado = {estado.Value} ORDER BY FechaCreacion, Id").ToListAsync();
        }
    }

    // crea una fila en Tareas usando el INSERT exacto pedido y devuelve la fila insertada
    public async Task<TareaRow?> CreateTareaRowAsync(string titulo, string? descripcion, DateTime? fechaVencimiento)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new InvalidOperationException("Titulo es requerido.");

        // normalizo el título
        var tituloTrim = titulo.Trim();

        // verifico si existe un título igual (coincidencia exacta)
        var exists = await _db.TareasRows.AnyAsync(t => t.Titulo == tituloTrim);
        if (exists)
            throw new InvalidOperationException("Ya existe una tarea con el mismo título.");

        // valido fechaVencimiento si viene (debe ser >= hoy)
        if (fechaVencimiento.HasValue)
        {
            var today = DateTime.Now.Date;
            if (fechaVencimiento.Value.Date < today)
                throw new InvalidOperationException("La fecha de vencimiento debe ser mayor o igual a la fecha actual.");
        }

    // uso transacción y OUTPUT para devolver la fila insertada
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        // uso DbTransaction nativa de la conexión para asociarla al DbCommand
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

    // actualiza una tarea por id. devuelve la fila actualizada o null si no existe
    public async Task<TareaRow?> UpdateTareaRowAsync(int id, string titulo, string? descripcion, DateTime? fechaVencimiento)
    {
        if (id <= 0) throw new InvalidOperationException("Id inválido.");
        if (string.IsNullOrWhiteSpace(titulo)) throw new InvalidOperationException("Titulo es requerido.");

        var tituloTrim = titulo.Trim();

        // verifico título duplicado excluyendo este id
        var exists = await _db.TareasRows.AnyAsync(t => t.Titulo == tituloTrim && t.Id != id);
        if (exists) throw new InvalidOperationException("Ya existe una tarea con el mismo título.");

        // si viene fecha, la valido
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

    // elimina una tarea por id. devuelve true si borró una fila, false si no la encontró
    public async Task<bool> DeleteTareaAsync(int id)
    {
        if (id <= 0) throw new InvalidOperationException("Id inválido.");

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        await using var dbTrans = await conn.BeginTransactionAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = dbTrans;
            cmd.CommandText = "DELETE FROM dbo.Tareas WHERE Id = @id; SELECT @@ROWCOUNT;";

            var pId = cmd.CreateParameter();
            pId.ParameterName = "@id";
            pId.Value = id;
            cmd.Parameters.Add(pId);

            var result = await cmd.ExecuteScalarAsync();
            await dbTrans.CommitAsync();

            if (result is int rows)
                return rows > 0;

            // algunos proveedores devuelven long
            if (result is long l)
                return l > 0;

            return false;
        }
        catch
        {
            try { await dbTrans.RollbackAsync(); } catch { }
            throw;
        }
    }
}
