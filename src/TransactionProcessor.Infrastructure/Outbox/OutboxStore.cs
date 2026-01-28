using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Application.Outbox;
using TransactionProcessor.Infrastructure.Context;

namespace TransactionProcessor.Infrastructure.Outbox;

public class OutboxStore : IOutboxStore
{
    private readonly AppDbContext _db;

    public OutboxStore(AppDbContext db) => _db = db;

    public async Task AddAsync(OutboxMessageData msg, CancellationToken ct)
    {
        await _db.OutboxMessages.AddAsync(new OutboxMessage
        {
            Id = msg.Id,
            Type = msg.Type,
            Payload = msg.Payload,
            OccurredAt = msg.OccurredAt,
            Attempts = msg.Attempts,
            NextAttemptAt = msg.NextAttemptAt,
            ProcessedAt = msg.ProcessedAt,
            LastError = msg.LastError
        }, ct);
    }

    public async Task<IReadOnlyList<OutboxMessageData>> GetPendingAsync(int take, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        return await _db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.OccurredAt)
            .Take(take)
            .Select(x => new OutboxMessageData(
                x.Id, x.Type, x.Payload, x.OccurredAt,
                x.Attempts, x.NextAttemptAt, x.ProcessedAt, x.LastError
            ))
            .ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(Guid id, DateTimeOffset processedAt, CancellationToken ct)
    {
        var entity = await _db.OutboxMessages.FirstAsync(x => x.Id == id, ct);
        entity.ProcessedAt = processedAt;
        entity.LastError = null;
        entity.NextAttemptAt = null;
    }

    public async Task MarkFailedAsync(Guid id, int attempts, DateTimeOffset nextAttemptAt, string error, CancellationToken ct)
    {
        var entity = await _db.OutboxMessages.FirstAsync(x => x.Id == id, ct);
        entity.Attempts = attempts;
        entity.NextAttemptAt = nextAttemptAt;
        entity.LastError = error;
    }
}
