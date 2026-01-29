using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransactionProcessor.Application.DTOs;

namespace TransactionProcessor.Application.Interfaces
{
    public interface IAccountService
    {
        Task<AccountResultDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken cancellationToken);
        Task<AccountResultDto?> GetAccountAsync(Guid id, CancellationToken cancellationToken);
    }
}
