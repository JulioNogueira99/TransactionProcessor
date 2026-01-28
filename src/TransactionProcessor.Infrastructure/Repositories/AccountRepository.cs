using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Infrastructure.Context;

namespace TransactionProcessor.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            await _context.Accounts.AddAsync(account, cancellationToken);
        }

        public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public void Update(Account account)
        {
            _context.Accounts.Update(account);
        }
    }
}
