namespace Challenge.Api.Exceptions;

public sealed class InvalidAmountException : Exception
{
    public InvalidAmountException() : base("The movement amount must be greater than zero.")
    {
    }
}
