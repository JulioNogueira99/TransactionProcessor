using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Domain.Entities;

namespace TransactionProcessor.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAccountRepository _accountRepository;

        public AccountService(IUnitOfWork unitOfWork, IAccountRepository accountRepository)
        {
            _unitOfWork = unitOfWork;
            _accountRepository = accountRepository;
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
    }
}
