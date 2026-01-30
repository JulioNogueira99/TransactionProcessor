using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Infrastructure.Context;

namespace TransactionProcessor.Infrastructure.Locking;

public sealed class SqlServerAccountLock : IAccountLock
{
    private readonly AppDbContext _db;

    public SqlServerAccountLock(AppDbContext db) => _db = db;

    public async Task AcquireAsync(Guid accountId, CancellationToken ct)
    {
        var resource = $"account:{accountId:D}";
        const int timeoutMs = 10_000;

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = @"
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = @timeout;
            SELECT @result;
            ";

        // Garante que o comando usa a transação atual do EF
        var currentTx = _db.Database.CurrentTransaction;
        if (currentTx is not null)
            cmd.Transaction = currentTx.GetDbTransaction();

        var pResource = new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource };
        var pTimeout = new SqlParameter("@timeout", SqlDbType.Int) { Value = timeoutMs };

        cmd.Parameters.Add(pResource);
        cmd.Parameters.Add(pTimeout);

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var result = scalar is null ? -999 : Convert.ToInt32(scalar);

        // >= 0 = sucesso; < 0 = falha (timeout/cancel/etc)
        if (result < 0)
            throw new TimeoutException($"Could not acquire account lock (sp_getapplock result={result}).");
    }

    private sealed class AppLockResult
    {
        public int Result { get; set; }
    }
}
