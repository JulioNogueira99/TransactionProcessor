using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Interfaces;
using TransactionProcessor.Infrastructure.Context;

namespace TransactionProcessor.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            await _context.Transactions.AddAsync(transaction, cancellationToken);
        }

        public async Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken ct)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.ReferenceId == referenceId, ct);
        }
    }
}
