using System.Text.Json.Serialization;

namespace TransactionProcessor.Application.DTOs
{
    public record CreateAccountDto(
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("credit_limit")] decimal CreditLimit
    );

    public record AccountResultDto(
        [property: JsonPropertyName("account_id")] Guid AccountId,
        [property: JsonPropertyName("balance")] decimal Balance,
        [property: JsonPropertyName("available_balance")] decimal AvailableBalance,
        [property: JsonPropertyName("credit_limit")] decimal CreditLimit
    );
}
