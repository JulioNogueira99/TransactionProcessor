using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Infrastructure.Mappings
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(x => x.Id);

            // Alta precisão para dinheiro (18 dígitos, 2 ou 4 decimais)
            builder.Property(x => x.Balance)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.ReservedBalance)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.CreditLimit)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.Version)
                .IsConcurrencyToken()
                .IsRequired();
        }
    }
}
