using Microsoft.Extensions.Logging;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Application.Services;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUnitOfWork unitOfWork,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        ILogger<AccountService> logger)
    {
        _unitOfWork = unitOfWork;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<AccountResultDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken ct)
    {
        return await _unitOfWork.ExecuteAsync(async ct2 =>
        {
            var clientId = dto.ClientId.Trim();

            var customer = await _customerRepository.GetByClientIdAsync(clientId, ct2);
            if (customer is null)
            {
                customer = new Customer(clientId);
                await _customerRepository.AddAsync(customer, ct2);
                _logger.LogInformation("Customer created: {ClientId} -> {CustomerId}", clientId, customer.Id);
            }

            var account = new Account(customer.Id, dto.CreditLimit);
            await _accountRepository.AddAsync(account, ct2);

            await _unitOfWork.CommitAsync(ct2);

            return new AccountResultDto(account.Id, account.Balance, account.AvailableBalance, account.CreditLimit);
        }, ct);
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
