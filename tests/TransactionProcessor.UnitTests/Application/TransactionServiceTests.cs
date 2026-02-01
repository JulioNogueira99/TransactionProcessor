using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionProcessor.Application.DTOs;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Application.Outbox;
using TransactionProcessor.Application.Services;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Enums;
using TransactionProcessor.Domain.Exceptions;
using Xunit;

namespace TransactionProcessor.UnitTests.Application;

public class TransactionServiceTests
{
    private sealed class DummyAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ProcessTransactionAsync_ShouldReturnExistingTransaction_WhenReferenceIdAlreadyExists()
    {
        var accountId = Guid.NewGuid();
        var referenceId = "ACC-001";

        var existingTx = new Transaction(
            accountId: accountId,
            type: TransactionType.Credit,
            amount: 10m,
            currency: "BRL",
            referenceId: referenceId,
            leg: 0,
            counterpartyAccountId: null
        );
        existingTx.MarkAsSuccess();

        var account = new Account(customerId: Guid.NewGuid(), creditLimit: 100);

        var accountRepo = new Mock<IAccountRepository>(MockBehavior.Strict);
        var txRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxStore>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TransactionService>>();
        var accountLock = new Mock<IAccountLock>(MockBehavior.Strict);

        txRepo.Setup(r => r.GetByReferenceIdAsync(referenceId, (byte)0, It.IsAny<CancellationToken>()))
              .ReturnsAsync(existingTx);

        accountRepo.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(account);

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
            ReferenceId: referenceId,
            DestinationAccountId: null
        );

        var result = await service.ProcessTransactionAsync(dto, CancellationToken.None);

        Assert.Equal(existingTx.Id.ToString(), result.TransactionId);

        txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        outbox.Verify(r => r.AddAsync(It.IsAny<OutboxMessageData>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(r => r.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);

        accountRepo.Verify(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        txRepo.Verify(r => r.GetByReferenceIdAsync(referenceId, (byte)0, It.IsAny<CancellationToken>()), Times.Once);

        accountLock.Verify(l => l.AcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<TransactionResultDto>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTransactionAsync_ShouldPersistTransaction_Outbox_AndCommit_OnSuccess()
    {
        var accountId = Guid.NewGuid();
        var referenceId = "ACC-002";

        var account = new Account(customerId: Guid.NewGuid(), creditLimit: 100);

        var accountRepo = new Mock<IAccountRepository>(MockBehavior.Strict);
        var txRepo = new Mock<ITransactionRepository>(MockBehavior.Strict);
        var outbox = new Mock<IOutboxStore>(MockBehavior.Strict);
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TransactionService>>();
        var accountLock = new Mock<IAccountLock>(MockBehavior.Strict);

        txRepo.Setup(r => r.GetByReferenceIdAsync(referenceId, (byte)0, It.IsAny<CancellationToken>()))
              .ReturnsAsync((Transaction?)null);

        accountRepo.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(account);

        accountLock.Setup(l => l.AcquireAsync(accountId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new DummyAsyncDisposable());

        uow.Setup(u => u.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<TransactionResultDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<TransactionResultDto>>, CancellationToken>((fn, token) => fn(token));

        var uowTx = new Mock<IUnitOfWorkTransaction>(MockBehavior.Strict);
        uowTx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uowTx.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uowTx.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(uowTx.Object);

        Transaction? savedTx = null;
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
              .Callback<Transaction, CancellationToken>((t, _) => savedTx = t)
              .Returns(Task.CompletedTask);

        outbox.Setup(o => o.AddAsync(It.IsAny<OutboxMessageData>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
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
            ReferenceId: referenceId,
            DestinationAccountId: null
        );

        var result = await service.ProcessTransactionAsync(dto, CancellationToken.None);

        Assert.NotEqual(Guid.Empty.ToString(), result.TransactionId);
        Assert.Equal("success", result.Status);

        txRepo.Verify(r => r.GetByReferenceIdAsync(referenceId, (byte)0, It.IsAny<CancellationToken>()), Times.Once);
        accountRepo.Verify(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        accountLock.Verify(l => l.AcquireAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);

        uow.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(o => o.AddAsync(It.IsAny<OutboxMessageData>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        uowTx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(savedTx);
        Assert.Equal(referenceId, savedTx!.ReferenceId);
        Assert.Equal(TransactionType.Credit, savedTx.Type);
        Assert.Equal(10m, savedTx.Amount);
        Assert.Equal("BRL", savedTx.Currency);
        Assert.Equal((byte)0, savedTx.Leg);
    }
}
