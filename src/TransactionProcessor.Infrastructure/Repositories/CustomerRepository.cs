using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Infrastructure.Context;

namespace TransactionProcessor.Infrastructure.Repositories
{
    public sealed class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _db;

        public CustomerRepository(AppDbContext db) => _db = db;

        public Task<Customer?> GetByClientIdAsync(string clientId, CancellationToken ct) =>
            _db.Customers.FirstOrDefaultAsync(x => x.ClientId == clientId, ct);

        public Task AddAsync(Customer customer, CancellationToken ct) =>
            _db.Customers.AddAsync(customer, ct).AsTask();
    }
}
