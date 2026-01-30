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
        // Lock por recurso: "account:{id}"
        var resource = $"account:{accountId:D}";

        // LockOwner='Transaction' prende o lock até commit/rollback
        // LockTimeout = 10s (ajuste se quiser)
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            SELECT @result;",
            ct);

        // Observação: ExecuteSqlInterpolatedAsync retorna número de linhas, não o @result.
        // Então, para validar o retorno corretamente, o ideal é usar FromSql e ler o valor.
        // Pra manter simples e 100% correto, use o método abaixo:
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

        // >= 0 = sucesso; < 0 = falha (timeout/cancel/etc)
        if (result < 0)
            throw new TimeoutException($"Could not acquire account lock (sp_getapplock result={result}).");
    }

    // Tipo “shadow” só pra ler o resultado do SELECT
    private sealed class AppLockResult
    {
        public int Result { get; set; }
    }
}
