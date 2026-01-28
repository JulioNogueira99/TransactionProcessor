using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Entitities;

namespace TransactionProcessor.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction, CancellationToken cancellationToken);

        Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken);
    }
}
