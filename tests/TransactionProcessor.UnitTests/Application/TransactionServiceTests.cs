using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Application.Outbox;
using TransactionProcessor.Application.Services;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Enums;
using Xunit;

namespace TransactionProcessor.UnitTests.Application;

public class TransactionServiceTests
{
    [Fact]
    public async Task ProcessTransactionAsync_ShouldReturnExistingTransaction_WhenReferenceIdAlreadyExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var referenceId = "ACC-001";

        // Transação já existente
        var existingTx = new Transaction(
            accountId: accountId,
            type: TransactionType.Credit,
            amount: 10m,
            currency: "BRL",
            referenceId: referenceId
        );
        existingTx.MarkAsSuccess();

        var accountRepo = new Mock<IAccountRepository>(MockBehavior.Strict);
        var txRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxStore>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TransactionService>>();
        var accountLock = new Mock<IAccountLock>(MockBehavior.Strict);


        txRepo.Setup(r => r.GetByReferenceIdAsync(referenceId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(existingTx);

        var service = new TransactionService(
            accountRepo.Object,
            txRepo.Object,
            outbox.Object,
            uow.Object,
            logger.Object,
            accountLock.Object
        );

        var dto = new CreateTransactionDto(
            AccountId: accountId,
            Operation: "credit",
            Amount: 10m,
            Currency: "BRL",
            ReferenceId: referenceId
        );

        // Act
        var result = await service.ProcessTransactionAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal(existingTx.Id.ToString(), result.TransactionId);

        txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        outbox.Verify(r => r.AddAsync(It.IsAny<OutboxMessageData>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        accountRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        txRepo.Verify(r => r.GetByReferenceIdAsync(referenceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionAsync_ShouldPersistTransaction_Outbox_AndCommit_OnSuccess()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var referenceId = "ACC-002";

        var account = new Account(creditLimit: 100);

        var accountRepo = new Mock<IAccountRepository>(MockBehavior.Strict);
        var txRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxStore>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TransactionService>>();
        var accountLock = new Mock<IAccountLock>(MockBehavior.Strict);

        txRepo.Setup(r => r.GetByReferenceIdAsync(referenceId, It.IsAny<CancellationToken>()))
              .ReturnsAsync((Transaction?)null);

        accountRepo.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(account);

        Transaction? savedTx = null;
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
              .Callback<Transaction, CancellationToken>((t, _) => savedTx = t)
              .Returns(Task.CompletedTask);

        outbox.Setup(r => r.AddAsync(It.IsAny<OutboxMessageData>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        uow.Setup(r => r.CommitAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var service = new TransactionService(
            accountRepo.Object,
            txRepo.Object,
            outbox.Object,
            uow.Object,
            logger.Object,
            accountLock.Object
        );

        var dto = new CreateTransactionDto(
            AccountId: accountId,
            Operation: "credit",
            Amount: 10m,
            Currency: "BRL",
            ReferenceId: referenceId
        );

        // Act
        var result = await service.ProcessTransactionAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty.ToString(), result.TransactionId);
        Assert.Equal("success", result.Status);

        txRepo.Verify(r => r.GetByReferenceIdAsync(referenceId, It.IsAny<CancellationToken>()), Times.Once);
        accountRepo.Verify(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);

        txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(r => r.AddAsync(It.IsAny<OutboxMessageData>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(savedTx);
        Assert.Equal(referenceId, savedTx!.ReferenceId);
        Assert.Equal(TransactionType.Credit, savedTx.Type);
        Assert.Equal(10m, savedTx.Amount);
        Assert.Equal("BRL", savedTx.Currency);
    }
}
