using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;
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

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct)
        {
            var tx = await Database.BeginTransactionAsync(ct);
            return new EfUnitOfWorkTransaction(tx);
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
        {
            var strategy = Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () => await action(ct));
        }

        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
        {
            var strategy = Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () => await action(ct));
        }

        private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
        {
            private readonly IDbContextTransaction _tx;
            public EfUnitOfWorkTransaction(IDbContextTransaction tx) => _tx = tx;

            public Task CommitAsync(CancellationToken ct) => _tx.CommitAsync(ct);
            public Task RollbackAsync(CancellationToken ct) => _tx.RollbackAsync(ct);
            public ValueTask DisposeAsync() => _tx.DisposeAsync();
        }
    }
}
