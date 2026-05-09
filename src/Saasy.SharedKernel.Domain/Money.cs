namespace Saasy.SharedKernel.Domain;

public sealed record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        return new Money(amount, currency);
    }

    public Money Add(Money other)
    {
        RequireSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        RequireSameCurrency(other);
        decimal result = Amount - other.Amount;
        if (result < 0)
            throw new InvalidOperationException(
                $"Subtraction would produce a negative amount ({Amount} - {other.Amount}).");
        return new Money(result, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative.", nameof(factor));
        return new Money(Amount * factor, Currency);
    }

    private void RequireSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Currency mismatch: cannot operate on {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Amount} {Currency}";
}
