using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TransactionProcessor.Application.DTOs
{
    public record TransactionResultDto(
        [property: JsonPropertyName("transaction_id")] string TransactionId,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("balance")] decimal Balance,
        [property: JsonPropertyName("reserved_balance")] decimal ReservedBalance,
        [property: JsonPropertyName("available_balance")] decimal AvailableBalance,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp,
        [property: JsonPropertyName("error_message")] string? ErrorMessage
    );
}
