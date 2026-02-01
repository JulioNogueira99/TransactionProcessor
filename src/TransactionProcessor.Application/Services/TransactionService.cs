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
        if (dto is null) return Fail("Invalid request");

        var op = (dto.Operation ?? string.Empty).Trim();

        if (op.Equals("transfer", StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessTransferInternalAsync(dto, ct);
        }

        const byte leg = 0;

        var existing = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, leg, ct);
        if (existing is not null)
        {
            var accExisting = await _accountRepository.GetByIdAsync(existing.AccountId, ct);
            return MapToResult(existing, accExisting);
        }

        if (!Enum.TryParse<TransactionType>(op, true, out var type))
            return Fail($"Unknown operation: {dto.Operation}");

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(async ct2 =>
                {
                    await using var tx = await _unitOfWork.BeginTransactionAsync(ct2);
                    await using var _lock = await _accountLock.AcquireAsync(dto.AccountId, ct2);

                    var account = await _accountRepository.GetByIdAsync(dto.AccountId, ct2);
                    if (account is null) return Fail("Account not found");

                    var transaction = new Transaction(
                        accountId: account.Id,
                        type: type,
                        amount: dto.Amount,
                        currency: dto.Currency,
                        referenceId: dto.ReferenceId,
                        leg: leg,
                        counterpartyAccountId: null
                    );

                    try
                    {
                        account.EnsureActive();

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

                    await _transactionRepository.AddAsync(transaction, ct2);
                    await _outboxStore.AddAsync(BuildOutboxMessage(transaction, dto, account), ct2);

                    await _unitOfWork.CommitAsync(ct2);
                    await tx.CommitAsync(ct2);

                    _logger.LogInformation(
                        "Transaction processed: reference_id={ReferenceId} transaction_id={TransactionId} status={Status} account_id={AccountId}",
                        dto.ReferenceId, transaction.Id, ToApiStatus(transaction.Status), dto.AccountId);

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
                var already = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, leg, ct);
                if (already is not null)
                {
                    var acc = await _accountRepository.GetByIdAsync(already.AccountId, ct);
                    return MapToResult(already, acc);
                }

                throw;
            }
        }

        return Fail("Unexpected failure");
    }

    private async Task<TransactionResultDto> ProcessTransferInternalAsync(CreateTransactionDto dto, CancellationToken ct)
    {
        if (dto.DestinationAccountId is null)
            return Fail("destination_account_id is required for transfer.");

        var fromId = dto.AccountId;
        var toId = dto.DestinationAccountId.Value;

        if (fromId == toId)
            return Fail("Cannot transfer to the same account.");

        const byte debitLeg = 1;
        const byte creditLeg = 2;

        var existingDebit = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, debitLeg, ct);
        var existingCredit = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, creditLeg, ct);

        if (existingDebit is not null && existingCredit is not null)
        {
            var accFrom = await _accountRepository.GetByIdAsync(existingDebit.AccountId, ct);
            return MapToResult(existingDebit, accFrom);
        }

        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteAsync(async ct2 =>
                {
                    await using var tx = await _unitOfWork.BeginTransactionAsync(ct2);

                    var (first, second) = OrderAccounts(fromId, toId);
                    await using var _l1 = await _accountLock.AcquireAsync(first, ct2);
                    await using var _l2 = await _accountLock.AcquireAsync(second, ct2);

                    var from = await _accountRepository.GetByIdAsync(fromId, ct2);
                    var to = await _accountRepository.GetByIdAsync(toId, ct2);

                    if (from is null || to is null)
                        return Fail("Account not found");

                    var debitTx = new Transaction(
                        accountId: from.Id,
                        type: TransactionType.Debit,
                        amount: dto.Amount,
                        currency: dto.Currency,
                        referenceId: dto.ReferenceId,
                        leg: debitLeg,
                        counterpartyAccountId: to.Id
                    );

                    var creditTx = new Transaction(
                        accountId: to.Id,
                        type: TransactionType.Credit,
                        amount: dto.Amount,
                        currency: dto.Currency,
                        referenceId: dto.ReferenceId,
                        leg: creditLeg,
                        counterpartyAccountId: from.Id
                    );

                    try
                    {
                        from.EnsureActive();
                        to.EnsureActive();

                        from.Debit(dto.Amount);
                        to.Credit(dto.Amount);

                        debitTx.MarkAsSuccess();
                        creditTx.MarkAsSuccess();
                    }
                    catch (DomainException ex)
                    {
                        debitTx.MarkAsFailed(ex.Message);
                        creditTx.MarkAsFailed(ex.Message);
                    }

                    await _transactionRepository.AddAsync(debitTx, ct2);
                    await _transactionRepository.AddAsync(creditTx, ct2);

                    await _outboxStore.AddAsync(BuildTransferOutboxMessage(debitTx, creditTx, dto, from, to), ct2);

                    await _unitOfWork.CommitAsync(ct2);
                    await tx.CommitAsync(ct2);

                    _logger.LogInformation(
                        "Transfer processed: reference_id={ReferenceId} debit_tx={DebitTx} credit_tx={CreditTx} status={Status} from={From} to={To}",
                        dto.ReferenceId, debitTx.Id, creditTx.Id, ToApiStatus(debitTx.Status), fromId, toId);

                    return MapToResult(debitTx, from);
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
                var d = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, debitLeg, ct);
                var c = await _transactionRepository.GetByReferenceIdAsync(dto.ReferenceId, creditLeg, ct);

                if (d is not null && c is not null)
                {
                    var accFrom = await _accountRepository.GetByIdAsync(d.AccountId, ct);
                    return MapToResult(d, accFrom);
                }

                throw;
            }
        }

        return Fail("Unexpected failure");
    }

    private static (Guid first, Guid second) OrderAccounts(Guid a, Guid b)
        => a.CompareTo(b) <= 0 ? (a, b) : (b, a);

    private static string ToApiStatus(TransactionStatus status)
        => status switch
        {
            TransactionStatus.Pending => "pending",
            TransactionStatus.Success => "success",
            TransactionStatus.Failed => "failed",
            _ => "unknown"
        };

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
            status = ToApiStatus(transaction.Status),
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

    private static OutboxMessageData BuildTransferOutboxMessage(
        Transaction debitTx,
        Transaction creditTx,
        CreateTransactionDto dto,
        Account from,
        Account to)
    {
        var payload = JsonSerializer.Serialize(new
        {
            reference_id = dto.ReferenceId,
            operation = "transfer",
            amount = dto.Amount,
            currency = dto.Currency,
            status = ToApiStatus(debitTx.Status),
            from_account_id = from.Id,
            to_account_id = to.Id,
            legs = new[]
            {
                new
                {
                    transaction_id = debitTx.Id,
                    account_id = from.Id,
                    counterparty_account_id = to.Id,
                    leg = debitTx.Leg,
                    operation = "debit",
                    status = ToApiStatus(debitTx.Status),
                    error_message = debitTx.ErrorMessage
                },
                new
                {
                    transaction_id = creditTx.Id,
                    account_id = to.Id,
                    counterparty_account_id = from.Id,
                    leg = creditTx.Leg,
                    operation = "credit",
                    status = ToApiStatus(creditTx.Status),
                    error_message = creditTx.ErrorMessage
                }
            },
            occurred_at = DateTimeOffset.UtcNow
        });

        return new OutboxMessageData(
            Id: Guid.NewGuid(),
            Type: "transfer.processed",
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
        => new(
            transaction.Id.ToString(),
            ToApiStatus(transaction.Status),
            account?.Balance ?? 0,
            account?.ReservedBalance ?? 0,
            account?.AvailableBalance ?? 0,
            transaction.CreatedAt,
            transaction.ErrorMessage
        );
}
