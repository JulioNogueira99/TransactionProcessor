using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Entitities;

namespace TransactionProcessor.Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(Account account, CancellationToken cancellationToken);
        void Update(Account account);
    }
}
