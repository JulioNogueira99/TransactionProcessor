using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionProcessor.Infrastructure.Outbox;

namespace TransactionProcessor.Infrastructure.Mappings;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();

        builder.Property(x => x.Attempts).IsRequired();

        builder.HasIndex(x => new { x.ProcessedAt, x.NextAttemptAt });
    }
}