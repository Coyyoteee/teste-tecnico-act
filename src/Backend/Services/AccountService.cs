using Challenge.Api.Domain;
using Challenge.Api.Persistence;

namespace Challenge.Api.Services;

public sealed class AccountService : IAccountService, IDisposable
{
    private readonly IMovementRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AccountService(IMovementRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Movement> CreateMovementAsync(
        MovementType type,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var account = new Account(await _repository.GetAllAsync(cancellationToken));
            var occurredAt = _timeProvider.GetUtcNow();
            var movement = type switch
            {
                MovementType.Credit => account.Deposit(amount, occurredAt),
                MovementType.Debit => account.Withdraw(amount, occurredAt),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown movement type.")
            };

            await _repository.AddAsync(movement, cancellationToken);
            return movement;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<decimal> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return new Account(await _repository.GetAllAsync(cancellationToken)).Balance;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyCollection<Movement>> GetHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await _repository.GetAllAsync(cancellationToken))
                .Select((movement, index) => new { Movement = movement, Index = index })
                .OrderByDescending(entry => entry.Movement.OccurredAt)
                .ThenByDescending(entry => entry.Index)
                .Select(entry => entry.Movement)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
