namespace TransactionProcessor.Infrastructure.Outbox
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public DateTimeOffset OccurredAt { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }
        public int Attempts { get; set; }
        public DateTimeOffset? NextAttemptAt { get; set; }
        public string? LastError { get; set; }
    }
}
