using Challenge.Api.Domain;
using Challenge.Api.Persistence;

namespace Challenge.UnitTests.TestDoubles;

internal sealed class FakeMovementRepository : IMovementRepository
{
    private readonly object _sync = new();
    private readonly List<Movement> _movements;

    public FakeMovementRepository(IEnumerable<Movement>? movements = null)
    {
        _movements = movements?.ToList() ?? [];
    }

    public int AddCalls { get; private set; }

    public Task<IReadOnlyCollection<Movement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyCollection<Movement>>(_movements.ToArray());
        }
    }

    public Task AddAsync(Movement movement, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            AddCalls++;
            _movements.Add(movement);
        }

        return Task.CompletedTask;
    }
}
