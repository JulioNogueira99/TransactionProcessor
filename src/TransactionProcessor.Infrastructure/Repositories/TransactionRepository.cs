using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;
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

        public async Task<Transaction?> GetByReferenceIdAsync(string referenceId, byte leg, CancellationToken ct)
        {
            return await _context.Transactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReferenceId == referenceId && x.Leg == leg, ct);
        }

        public async Task<List<Transaction>> GetAllByReferenceIdAsync(string referenceId, CancellationToken ct)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(x => x.ReferenceId == referenceId)
                .OrderBy(x => x.Leg)
                .ToListAsync(ct);
        }
    }

}
