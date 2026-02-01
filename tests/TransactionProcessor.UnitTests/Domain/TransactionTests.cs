using System;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Enums;
using Xunit;

namespace TransactionProcessor.UnitTests.Domain;

public class TransactionTests
{
    [Fact]
    public void NewTransaction_ShouldStartWithPendingStatus()
    {
        var accountId = Guid.NewGuid();

        var tx = new Transaction(
            accountId: accountId,
            type: TransactionType.Credit,
            amount: 10m,
            currency: "BRL",
            referenceId: "ACC-TRX-001"
        );

        Assert.Equal(accountId, tx.AccountId);
        Assert.Equal(TransactionType.Credit, tx.Type);
        Assert.Equal(10m, tx.Amount);
        Assert.Equal("BRL", tx.Currency);
        Assert.Equal("ACC-TRX-001", tx.ReferenceId);

        Assert.Equal(TransactionStatus.Pending, tx.Status);

        Assert.True(tx.CreatedAt <= DateTime.UtcNow.AddSeconds(2));
        Assert.Null(tx.ErrorMessage);
    }

    [Fact]
    public void MarkAsSuccess_ShouldSetStatusToSuccess_AndClearError()
    {
        var tx = new Transaction(
            accountId: Guid.NewGuid(),
            type: TransactionType.Credit,
            amount: 10m,
            currency: "BRL",
            referenceId: "ACC-TRX-002"
        );

        tx.MarkAsFailed("any error");
        Assert.Equal(TransactionStatus.Failed, tx.Status);
        Assert.NotNull(tx.ErrorMessage);

        tx.MarkAsSuccess();

        Assert.Equal(TransactionStatus.Success, tx.Status);
        Assert.True(string.IsNullOrWhiteSpace(tx.ErrorMessage));
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusToFailed_AndSetErrorMessage()
    {
        var tx = new Transaction(
            accountId: Guid.NewGuid(),
            type: TransactionType.Debit,
            amount: 10m,
            currency: "BRL",
            referenceId: "ACC-TRX-003"
        );

        tx.MarkAsFailed("Insufficient funds");

        Assert.Equal(TransactionStatus.Failed, tx.Status);
        Assert.Equal("Insufficient funds", tx.ErrorMessage);
    }
}
