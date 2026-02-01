using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Infrastructure.Mappings
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Balance)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.ReservedBalance)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.CreditLimit)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.CustomerId).IsRequired();
            builder.HasIndex(x => x.CustomerId);
        }
    }
}
