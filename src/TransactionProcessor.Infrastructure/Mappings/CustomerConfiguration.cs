using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Infrastructure.Mappings
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ClientId)
                .HasMaxLength(32)
                .IsRequired();

            builder.HasIndex(x => x.ClientId)
                .IsUnique();
        }
    }
}
