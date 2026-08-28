namespace Challenge.Api.Exceptions;

public sealed class InsufficientFundsException : Exception
{
    public InsufficientFundsException() : base("The available balance is insufficient for this debit.")
    {
    }
}
