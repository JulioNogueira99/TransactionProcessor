using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TransactionProcessor.IntegrationTests.Infrastructure;
using TransactionProcessor.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TransactionProcessor.IntegrationTests.Api;

public class TransactionsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TransactionsFlowTests(CustomWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task FullFlow_ShouldCreateAccount_ProcessTransaction_AndBeIdempotent_AndWriteOutbox()
    {
        var client = _factory.CreateClient();

        var createAccountPayload = new
        {
            credit_limit = 100
        };

        var accountResponse = await client.PostAsJsonAsync("/api/accounts", createAccountPayload);
        Assert.Equal(HttpStatusCode.Created, accountResponse.StatusCode);

        var accountJson = await accountResponse.Content.ReadAsStringAsync();
        using var accountDoc = JsonDocument.Parse(accountJson);

        var accountId =
            accountDoc.RootElement.TryGetProperty("id", out var idProp)
                ? idProp.GetGuid()
                : accountDoc.RootElement.GetProperty("account_id").GetGuid();

        var referenceId = "ACC-IT-001";

        var txPayload = new
        {
            account_id = accountId,
            operation = "credit",
            amount = 10,
            currency = "BRL",
            reference_id = referenceId
        };

        var txResponse1 = await client.PostAsJsonAsync("/api/transactions", txPayload);

        if (txResponse1.StatusCode != HttpStatusCode.OK)
        {
            var body = await txResponse1.Content.ReadAsStringAsync();
            throw new Exception($"POST /api/transactions failed: {(int)txResponse1.StatusCode} {txResponse1.StatusCode}\n{body}");
        }

        Assert.Equal(HttpStatusCode.OK, txResponse1.StatusCode);

        var txJson1 = await txResponse1.Content.ReadAsStringAsync();
        using var txDoc1 = JsonDocument.Parse(txJson1);

        var txId1 =
            txDoc1.RootElement.TryGetProperty("transaction_id", out var tid1)
                ? tid1.GetString()
                : txDoc1.RootElement.GetProperty("transactionId").GetString();

        var status1 =
            txDoc1.RootElement.TryGetProperty("status", out var st1)
                ? st1.GetString()
                : null;

        Assert.Equal("success", status1);

        var txResponse2 = await client.PostAsJsonAsync("/api/transactions", txPayload);
        Assert.Equal(HttpStatusCode.OK, txResponse2.StatusCode);

        var txJson2 = await txResponse2.Content.ReadAsStringAsync();
        using var txDoc2 = JsonDocument.Parse(txJson2);

        var txId2 =
            txDoc2.RootElement.TryGetProperty("transaction_id", out var tid2)
                ? tid2.GetString()
                : txDoc2.RootElement.GetProperty("transactionId").GetString();

        Assert.Equal(txId1, txId2);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var outboxCount = await db.OutboxMessages.CountAsync();
        Assert.True(outboxCount >= 1, $"Expected >= 1 outbox message, found {outboxCount}");
    }
}
