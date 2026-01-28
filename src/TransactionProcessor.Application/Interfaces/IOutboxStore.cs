using TransactionProcessor.Application.Outbox;

namespace TransactionProcessor.Application.Interfaces;

public interface IOutboxStore
{
    Task AddAsync(OutboxMessageData message, CancellationToken ct);
    Task<IReadOnlyList<OutboxMessageData>> GetPendingAsync(int take, CancellationToken ct);
    Task MarkProcessedAsync(Guid id, DateTimeOffset processedAt, CancellationToken ct);
    Task MarkFailedAsync(Guid id, int attempts, DateTimeOffset nextAttemptAt, string error, CancellationToken ct);
}