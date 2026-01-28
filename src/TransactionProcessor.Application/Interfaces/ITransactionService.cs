using TransactionProcessor.Application.DTOs;

namespace TransactionProcessor.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResultDto> ProcessTransactionAsync(CreateTransactionDto dto, CancellationToken cancellationToken);
        Task<AccountResultDto> CreateAccountAsync(CreateAccountDto dto, CancellationToken cancellationToken);
        Task<AccountResultDto?> GetAccountAsync(Guid id, CancellationToken cancellationToken);
    }
}
