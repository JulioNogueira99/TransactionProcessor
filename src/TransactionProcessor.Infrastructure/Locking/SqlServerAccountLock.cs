using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Infrastructure.Context;

namespace TransactionProcessor.Infrastructure.Locking;

public sealed class SqlServerAccountLock : IAccountLock
{
    private readonly AppDbContext _db;

    public SqlServerAccountLock(AppDbContext db) => _db = db;

    public async Task<IAsyncDisposable> AcquireAsync(Guid accountId, CancellationToken ct)
    {
        var resource = $"account:{accountId:D}";
        const int timeoutMs = 10_000;

        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        IDbContextTransaction? currentTx = _db.Database.CurrentTransaction;
        if (currentTx is null)
            throw new InvalidOperationException(
                "Account lock requires an active database transaction (LockOwner='Transaction'). Call BeginTransactionAsync before AcquireAsync."
            );

        DbTransaction dbTx = currentTx.GetDbTransaction();

        using (var cmd = conn.CreateCommand())
        {
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
            cmd.Transaction = dbTx;

            cmd.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource });
            cmd.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int) { Value = timeoutMs });

            var scalar = await cmd.ExecuteScalarAsync(ct);
            var result = scalar is null ? -999 : Convert.ToInt32(scalar);

            if (result < 0)
                throw new TimeoutException($"Could not acquire account lock (sp_getapplock result={result}).");
        }

        return new SqlAppLockHandle(conn, dbTx, resource);
    }

    private sealed class SqlAppLockHandle : IAsyncDisposable
    {
        private readonly DbConnection _conn;
        private readonly DbTransaction _tx;
        private readonly string _resource;
        private int _disposed;

        public SqlAppLockHandle(DbConnection conn, DbTransaction tx, string resource)
        {
            _conn = conn;
            _tx = tx;
            _resource = resource;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            try
            {
                if (_conn.State != ConnectionState.Open)
                    await _conn.OpenAsync(CancellationToken.None);

                using var cmd = _conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
                    DECLARE @result int;
                    EXEC @result = sp_releaseapplock
                        @Resource = @resource,
                        @LockOwner = 'Transaction';
                    SELECT @result;
                    ";
                cmd.Transaction = _tx;
                cmd.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = _resource });

                await cmd.ExecuteScalarAsync(CancellationToken.None);
            }
            catch
            {
            }
        }
    }
}
