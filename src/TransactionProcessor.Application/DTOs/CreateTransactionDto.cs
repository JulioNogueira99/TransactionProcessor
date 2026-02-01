using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TransactionProcessor.Application.DTOs
{
    public record CreateTransactionDto(
        [property: JsonPropertyName("account_id")] Guid AccountId,
        [property: JsonPropertyName("operation")] string Operation,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("reference_id")] string ReferenceId,
        [property: JsonPropertyName("destination_account_id")] Guid? DestinationAccountId
    );
}
