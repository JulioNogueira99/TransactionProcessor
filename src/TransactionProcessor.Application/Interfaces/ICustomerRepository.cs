using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Application.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByClientIdAsync(string clientId, CancellationToken ct);
        Task AddAsync(Customer customer, CancellationToken ct);
    }
}
