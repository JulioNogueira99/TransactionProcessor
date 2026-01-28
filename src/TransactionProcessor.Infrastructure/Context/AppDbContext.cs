using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Infrastructure.Outbox;

namespace TransactionProcessor.Infrastructure.Context
{
    public class AppDbContext : DbContext, IUnitOfWork
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
            => await SaveChangesAsync(cancellationToken);

        public void ClearTracking()
            => ChangeTracker.Clear();
    }
}
