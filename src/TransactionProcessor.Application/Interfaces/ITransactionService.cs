using TransactionProcessor.Application.DTOs;

namespace TransactionProcessor.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResultDto> ProcessTransactionAsync(CreateTransactionDto dto, CancellationToken ct);
    }
}
