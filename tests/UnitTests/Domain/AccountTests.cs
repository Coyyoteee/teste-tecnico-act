using Challenge.Api.Domain;
using Challenge.Api.Exceptions;

namespace Challenge.UnitTests.Domain;

public sealed class AccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Deposit_WithPositiveAmount_IncreasesBalance()
    {
        var account = new Account();

        var movement = account.Deposit(100m, Now);

        Assert.Equal(100m, account.Balance);
        Assert.Equal(MovementType.Credit, movement.Type);
        Assert.Equal(100m, movement.Amount);
    }

    [Fact]
    public void Withdraw_WithAvailableFunds_DecreasesBalance()
    {
        var account = AccountWithBalance(100m);

        account.Withdraw(40m, Now.AddMinutes(1));

        Assert.Equal(60m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deposit_WithNonPositiveAmount_IsRejected(decimal amount)
    {
        Assert.Throws<InvalidAmountException>(() => new Account().Deposit(amount, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Withdraw_WithNonPositiveAmount_IsRejected(decimal amount)
    {
        Assert.Throws<InvalidAmountException>(() => AccountWithBalance(100m).Withdraw(amount, Now));
    }

    [Fact]
    public void Withdraw_AboveBalance_IsRejected()
    {
        Assert.Throws<InsufficientFundsException>(() => AccountWithBalance(100m).Withdraw(100.01m, Now));
    }

    [Fact]
    public void Withdraw_EqualToBalance_IsAllowed()
    {
        var account = AccountWithBalance(100m);

        account.Withdraw(100m, Now);

        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Constructor_RebuildsBalanceFromHistory()
    {
        var history = new[]
        {
            new Movement(Guid.NewGuid(), MovementType.Credit, 150m, Now),
            new Movement(Guid.NewGuid(), MovementType.Debit, 40m, Now.AddMinutes(1)),
            new Movement(Guid.NewGuid(), MovementType.Credit, 10m, Now.AddMinutes(2))
        };

        var account = new Account(history);

        Assert.Equal(120m, account.Balance);
    }

    [Fact]
    public void Constructor_WithTimestampsOutOfOrder_PreservesRepositoryOrder()
    {
        var history = new[]
        {
            new Movement(Guid.NewGuid(), MovementType.Credit, 100m, Now),
            new Movement(Guid.NewGuid(), MovementType.Debit, 80m, Now.AddMinutes(-1))
        };

        var account = new Account(history);

        Assert.Equal(20m, account.Balance);
        Assert.Equal(history, account.Movements);
    }

    [Fact]
    public void Constructor_WithEmptyHistory_HasZeroBalance()
    {
        Assert.Equal(0m, new Account().Balance);
    }

    [Fact]
    public void Movement_WithEmptyId_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Movement(Guid.Empty, MovementType.Credit, 10m, Now));
    }

    [Fact]
    public void Movement_WithUndefinedType_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Movement(Guid.NewGuid(), (MovementType)2, 10m, Now));
    }

    private static Account AccountWithBalance(decimal balance) =>
        new([new Movement(Guid.NewGuid(), MovementType.Credit, balance, Now.AddMinutes(-1))]);
}
