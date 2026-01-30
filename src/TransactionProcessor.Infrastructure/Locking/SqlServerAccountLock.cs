using Microsoft.EntityFrameworkCore;
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

        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            SELECT @result;",
            ct);

        await EnsureLockAcquired(resource, ct);
    }

    private async Task EnsureLockAcquired(string resource, CancellationToken ct)
    {
        var result = await _db.Set<AppLockResult>()
            .FromSqlInterpolated($@"
                DECLARE @result int;
                EXEC @result = sp_getapplock
                    @Resource = {resource},
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;
                SELECT @result AS Result;")
            .AsNoTracking()
            .Select(x => x.Result)
            .FirstAsync(ct);

        if (result < 0)
            throw new TimeoutException($"Could not acquire account lock (sp_getapplock result={result}).");
    }

    private sealed class AppLockResult
    {
        public int Result { get; set; }
    }
}
