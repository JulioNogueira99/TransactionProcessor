using TransactionProcessor.Domain.Enums;
using TransactionProcessor.Domain.Exceptions;

namespace TransactionProcessor.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; private set; }

        public Guid CustomerId { get; private set; }
        public AccountStatus Status { get; private set; } = AccountStatus.Active;

        public decimal Balance { get; private set; }
        public decimal ReservedBalance { get; private set; }
        public decimal CreditLimit { get; private set; }
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
        public decimal CashAvailable => Balance - ReservedBalance;
        public decimal AvailableBalance => CashAvailable + CreditLimit;
        public decimal SpendingPower => AvailableBalance;

        protected Account() { }

        public Account(Guid customerId, decimal creditLimit)
        {
            if (customerId == Guid.Empty) throw new DomainException("CustomerId is required.");
            if (creditLimit < 0) throw new DomainException("Credit limit cannot be negative.");

            Id = Guid.NewGuid();
            CustomerId = customerId;
            Balance = 0;
            ReservedBalance = 0;
            CreditLimit = creditLimit;
            Status = AccountStatus.Active;
        }

        public void EnsureActive()
        {
            if (Status != AccountStatus.Active)
                throw new DomainException($"Account is {Status}.");
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
                throw new DomainException($"Insufficient funds. Available: {AvailableBalance}, Required: {amount}");

            Balance -= amount;
        }

        public void Reserve(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Reserve amount must be positive.");

            if (CashAvailable < amount)
                throw new DomainException("Insufficient funds for reservation.");

            ReservedBalance += amount;
        }

        public void Capture(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Capture amount must be positive.");

            if (ReservedBalance < amount)
                throw new DomainException("Capture amount exceeds reserved balance.");

            ReservedBalance -= amount;
            Balance -= amount;
        }
    }
}
