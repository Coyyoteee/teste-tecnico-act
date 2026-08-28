using Challenge.Api.Domain;

namespace Challenge.Api.Contracts.Responses;

public sealed record MovementResponse(
    Guid Id,
    MovementType Type,
    decimal Amount,
    DateTimeOffset OccurredAt)
{
    public static MovementResponse FromDomain(Movement movement) =>
        new(movement.Id, movement.Type, movement.Amount, movement.OccurredAt);
}
