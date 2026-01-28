using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionProcessor.Application.Outbox
{
    public sealed record OutboxMessageData(
        Guid Id,
        string Type,
        string Payload,
        DateTimeOffset OccurredAt,
        int Attempts,
        DateTimeOffset? NextAttemptAt,
        DateTimeOffset? ProcessedAt,
        string? LastError
    );
}
