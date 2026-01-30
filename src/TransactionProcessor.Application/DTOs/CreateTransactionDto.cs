using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TransactionProcessor.Application.DTOs
{
    public record CreateTransactionDto(
    [property: JsonPropertyName("account_id")]
    [property: Required] Guid AccountId,

    [property: JsonPropertyName("operation")]
    [property: Required, MinLength(3), MaxLength(20)] string Operation,

    [property: JsonPropertyName("amount")]
    [property: Range(typeof(decimal), "0.01", "999999999999")] decimal Amount,

    [property: JsonPropertyName("currency")]
    [property: Required, MinLength(3), MaxLength(3)] string Currency,

    [property: JsonPropertyName("reference_id")]
    [property: Required, MinLength(3), MaxLength(64)] string ReferenceId
);
}
