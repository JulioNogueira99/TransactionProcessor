using Microsoft.Extensions.Logging;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Application.Services;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUnitOfWork unitOfWork,
        IAccountRepository accountRepository,
        ILogger<AccountService> logger)
    {
        _unitOfWork = unitOfWork;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<AccountResultDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct)
    {
        _logger.LogInformation("Creating account with credit_limit={CreditLimit}", dto.CreditLimit);

        var account = new Account(dto.CreditLimit);
        await _accountRepository.AddAsync(account, ct);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation("Account created: account_id={AccountId} credit_limit={CreditLimit}",
            account.Id, account.CreditLimit);

        return new AccountResultDto(account.Id, account.Balance, account.AvailableBalance, account.CreditLimit);
    }

    public async Task<AccountResultDto?> GetAccountAsync(Guid id, CancellationToken ct)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);

        if (account is null)
        {
            _logger.LogInformation("Account not found: account_id={AccountId}", id);
            return null;
        }

        _logger.LogInformation("Account retrieved: account_id={AccountId} balance={Balance} reserved={Reserved} available={Available} credit_limit={CreditLimit}",
            account.Id, account.Balance, account.ReservedBalance, account.AvailableBalance, account.CreditLimit);

        return new AccountResultDto(account.Id, account.Balance, account.AvailableBalance, account.CreditLimit);
    }
}
