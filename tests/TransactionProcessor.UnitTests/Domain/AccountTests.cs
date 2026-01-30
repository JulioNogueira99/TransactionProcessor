using System;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Exceptions;
using Xunit;

namespace TransactionProcessor.UnitTests.Domain;

public class AccountTests
{
    [Fact]
    public void CreateAccount_ShouldThrow_WhenCreditLimitIsNegative()
    {
        var ex = Assert.Throws<DomainException>(() => new Account(-1));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Credit_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = new Account(100);

        Assert.Throws<DomainException>(() => account.Credit(0));
        Assert.Throws<DomainException>(() => account.Credit(-10));
    }

    [Fact]
    public void Debit_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = new Account(100);

        Assert.Throws<DomainException>(() => account.Debit(0));
        Assert.Throws<DomainException>(() => account.Debit(-10));
    }

    [Fact]
    public void Reserve_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = new Account(100);

        Assert.Throws<DomainException>(() => account.Reserve(0));
        Assert.Throws<DomainException>(() => account.Reserve(-10));
    }

    [Fact]
    public void Capture_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = new Account(100);

        Assert.Throws<DomainException>(() => account.Capture(0));
        Assert.Throws<DomainException>(() => account.Capture(-10));
    }

    [Fact]
    public void Debit_ShouldThrowDomainException_WhenInsufficientSpendingPower()
    {
        var account = new Account(creditLimit: 0);

        var ex = Assert.Throws<DomainException>(() => account.Debit(10));

        Assert.Contains("Insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Debit_ShouldAllow_UsingCreditLimit_WhenBalanceIsNotEnough()
    {
        var account = new Account(creditLimit: 100);

        account.Debit(50);

        Assert.Equal(-50m, account.Balance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(-50m, account.AvailableBalance); 
        Assert.Equal(50m, account.SpendingPower);     
    }

    [Fact]
    public void Debit_ShouldThrow_WhenAmountExceedsSpendingPower()
    {
        var account = new Account(creditLimit: 100);

        var ex = Assert.Throws<DomainException>(() => account.Debit(101));

        Assert.Contains("Insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reserve_ShouldThrow_WhenAvailableBalanceIsInsufficient()
    {
        var account = new Account(100);
        account.Credit(50); 

        var ex = Assert.Throws<DomainException>(() => account.Reserve(60)); 

        Assert.Contains("Insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Capture_ShouldThrow_WhenCaptureExceedsReservedBalance()
    {
        var account = new Account(100);
        account.Credit(50);
        account.Reserve(30);

        var ex = Assert.Throws<DomainException>(() => account.Capture(40));

        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HappyPath_Credit_Reserve_Capture_ShouldUpdateBalancesCorrectly()
    {
        var account = new Account(100);

        account.Credit(50);
        Assert.Equal(50m, account.Balance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(50m, account.AvailableBalance);
        Assert.Equal(150m, account.SpendingPower);

        account.Reserve(30);
        Assert.Equal(50m, account.Balance);
        Assert.Equal(30m, account.ReservedBalance);
        Assert.Equal(20m, account.AvailableBalance);
        Assert.Equal(120m, account.SpendingPower);

        account.Capture(10);
        Assert.Equal(40m, account.Balance);        
        Assert.Equal(20m, account.ReservedBalance); 
        Assert.Equal(20m, account.AvailableBalance);
        Assert.Equal(120m, account.SpendingPower);
    }
}
