using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Wrap;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Helpers;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Enums;
using TransactionProcessor.Domain.Exceptions;

namespace TransactionProcessor.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    private readonly AsyncPolicyWrap<TransactionResultDto> _resilienceStrategy;

    private static AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public TransactionService(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;

        var retryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)));

        if (_circuitBreakerPolicy == null)
        {
            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }

        var fallbackPolicy = Policy<TransactionResultDto>
            .Handle<Exception>()
            .FallbackAsync(async (cancellationToken) =>
            {
                return new TransactionResultDto(
                    Guid.Empty.ToString(),
                    "failed",
                    0, 0, 0,
                    DateTime.UtcNow,
                    "Service is temporarily unavailable. Please try again later."
                );
            });

        _resilienceStrategy = Policy.WrapAsync(fallbackPolicy, _circuitBreakerPolicy.AsAsyncPolicy<TransactionResultDto>(), retryPolicy.AsAsyncPolicy<TransactionResultDto>());
    }

    public async Task<AccountResultDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken cancellationToken)
    {
        var account = new Account(dto.CreditLimit);
        await _accountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new AccountResultDto(account.Id, account.Balance, account.AvailableBalance, account.CreditLimit);
    }

    public async Task<AccountResultDto?> GetAccountAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null) return null;
        return new AccountResultDto(account.Id, account.Balance, account.AvailableBalance, account.CreditLimit);
    }

    public async Task<TransactionResultDto> ProcessTransactionAsync(CreateTransactionDto dto, CancellationToken ct)
    {
        var existing = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, ct);
        if (existing != null)
            return MapToResult(existing, existing.Account);

        if (!Enum.TryParse<TransactionType>(dto.Operation, true, out var type))
            return Fail($"Unknown operation: {dto.Operation}");

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            Transaction? transaction = null;

            try
            {
                var account = await _accountRepository.GetByIdAsync(dto.AccountId, ct);
                if (account is null) return Fail("Account not found");

                transaction = new Transaction(account.Id, type, dto.Amount, dto.Currency, dto.ReferenceId);

                try
                {
                    switch (type)
                    {
                        case TransactionType.Credit: account.Credit(dto.Amount); break;
                        case TransactionType.Debit: account.Debit(dto.Amount); break;
                        case TransactionType.Reserve: account.Reserve(dto.Amount); break;
                        case TransactionType.Capture: account.Capture(dto.Amount); break;
                        default: throw new DomainException("Operation not supported yet.");
                    }
                    transaction.MarkAsSuccess();
                }
                catch (DomainException ex)
                {
                    transaction.MarkAsFailed(ex.Message);
                }

                await _transactionRepository.AddAsync(transaction, ct);

                await _unitOfWork.CommitAsync(ct);

                return MapToResult(transaction, account);
            }
            catch (DbUpdateConcurrencyException)
            {
                _unitOfWork.ClearTracking();
                if (attempt == maxAttempts) throw;
            }
            catch (DbUpdateException ex) when (SqlServerErrors.IsUniqueViolation(ex))
            {
                var already = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, ct);
                if (already != null) return MapToResult(already, already.Account);

                throw;
            }
        }

        return Fail("Unexpected failure");
    }

    #region PRIVATE METHODS
    private static TransactionResultDto Fail(string msg) =>
        new(Guid.Empty.ToString(), "failed", 0, 0, 0, DateTime.UtcNow, msg);

    private TransactionResultDto MapToResult(Transaction transaction, Account? account)
    {
        return new TransactionResultDto(
            transaction.Id.ToString(),
            transaction.Status.ToString().ToLower(),
            account?.Balance ?? 0,
            account?.ReservedBalance ?? 0,
            account?.AvailableBalance ?? 0,
            transaction.CreatedAt,
            transaction.ErrorMessage
        );
    }
    #endregion
}