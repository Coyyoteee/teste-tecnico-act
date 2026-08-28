using Challenge.Api.Domain;
using Challenge.Api.Exceptions;
using Challenge.Api.Services;
using Challenge.UnitTests.TestDoubles;

namespace Challenge.UnitTests.Services;

public sealed class AccountServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateMovementAsync_CreatesCredit()
    {
        var repository = new FakeMovementRepository();
        using var service = CreateService(repository);

        var movement = await service.CreateMovementAsync(MovementType.Credit, 100m);

        Assert.Equal(MovementType.Credit, movement.Type);
        Assert.Equal(Now, movement.OccurredAt);
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    public async Task CreateMovementAsync_CreatesValidDebit()
    {
        var repository = new FakeMovementRepository([Credit(100m)]);
        using var service = CreateService(repository);

        var movement = await service.CreateMovementAsync(MovementType.Debit, 40m);

        Assert.Equal(MovementType.Debit, movement.Type);
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    public async Task CreateMovementAsync_WithInsufficientFunds_DoesNotPersist()
    {
        var repository = new FakeMovementRepository([Credit(50m)]);
        using var service = CreateService(repository);

        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => service.CreateMovementAsync(MovementType.Debit, 60m));

        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task GetBalanceAsync_DerivesBalanceFromHistory()
    {
        var repository = new FakeMovementRepository([
            Credit(100m),
            new Movement(Guid.NewGuid(), MovementType.Debit, 25m, Now.AddMinutes(1))]);
        using var service = CreateService(repository);

        Assert.Equal(75m, await service.GetBalanceAsync());
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsNewestFirst()
    {
        var older = Credit(10m);
        var newer = new Movement(Guid.NewGuid(), MovementType.Credit, 20m, Now.AddMinutes(2));
        var repository = new FakeMovementRepository([older, newer]);
        using var service = CreateService(repository);

        var history = (await service.GetHistoryAsync()).ToArray();

        Assert.Equal(newer.Id, history[0].Id);
        Assert.Equal(older.Id, history[1].Id);
    }

    [Fact]
    public async Task GetHistoryAsync_WithEqualTimestamps_ReturnsLastPersistedFirst()
    {
        var first = Credit(10m);
        var second = Credit(20m);
        var repository = new FakeMovementRepository([first, second]);
        using var service = CreateService(repository);

        var history = (await service.GetHistoryAsync()).ToArray();

        Assert.Equal(second.Id, history[0].Id);
        Assert.Equal(first.Id, history[1].Id);
    }

    [Fact]
    public async Task CreateMovementAsync_WithTwoIncompatibleConcurrentDebits_PersistsOnlyOne()
    {
        var repository = new FakeMovementRepository([Credit(100m)]);
        using var service = CreateService(repository);

        var attempts = new[]
        {
            service.CreateMovementAsync(MovementType.Debit, 80m),
            service.CreateMovementAsync(MovementType.Debit, 80m)
        };

        var outcomes = await Task.WhenAll(attempts.Select(CaptureAsync));

        Assert.Single(outcomes, outcome => outcome is Movement);
        Assert.Single(outcomes, outcome => outcome is InsufficientFundsException);
        Assert.Equal(20m, await service.GetBalanceAsync());
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    public async Task GetBalanceAsync_PropagatesCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        using var service = CreateService(new FakeMovementRepository());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetBalanceAsync(source.Token));
    }

    private static AccountService CreateService(FakeMovementRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static Movement Credit(decimal amount) =>
        new(Guid.NewGuid(), MovementType.Credit, amount, Now);

    private static async Task<object> CaptureAsync(Task<Movement> task)
    {
        try
        {
            return await task;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
