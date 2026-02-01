using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Application.Interfaces
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
        Task<Transaction?> GetByReferenceIdAsync(string referenceId, byte leg, CancellationToken cancellationToken);
        Task<List<Transaction>> GetAllByReferenceIdAsync(string referenceId, CancellationToken cancellationToken);
    }

}
