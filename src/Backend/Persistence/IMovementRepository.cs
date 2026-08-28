using Challenge.Api.Domain;

namespace Challenge.Api.Persistence;

public interface IMovementRepository
{
    Task<IReadOnlyCollection<Movement>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Movement movement, CancellationToken cancellationToken = default);
}
