using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Domain.Exceptions;

namespace TransactionProcessor.Domain.Entitities
{
    public class Account
    {
        public Guid Id { get; private set; }
        public decimal Balance { get; private set; }
        public decimal ReservedBalance { get; private set; }
        public decimal CreditLimit { get; private set; }
        public uint Version { get; private set; }

        public decimal AvailableBalance => (Balance + CreditLimit) - ReservedBalance;

        protected Account() { }

        public Account(decimal creditLimit)
        {
            Id = Guid.NewGuid();
            Balance = 0;
            ReservedBalance = 0;
            CreditLimit = creditLimit;
            if (creditLimit < 0) throw new DomainException("Credit limit cannot be negative.");
        }

        public void Credit(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Credit amount must be positive.");
            Balance += amount;
        }

        public void Debit(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Debit amount must be positive.");

            if (AvailableBalance < amount)
            {
                throw new DomainException($"Insufficient funds. Available: {AvailableBalance}, Required: {amount}");
            }

            Balance -= amount;
        }

        public void Reserve(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Reserve amount must be positive.");

            if (AvailableBalance < amount)
            {
                throw new DomainException("Insufficient funds for reservation.");
            }

            ReservedBalance += amount;
        }

        public void Capture(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Capture amount must be positive.");

            if (ReservedBalance < amount)
            {
                throw new DomainException("Capture amount exceeds reserved balance.");
            }

            ReservedBalance -= amount;
            Balance -= amount;
        }
    }
}
