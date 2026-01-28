using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Enums;
using TransactionProcessor.Domain.Exceptions;

namespace TransactionProcessor.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; private set; }
        public Guid AccountId { get; private set; }
        public TransactionType Type { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        public string ReferenceId { get; private set; }

        public TransactionStatus Status { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public virtual Account Account { get; private set; }

        protected Transaction() { }

        public Transaction(Guid accountId, TransactionType type, decimal amount, string currency, string referenceId)
        {
            if (amount <= 0) throw new DomainException("Transaction amount must be positive.");
            if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency is required.");
            if (string.IsNullOrWhiteSpace(referenceId)) throw new DomainException("ReferenceId is required for idempotency.");

            Id = Guid.NewGuid();
            AccountId = accountId;
            Type = type;
            Amount = amount;
            Currency = currency;
            ReferenceId = referenceId;
            Status = TransactionStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void MarkAsSuccess()
        {
            Status = TransactionStatus.Success;
        }

        public void MarkAsFailed(string error)
        {
            Status = TransactionStatus.Failed;
            ErrorMessage = error;
        }
    }
}
