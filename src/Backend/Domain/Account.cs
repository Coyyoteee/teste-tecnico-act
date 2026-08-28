using Challenge.Api.Exceptions;

namespace Challenge.Api.Domain;

public sealed class Account
{
    private readonly List<Movement> _movements;

    public Account(IEnumerable<Movement>? movements = null)
    {
        _movements = movements?.ToList() ?? [];
        Balance = CalculateBalance(_movements);
    }

    public decimal Balance { get; private set; }
    public IReadOnlyCollection<Movement> Movements => _movements.AsReadOnly();

    public Movement Deposit(decimal amount, DateTimeOffset occurredAt)
    {
        var movement = CreateMovement(MovementType.Credit, amount, occurredAt);
        Balance += movement.Amount;
        _movements.Add(movement);
        return movement;
    }

    public Movement Withdraw(decimal amount, DateTimeOffset occurredAt)
    {
        ValidateAmount(amount);
        if (amount > Balance)
        {
            throw new InsufficientFundsException();
        }

        var movement = CreateMovement(MovementType.Debit, amount, occurredAt);
        Balance -= movement.Amount;
        _movements.Add(movement);
        return movement;
    }

    private static Movement CreateMovement(MovementType type, decimal amount, DateTimeOffset occurredAt)
    {
        ValidateAmount(amount);
        return new Movement(Guid.NewGuid(), type, amount, occurredAt);
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidAmountException();
        }
    }

    private static decimal CalculateBalance(IEnumerable<Movement> movements)
    {
        decimal balance = 0;
        foreach (var movement in movements)
        {
            balance += movement.Type == MovementType.Credit ? movement.Amount : -movement.Amount;
            if (balance < 0)
            {
                throw new InvalidDataException("The movement history produces a negative balance.");
            }
        }

        return balance;
    }
}
