using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Helpers;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Application.Outbox;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Enums;
using TransactionProcessor.Domain.Exceptions;

namespace TransactionProcessor.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IOutboxStore _outboxStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionService> _logger;
    private readonly IAccountLock _accountLock;

    public TransactionService(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IOutboxStore outboxStore,
        IUnitOfWork unitOfWork,
        ILogger<TransactionService> logger,
        IAccountLock accountLock)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _outboxStore = outboxStore;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _accountLock = accountLock;
    }

    public async Task<TransactionResultDto> ProcessTransactionAsync(CreateTransactionDto dto, CancellationToken ct)
    {
        var existing = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, ct);
        if (existing != null)
        {
            var account = await _accountRepository.GetByIdAsync(existing.AccountId, ct);
            return MapToResult(existing, account);
        }


        if (!Enum.TryParse<TransactionType>(dto.Operation, true, out var type))
            return Fail($"Unknown operation: {dto.Operation}");

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(async ct2 =>
                {
                    await using var tx = await _unitOfWork.BeginTransactionAsync(ct2);

                    await _accountLock.AcquireAsync(dto.AccountId, ct2);

                    var account = await _accountRepository.GetByIdAsync(dto.AccountId, ct);
                    if (account is null) return Fail("Account not found");

                    var transaction = new Transaction(account.Id, type, dto.Amount, dto.Currency, dto.ReferenceId);

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
                    await _outboxStore.AddAsync(BuildOutboxMessage(transaction, dto, account), ct);

                    await _unitOfWork.CommitAsync(ct2);
                    await tx.CommitAsync(ct2);

                    _logger.LogInformation("Transaction processed: reference_id={ReferenceId} transaction_id={TransactionId} status={Status} account_id={AccountId}",
                         dto.ReferenceId, transaction.Id, transaction.Status, dto.AccountId);
                    return MapToResult(transaction, account);
                }, ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                _unitOfWork.ClearTracking();
                if (attempt == maxAttempts) throw;

                var delayMs = (int)Math.Min(2000, 200 * Math.Pow(2, attempt));
                await Task.Delay(delayMs, ct);
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
    private static OutboxMessageData BuildOutboxMessage(Transaction transaction, CreateTransactionDto dto, Account account)
    {
        var payload = JsonSerializer.Serialize(new
        {
            transaction_id = transaction.Id,
            reference_id = dto.ReferenceId,
            account_id = dto.AccountId,
            operation = dto.Operation,
            amount = dto.Amount,
            currency = dto.Currency,
            status = transaction.Status.ToString().ToLowerInvariant(),
            balance = account.Balance,
            reserved_balance = account.ReservedBalance,
            available_balance = account.AvailableBalance,
            error_message = transaction.ErrorMessage,
            occurred_at = DateTimeOffset.UtcNow
        });

        return new OutboxMessageData(
            Id: Guid.NewGuid(),
            Type: "transaction.processed",
            Payload: payload,
            OccurredAt: DateTimeOffset.UtcNow,
            Attempts: 0,
            NextAttemptAt: null,
            ProcessedAt: null,
            LastError: null
        );
    }

    private static TransactionResultDto Fail(string msg) =>
        new(Guid.Empty.ToString(), "failed", 0, 0, 0, DateTime.UtcNow, msg);

    private static TransactionResultDto MapToResult(Transaction transaction, Account? account)
    {
        return new TransactionResultDto(
            transaction.Id.ToString(),
            transaction.Status.ToString().ToLowerInvariant(),
            account?.Balance ?? 0,
            account?.ReservedBalance ?? 0,
            account?.AvailableBalance ?? 0,
            transaction.CreatedAt,
            transaction.ErrorMessage
        );
    }
    #endregion
}
