using Challenge.Api.Exceptions;

namespace Challenge.Api.Domain;

public sealed class Movement
{
    public Movement(Guid id, MovementType type, decimal amount, DateTimeOffset occurredAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The movement id must not be empty.", nameof(id));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown movement type.");
        }

        if (amount <= 0)
        {
            throw new InvalidAmountException();
        }

        Id = id;
        Type = type;
        Amount = amount;
        OccurredAt = occurredAt.ToUniversalTime();
    }

    public Guid Id { get; }
    public MovementType Type { get; }
    public decimal Amount { get; }
    public DateTimeOffset OccurredAt { get; }
}
