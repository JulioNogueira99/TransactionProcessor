namespace TransactionProcessor.Application.Interfaces;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
    void ClearTracking();

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct);

    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
