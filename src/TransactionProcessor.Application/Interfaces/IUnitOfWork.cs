namespace TransactionProcessor.Application.Interfaces;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
    void ClearTracking();

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
