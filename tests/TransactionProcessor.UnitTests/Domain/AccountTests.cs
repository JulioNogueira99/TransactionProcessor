using System;
using TransactionProcessor.Domain.Entities;
using TransactionProcessor.Domain.Exceptions;
using Xunit;

namespace TransactionProcessor.UnitTests.Domain;

public class AccountTests
{
    private static Account NewAccount(decimal creditLimit = 0m)
        => new Account(Guid.NewGuid(), creditLimit);

    [Fact]
    public void CreateAccount_ShouldThrow_WhenCustomerIdIsEmpty()
    {
        var ex = Assert.Throws<DomainException>(() => new Account(Guid.Empty, 100));
        Assert.Contains("CustomerId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateAccount_ShouldThrow_WhenCreditLimitIsNegative()
    {
        var ex = Assert.Throws<DomainException>(() => new Account(Guid.NewGuid(), -1));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Credit_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = NewAccount(100);

        Assert.Throws<DomainException>(() => account.Credit(0));
        Assert.Throws<DomainException>(() => account.Credit(-10));
    }

    [Fact]
    public void Debit_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = NewAccount(100);

        Assert.Throws<DomainException>(() => account.Debit(0));
        Assert.Throws<DomainException>(() => account.Debit(-10));
    }

    [Fact]
    public void Reserve_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = NewAccount(100);

        Assert.Throws<DomainException>(() => account.Reserve(0));
        Assert.Throws<DomainException>(() => account.Reserve(-10));
    }

    [Fact]
    public void Capture_ShouldThrow_WhenAmountIsZeroOrNegative()
    {
        var account = NewAccount(100);

        Assert.Throws<DomainException>(() => account.Capture(0));
        Assert.Throws<DomainException>(() => account.Capture(-10));
    }

    [Fact]
    public void Debit_ShouldThrow_WhenAmountExceedsAvailableBalance_CashPlusCreditLimit()
    {
        var account = NewAccount(creditLimit: 100);

        var ex = Assert.Throws<DomainException>(() => account.Debit(101));

        Assert.Contains("Insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Debit_ShouldAllow_UsingCreditLimit_WhenCashIsNotEnough_AndKeepAvailableNonNegative()
    {
        var account = NewAccount(creditLimit: 100);

        account.Debit(50);

        Assert.Equal(-50m, account.Balance);
        Assert.Equal(0m, account.ReservedBalance);

        Assert.Equal(-50m, account.CashAvailable);         
        Assert.Equal(50m, account.AvailableBalance);       
        Assert.True(account.AvailableBalance >= 0m);
        Assert.Equal(account.AvailableBalance, account.SpendingPower);
    }

    [Fact]
    public void Debit_ShouldNotMakeAvailableBalanceNegative()
    {
        var account = NewAccount(creditLimit: 100);

        account.Debit(100);

        Assert.Equal(-100m, account.Balance);
        Assert.Equal(0m, account.ReservedBalance);

        Assert.Equal(-100m, account.CashAvailable);
        Assert.Equal(0m, account.AvailableBalance);
        Assert.True(account.AvailableBalance >= 0m);
    }

    [Fact]
    public void Reserve_ShouldUseOnlyCashAvailable_ShouldThrow_WhenOnlyCreditLimitWouldCover()
    {
        var account = NewAccount(creditLimit: 100);

        var ex = Assert.Throws<DomainException>(() => account.Reserve(1));

        Assert.Contains("Insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reserve_ShouldThrow_WhenCashAvailableIsInsufficient()
    {
        var account = NewAccount(creditLimit: 100);
        account.Credit(50);

        var ex = Assert.Throws<DomainException>(() => account.Reserve(60)); 

        Assert.Contains("Insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reserve_ShouldDecreaseCashAvailable_ButNotAllowGoingNegative()
    {
        var account = NewAccount(creditLimit: 100);
        account.Credit(50); 

        account.Reserve(30);

        Assert.Equal(50m, account.Balance);
        Assert.Equal(30m, account.ReservedBalance);

        Assert.Equal(20m, account.CashAvailable);     
        Assert.Equal(120m, account.AvailableBalance); 
        Assert.True(account.AvailableBalance >= 0m);
    }

    [Fact]
    public void Capture_ShouldThrow_WhenCaptureExceedsReservedBalance()
    {
        var account = NewAccount(creditLimit: 100);
        account.Credit(50);
        account.Reserve(30);

        var ex = Assert.Throws<DomainException>(() => account.Capture(40));

        Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HappyPath_Credit_Reserve_Capture_ShouldUpdateBalancesCorrectly_AndKeepAvailableNonNegative()
    {
        var account = NewAccount(creditLimit: 100);

        account.Credit(50);
        Assert.Equal(50m, account.Balance);
        Assert.Equal(0m, account.ReservedBalance);

        Assert.Equal(50m, account.CashAvailable);        
        Assert.Equal(150m, account.AvailableBalance);   
        Assert.True(account.AvailableBalance >= 0m);

        account.Reserve(30);
        Assert.Equal(50m, account.Balance);
        Assert.Equal(30m, account.ReservedBalance);

        Assert.Equal(20m, account.CashAvailable);        
        Assert.Equal(120m, account.AvailableBalance);    
        Assert.True(account.AvailableBalance >= 0m);

        account.Capture(10);
        Assert.Equal(40m, account.Balance);              
        Assert.Equal(20m, account.ReservedBalance);      

        Assert.Equal(20m, account.CashAvailable);       
        Assert.Equal(120m, account.AvailableBalance);
        Assert.True(account.AvailableBalance >= 0m);
    }
}
