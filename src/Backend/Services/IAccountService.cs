using Challenge.Api.Domain;

namespace Challenge.Api.Services;

public interface IAccountService
{
    Task<Movement> CreateMovementAsync(
        MovementType type,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Movement>> GetHistoryAsync(CancellationToken cancellationToken = default);
}
